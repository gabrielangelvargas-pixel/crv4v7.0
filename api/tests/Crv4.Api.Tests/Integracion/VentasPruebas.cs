using System.Net;
using System.Net.Http.Json;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Clientes;
using Crv4.Api.Modulos.Seguridad;
using Crv4.Api.Modulos.Ventas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Crv4.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class VentasPruebas(ApiFixture api)
{
    private async Task PrepararAsync()
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        string[] sentencias =
        [
            "INSERT IGNORE INTO estados (IdEstado, NombreEstado) VALUES (1, 'Pendiente'), (2, 'Parcial'), (3, 'Pagada'), (4, 'Anulada')",
            "INSERT IGNORE INTO vendedores (IdVendedor, NombreVendedor) VALUES (91, 'Vendedor prueba')",
            "INSERT IGNORE INTO mediospago (IdMedioPago, NombreMedioPago, Activo, RequiereNumeroOperacion) VALUES (91, 'Efectivo prueba', 1, 0), (92, 'Transferencia prueba', 1, 1)",
            "INSERT IGNORE INTO zonas (IdZona, NombreZona) VALUES (91, 'Zona norte')",
            "INSERT IGNORE INTO fiscales (IdFiscal, NombreFiscal) VALUES (91, 'Consumidor final')",
            "INSERT IGNORE INTO proveedores (IdProveedor, NombreProveedor) VALUES ('02', 'Bonificado'), ('P1', 'Proveedor uno')",
            "INSERT IGNORE INTO rubros (IdRubro, NombreRubro, Orden, Activo) VALUES ('VTA', 'Ventas prueba', 0, 1)",
            "INSERT IGNORE INTO productos (IdProducto, IdProveedor, IdRubro, Descripcion, precioCompra, Descuento, PrecioVenta, Utilidad, Activo) VALUES " +
                "('VT-BON', '02', 'VTA', 'Producto bonificado', 500, 0, 1000, 100, 1), ('VT-COM', 'P1', 'VTA', 'Producto común', 50, 0, 100, 100, 1)",
            "INSERT IGNORE INTO productos (IdProducto, IdProveedor, IdRubro, Descripcion, precioCompra, Descuento, PrecioVenta, PrecioOferta, Utilidad, Activo) VALUES " +
                "('VT-OFE', 'P1', 'VTA', 'Producto en oferta', 50, 0, 100, 80, 100, 1)",
        ];
        foreach (var sql in sentencias)
            await db.Database.ExecuteSqlRawAsync(sql);
    }

    private async Task<int> CrearClienteAsync(HttpClient http, string nombre) =>
        (await (await http.PostAsJsonAsync("/api/clientes", new { nombre, idZona = 91, idFiscal = 91, saldoCuenta = 0 }))
            .Content.ReadFromJsonAsync<ClienteDto>())!.Id;

    private static object Venta(int cliente, int estado = 1, decimal bono = 0, params (string codigo, int cantidad, decimal? precio)[] items) =>
        new { idCliente = cliente, idVendedor = 91, idEstado = estado, descuentoBono = bono,
            items = items.Select(i => new { idProducto = i.codigo, cantidad = i.cantidad, precioVenta = i.precio }).ToList() };

    private async Task<decimal> SaldoAsync(HttpClient http, int cliente) =>
        (await http.GetFromJsonAsync<ClienteDto>($"/api/clientes/{cliente}"))!.SaldoCuenta;

    [FactConBase]
    public async Task Alta_calcula_descuento_por_escalon_oferta_y_saldo()
    {
        await PrepararAsync();
        await api.CrearUsuarioAsync("vta_admin");
        var http = api.Cliente((await api.LoginAsync("vta_admin")).TokenAcceso);
        var cliente = await CrearClienteAsync(http, "VTA Cliente uno");

        // 150 bonificados a $1.000 = $150.000: supera el primer escalón (6% = $9.000).
        var alta = await http.PostAsJsonAsync("/api/ventas", Venta(cliente, items: [("VT-BON", 150, null), ("VT-COM", 2, null), ("VT-OFE", 1, null)]));
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var v = (await alta.Content.ReadFromJsonAsync<VentaDto>())!;
        Assert.Equal(150_000m, v.SubtotalProveedoresBonificados);
        Assert.Equal(9_000m, v.DescuentoEscalon1);
        Assert.Equal(0m, v.DescuentoEscalon2);
        Assert.Equal(150_000m - 9_000m + 200m + 80m, v.TotalVenta);
        Assert.StartsWith("OFF... ", v.Items.Single(i => i.IdProducto == "VT-OFE").Descripcion);
        Assert.Equal(75_000m + 100m + 50m, v.TotalCosto);
        Assert.Equal(v.TotalVenta, await SaldoAsync(http, cliente));

        // Al editar, el saldo del cliente se recalcula con el total nuevo.
        var edicion = await http.PutAsJsonAsync($"/api/ventas/{v.Id}", Venta(cliente, bono: 50, items: [("VT-COM", 3, 120m)]));
        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
        var editada = (await edicion.Content.ReadFromJsonAsync<VentaDto>())!;
        Assert.Equal(310m, editada.TotalVenta);
        Assert.Equal(0m, editada.DescuentoProveedores);
        Assert.Equal(310m, await SaldoAsync(http, cliente));

        var lista = await http.GetFromJsonAsync<Comun.Paginado<VentaResumenDto>>($"/api/ventas?idCliente={cliente}");
        Assert.Equal(310m, Assert.Single(lista!.Items).Total);

        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/ventas", Venta(cliente, items: [("NO-EXISTE", 1, null)]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/ventas", Venta(cliente))).StatusCode);
    }

    [FactConBase]
    public async Task Cobro_parcial_total_y_redondeo()
    {
        await PrepararAsync();
        await api.CrearUsuarioAsync("vta_cobros");
        var http = api.Cliente((await api.LoginAsync("vta_cobros")).TokenAcceso);
        var cliente = await CrearClienteAsync(http, "VTA Cliente cobros");
        var v = (await (await http.PostAsJsonAsync("/api/ventas", Venta(cliente, items: [("VT-COM", 10, null)]))).Content.ReadFromJsonAsync<VentaDto>())!;
        Assert.Equal(1000m, v.TotalVenta);

        object Pago(decimal importe, bool ajustar = false, int medio = 91, string? operacion = null) =>
            new { idFactura = v.Id, ajustarImporte = ajustar, detalles = new[] { new { idMedioPago = medio, importe, numeroOperacion = operacion } } };

        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/pagos", Pago(1200))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/pagos", Pago(100, medio: 92))).StatusCode);

        var parcial = (await (await http.PostAsJsonAsync("/api/pagos", Pago(400, medio: 92, operacion: "OP-1"))).Content.ReadFromJsonAsync<PagoDto>())!;
        Assert.Equal(EstadoVenta.Parcial, parcial.IdEstadoVenta);
        Assert.Equal(600m, parcial.SaldoVenta);
        Assert.Equal(600m, await SaldoAsync(http, cliente));

        // Paga $595 y se ajustan los $5 que faltan con un renglón de redondeo.
        var final = (await (await http.PostAsJsonAsync("/api/pagos", Pago(595, ajustar: true))).Content.ReadFromJsonAsync<PagoDto>())!;
        Assert.Equal(EstadoVenta.Pagada, final.IdEstadoVenta);
        Assert.Equal(0m, final.SaldoVenta);
        Assert.Equal(0m, await SaldoAsync(http, cliente));
        var cobrada = (await http.GetFromJsonAsync<VentaDto>($"/api/ventas/{v.Id}"))!;
        Assert.Equal(995m, cobrada.TotalVenta);
        Assert.Equal(995m, cobrada.Pagado);
        Assert.Contains(cobrada.Items, i => i.IdProducto == VentasController.ProductoRedondeo && i.PrecioVenta == -5m);

        Assert.Equal(HttpStatusCode.Conflict, (await http.DeleteAsync($"/api/ventas/{v.Id}")).StatusCode);
        // Se puede volver a guardar con el renglón de redondeo.
        var items = cobrada.Items.Select(i => (i.IdProducto, i.Cantidad, (decimal?)i.PrecioVenta)).ToArray();
        Assert.Equal(HttpStatusCode.OK, (await http.PutAsJsonAsync($"/api/ventas/{v.Id}", Venta(cliente, estado: 3, items: items))).StatusCode);
    }

    [FactConBase]
    public async Task La_ultima_se_elimina_y_las_anteriores_se_anulan()
    {
        await PrepararAsync();
        await api.CrearUsuarioAsync("vta_bajas");
        var http = api.Cliente((await api.LoginAsync("vta_bajas")).TokenAcceso);
        var cliente = await CrearClienteAsync(http, "VTA Cliente bajas");
        var primera = (await (await http.PostAsJsonAsync("/api/ventas", Venta(cliente, items: [("VT-COM", 1, null)]))).Content.ReadFromJsonAsync<VentaDto>())!;
        var segunda = (await (await http.PostAsJsonAsync("/api/ventas", Venta(cliente, items: [("VT-COM", 2, null)]))).Content.ReadFromJsonAsync<VentaDto>())!;
        Assert.Equal(primera.Id + 1, segunda.Id);
        Assert.Equal(300m, await SaldoAsync(http, cliente));

        var borrada = (await (await http.DeleteAsync($"/api/ventas/{segunda.Id}")).Content.ReadFromJsonAsync<VentaEliminada>())!;
        Assert.Equal("eliminada", borrada.Accion);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"/api/ventas/{segunda.Id}")).StatusCode);
        // El número de la venta eliminada se vuelve a usar.
        var tercera = (await (await http.PostAsJsonAsync("/api/ventas", Venta(cliente, items: [("VT-COM", 1, null)]))).Content.ReadFromJsonAsync<VentaDto>())!;
        Assert.Equal(segunda.Id, tercera.Id);

        var anulada = (await (await http.DeleteAsync($"/api/ventas/{primera.Id}")).Content.ReadFromJsonAsync<VentaEliminada>())!;
        Assert.Equal("anulada", anulada.Accion);
        var leida = (await http.GetFromJsonAsync<VentaDto>($"/api/ventas/{primera.Id}"))!;
        Assert.Equal(EstadoVenta.Anulada, leida.IdEstado);
        Assert.Equal(0m, leida.TotalVenta);
        Assert.Empty(leida.Items);
        Assert.Equal(100m, await SaldoAsync(http, cliente));
    }

    [FactConBase]
    public async Task Solo_ver_no_puede_cargar_ni_cobrar()
    {
        await PrepararAsync();
        var rol = await api.CrearRolAsync("Consulta ventas", Permisos.VentasVer);
        await api.CrearUsuarioAsync("vta_consulta", rol);
        var http = api.Cliente((await api.LoginAsync("vta_consulta")).TokenAcceso);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/ventas")).StatusCode);
        Assert.NotEmpty((await http.GetFromJsonAsync<List<OpcionVentaDto>>("/api/estados-venta"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/ventas", Venta(1, items: [("VT-COM", 1, null)]))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/pagos", new { idFactura = 1, ajustarImporte = false, detalles = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.DeleteAsync("/api/ventas/1")).StatusCode);
    }
}
