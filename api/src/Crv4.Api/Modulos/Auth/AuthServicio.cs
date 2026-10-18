using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crv4.Api.Modulos.Auth;

public class AuthServicio(
    AppDbContext db,
    TokenServicio tokens,
    PasswordServicio passwords,
    IOptions<JwtOpciones> jwt,
    IOptions<SeguridadOpciones> seguridad,
    TimeProvider reloj,
    ILogger<AuthServicio> log)
{
    private const string CredencialesInvalidas = "Usuario o contraseña incorrectos.";

    public async Task<SesionRespuesta> LoginAsync(LoginSolicitud solicitud, string? ip, string? dispositivo, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var nombre = solicitud.Usuario.Trim();
        var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.NombreUsuario == nombre, ct);

        if (usuario is null)
        {
            log.LogWarning("Login fallido para usuario inexistente {Usuario} desde {Ip}", nombre, ip);
            throw new ErrorNegocio(CredencialesInvalidas, StatusCodes.Status401Unauthorized);
        }
        if (usuario.BloqueadoHasta > ahora)
            throw new ErrorNegocio("El usuario está bloqueado por varios intentos fallidos. Probá de nuevo más tarde.", StatusCodes.Status423Locked);

        if (!passwords.Verificar(usuario, solicitud.Password, out var requiereRehash))
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= seguridad.Value.IntentosFallidosMaximos)
            {
                usuario.BloqueadoHasta = ahora.AddMinutes(seguridad.Value.MinutosBloqueo);
                usuario.IntentosFallidos = 0;
                log.LogWarning("Usuario {Usuario} bloqueado por intentos fallidos desde {Ip}", nombre, ip);
            }
            await db.SaveChangesAsync(ct);
            throw new ErrorNegocio(CredencialesInvalidas, StatusCodes.Status401Unauthorized);
        }
        if (!usuario.Activo)
            throw new ErrorNegocio("El usuario está desactivado.", StatusCodes.Status403Forbidden);

        if (requiereRehash)
            usuario.PasswordHash = passwords.Hashear(usuario, solicitud.Password);
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoIngreso = ahora;

        var tokenRenovacion = TokenServicio.CrearTokenRenovacion();
        var sesion = new Sesion
        {
            UsuarioId = usuario.Id,
            TokenHash = TokenServicio.Hash(tokenRenovacion),
            CreadaEn = ahora,
            ExpiraEn = ahora.AddHours(jwt.Value.HorasSesion),
            Ip = ip,
            Dispositivo = Recortar(dispositivo, 250),
        };
        db.Sesiones.Add(sesion);
        await db.SaveChangesAsync(ct);

        return await ArmarRespuestaAsync(usuario, sesion, tokenRenovacion, ct);
    }

    public async Task<SesionRespuesta> RenovarAsync(string tokenRenovacion, string? ip, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var hash = TokenServicio.Hash(tokenRenovacion);
        var sesion = await db.Sesiones.Include(s => s.Usuario).ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(s => s.TokenHash == hash, ct)
            ?? throw SesionVencida();

        if (sesion.RevocadaEn is not null)
        {
            // Un token ya usado volvió a aparecer: puede ser una copia robada. Se cierran todas las sesiones del usuario.
            log.LogWarning("Reuso de token de renovación del usuario {UsuarioId} desde {Ip}. Se revocan sus sesiones.", sesion.UsuarioId, ip);
            await RevocarTodasAsync(sesion.UsuarioId, ct);
            throw SesionVencida();
        }
        if (sesion.ExpiraEn <= ahora || !sesion.Usuario.Activo)
        {
            sesion.RevocadaEn = ahora;
            await db.SaveChangesAsync(ct);
            throw SesionVencida();
        }

        var nuevoToken = TokenServicio.CrearTokenRenovacion();
        var nueva = new Sesion
        {
            UsuarioId = sesion.UsuarioId,
            TokenHash = TokenServicio.Hash(nuevoToken),
            CreadaEn = ahora,
            ExpiraEn = sesion.ExpiraEn, // el vencimiento absoluto no se estira
            Ip = ip,
            Dispositivo = sesion.Dispositivo,
        };
        db.Sesiones.Add(nueva);
        sesion.RevocadaEn = ahora;
        await db.SaveChangesAsync(ct);
        sesion.ReemplazadaPorId = nueva.Id;
        await db.SaveChangesAsync(ct);

        return await ArmarRespuestaAsync(sesion.Usuario, nueva, nuevoToken, ct);
    }

    public async Task SalirAsync(string tokenRenovacion, CancellationToken ct)
    {
        var hash = TokenServicio.Hash(tokenRenovacion);
        await db.Sesiones.Where(s => s.TokenHash == hash && s.RevocadaEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, reloj.GetUtcNow().UtcDateTime), ct);
    }

    public async Task<SesionRespuesta> CambiarPasswordAsync(int usuarioId, CambiarPasswordSolicitud solicitud, string? ip, string? dispositivo, CancellationToken ct)
    {
        var usuario = await db.Usuarios.Include(u => u.Rol).FirstAsync(u => u.Id == usuarioId, ct);
        if (!passwords.Verificar(usuario, solicitud.PasswordActual, out _))
            throw new ErrorNegocio("La contraseña actual no es correcta.");
        if (solicitud.PasswordActual == solicitud.PasswordNueva)
            throw new ErrorNegocio("La contraseña nueva tiene que ser distinta de la actual.");
        if (passwords.Validar(solicitud.PasswordNueva, usuario.NombreUsuario) is { } motivo)
            throw new ErrorNegocio(motivo);

        usuario.PasswordHash = passwords.Hashear(usuario, solicitud.PasswordNueva);
        usuario.DebeCambiarPassword = false;
        await db.SaveChangesAsync(ct);

        // Se cierran todas las sesiones (incluida la actual) y se abre una nueva.
        await RevocarTodasAsync(usuario.Id, ct);
        return await LoginAsync(new LoginSolicitud(usuario.NombreUsuario, solicitud.PasswordNueva), ip, dispositivo, ct);
    }

    public async Task<UsuarioSesion> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct)
    {
        var usuario = await db.Usuarios.Include(u => u.Rol).FirstAsync(u => u.Id == usuarioId, ct);
        return new UsuarioSesion(usuario.Id, usuario.NombreUsuario, usuario.Nombre, usuario.Rol.Nombre,
            await PermisosDeRolAsync(usuario.RolId, ct), usuario.DebeCambiarPassword);
    }

    public Task RevocarTodasAsync(int usuarioId, CancellationToken ct) =>
        db.Sesiones.Where(s => s.UsuarioId == usuarioId && s.RevocadaEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, reloj.GetUtcNow().UtcDateTime), ct);

    private async Task<SesionRespuesta> ArmarRespuestaAsync(Usuario usuario, Sesion sesion, string tokenRenovacion, CancellationToken ct)
    {
        var permisos = await PermisosDeRolAsync(usuario.RolId, ct);
        var acceso = tokens.CrearTokenAcceso(usuario, permisos, sesion.Id);
        var datos = new UsuarioSesion(usuario.Id, usuario.NombreUsuario, usuario.Nombre, usuario.Rol.Nombre, permisos, usuario.DebeCambiarPassword);
        return new SesionRespuesta(acceso.Token, acceso.ExpiraEn, tokenRenovacion, sesion.ExpiraEn, datos);
    }

    private async Task<IReadOnlyList<string>> PermisosDeRolAsync(int rolId, CancellationToken ct) =>
        await db.RolPermisos.Where(rp => rp.RolId == rolId).Select(rp => rp.PermisoCodigo).OrderBy(c => c).ToListAsync(ct);

    private static ErrorNegocio SesionVencida() =>
        new("La sesión venció. Ingresá de nuevo.", StatusCodes.Status401Unauthorized);

    private static string? Recortar(string? texto, int largo) =>
        texto is null || texto.Length <= largo ? texto : texto[..largo];
}
