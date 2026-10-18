using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Seguridad;

[ApiController]
[Route("api/seguridad")]
public class RolesController(AppDbContext db) : ControllerBase
{
    [HttpGet("permisos")]
    [TienePermiso(Permisos.SeguridadRoles)]
    public async Task<IReadOnlyList<PermisoDto>> ListarPermisos(CancellationToken ct) =>
        await db.Permisos.AsNoTracking().OrderBy(p => p.Modulo).ThenBy(p => p.Codigo)
            .Select(p => new PermisoDto(p.Codigo, p.Modulo, p.Descripcion)).ToListAsync(ct);

    // También lo usa la pantalla de usuarios para elegir el rol.
    [HttpGet("roles")]
    [TienePermiso(Permisos.SeguridadRoles, Permisos.SeguridadUsuarios)]
    public async Task<IReadOnlyList<RolDto>> ListarRoles(CancellationToken ct) =>
        await db.Roles.AsNoTracking().OrderBy(r => r.Nombre)
            .Select(r => new RolDto(r.Id, r.Nombre, r.Descripcion, r.EsSistema, r.Usuarios.Count,
                r.Permisos.Select(p => p.PermisoCodigo).OrderBy(c => c).ToList()))
            .ToListAsync(ct);

    [HttpPost("roles")]
    [TienePermiso(Permisos.SeguridadRoles)]
    public async Task<ActionResult<RolDto>> Crear(RolGuardar datos, CancellationToken ct)
    {
        var rol = new Rol { Nombre = datos.Nombre.Trim(), Descripcion = datos.Descripcion?.Trim() };
        await ValidarAsync(rol.Nombre, null, datos.Permisos, ct);
        rol.Permisos = datos.Permisos.Distinct().Select(c => new RolPermiso { PermisoCodigo = c }).ToList();
        db.Roles.Add(rol);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(ListarRoles), new RolDto(rol.Id, rol.Nombre, rol.Descripcion, false, 0, datos.Permisos.Distinct().Order().ToList()));
    }

    [HttpPut("roles/{id:int}")]
    [TienePermiso(Permisos.SeguridadRoles)]
    public async Task<IActionResult> Actualizar(int id, RolGuardar datos, CancellationToken ct)
    {
        var rol = await db.Roles.Include(r => r.Permisos).FirstOrDefaultAsync(r => r.Id == id, ct)
                  ?? throw new NoEncontrado("El rol no existe.");
        if (rol.EsSistema)
            throw new ErrorNegocio("El rol Administrador no se puede modificar: siempre tiene todos los permisos.");
        await ValidarAsync(datos.Nombre.Trim(), id, datos.Permisos, ct);

        rol.Nombre = datos.Nombre.Trim();
        rol.Descripcion = datos.Descripcion?.Trim();
        rol.Permisos.Clear();
        rol.Permisos.AddRange(datos.Permisos.Distinct().Select(c => new RolPermiso { RolId = id, PermisoCodigo = c }));
        await db.SaveChangesAsync(ct);
        // Los permisos nuevos llegan a cada usuario en su próxima renovación de token (pocos minutos).
        return NoContent();
    }

    [HttpDelete("roles/{id:int}")]
    [TienePermiso(Permisos.SeguridadRoles)]
    public async Task<IActionResult> Borrar(int id, CancellationToken ct)
    {
        var rol = await db.Roles.Include(r => r.Usuarios).FirstOrDefaultAsync(r => r.Id == id, ct)
                  ?? throw new NoEncontrado("El rol no existe.");
        if (rol.EsSistema)
            throw new ErrorNegocio("El rol Administrador no se puede borrar.");
        if (rol.Usuarios.Count > 0)
            throw new ErrorNegocio("El rol tiene usuarios asignados. Cambiales el rol antes de borrarlo.", StatusCodes.Status409Conflict);
        db.Roles.Remove(rol);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task ValidarAsync(string nombre, int? id, IReadOnlyList<string> permisos, CancellationToken ct)
    {
        if (await db.Roles.AnyAsync(r => r.Nombre == nombre && r.Id != id, ct))
            throw new ErrorNegocio("Ya existe un rol con ese nombre.", StatusCodes.Status409Conflict);
        var validos = Permisos.Todos.Select(p => p.Codigo).ToHashSet();
        var desconocidos = permisos.Where(p => !validos.Contains(p)).ToList();
        if (desconocidos.Count > 0)
            throw new ErrorNegocio($"Permisos desconocidos: {string.Join(", ", desconocidos)}.");
    }
}
