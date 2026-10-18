using Crv4.Api.Datos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Crv4.Api.Modulos.Sistema;

public static class Numeradores
{
    public const string Ventas = "ventas";
    public const string Clientes = "clientes";

    /// <summary>
    /// Para cada numerador, el máximo que ya existe en su tabla. Mientras Sistema03 siga cargando con MAX(id)+1
    /// sobre la misma base, el numerador nunca entrega un número menor o igual a uno ya usado.
    /// </summary>
    internal static readonly Dictionary<string, string> MaximoExistente = new()
    {
        [Ventas] = "SELECT COALESCE(MAX(IdFactura), 0) FROM facturas",
        [Clientes] = "SELECT COALESCE(MAX(IdCliente), 0) FROM clientes",
    };
}

public interface INumeradorServicio
{
    /// <summary>Devuelve el próximo número para la clave. Seguro con varias PCs cargando a la vez.</summary>
    Task<long> SiguienteAsync(string clave, CancellationToken ct = default);
}

public class NumeradorServicio(AppDbContext db) : INumeradorServicio
{
    public async Task<long> SiguienteAsync(string clave, CancellationToken ct = default)
    {
        // UPDATE ... LAST_INSERT_ID(expr) incrementa en una sola sentencia atómica (bloquea la fila del numerador) y deja
        // el valor nuevo disponible para esta conexión, sin carreras entre PCs.
        // Si se llama dentro de una transacción, el número se libera si la transacción se revierte.
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            var conexion = db.Database.GetDbConnection();
            var transaccion = db.Database.CurrentTransaction?.GetDbTransaction();

            await using (var actualizar = conexion.CreateCommand())
            {
                actualizar.Transaction = transaccion;
                var maximo = Numeradores.MaximoExistente.TryGetValue(clave, out var consulta) ? $"({consulta})" : "0";
                actualizar.CommandText =
                    $"UPDATE numeradores SET UltimoNumero = LAST_INSERT_ID(GREATEST(UltimoNumero, {maximo}) + 1) WHERE Clave = @clave";
                var parametro = actualizar.CreateParameter();
                parametro.ParameterName = "@clave";
                parametro.Value = clave;
                actualizar.Parameters.Add(parametro);
                if (await actualizar.ExecuteNonQueryAsync(ct) != 1)
                    throw new InvalidOperationException($"No existe el numerador '{clave}'.");
            }

            await using var leer = conexion.CreateCommand();
            leer.Transaction = transaccion;
            leer.CommandText = "SELECT LAST_INSERT_ID()";
            return Convert.ToInt64(await leer.ExecuteScalarAsync(ct));
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
