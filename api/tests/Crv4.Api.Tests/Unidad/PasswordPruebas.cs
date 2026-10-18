using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.Extensions.Options;

namespace Crv4.Api.Tests.Unidad;

public class PasswordPruebas
{
    private readonly PasswordServicio _servicio = new(Options.Create(new SeguridadOpciones()));

    [Theory]
    [InlineData("corta1", false)]
    [InlineData("sololetras", false)]
    [InlineData("12345678", false)]
    [InlineData("juanperez1", false)] // contiene el usuario
    [InlineData("Mostrador2026", true)]
    public void Valida_reglas_minimas(string password, bool valida) =>
        Assert.Equal(valida, _servicio.Validar(password, "juanperez") is null);

    [Fact]
    public void Hash_verifica_y_no_guarda_el_texto()
    {
        var usuario = new Usuario { NombreUsuario = "u", Nombre = "U", PasswordHash = "" };
        usuario.PasswordHash = _servicio.Hashear(usuario, "Mostrador2026");
        Assert.DoesNotContain("Mostrador2026", usuario.PasswordHash);
        Assert.True(_servicio.Verificar(usuario, "Mostrador2026", out _));
        Assert.False(_servicio.Verificar(usuario, "otra", out _));
    }
}
