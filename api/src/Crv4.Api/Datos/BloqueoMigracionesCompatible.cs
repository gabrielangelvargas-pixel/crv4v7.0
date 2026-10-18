using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Crv4.Api.Datos;

/// <summary>
/// Hace que "migrar" funcione también en MariaDB (por ejemplo la base de Hostinger).
/// Al migrar, el proveedor bloquea con GET_LOCK('__EFMigrationsLock',-1) (espera infinita): MySQL lo acepta,
/// pero MariaDB devuelve NULL y la migración falla con "Unable to cast object of type 'System.DBNull' to type 'System.Int64'".
/// Se cambia por una espera de una hora, que vale para los dos.
/// </summary>
public sealed class BloqueoMigracionesCompatible : DbCommandInterceptor
{
    private const string Original = "GET_LOCK('__EFMigrationsLock',-1)";
    private const string Compatible = "GET_LOCK('__EFMigrationsLock',3600)";

    public static readonly BloqueoMigracionesCompatible Instancia = new();

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Ajustar(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        Ajustar(command);
        return ValueTask.FromResult(result);
    }

    private static void Ajustar(DbCommand command)
    {
        if (command.CommandText.Contains(Original, StringComparison.Ordinal))
            command.CommandText = command.CommandText.Replace(Original, Compatible, StringComparison.Ordinal);
    }
}
