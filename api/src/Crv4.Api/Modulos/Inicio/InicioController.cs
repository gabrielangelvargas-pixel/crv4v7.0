using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Inicio;

public record ResumenInicio(int ProductosActivos, int Clientes, int Rubros);

/// <summary>
/// La pantalla de Inicio la ven todos (los que no son administradores ven un reloj),
/// pero los números del negocio solo el rol Administrador.
/// </summary>
[ApiController]
[Route("api/inicio")]
public class InicioController(AppDbContext db) : ControllerBase
{
    [HttpGet("resumen")]
    public async Task<ResumenInicio> Resumen(CancellationToken ct)
    {
        if (!User.HasClaim(Claims.Rol, Rol.NombreAdministrador))
            throw new ErrorNegocio("Solo los administradores ven el resumen.", StatusCodes.Status403Forbidden);
        return new(
            ProductosActivos: await db.Productos.CountAsync(p => p.Activo, ct),
            Clientes: await db.Clientes.CountAsync(ct),
            Rubros: await db.Rubros.CountAsync(r => r.Activo, ct));
    }
}
