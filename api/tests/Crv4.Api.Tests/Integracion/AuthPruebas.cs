using System.Net;
using System.Net.Http.Json;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;

namespace Crv4.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class AuthPruebas(ApiFixture api)
{
    [FactConBase]
    public async Task Salud_responde_sin_login()
    {
        var respuesta = await api.CreateClient().GetAsync("/api/salud");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [FactConBase]
    public async Task Endpoints_protegidos_exigen_login()
    {
        var respuesta = await api.CreateClient().GetAsync("/api/rubros");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [FactConBase]
    public async Task Login_correcto_devuelve_tokens_y_permisos()
    {
        await api.CrearUsuarioAsync("login_ok");
        var sesion = await api.LoginAsync("login_ok");

        Assert.False(string.IsNullOrEmpty(sesion.TokenAcceso));
        Assert.False(string.IsNullOrEmpty(sesion.TokenRenovacion));
        Assert.Contains(Permisos.RubrosEditar, sesion.Usuario.Permisos);
        Assert.True(sesion.AccesoExpiraEn < DateTime.UtcNow.AddMinutes(16), "El token de acceso tiene que ser corto.");
        Assert.True(sesion.SesionExpiraEn < DateTime.UtcNow.AddHours(13), "La sesión tiene que vencer.");
    }

    [FactConBase]
    public async Task Varios_intentos_fallidos_bloquean_el_usuario()
    {
        await api.CrearUsuarioAsync("login_bloqueo");
        var cliente = api.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            var mal = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario = "login_bloqueo", password = "incorrecta1" });
            Assert.Equal(HttpStatusCode.Unauthorized, mal.StatusCode);
        }
        var bien = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario = "login_bloqueo", password = ApiFixture.Password });
        Assert.Equal(HttpStatusCode.Locked, bien.StatusCode);
    }

    [FactConBase]
    public async Task Renovar_rota_el_token_y_el_reuso_cierra_todas_las_sesiones()
    {
        await api.CrearUsuarioAsync("login_rota");
        var sesion = await api.LoginAsync("login_rota");
        var cliente = api.CreateClient();

        var renovada = await cliente.PostAsJsonAsync("/api/auth/renovar", new { tokenRenovacion = sesion.TokenRenovacion });
        renovada.EnsureSuccessStatusCode();
        var nueva = (await renovada.Content.ReadFromJsonAsync<SesionRespuesta>())!;
        Assert.NotEqual(sesion.TokenRenovacion, nueva.TokenRenovacion);
        Assert.Equal(sesion.SesionExpiraEn, nueva.SesionExpiraEn, TimeSpan.FromSeconds(1));

        // Reusar el token viejo (posible robo) invalida también el nuevo.
        var reuso = await cliente.PostAsJsonAsync("/api/auth/renovar", new { tokenRenovacion = sesion.TokenRenovacion });
        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);
        var conNuevo = await cliente.PostAsJsonAsync("/api/auth/renovar", new { tokenRenovacion = nueva.TokenRenovacion });
        Assert.Equal(HttpStatusCode.Unauthorized, conNuevo.StatusCode);
    }

    [FactConBase]
    public async Task Salir_invalida_el_token_de_renovacion()
    {
        await api.CrearUsuarioAsync("login_salir");
        var sesion = await api.LoginAsync("login_salir");
        var cliente = api.CreateClient();
        (await cliente.PostAsJsonAsync("/api/auth/salir", new { tokenRenovacion = sesion.TokenRenovacion })).EnsureSuccessStatusCode();
        var renovar = await cliente.PostAsJsonAsync("/api/auth/renovar", new { tokenRenovacion = sesion.TokenRenovacion });
        Assert.Equal(HttpStatusCode.Unauthorized, renovar.StatusCode);
    }

    [FactConBase]
    public async Task Con_cambio_de_password_pendiente_solo_puede_cambiarla()
    {
        await api.CrearUsuarioAsync("login_cambio", debeCambiar: true);
        var sesion = await api.LoginAsync("login_cambio");
        var cliente = api.Cliente(sesion.TokenAcceso);

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/rubros")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/auth/yo")).StatusCode);

        var debil = await cliente.PostAsJsonAsync("/api/auth/cambiar-password", new { passwordActual = ApiFixture.Password, passwordNueva = "corta" });
        Assert.Equal(HttpStatusCode.BadRequest, debil.StatusCode);

        var cambio = await cliente.PostAsJsonAsync("/api/auth/cambiar-password", new { passwordActual = ApiFixture.Password, passwordNueva = "NuevaClave99" });
        cambio.EnsureSuccessStatusCode();
        var nueva = (await cambio.Content.ReadFromJsonAsync<SesionRespuesta>())!;
        Assert.False(nueva.Usuario.DebeCambiarPassword);
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente(nueva.TokenAcceso).GetAsync("/api/rubros")).StatusCode);

        // La sesión anterior quedó cerrada.
        var vieja = await api.CreateClient().PostAsJsonAsync("/api/auth/renovar", new { tokenRenovacion = sesion.TokenRenovacion });
        Assert.Equal(HttpStatusCode.Unauthorized, vieja.StatusCode);
    }

    [FactConBase]
    public async Task Sin_el_permiso_devuelve_403()
    {
        var rol = await api.CrearRolAsync("Solo lectura rubros", Permisos.RubrosVer);
        await api.CrearUsuarioAsync("login_lectura", rol);
        var cliente = api.Cliente((await api.LoginAsync("login_lectura")).TokenAcceso);

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/rubros")).StatusCode);
        var crear = await cliente.PostAsJsonAsync("/api/rubros", new { codigo = "ND", nombre = "No debería", orden = 0 });
        Assert.Equal(HttpStatusCode.Forbidden, crear.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/seguridad/usuarios")).StatusCode);
    }

    [FactConBase]
    public async Task Desactivar_un_usuario_cierra_sus_sesiones()
    {
        await api.CrearUsuarioAsync("login_admin_desact");
        var admin = api.Cliente((await api.LoginAsync("login_admin_desact")).TokenAcceso);
        var empleado = await api.CrearUsuarioAsync("login_empleado");
        var sesionEmpleado = await api.LoginAsync("login_empleado");

        var r = await admin.PutAsJsonAsync($"/api/seguridad/usuarios/{empleado.Id}", new { nombre = "Empleado", rolIds = new[] { empleado.Roles[0].RolId }, activo = false });
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);

        var renovar = await api.CreateClient().PostAsJsonAsync("/api/auth/renovar", new { tokenRenovacion = sesionEmpleado.TokenRenovacion });
        Assert.Equal(HttpStatusCode.Unauthorized, renovar.StatusCode);
    }

    [FactConBase]
    public async Task Cors_y_red_privada_solo_para_el_origen_permitido()
    {
        var cliente = api.CreateClient();
        HttpRequestMessage Preflight(string origen)
        {
            var m = new HttpRequestMessage(HttpMethod.Options, "/api/rubros");
            m.Headers.Add("Origin", origen);
            m.Headers.Add("Access-Control-Request-Method", "GET");
            m.Headers.Add("Access-Control-Request-Private-Network", "true");
            return m;
        }

        var ok = await cliente.SendAsync(Preflight("https://web.ejemplo.com"));
        Assert.Equal("https://web.ejemplo.com", ok.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", ok.Headers.GetValues("Access-Control-Allow-Private-Network").Single());

        var otro = await cliente.SendAsync(Preflight("https://otro-sitio.com"));
        Assert.False(otro.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(otro.Headers.Contains("Access-Control-Allow-Private-Network"));
    }
}
