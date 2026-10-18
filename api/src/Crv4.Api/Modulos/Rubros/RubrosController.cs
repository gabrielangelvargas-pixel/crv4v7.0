using System.ComponentModel.DataAnnotations;
using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Rubros;

public record RubroDto(int Id, string Codigo, string Nombre, string? Descripcion, int? IdPadre, string? NombrePadre,
    int Orden, bool Activo, int Productos);

public record RubroGuardar(
    [Required(ErrorMessage = "Ingresá el código."), MaxLength(6, ErrorMessage = "El código tiene hasta 6 caracteres.")] string Codigo,
    [Required(ErrorMessage = "Ingresá el nombre."), MaxLength(45, ErrorMessage = "El nombre tiene hasta 45 caracteres.")] string Nombre,
    [MaxLength(255)] string? Descripcion,
    int? IdPadre,
    int Orden,
    bool Activo = true);

[ApiController]
[Route("api/rubros")]
public class RubrosController(AppDbContext db, TimeProvider reloj) : ControllerBase
{
    [HttpGet]
    [TienePermiso(Permisos.RubrosVer)]
    public async Task<IReadOnlyList<RubroDto>> Listar([FromQuery] string? buscar, [FromQuery] bool? activo, CancellationToken ct)
    {
        var consulta = db.Rubros.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(r => r.Nombre.Contains(texto) || r.Codigo.Contains(texto));
        }
        if (activo is not null)
            consulta = consulta.Where(r => r.Activo == activo);
        return await Proyectar(consulta.OrderBy(r => r.Orden).ThenBy(r => r.Nombre)).ToListAsync(ct);
    }

    [HttpGet("{id:int}")]
    [TienePermiso(Permisos.RubrosVer)]
    public async Task<RubroDto> Obtener(int id, CancellationToken ct) =>
        await Proyectar(db.Rubros.AsNoTracking().Where(r => r.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new NoEncontrado("El rubro no existe.");

    [HttpPost]
    [TienePermiso(Permisos.RubrosCrear)]
    public async Task<ActionResult<RubroDto>> Crear(RubroGuardar datos, CancellationToken ct)
    {
        var codigo = datos.Codigo.Trim().ToUpperInvariant();
        var nombre = datos.Nombre.Trim();
        await ValidarAsync(codigo, nombre, datos.IdPadre, null, ct);
        var rubro = new Rubro
        {
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = Limpio(datos.Descripcion),
            IdPadre = datos.IdPadre,
            Orden = datos.Orden,
            Activo = datos.Activo,
            FechaModificacion = Ahora,
        };
        db.Rubros.Add(rubro);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = rubro.Id }, await Obtener(rubro.Id, ct));
    }

    [HttpPut("{id:int}")]
    [TienePermiso(Permisos.RubrosEditar)]
    public async Task<IActionResult> Actualizar(int id, RubroGuardar datos, CancellationToken ct)
    {
        var rubro = await db.Rubros.FindAsync([id], ct) ?? throw new NoEncontrado("El rubro no existe.");
        var codigo = datos.Codigo.Trim().ToUpperInvariant();
        var nombre = datos.Nombre.Trim();
        await ValidarAsync(codigo, nombre, datos.IdPadre, id, ct);
        // Los productos guardan el código del rubro: cambiarlo los dejaría apuntando a un rubro que no existe.
        if (codigo != rubro.Codigo && await db.Productos.AnyAsync(p => p.IdRubro == rubro.Codigo, ct))
            throw new ErrorNegocio("El rubro tiene productos: no se puede cambiar el código.", StatusCodes.Status409Conflict);
        rubro.Codigo = codigo;
        rubro.Nombre = nombre;
        rubro.Descripcion = Limpio(datos.Descripcion);
        rubro.IdPadre = datos.IdPadre;
        rubro.Orden = datos.Orden;
        rubro.Activo = datos.Activo;
        rubro.FechaModificacion = Ahora;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [TienePermiso(Permisos.RubrosEliminar)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var rubro = await db.Rubros.FindAsync([id], ct) ?? throw new NoEncontrado("El rubro no existe.");
        if (await db.Rubros.AnyAsync(r => r.IdPadre == id, ct))
            throw new ErrorNegocio("El rubro tiene subrubros. Movelos o eliminalos primero.", StatusCodes.Status409Conflict);
        if (await db.Productos.AnyAsync(p => p.IdRubro == rubro.Codigo, ct))
            throw new ErrorNegocio("El rubro tiene productos. Desactivalo en lugar de eliminarlo.", StatusCodes.Status409Conflict);
        db.Rubros.Remove(rubro);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private DateTime Ahora => reloj.GetLocalNow().DateTime;

    private IQueryable<RubroDto> Proyectar(IQueryable<Rubro> consulta) =>
        consulta.Select(r => new RubroDto(r.Id, r.Codigo, r.Nombre, r.Descripcion, r.IdPadre,
            r.Padre == null ? null : r.Padre.Nombre, r.Orden, r.Activo,
            db.Productos.Count(p => p.IdRubro == r.Codigo)));

    private async Task ValidarAsync(string codigo, string nombre, int? idPadre, int? id, CancellationToken ct)
    {
        if (await db.Rubros.AnyAsync(r => r.Codigo == codigo && r.Id != id, ct))
            throw new ErrorNegocio("Ya existe un rubro con ese código.", StatusCodes.Status409Conflict);
        if (await db.Rubros.AnyAsync(r => r.Nombre == nombre && r.Id != id, ct))
            throw new ErrorNegocio("Ya existe un rubro con ese nombre.", StatusCodes.Status409Conflict);
        if (idPadre is null) return;
        if (idPadre == id)
            throw new ErrorNegocio("Un rubro no puede ser su propio padre.");
        // Recorre los ancestros del padre elegido para no armar un ciclo.
        var actual = idPadre;
        var vistos = new HashSet<int>();
        while (actual is { } a && vistos.Add(a))
        {
            var padre = await db.Rubros.AsNoTracking().Where(r => r.Id == a).Select(r => new { r.IdPadre }).FirstOrDefaultAsync(ct)
                ?? throw new ErrorNegocio("El rubro padre no existe.");
            if (padre.IdPadre == id && id is not null)
                throw new ErrorNegocio("Ese padre está dentro de este rubro: se formaría un ciclo.");
            actual = padre.IdPadre;
        }
    }

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
