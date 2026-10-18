using System.Net;
using System.Net.Http.Json;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Rubros;
using Crv4.Api.Modulos.Sistema;
using Microsoft.Extensions.DependencyInjection;

namespace Crv4.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class RubrosYNumeracionPruebas(ApiFixture api)
{
    [FactConBase]
    public async Task Rubros_alta_modificacion_y_validaciones()
    {
        await api.CrearUsuarioAsync("rubros_admin");
        var cliente = api.Cliente((await api.LoginAsync("rubros_admin")).TokenAcceso);

        var alta = await cliente.PostAsJsonAsync("/api/rubros", new { nombre = "  Bazar  ", orden = 2 });
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var rubro = (await alta.Content.ReadFromJsonAsync<RubroDto>())!;
        Assert.Equal("Bazar", rubro.Nombre);

        var duplicado = await cliente.PostAsJsonAsync("/api/rubros", new { nombre = "Bazar", orden = 3 });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        var sinNombre = await cliente.PostAsJsonAsync("/api/rubros", new { orden = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, sinNombre.StatusCode);

        var cambio = await cliente.PutAsJsonAsync($"/api/rubros/{rubro.Id}", new { nombre = "Bazar y hogar", orden = 1, activo = false });
        Assert.Equal(HttpStatusCode.NoContent, cambio.StatusCode);
        var leido = await cliente.GetFromJsonAsync<RubroDto>($"/api/rubros/{rubro.Id}");
        Assert.Equal("Bazar y hogar", leido!.Nombre);
        Assert.False(leido.Activo);
    }

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
