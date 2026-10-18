using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Seguridad;

[ApiController]
[Route("api/seguridad/usuarios")]
[TienePermiso(Permisos.UsuariosAdministrar)]
public class UsuariosController(AppDbContext db, PasswordServicio passwords, AuthServicio auth, TimeProvider reloj) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<UsuarioDto>> Listar(CancellationToken ct)
    {
        var ahora = reloj.GetLocalNow().DateTime;
        return await db.Usuarios.AsNoTracking().OrderBy(u => u.Nombre)
            .Select(u => new UsuarioDto(u.Id, u.NombreUsuario, u.Nombre, u.Email,
                u.Roles.OrderBy(r => r.Rol.Nombre).Select(r => new RolResumen(r.RolId, r.Rol.Nombre)).ToList(),
                u.Activo, u.DebeCambiarPassword, u.BloqueadoHasta > ahora, u.UltimoIngreso))
            .ToListAsync(ct);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(UsuarioCrear datos, CancellationToken ct)
    {
        var nombreUsuario = datos.NombreUsuario.Trim();
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario, ct))
            throw new ErrorNegocio("Ya existe un usuario con ese nombre.", StatusCodes.Status409Conflict);
        var email = Limpio(datos.Email);
        await ValidarEmailAsync(email, null, ct);
        var roles = await RolesValidosAsync(datos.RolIds, ct);
        if (passwords.Validar(datos.PasswordInicial, nombreUsuario) is { } motivo)
            throw new ErrorNegocio(motivo);

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            Nombre = datos.Nombre.Trim(),
            Email = email,
            PasswordHash = "",
            DebeCambiarPassword = true,
            Roles = roles.Select(r => new UsuarioRol { RolId = r.Id }).ToList(),
        };
        usuario.PasswordHash = passwords.Hashear(usuario, datos.PasswordInicial);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Listar), new UsuarioDto(usuario.Id, usuario.NombreUsuario, usuario.Nombre, usuario.Email,
            roles.Select(r => new RolResumen(r.Id, r.Nombre)).ToList(), true, true, false, null));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, UsuarioActualizar datos, CancellationToken ct)
    {
        var usuario = await db.Usuarios.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id, ct)
                      ?? throw new NoEncontrado("El usuario no existe.");
        var roles = await RolesValidosAsync(datos.RolIds, ct);
        var email = Limpio(datos.Email);
        await ValidarEmailAsync(email, id, ct);

        var rolesAntes = usuario.Roles.Select(r => r.RolId).Order().ToList();
        var rolesDespues = roles.Select(r => r.Id).Order().ToList();
        var cambioAcceso = !rolesAntes.SequenceEqual(rolesDespues) || usuario.Activo != datos.Activo;

        if (id == User.UsuarioId() && cambioAcceso)
            throw new ErrorNegocio("No podés desactivarte ni cambiarte los roles a vos mismo.");
        var adminId = await db.Roles.Where(r => r.Nombre == Rol.NombreAdministrador).Select(r => (int?)r.Id).FirstOrDefaultAsync(ct);
        var eraAdmin = adminId is not null && rolesAntes.Contains(adminId.Value) && usuario.Activo;
        var siguesAdmin = adminId is not null && rolesDespues.Contains(adminId.Value) && datos.Activo;
        if (eraAdmin && !siguesAdmin)
            await ExigirOtroAdministradorActivoAsync(id, adminId!.Value, ct);

        usuario.Nombre = datos.Nombre.Trim();
        usuario.Email = email;
        usuario.Activo = datos.Activo;
        usuario.Roles.RemoveAll(r => !rolesDespues.Contains(r.RolId));
        usuario.Roles.AddRange(rolesDespues.Except(rolesAntes).Select(rolId => new UsuarioRol { UsuarioId = id, RolId = rolId }));
        await db.SaveChangesAsync(ct);

        // Si cambiaron los roles o se desactivó, las sesiones abiertas se cierran para que tome efecto ya.
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

    private async Task<List<Rol>> RolesValidosAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        var distintos = ids.Distinct().ToList();
        var roles = await db.Roles.Where(r => distintos.Contains(r.Id)).ToListAsync(ct);
        if (roles.Count != distintos.Count)
            throw new ErrorNegocio("Alguno de los roles no existe.");
        return roles;
    }

    private async Task ValidarEmailAsync(string? email, int? id, CancellationToken ct)
    {
        if (email is not null && await db.Usuarios.AnyAsync(u => u.Email == email && u.Id != id, ct))
            throw new ErrorNegocio("Ese email ya lo usa otro usuario.", StatusCodes.Status409Conflict);
    }

    private async Task ExigirOtroAdministradorActivoAsync(int excluirId, int adminId, CancellationToken ct)
    {
        if (!await db.UsuariosRoles.AnyAsync(ur => ur.UsuarioId != excluirId && ur.RolId == adminId && ur.Usuario.Activo, ct))
            throw new ErrorNegocio("Tiene que quedar al menos un administrador activo.");
    }

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
