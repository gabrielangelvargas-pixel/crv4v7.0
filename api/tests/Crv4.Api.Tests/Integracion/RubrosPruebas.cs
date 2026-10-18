using System.Net;
using System.Net.Http.Json;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Productos;
using Crv4.Api.Modulos.Rubros;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Crv4.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class RubrosPruebas(ApiFixture api)
{
    [FactConBase]
    public async Task Rubros_alta_modificacion_y_validaciones()
    {
        await api.CrearUsuarioAsync("rubros_admin");
        var cliente = api.Cliente((await api.LoginAsync("rubros_admin")).TokenAcceso);

        var alta = await cliente.PostAsJsonAsync("/api/rubros", new { codigo = " bz ", nombre = "  Bazar  ", orden = 2 });
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var rubro = (await alta.Content.ReadFromJsonAsync<RubroDto>())!;
        Assert.Equal("Bazar", rubro.Nombre);
        Assert.Equal("BZ", rubro.Codigo);

        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/rubros", new { codigo = "BZ2", nombre = "Bazar", orden = 3 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/rubros", new { codigo = "BZ", nombre = "Otro", orden = 3 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/rubros", new { codigo = "X", orden = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/rubros", new { codigo = "MUYLARGO", nombre = "Largo", orden = 1 })).StatusCode);

        var cambio = await cliente.PutAsJsonAsync($"/api/rubros/{rubro.Id}", new { codigo = "BZ", nombre = "Bazar y hogar", orden = 1, activo = false });
        Assert.Equal(HttpStatusCode.NoContent, cambio.StatusCode);
        var leido = await cliente.GetFromJsonAsync<RubroDto>($"/api/rubros/{rubro.Id}");
        Assert.Equal("Bazar y hogar", leido!.Nombre);
        Assert.False(leido.Activo);
    }

    [FactConBase]
    public async Task Subrubros_ciclos_y_eliminacion()
    {
        await api.CrearUsuarioAsync("rubros_arbol");
        var cliente = api.Cliente((await api.LoginAsync("rubros_arbol")).TokenAcceso);
        async Task<RubroDto> Alta(string codigo, int? idPadre = null) =>
            (await (await cliente.PostAsJsonAsync("/api/rubros", new { codigo, nombre = "Rubro " + codigo, idPadre, orden = 0 }))
                .Content.ReadFromJsonAsync<RubroDto>())!;

        var padre = await Alta("PA");
        var hijo = await Alta("PA01", padre.Id);
        Assert.Equal("Rubro PA", hijo.NombrePadre);

        // El padre no puede pasar a colgar de su propio hijo.
        var ciclo = await cliente.PutAsJsonAsync($"/api/rubros/{padre.Id}", new { codigo = "PA", nombre = "Rubro PA", idPadre = hijo.Id, orden = 0, activo = true });
        Assert.Equal(HttpStatusCode.BadRequest, ciclo.StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await cliente.DeleteAsync($"/api/rubros/{padre.Id}")).StatusCode);

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.ExecuteSqlRaw("INSERT IGNORE INTO proveedores (IdProveedor, NombreProveedor) VALUES ('00', 'Sin proveedor')");
            db.Productos.Add(new Producto { Codigo = "PRUEBA0001", IdProveedor = "00", IdRubro = "PA01", Descripcion = "Producto de prueba" });
            await db.SaveChangesAsync();
        }
        Assert.Equal(1, (await cliente.GetFromJsonAsync<RubroDto>($"/api/rubros/{hijo.Id}"))!.Productos);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.DeleteAsync($"/api/rubros/{hijo.Id}")).StatusCode);
        var cambiaCodigo = await cliente.PutAsJsonAsync($"/api/rubros/{hijo.Id}", new { codigo = "PA02", nombre = "Rubro PA01", idPadre = padre.Id, orden = 0, activo = true });
        Assert.Equal(HttpStatusCode.Conflict, cambiaCodigo.StatusCode);

        var suelto = await Alta("SU");
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/rubros/{suelto.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/rubros/{suelto.Id}")).StatusCode);
    }

    [FactConBase]
    public async Task Inicio_cuenta_productos_clientes_y_rubros()
    {
        await api.CrearUsuarioAsync("inicio_ver");
        var cliente = api.Cliente((await api.LoginAsync("inicio_ver")).TokenAcceso);
        var r = await cliente.GetAsync("/api/inicio/resumen");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        // Quien no es administrador no ve los números del negocio.
        var rol = await api.CrearRolAsync("Solo rubros inicio", Crv4.Api.Modulos.Seguridad.Permisos.RubrosVer);
        await api.CrearUsuarioAsync("inicio_comun", rol);
        var comun = api.Cliente((await api.LoginAsync("inicio_comun")).TokenAcceso);
        Assert.Equal(HttpStatusCode.Forbidden, (await comun.GetAsync("/api/inicio/resumen")).StatusCode);
    }
}
