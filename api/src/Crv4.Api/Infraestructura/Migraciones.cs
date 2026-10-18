using Crv4.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Infraestructura;

public static class Migraciones
{
    /// <summary>
    /// La API nunca cambia la estructura de la base al arrancar. Si hay migraciones sin aplicar,
    /// avisa y no arranca, para que no trabaje con un esquema que no coincide con el código.
    /// Las migraciones se aplican a propósito con: Crv4.Api.exe migrar
    /// </summary>
    public static async Task<bool> VerificarAsync(IServiceProvider servicios, ILogger log)
    {
        await using var scope = servicios.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            var pendientes = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pendientes.Count == 0)
                return true;
            log.LogCritical("La base tiene {Cantidad} migraciones sin aplicar ({Lista}). Ejecutá 'Crv4.Api.exe migrar' (o deploy/servidor/Aplicar-Migraciones.ps1) y volvé a iniciar el servicio.",
                pendientes.Count, string.Join(", ", pendientes));
            return false;
        }
        catch (Exception ex)
        {
            // Sin conexión no se bloquea el arranque: /api/salud lo informa y se reintenta en cada solicitud.
            log.LogError(ex, "No se pudo conectar a MySQL para verificar las migraciones.");
            return true;
        }
    }
}
