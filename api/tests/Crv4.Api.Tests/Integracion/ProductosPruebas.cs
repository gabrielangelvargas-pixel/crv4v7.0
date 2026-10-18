using System.Net;
using System.Net.Http.Json;
using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Productos;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Crv4.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class ProductosPruebas(ApiFixture api)
{
    private async Task PrepararAsync()
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("INSERT IGNORE INTO proveedores (IdProveedor, NombreProveedor) VALUES ('P1', 'Proveedor uno'), ('P2', 'Proveedor dos')");
        await db.Database.ExecuteSqlRawAsync("INSERT IGNORE INTO rubros (IdRubro, NombreRubro, Orden, Activo) VALUES ('PRA', 'Productos A', 0, 1)");
        await db.Database.ExecuteSqlRawAsync("INSERT IGNORE INTO rubros (IdRubro, NombreRubro, IdPadre, Orden, Activo) SELECT 'PRA01', 'Productos A hijo', Id, 0, 1 FROM rubros WHERE IdRubro = 'PRA'");
    }

    private static object Datos(string codigo, string rubro = "PRA01", decimal compra = 1000, decimal descuento = 10, decimal utilidad = 50,
        decimal? venta = null, bool activo = true, string? referencia = "REF-9") =>
        new { codigo, descripcion = "Producto " + codigo, idRubro = rubro, idProveedor = "P1", referenciaProveedor = referencia,
            precioCompra = compra, descuento, utilidad, precioVenta = venta, precioOferta = (decimal?)null, activo };

    [FactConBase]
    public async Task Alta_calcula_precios_y_filtra_por_rubro_padre()
    {
        await PrepararAsync();
        await api.CrearUsuarioAsync("prod_admin");
        var cliente = api.Cliente((await api.LoginAsync("prod_admin")).TokenAcceso);

        var alta = await cliente.PostAsJsonAsync("/api/productos", Datos("PT-001"));
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var p = (await alta.Content.ReadFromJsonAsync<ProductoDto>())!;
        Assert.Equal(900m, p.PrecioCosto);
        Assert.Equal(1350m, p.PrecioVenta);
        Assert.Equal("Productos A hijo", p.Rubro);
        Assert.Equal("REF-9", p.ReferenciaProveedor);

        // Con precio de venta cargado, la utilidad sale de ese precio.
        var otro = (await (await cliente.PostAsJsonAsync("/api/productos", Datos("PT-002", venta: 1800))).Content.ReadFromJsonAsync<ProductoDto>())!;
        Assert.Equal(100m, otro.Utilidad);

        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/productos", Datos("PT-001"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/productos", Datos("PT-003", rubro: "NOEXISTE"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/productos", Datos("PT-0000000000004"))).StatusCode);

        var porPadre = await cliente.GetFromJsonAsync<Paginado<ProductoDto>>("/api/productos?idRubro=PRA&buscar=PT-");
        Assert.Equal(2, porPadre!.Total);
        var inactivos = await cliente.GetFromJsonAsync<Paginado<ProductoDto>>("/api/productos?idRubro=PRA&activo=false&buscar=PT-");
        Assert.Equal(0, inactivos!.Total);
    }

    [FactConBase]
    public async Task Cambio_de_codigo_eliminacion_y_ventas()
    {
        await PrepararAsync();
        await api.CrearUsuarioAsync("prod_cambios");
        var cliente = api.Cliente((await api.LoginAsync("prod_cambios")).TokenAcceso);
        await cliente.PostAsJsonAsync("/api/productos", Datos("PC-001"));

        var cambio = await cliente.PutAsJsonAsync("/api/productos/PC-001", Datos("PC-001B", compra: 2000, activo: false));
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        var leido = (await cambio.Content.ReadFromJsonAsync<ProductoDto>())!;
        Assert.Equal("PC-001B", leido.Codigo);
        Assert.False(leido.Activo);
        Assert.Equal(2700m, leido.PrecioVenta);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/productos/PC-001")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync("/api/productos/PC-001B")).StatusCode);

        await cliente.PostAsJsonAsync("/api/productos", Datos("PC-002"));
        await InsertarVentaAsync("PC-002");
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.DeleteAsync("/api/productos/PC-002")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PutAsJsonAsync("/api/productos/PC-002", Datos("PC-002B"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PutAsJsonAsync("/api/productos/PC-002", Datos("PC-002", activo: false))).StatusCode);
    }

    [FactConBase]
    public async Task Sin_permiso_de_edicion_no_ve_costos_ni_referencia()
    {
        await PrepararAsync();
        await api.CrearUsuarioAsync("prod_dueno");
        var admin = api.Cliente((await api.LoginAsync("prod_dueno")).TokenAcceso);
        await admin.PostAsJsonAsync("/api/productos", Datos("PV-001"));

        var rol = await api.CrearRolAsync("Vendedor productos", Permisos.ProductosVer);
        await api.CrearUsuarioAsync("prod_vendedor", rol);
        var vendedor = api.Cliente((await api.LoginAsync("prod_vendedor")).TokenAcceso);
        var p = (await vendedor.GetFromJsonAsync<ProductoDto>("/api/productos/PV-001"))!;
        Assert.Null(p.PrecioCompra);
        Assert.Null(p.Utilidad);
        Assert.Null(p.ReferenciaProveedor);
        Assert.Equal(1350m, p.PrecioVenta);
        Assert.Equal(HttpStatusCode.Forbidden, (await vendedor.PostAsJsonAsync("/api/productos", Datos("PV-002"))).StatusCode);
        Assert.NotEmpty((await vendedor.GetFromJsonAsync<List<ProveedorDto>>("/api/proveedores"))!);
    }

    private async Task InsertarVentaAsync(string codigo)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Solo hace falta una fila con el código: se desactivan las claves foráneas para no armar una venta completa.
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0");
        await db.Database.ExecuteSqlRawAsync(FilaVenta, codigo);
        await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");
    }

    private const string FilaVenta = "INSERT INTO facturasdetalles (IdFactura, IdProducto, Cantidad) VALUES (999999, {0}, 1)";
}
