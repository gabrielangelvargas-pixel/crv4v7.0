using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Crv4.Api.Datos;

/// <summary>
/// Hace que "migrar" funcione también en MariaDB (por ejemplo la base de Hostinger).
/// Al migrar, el proveedor bloquea con GET_LOCK('__EFMigrationsLock',-1) y lee el resultado como BIGINT.
/// En MariaDB eso falla de dos formas:
///   - la espera -1 (infinita) devuelve NULL ("Unable to cast object of type 'System.DBNull' to type 'System.Int64'");
///   - el resultado llega como INT ("... 'System.Int32' to type 'System.Int64'"), aunque se use CAST(... AS SIGNED).
/// Se cambia por una espera de una hora y el resultado se entrega siempre como long. En MySQL no cambia nada.
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

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result) =>
        ComoLong(command, result);

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ComoLong(command, result));

    private static void Ajustar(DbCommand command)
    {
        if (command.CommandText.Contains(Original, StringComparison.Ordinal))
            command.CommandText = command.CommandText.Replace(Original, Compatible, StringComparison.Ordinal);
    }

    private static object? ComoLong(DbCommand command, object? result) =>
        command.CommandText.Contains("_LOCK('__EFMigrationsLock'", StringComparison.Ordinal) && result is not (null or DBNull)
            ? Convert.ToInt64(result)
            : result;
}
