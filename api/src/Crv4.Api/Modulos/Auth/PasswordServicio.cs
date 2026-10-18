using Crv4.Api.Modulos.Seguridad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Crv4.Api.Modulos.Auth;

/// <summary>Hash de contraseñas con PBKDF2 (el mismo algoritmo de ASP.NET Core Identity).</summary>
public class PasswordServicio(IOptions<SeguridadOpciones> opciones)
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hashear(Usuario usuario, string password) => _hasher.HashPassword(usuario, password);

    public bool Verificar(Usuario usuario, string password, out bool requiereRehash)
    {
        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
        requiereRehash = resultado == PasswordVerificationResult.SuccessRehashNeeded;
        return resultado != PasswordVerificationResult.Failed;
    }

    /// <summary>Devuelve el motivo por el que la contraseña no sirve, o null si es válida.</summary>
    public string? Validar(string password, string? nombreUsuario = null)
    {
        var minimo = opciones.Value.LargoMinimoPassword;
        if (string.IsNullOrWhiteSpace(password) || password.Length < minimo)
            return $"La contraseña debe tener al menos {minimo} caracteres.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "La contraseña debe combinar letras y números.";
        if (nombreUsuario is not null && password.Contains(nombreUsuario, StringComparison.OrdinalIgnoreCase))
            return "La contraseña no puede contener el nombre de usuario.";
        return null;
    }
}
