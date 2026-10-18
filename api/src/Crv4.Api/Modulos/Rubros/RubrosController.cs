using System.ComponentModel.DataAnnotations;
using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Rubros;

public record RubroDto(int Id, string Nombre, string? Descripcion, int Orden, bool Activo);

public record RubroGuardar(
    [Required(ErrorMessage = "Ingresá el nombre."), MaxLength(100)] string Nombre,
    [MaxLength(500)] string? Descripcion,
    int Orden,
    bool Activo = true);

[ApiController]
[Route("api/rubros")]
public class RubrosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [TienePermiso(Permisos.RubrosVer)]
    public async Task<IReadOnlyList<RubroDto>> Listar([FromQuery] string? buscar, [FromQuery] bool? activo, CancellationToken ct)
    {
        var consulta = db.Rubros.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
            consulta = consulta.Where(r => r.Nombre.Contains(buscar.Trim()));
        if (activo is not null)
            consulta = consulta.Where(r => r.Activo == activo);
        return await consulta.OrderBy(r => r.Orden).ThenBy(r => r.Nombre)
            .Select(r => new RubroDto(r.Id, r.Nombre, r.Descripcion, r.Orden, r.Activo))
            .ToListAsync(ct);
    }

    [HttpGet("{id:int}")]
    [TienePermiso(Permisos.RubrosVer)]
    public async Task<RubroDto> Obtener(int id, CancellationToken ct) =>
        await db.Rubros.AsNoTracking().Where(r => r.Id == id)
            .Select(r => new RubroDto(r.Id, r.Nombre, r.Descripcion, r.Orden, r.Activo))
            .FirstOrDefaultAsync(ct) ?? throw new NoEncontrado("El rubro no existe.");

    [HttpPost]
    [TienePermiso(Permisos.RubrosEditar)]
    public async Task<ActionResult<RubroDto>> Crear(RubroGuardar datos, CancellationToken ct)
    {
        var nombre = datos.Nombre.Trim();
        await ValidarNombreAsync(nombre, null, ct);
        var rubro = new Rubro { Nombre = nombre, Descripcion = Limpio(datos.Descripcion), Orden = datos.Orden, Activo = datos.Activo };
        db.Rubros.Add(rubro);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = rubro.Id },
            new RubroDto(rubro.Id, rubro.Nombre, rubro.Descripcion, rubro.Orden, rubro.Activo));
    }

    [HttpPut("{id:int}")]
    [TienePermiso(Permisos.RubrosEditar)]
    public async Task<IActionResult> Actualizar(int id, RubroGuardar datos, CancellationToken ct)
    {
        var rubro = await db.Rubros.FindAsync([id], ct) ?? throw new NoEncontrado("El rubro no existe.");
        var nombre = datos.Nombre.Trim();
        await ValidarNombreAsync(nombre, id, ct);
        rubro.Nombre = nombre;
        rubro.Descripcion = Limpio(datos.Descripcion);
        rubro.Orden = datos.Orden;
        rubro.Activo = datos.Activo;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // No hay DELETE: un rubro se desactiva. Cuando existan productos, borrarlo dejaría productos huérfanos.

    private async Task ValidarNombreAsync(string nombre, int? id, CancellationToken ct)
    {
        if (await db.Rubros.AnyAsync(r => r.Nombre == nombre && r.Id != id, ct))
            throw new ErrorNegocio("Ya existe un rubro con ese nombre.", StatusCodes.Status409Conflict);
    }

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
