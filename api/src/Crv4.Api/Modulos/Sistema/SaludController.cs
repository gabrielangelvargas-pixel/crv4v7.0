using System.Reflection;
using Crv4.Api.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Sistema;

public record EstadoSalud(string Estado, string Version, bool BaseDatos, int MigracionesPendientes, DateTime Hora);

/// <summary>
/// Sin login: la web lo usa para saber si llega al servidor (y si el certificado HTTPS es de confianza).
/// No expone datos sensibles.
/// </summary>
[ApiController]
[Route("api/salud")]
[AllowAnonymous]
public class SaludController(AppDbContext db) : ControllerBase
{
    public static readonly string Version =
        typeof(SaludController).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";

    [HttpGet]
    public async Task<EstadoSalud> Get(CancellationToken ct)
    {
        try
        {
            var pendientes = (await db.Database.GetPendingMigrationsAsync(ct)).Count();
            return new EstadoSalud(pendientes == 0 ? "ok" : "migraciones-pendientes", Version, true, pendientes, DateTime.UtcNow);
        }
        catch (Exception)
        {
            return new EstadoSalud("sin-base-datos", Version, false, 0, DateTime.UtcNow);
        }
    }
}
