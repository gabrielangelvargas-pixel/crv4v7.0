using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Inicio;

/// <summary>Los contadores que todavía no tienen módulo vuelven en null y la web los muestra como pendientes.</summary>
public record ResumenInicio(int? ProductosActivos, int? Clientes, int Rubros, int Usuarios);

[ApiController]
[Route("api/inicio")]
public class InicioController(AppDbContext db) : ControllerBase
{
    [HttpGet("resumen")]
    [TienePermiso(Permisos.InicioVer)]
    public async Task<ResumenInicio> Resumen(CancellationToken ct) => new(
        ProductosActivos: null,
        Clientes: null,
        Rubros: await db.Rubros.CountAsync(r => r.Activo, ct),
        Usuarios: await db.Usuarios.CountAsync(u => u.Activo, ct));
}
