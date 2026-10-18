using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Seguridad;

[ApiController]
[Route("api/seguridad/usuarios")]
[TienePermiso(Permisos.SeguridadUsuarios)]
public class UsuariosController(AppDbContext db, PasswordServicio passwords, AuthServicio auth, TimeProvider reloj) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<UsuarioDto>> Listar(CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        return await db.Usuarios.AsNoTracking().OrderBy(u => u.Nombre)
            .Select(u => new UsuarioDto(u.Id, u.NombreUsuario, u.Nombre, u.RolId, u.Rol.Nombre, u.Activo,
                u.DebeCambiarPassword, u.BloqueadoHasta > ahora, u.UltimoIngreso))
            .ToListAsync(ct);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(UsuarioCrear datos, CancellationToken ct)
    {
        var nombreUsuario = datos.NombreUsuario.Trim();
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario, ct))
            throw new ErrorNegocio("Ya existe un usuario con ese nombre.", StatusCodes.Status409Conflict);
        var rol = await db.Roles.FindAsync([datos.RolId], ct) ?? throw new ErrorNegocio("El rol no existe.");
        if (passwords.Validar(datos.PasswordInicial, nombreUsuario) is { } motivo)
            throw new ErrorNegocio(motivo);

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            Nombre = datos.Nombre.Trim(),
            RolId = rol.Id,
            PasswordHash = "",
            DebeCambiarPassword = true,
        };
        usuario.PasswordHash = passwords.Hashear(usuario, datos.PasswordInicial);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Listar), new UsuarioDto(usuario.Id, usuario.NombreUsuario, usuario.Nombre,
            rol.Id, rol.Nombre, true, true, false, null));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, UsuarioActualizar datos, CancellationToken ct)
    {
        var usuario = await db.Usuarios.FindAsync([id], ct) ?? throw new NoEncontrado("El usuario no existe.");
        if (!await db.Roles.AnyAsync(r => r.Id == datos.RolId, ct))
            throw new ErrorNegocio("El rol no existe.");
        if (id == User.UsuarioId() && (!datos.Activo || datos.RolId != usuario.RolId))
            throw new ErrorNegocio("No podés desactivarte ni cambiarte el rol a vos mismo.");
        if (usuario.RolId == RolesSistema.AdministradorId && (datos.RolId != usuario.RolId || !datos.Activo))
            await ExigirOtroAdministradorActivoAsync(id, ct);

        var cambioAcceso = usuario.RolId != datos.RolId || usuario.Activo != datos.Activo;
        usuario.Nombre = datos.Nombre.Trim();
        usuario.RolId = datos.RolId;
        usuario.Activo = datos.Activo;
        await db.SaveChangesAsync(ct);

        // Si cambió el rol o se desactivó, las sesiones abiertas se cierran para que tome efecto ya.
        if (cambioAcceso)
            await auth.RevocarTodasAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/reiniciar-password")]
    public async Task<IActionResult> ReiniciarPassword(int id, PasswordReinicio datos, CancellationToken ct)
    {
        var usuario = await db.Usuarios.FindAsync([id], ct) ?? throw new NoEncontrado("El usuario no existe.");
        if (passwords.Validar(datos.PasswordTemporal, usuario.NombreUsuario) is { } motivo)
            throw new ErrorNegocio(motivo);

        usuario.PasswordHash = passwords.Hashear(usuario, datos.PasswordTemporal);
        usuario.DebeCambiarPassword = true;
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        await db.SaveChangesAsync(ct);
        await auth.RevocarTodasAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/desbloquear")]
    public async Task<IActionResult> Desbloquear(int id, CancellationToken ct)
    {
        var filas = await db.Usuarios.Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IntentosFallidos, 0).SetProperty(u => u.BloqueadoHasta, (DateTime?)null), ct);
        return filas == 0 ? throw new NoEncontrado("El usuario no existe.") : NoContent();
    }

    private async Task ExigirOtroAdministradorActivoAsync(int excluirId, CancellationToken ct)
    {
        if (!await db.Usuarios.AnyAsync(u => u.Id != excluirId && u.Activo && u.RolId == RolesSistema.AdministradorId, ct))
            throw new ErrorNegocio("Tiene que quedar al menos un administrador activo.");
    }
}
