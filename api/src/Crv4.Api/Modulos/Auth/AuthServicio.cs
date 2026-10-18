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

    // Hora local del servidor, como el resto de las fechas de crv4_datos.
    private DateTime Ahora => reloj.GetLocalNow().DateTime;

    public async Task<SesionRespuesta> LoginAsync(LoginSolicitud solicitud, string? ip, string? dispositivo, CancellationToken ct)
    {
        var ahora = Ahora;
        var nombre = solicitud.Usuario.Trim();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombre, ct);

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
        var ahora = Ahora;
        var hash = TokenServicio.Hash(tokenRenovacion);
        var sesion = await db.Sesiones.Include(s => s.Usuario).FirstOrDefaultAsync(s => s.TokenHash == hash, ct)
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
        var ahora = Ahora;
        await db.Sesiones.Where(s => s.TokenHash == hash && s.RevocadaEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, ahora), ct);
    }

    public async Task<SesionRespuesta> CambiarPasswordAsync(int usuarioId, CambiarPasswordSolicitud solicitud, string? ip, string? dispositivo, CancellationToken ct)
    {
        var usuario = await db.Usuarios.FirstAsync(u => u.Id == usuarioId, ct);
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
        var usuario = await db.Usuarios.FirstAsync(u => u.Id == usuarioId, ct);
        var (roles, permisos) = await AccesosAsync(usuario.Id, ct);
        return new UsuarioSesion(usuario.Id, usuario.NombreUsuario, usuario.Nombre, roles, permisos, usuario.DebeCambiarPassword);
    }

    public Task RevocarTodasAsync(int usuarioId, CancellationToken ct)
    {
        var ahora = Ahora;
        return db.Sesiones.Where(s => s.UsuarioId == usuarioId && s.RevocadaEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, ahora), ct);
    }

    /// <summary>Roles activos del usuario y la unión de sus permisos. El Administrador tiene todos.</summary>
    public async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permisos)> AccesosAsync(int usuarioId, CancellationToken ct)
    {
        var roles = await db.UsuariosRoles.Where(ur => ur.UsuarioId == usuarioId && ur.Rol.Activo)
            .Select(ur => ur.Rol.Nombre).OrderBy(n => n).ToListAsync(ct);

        var permisos = roles.Contains(Rol.NombreAdministrador)
            ? await db.Permisos.Where(p => p.Activo).Select(p => p.Codigo).ToListAsync(ct)
            : await db.UsuariosRoles.Where(ur => ur.UsuarioId == usuarioId && ur.Rol.Activo)
                .SelectMany(ur => ur.Rol.Permisos).Where(rp => rp.Permiso.Activo)
                .Select(rp => rp.Permiso.Codigo).Distinct().ToListAsync(ct);
        permisos.Sort(StringComparer.Ordinal);
        return (roles, permisos);
    }

    private async Task<SesionRespuesta> ArmarRespuestaAsync(Usuario usuario, Sesion sesion, string tokenRenovacion, CancellationToken ct)
    {
        var (roles, permisos) = await AccesosAsync(usuario.Id, ct);
        var acceso = tokens.CrearTokenAcceso(usuario, roles, permisos, sesion.Id);
        var datos = new UsuarioSesion(usuario.Id, usuario.NombreUsuario, usuario.Nombre, roles, permisos, usuario.DebeCambiarPassword);
        return new SesionRespuesta(acceso.Token, acceso.ExpiraEn, tokenRenovacion, sesion.ExpiraEn, datos);
    }

    private static ErrorNegocio SesionVencida() =>
        new("La sesión venció. Ingresá de nuevo.", StatusCodes.Status401Unauthorized);

    private static string? Recortar(string? texto, int largo) =>
        texto is null || texto.Length <= largo ? texto : texto[..largo];
}
