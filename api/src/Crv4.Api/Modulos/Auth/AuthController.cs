using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Crv4.Api.Modulos.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthServicio auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesTasa.Login)]
    public Task<SesionRespuesta> Login(LoginSolicitud solicitud, CancellationToken ct) =>
        auth.LoginAsync(solicitud, Ip, Request.Headers.UserAgent.ToString(), ct);

    [HttpPost("renovar")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesTasa.Login)]
    public Task<SesionRespuesta> Renovar(RenovarSolicitud solicitud, CancellationToken ct) =>
        auth.RenovarAsync(solicitud.TokenRenovacion, Ip, ct);

    [HttpPost("salir")]
    [AllowAnonymous]
    public async Task<IActionResult> Salir(RenovarSolicitud solicitud, CancellationToken ct)
    {
        await auth.SalirAsync(solicitud.TokenRenovacion, ct);
        return NoContent();
    }

    [HttpGet("yo")]
    [Authorize]
    [PermitirConCambioPendiente]
    public Task<UsuarioSesion> Yo(CancellationToken ct) => auth.ObtenerUsuarioAsync(User.UsuarioId(), ct);

    [HttpPost("cambiar-password")]
    [Authorize]
    [PermitirConCambioPendiente]
    [EnableRateLimiting(LimitesTasa.Login)]
    public Task<SesionRespuesta> CambiarPassword(CambiarPasswordSolicitud solicitud, CancellationToken ct) =>
        auth.CambiarPasswordAsync(User.UsuarioId(), solicitud, Ip, Request.Headers.UserAgent.ToString(), ct);

    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();
}

public static class LimitesTasa
{
    public const string Login = "login";
}
