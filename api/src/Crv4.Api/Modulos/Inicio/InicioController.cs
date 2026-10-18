using Crv4.Api.Datos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Inicio;

public record ResumenInicio(int ProductosActivos, int Clientes, int Rubros);

/// <summary>Solo pide sesión iniciada: todos los usuarios ven el inicio.</summary>
[ApiController]
[Route("api/inicio")]
public class InicioController(AppDbContext db) : ControllerBase
{
    [HttpGet("resumen")]
    public async Task<ResumenInicio> Resumen(CancellationToken ct) => new(
        ProductosActivos: await db.Productos.CountAsync(p => p.Activo, ct),
        Clientes: await db.Clientes.CountAsync(ct),
        Rubros: await db.Rubros.CountAsync(r => r.Activo, ct));
}
