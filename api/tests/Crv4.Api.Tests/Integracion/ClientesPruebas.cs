using System.Net;
using System.Net.Http.Json;
using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Clientes;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Crv4.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class ClientesPruebas(ApiFixture api)
{
    private async Task<AppDbContext> PrepararAsync(IServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("INSERT IGNORE INTO zonas (IdZona, NombreZona) VALUES (91, 'Zona norte'), (92, 'Zona sur')");
        await db.Database.ExecuteSqlRawAsync("INSERT IGNORE INTO fiscales (IdFiscal, NombreFiscal) VALUES (91, 'Consumidor final'), (92, 'Resp. inscripto')");
        return db;
    }

    private static object Datos(string nombre, int? id = null, int zona = 91, int fiscal = 91, decimal saldo = 0, string? celular = null) =>
        new { id, nombre, direccion = "Calle 123", idZona = zona, idFiscal = fiscal, numeroFiscal = "20123456789",
            celular1 = celular, celular2 = (string?)null, telefonoFijo = (string?)null, saldoCuenta = saldo };

    [FactConBase]
    public async Task Alta_numera_sola_y_filtra()
    {
        using var scope = api.Services.CreateScope();
        await PrepararAsync(scope);
        await api.CrearUsuarioAsync("cli_admin");
        var http = api.Cliente((await api.LoginAsync("cli_admin")).TokenAcceso);

        var primero = (await (await http.PostAsJsonAsync("/api/clientes", Datos("CLT Uno", celular: "3815550001"))).Content.ReadFromJsonAsync<ClienteDto>())!;
        var segundo = (await (await http.PostAsJsonAsync("/api/clientes", Datos("CLT Dos", zona: 92, saldo: 1500.555m))).Content.ReadFromJsonAsync<ClienteDto>())!;
        Assert.Equal(primero.Id + 1, segundo.Id);
        Assert.Equal("Zona norte", primero.Zona);
        Assert.Equal("Consumidor final", primero.CondicionFiscal);
        Assert.Equal(1500.56m, segundo.SaldoCuenta);

        // Con un código elegido a mano, el siguiente automático sigue después.
        var manual = await http.PostAsJsonAsync("/api/clientes", Datos("CLT Manual", id: segundo.Id + 10));
        Assert.Equal(HttpStatusCode.Created, manual.StatusCode);
        var tercero = (await (await http.PostAsJsonAsync("/api/clientes", Datos("CLT Tres"))).Content.ReadFromJsonAsync<ClienteDto>())!;
        Assert.Equal(segundo.Id + 11, tercero.Id);

        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsJsonAsync("/api/clientes", Datos("CLT Repetido", id: primero.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/clientes", Datos("CLT Sin zona", zona: 999))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/clientes", Datos("CLT Negativo", saldo: -1))).StatusCode);

        Assert.Equal(1, (await http.GetFromJsonAsync<Paginado<ClienteDto>>("/api/clientes?buscar=3815550001"))!.Total);
        Assert.Equal(1, (await http.GetFromJsonAsync<Paginado<ClienteDto>>("/api/clientes?buscar=CLT&idZona=92"))!.Total);
        Assert.Equal(1, (await http.GetFromJsonAsync<Paginado<ClienteDto>>("/api/clientes?buscar=CLT&conSaldo=true"))!.Total);
        Assert.Equal(2, (await http.GetFromJsonAsync<List<OpcionDto>>("/api/zonas"))!.Count(z => z.Id is 91 or 92));
    }

    [FactConBase]
    public async Task Cambio_de_codigo_mueve_pagos_y_bonos_y_no_borra_con_movimientos()
    {
        using var scope = api.Services.CreateScope();
        var db = await PrepararAsync(scope);
        await api.CrearUsuarioAsync("cli_cambios");
        var http = api.Cliente((await api.LoginAsync("cli_cambios")).TokenAcceso);

        await http.PostAsJsonAsync("/api/clientes", Datos("CLC Uno", id: 880001));
        await db.Database.ExecuteSqlRawAsync("INSERT INTO pagos (IdCliente, ImporteTotal) VALUES (880001, 100)");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO bonosdescuento (CodigoBono, IdFacturaOrigen, IdCliente, FechaEmision, FechaVencimiento, Porcentaje, TotalFactura, ImporteBono) " +
            "VALUES ('CLC-B1', 880001, 880001, NOW(), NOW(), 5, 100, 5)");

        Assert.Equal(HttpStatusCode.Conflict, (await http.DeleteAsync("/api/clientes/880001")).StatusCode);

        var cambio = await http.PutAsJsonAsync("/api/clientes/880001", Datos("CLC Uno cambiado", id: 880002, fiscal: 92));
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        Assert.Equal("Resp. inscripto", (await cambio.Content.ReadFromJsonAsync<ClienteDto>())!.CondicionFiscal);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/clientes/880001")).StatusCode);
        Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pagos WHERE IdCliente = 880002").SingleAsync());
        Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM bonosdescuento WHERE IdCliente = 880002").SingleAsync());

        await http.PostAsJsonAsync("/api/clientes", Datos("CLC Libre", id: 880003));
        Assert.Equal(HttpStatusCode.Conflict, (await http.PutAsJsonAsync("/api/clientes/880003", Datos("CLC Libre", id: 880002))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync("/api/clientes/880003")).StatusCode);
    }

    [FactConBase]
    public async Task Solo_ver_no_puede_guardar()
    {
        using var scope = api.Services.CreateScope();
        await PrepararAsync(scope);
        var rol = await api.CrearRolAsync("Consulta clientes", Permisos.ClientesVer);
        await api.CrearUsuarioAsync("cli_consulta", rol);
        var http = api.Cliente((await api.LoginAsync("cli_consulta")).TokenAcceso);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/clientes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/clientes", Datos("CLS Uno"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.DeleteAsync("/api/clientes/1")).StatusCode);
    }
}
