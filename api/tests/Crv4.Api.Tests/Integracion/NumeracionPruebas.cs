using Crv4.Api.Datos;
using Crv4.Api.Modulos.Sistema;
using Microsoft.Extensions.DependencyInjection;

namespace Crv4.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class NumeracionPruebas(ApiFixture api)
{
    [FactConBase]
    public async Task Numerador_no_repite_numeros_con_carga_concurrente()
    {
        const int cantidad = 40;
        var tareas = Enumerable.Range(0, cantidad).Select(async _ =>
        {
            using var scope = api.Services.CreateScope();
            var numerador = scope.ServiceProvider.GetRequiredService<INumeradorServicio>();
            return await numerador.SiguienteAsync(Numeradores.Ventas);
        });
        var numeros = await Task.WhenAll(tareas);

        Assert.Equal(cantidad, numeros.Distinct().Count());
        Assert.Equal(numeros.Min() + cantidad - 1, numeros.Max());
    }

    [FactConBase]
    public async Task Numerador_dentro_de_transaccion_revertida_no_consume_numero()
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var numerador = scope.ServiceProvider.GetRequiredService<INumeradorServicio>();

        var antes = await numerador.SiguienteAsync(Numeradores.Clientes);
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await numerador.SiguienteAsync(Numeradores.Clientes);
            await tx.RollbackAsync();
        }
        Assert.Equal(antes + 1, await numerador.SiguienteAsync(Numeradores.Clientes));
    }
}
