using Crv4.Api.Modulos.Seguridad;

namespace Crv4.Api.Tests.Unidad;

public class PermisosPruebas
{
    [Fact]
    public void Catalogo_sin_codigos_repetidos()
    {
        var codigos = Permisos.Todos.Select(p => p.Codigo).ToList();
        Assert.Equal(codigos.Count, codigos.Distinct().Count());
    }

    [Fact]
    public void Cada_constante_esta_en_el_catalogo()
    {
        var constantes = typeof(Permisos).GetFields()
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);
        var catalogo = Permisos.Todos.Select(p => p.Codigo).ToHashSet();
        Assert.All(constantes, c => Assert.Contains(c, catalogo));
    }
}
