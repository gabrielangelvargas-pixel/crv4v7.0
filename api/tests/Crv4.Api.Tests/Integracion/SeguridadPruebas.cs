using System.Net;
using System.Net.Http.Json;
using Crv4.Api.Modulos.Seguridad;

namespace Crv4.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class SeguridadPruebas(ApiFixture api)
{
    [FactConBase]
    public async Task Usuario_con_varios_roles_suma_los_permisos()
    {
        var ventas = await api.CrearRolAsync("Prueba ventas", Permisos.VentasVer, Permisos.VentasCrear);
        var rubros = await api.CrearRolAsync("Prueba rubros", Permisos.RubrosVer);
        await api.CrearUsuarioAsync("seg_admin");
        var admin = api.Cliente((await api.LoginAsync("seg_admin")).TokenAcceso);

        var alta = await admin.PostAsJsonAsync("/api/seguridad/usuarios", new
        {
            nombreUsuario = "seg_mostrador", nombre = "Mostrador", email = "mostrador@ejemplo.com",
            rolIds = new[] { ventas, rubros }, passwordInicial = "Caja2026x",
        });
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var usuario = (await alta.Content.ReadFromJsonAsync<UsuarioDto>())!;
        Assert.Equal(2, usuario.Roles.Count);

        var sesion = await api.LoginAsync("seg_mostrador", "Caja2026x");
        Assert.Equal(
            new[] { Permisos.RubrosVer, Permisos.VentasCrear, Permisos.VentasVer },
            sesion.Usuario.Permisos.Order().ToArray());

        var emailRepetido = await admin.PostAsJsonAsync("/api/seguridad/usuarios", new
        {
            nombreUsuario = "seg_otro", nombre = "Otro", email = "mostrador@ejemplo.com", rolIds = new[] { ventas }, passwordInicial = "Caja2026x",
        });
        Assert.Equal(HttpStatusCode.Conflict, emailRepetido.StatusCode);
    }

    [FactConBase]
    public async Task El_rol_Administrador_no_se_modifica_ni_se_borra()
    {
        await api.CrearUsuarioAsync("seg_admin_rol");
        var admin = api.Cliente((await api.LoginAsync("seg_admin_rol")).TokenAcceso);
        var id = await api.AdministradorIdAsync();

        var cambio = await admin.PutAsJsonAsync($"/api/seguridad/roles/{id}", new { nombre = "Administrador", activo = true, permisos = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, cambio.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/api/seguridad/roles/{id}")).StatusCode);

        var permisos = await admin.GetFromJsonAsync<List<PermisoDto>>("/api/seguridad/permisos");
        Assert.All(Permisos.Todos, p => Assert.Contains(permisos!, x => x.Codigo == p.Codigo));
    }
}
