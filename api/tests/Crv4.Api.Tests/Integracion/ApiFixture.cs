using System.Net.Http.Headers;
using System.Net.Http.Json;
using Crv4.Api.Datos;
using Crv4.Api.Infraestructura;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Crv4.Api.Tests.Integracion;

/// <summary>
/// Levanta la API contra una base MySQL de prueba que se borra y se recrea con las migraciones.
/// Se activa con la variable CRV4_TEST_MYSQL, por ejemplo:
///   server=localhost;database=crv4_test;user=root;password=...
/// ¡Nunca apuntarla a la base real! El nombre de la base tiene que terminar en "_test".
/// </summary>
public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Password = "Prueba1234";
    public static readonly string? Cadena = Environment.GetEnvironmentVariable("CRV4_TEST_MYSQL");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Principal"] = Cadena,
            ["Jwt:Clave"] = "clave-de-pruebas-automaticas-de-32-caracteres-o-mas",
            ["Seguridad:SolicitudesLoginPorMinuto"] = "10000",
            ["Cors:OrigenesPermitidos:0"] = "https://web.ejemplo.com",
        }));
    }

    public async Task InitializeAsync()
    {
        if (Cadena is null) return;
        if (!new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(Cadena).Database.EndsWith("_test"))
            throw new InvalidOperationException("CRV4_TEST_MYSQL tiene que apuntar a una base que termine en _test.");

        var opciones = new DbContextOptionsBuilder<AppDbContext>();
        Servicios.ConfigurarBase(opciones, Cadena);
        await using var db = new AppDbContext(opciones.Options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;

    public async Task<Usuario> CrearUsuarioAsync(string nombreUsuario, int rolId = RolesSistema.AdministradorId, bool debeCambiar = false)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<PasswordServicio>();
        var usuario = new Usuario { NombreUsuario = nombreUsuario, Nombre = nombreUsuario, RolId = rolId, PasswordHash = "", DebeCambiarPassword = debeCambiar };
        usuario.PasswordHash = passwords.Hashear(usuario, Password);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    public async Task<int> CrearRolAsync(string nombre, params string[] permisos)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rol = new Rol { Nombre = nombre, Permisos = permisos.Select(p => new RolPermiso { PermisoCodigo = p }).ToList() };
        db.Roles.Add(rol);
        await db.SaveChangesAsync();
        return rol.Id;
    }

    public async Task<SesionRespuesta> LoginAsync(string usuario, string password = Password)
    {
        var respuesta = await CreateClient().PostAsJsonAsync("/api/auth/login", new { usuario, password });
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<SesionRespuesta>())!;
    }

    public HttpClient Cliente(string token)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

[CollectionDefinition(Nombre)]
public class ColeccionApi : ICollectionFixture<ApiFixture>
{
    public const string Nombre = "api";
}

/// <summary>Se saltea si no hay base de prueba configurada.</summary>
public sealed class FactConBaseAttribute : FactAttribute
{
    public FactConBaseAttribute()
    {
        if (ApiFixture.Cadena is null)
            Skip = "Definí CRV4_TEST_MYSQL para correr las pruebas con MySQL.";
    }
}
