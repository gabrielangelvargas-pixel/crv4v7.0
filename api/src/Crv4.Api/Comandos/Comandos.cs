using Crv4.Api.Datos;
using Crv4.Api.Infraestructura;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Comandos;

/// <summary>
/// Tareas de mantenimiento que se ejecutan a mano desde la consola, nunca al arrancar el servicio:
///   Crv4.Api.exe migrar       aplica las migraciones pendientes (usa ConnectionStrings:Migraciones)
///   Crv4.Api.exe crear-admin  crea el primer administrador pidiendo los datos por consola
/// </summary>
public static class Comandos
{
    public static bool EsComando(string[] args) => args.Length > 0 && args[0] is "migrar" or "crear-admin";

    public static async Task<int> EjecutarAsync(WebApplication app, string[] args)
    {
        try
        {
            return args[0] switch
            {
                "migrar" => await MigrarAsync(app),
                "crear-admin" => await CrearAdminAsync(app),
                _ => 1,
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> MigrarAsync(WebApplication app)
    {
        // La cuenta de migraciones necesita permisos de estructura (CREATE, ALTER, INDEX...).
        // La cuenta de la API (Principal) solo necesita leer y escribir datos.
        var cadena = app.Configuration.GetConnectionString("Migraciones");
        if (string.IsNullOrWhiteSpace(cadena))
            cadena = Servicios.CadenaConexion(app.Configuration, "Principal");

        var opciones = new DbContextOptionsBuilder<AppDbContext>();
        Servicios.ConfigurarBase(opciones, cadena);
        await using var db = new AppDbContext(opciones.Options);

        var pendientes = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pendientes.Count == 0)
        {
            Console.WriteLine("La base ya está al día. No hay migraciones pendientes.");
            return 0;
        }

        Console.WriteLine("Migraciones pendientes:");
        pendientes.ForEach(m => Console.WriteLine($"  - {m}"));
        if (!Environment.GetCommandLineArgs().Contains("--si"))
        {
            Console.Write("¿Hiciste una copia de seguridad y querés aplicarlas? (s/N): ");
            if (!string.Equals(Console.ReadLine()?.Trim(), "s", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Cancelado. No se cambió nada.");
                return 1;
            }
        }

        await db.Database.MigrateAsync();
        Console.WriteLine("Migraciones aplicadas.");
        return 0;
    }

    private static async Task<int> CrearAdminAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<PasswordServicio>();

        var adminId = await db.Roles.Where(r => r.Nombre == Rol.NombreAdministrador).Select(r => (int?)r.Id).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No existe el rol Administrador. Aplicá las migraciones primero (Crv4.Api.exe migrar).");
        if (await db.UsuariosRoles.AnyAsync(ur => ur.RolId == adminId && db.Usuarios.Any(u => u.Id == ur.UsuarioId && u.Activo)))
            Console.WriteLine("Aviso: ya existe al menos un administrador activo.");

        var nombreUsuario = Preguntar("Usuario: ");
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario))
            throw new InvalidOperationException("Ya existe un usuario con ese nombre.");
        var nombre = Preguntar("Nombre para mostrar: ");
        var password = PreguntarOculto("Contraseña: ");
        if (passwords.Validar(password, nombreUsuario) is { } motivo)
            throw new InvalidOperationException(motivo);
        if (PreguntarOculto("Repetir contraseña: ") != password)
            throw new InvalidOperationException("Las contraseñas no coinciden.");

        var usuario = new Usuario { NombreUsuario = nombreUsuario, Nombre = nombre, PasswordHash = "" };
        usuario.PasswordHash = passwords.Hashear(usuario, password);
        usuario.Roles.Add(new UsuarioRol { RolId = adminId });
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        Console.WriteLine($"Administrador '{nombreUsuario}' creado.");
        return 0;
    }

    private static string Preguntar(string texto)
    {
        Console.Write(texto);
        var valor = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(valor) ? throw new InvalidOperationException("Dato obligatorio.") : valor;
    }

    private static string PreguntarOculto(string texto)
    {
        Console.Write(texto);
        if (Console.IsInputRedirected)
            return Console.ReadLine() ?? "";
        var valor = new System.Text.StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) break;
            if (tecla.Key == ConsoleKey.Backspace) { if (valor.Length > 0) valor.Length--; continue; }
            if (!char.IsControl(tecla.KeyChar)) valor.Append(tecla.KeyChar);
        }
        Console.WriteLine();
        return valor.ToString();
    }
}
