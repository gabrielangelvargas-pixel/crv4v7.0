using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Seguridad;

[ApiController]
[Route("api/seguridad")]
[TienePermiso(Permisos.UsuariosAdministrar)]
public class RolesController(AppDbContext db) : ControllerBase
{
    [HttpGet("permisos")]
    public async Task<IReadOnlyList<PermisoDto>> ListarPermisos(CancellationToken ct)
    {
        var permisos = await db.Permisos.AsNoTracking().Where(p => p.Activo).ToListAsync(ct);
        return permisos.Select(p => new PermisoDto(p.Codigo, Permisos.ModuloDe(p.Codigo), p.Descripcion))
            .OrderBy(p => p.Modulo).ThenBy(p => p.Codigo).ToList();
    }

    [HttpGet("roles")]
    public async Task<IReadOnlyList<RolDto>> ListarRoles(CancellationToken ct) =>
        await db.Roles.AsNoTracking().OrderBy(r => r.Nombre)
            .Select(r => new RolDto(r.Id, r.Nombre, r.Descripcion, r.Activo, r.Nombre == Rol.NombreAdministrador, r.Usuarios.Count,
                r.Permisos.Select(p => p.Permiso.Codigo).OrderBy(c => c).ToList()))
            .ToListAsync(ct);

    [HttpPost("roles")]
    public async Task<ActionResult<RolDto>> Crear(RolGuardar datos, CancellationToken ct)
    {
        var nombre = datos.Nombre.Trim();
        await ValidarNombreAsync(nombre, null, ct);
        var permisos = await PermisosValidosAsync(datos.Permisos, ct);
        var rol = new Rol
        {
            Nombre = nombre,
            Descripcion = Limpio(datos.Descripcion),
            Activo = datos.Activo,
            Permisos = permisos.Select(p => new RolPermiso { PermisoId = p.Id }).ToList(),
        };
        db.Roles.Add(rol);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(ListarRoles), new RolDto(rol.Id, rol.Nombre, rol.Descripcion, rol.Activo, false, 0,
            permisos.Select(p => p.Codigo).Order().ToList()));
    }

    [HttpPut("roles/{id:int}")]
    public async Task<IActionResult> Actualizar(int id, RolGuardar datos, CancellationToken ct)
    {
        var rol = await db.Roles.Include(r => r.Permisos).FirstOrDefaultAsync(r => r.Id == id, ct)
                  ?? throw new NoEncontrado("El rol no existe.");
        if (rol.EsAdministrador)
            throw new ErrorNegocio("El rol Administrador no se puede modificar: siempre tiene todos los permisos.");
        var nombre = datos.Nombre.Trim();
        if (nombre == Rol.NombreAdministrador)
            throw new ErrorNegocio("Ese nombre está reservado.");
        await ValidarNombreAsync(nombre, id, ct);
        var permisos = await PermisosValidosAsync(datos.Permisos, ct);

        rol.Nombre = nombre;
        rol.Descripcion = Limpio(datos.Descripcion);
        rol.Activo = datos.Activo;
        var nuevos = permisos.Select(p => p.Id).ToHashSet();
        rol.Permisos.RemoveAll(rp => !nuevos.Contains(rp.PermisoId));
        rol.Permisos.AddRange(nuevos.Except(rol.Permisos.Select(rp => rp.PermisoId)).Select(pid => new RolPermiso { RolId = id, PermisoId = pid }));
        await db.SaveChangesAsync(ct);
        // Los permisos nuevos llegan a cada usuario en su próxima renovación de token (pocos minutos).
        return NoContent();
    }

    [HttpDelete("roles/{id:int}")]
    public async Task<IActionResult> Borrar(int id, CancellationToken ct)
    {
        var rol = await db.Roles.Include(r => r.Usuarios).FirstOrDefaultAsync(r => r.Id == id, ct)
                  ?? throw new NoEncontrado("El rol no existe.");
        if (rol.EsAdministrador)
            throw new ErrorNegocio("El rol Administrador no se puede borrar.");
        if (rol.Usuarios.Count > 0)
            throw new ErrorNegocio("El rol tiene usuarios asignados. Quitáselo a esos usuarios o desactivá el rol.", StatusCodes.Status409Conflict);
        db.Roles.Remove(rol);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task ValidarNombreAsync(string nombre, int? id, CancellationToken ct)
    {
        if (await db.Roles.AnyAsync(r => r.Nombre == nombre && r.Id != id, ct))
            throw new ErrorNegocio("Ya existe un rol con ese nombre.", StatusCodes.Status409Conflict);
    }

    private async Task<List<Permiso>> PermisosValidosAsync(IReadOnlyList<string> codigos, CancellationToken ct)
    {
        var distintos = codigos.Distinct().ToList();
        var permisos = await db.Permisos.Where(p => distintos.Contains(p.Codigo) && p.Activo).ToListAsync(ct);
        var desconocidos = distintos.Except(permisos.Select(p => p.Codigo)).ToList();
        if (desconocidos.Count > 0)
            throw new ErrorNegocio($"Permisos desconocidos: {string.Join(", ", desconocidos)}.");
        return permisos;
    }

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
