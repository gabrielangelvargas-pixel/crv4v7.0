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
/// Levanta la API contra una base MySQL de prueba que se borra, se recrea con la estructura actual de crv4_datos
/// (database/crv4_datos-estructura.sql) y después recibe las migraciones de la 7.0, igual que la base real.
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

        var datos = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(Cadena);
        var nombreBase = datos.Database;
        datos.Database = "";
        await using (var conexion = new MySql.Data.MySqlClient.MySqlConnection(datos.ConnectionString))
        {
            await conexion.OpenAsync();
            await new MySql.Data.MySqlClient.MySqlCommand(
                $"DROP DATABASE IF EXISTS `{nombreBase}`; CREATE DATABASE `{nombreBase}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;",
                conexion).ExecuteNonQueryAsync();
            await conexion.ChangeDatabaseAsync(nombreBase);
            await new MySql.Data.MySqlClient.MySqlScript(conexion, await File.ReadAllTextAsync(RutaEstructura())).ExecuteAsync();
        }

        var opciones = new DbContextOptionsBuilder<AppDbContext>();
        Servicios.ConfigurarBase(opciones, Cadena);
        await using var db = new AppDbContext(opciones.Options);
        await db.Database.MigrateAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;

    private static string RutaEstructura()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var ruta = Path.Combine(dir.FullName, "database", "crv4_datos-estructura.sql");
            if (File.Exists(ruta)) return ruta;
        }
        throw new FileNotFoundException("No encontré database/crv4_datos-estructura.sql.");
    }

    /// <summary>Id del rol Administrador que crea la migración.</summary>
    public async Task<int> AdministradorIdAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Roles.Where(r => r.Nombre == Rol.NombreAdministrador).Select(r => r.Id).SingleAsync();
    }

    /// <summary>Sin rol indicado, el usuario es Administrador.</summary>
    public async Task<Usuario> CrearUsuarioAsync(string nombreUsuario, int? rolId = null, bool debeCambiar = false)
    {
        var rol = rolId ?? await AdministradorIdAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<PasswordServicio>();
        var usuario = new Usuario { NombreUsuario = nombreUsuario, Nombre = nombreUsuario, PasswordHash = "", DebeCambiarPassword = debeCambiar };
        usuario.Roles.Add(new UsuarioRol { RolId = rol });
        usuario.PasswordHash = passwords.Hashear(usuario, Password);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    public async Task<int> CrearRolAsync(string nombre, params string[] permisos)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ids = await db.Permisos.Where(p => permisos.Contains(p.Codigo)).Select(p => p.Id).ToListAsync();
        var rol = new Rol { Nombre = nombre, Permisos = ids.Select(id => new RolPermiso { PermisoId = id }).ToList() };
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
