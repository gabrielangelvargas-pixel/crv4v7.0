using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Crv4.Api.Modulos.Auth;

public static class Claims
{
    public const string UsuarioId = "sub";
    public const string Nombre = "name";
    public const string Rol = "rol";
    public const string Permiso = "perm";
    public const string DebeCambiarPassword = "dcp";
    public const string SesionId = "sid";
}

public record TokenAcceso(string Token, DateTime ExpiraEn);

public class TokenServicio(IOptions<JwtOpciones> opciones, TimeProvider reloj)
{
    private readonly JwtOpciones _opciones = opciones.Value;

    public TokenAcceso CrearTokenAcceso(Usuario usuario, IEnumerable<string> roles, IEnumerable<string> permisos, long sesionId)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var expira = ahora.AddMinutes(_opciones.MinutosAcceso);

        var claims = new List<Claim>
        {
            new(Claims.UsuarioId, usuario.Id.ToString()),
            new(Claims.Nombre, usuario.Nombre),
            new(Claims.SesionId, sesionId.ToString()),
        };
        if (usuario.DebeCambiarPassword)
            claims.Add(new Claim(Claims.DebeCambiarPassword, "1"));
        claims.AddRange(roles.Select(r => new Claim(Claims.Rol, r)));
        claims.AddRange(permisos.Select(p => new Claim(Claims.Permiso, p)));

        var credenciales = new SigningCredentials(ClaveFirma(_opciones), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_opciones.Emisor, _opciones.Audiencia, claims, ahora, expira, credenciales);
        return new TokenAcceso(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    public static SymmetricSecurityKey ClaveFirma(JwtOpciones opciones) => new(Encoding.UTF8.GetBytes(opciones.Clave));

    /// <summary>Token de renovación opaco (256 bits aleatorios). Solo se guarda su hash.</summary>
    public static string CrearTokenRenovacion() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string tokenRenovacion) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tokenRenovacion)));
}
