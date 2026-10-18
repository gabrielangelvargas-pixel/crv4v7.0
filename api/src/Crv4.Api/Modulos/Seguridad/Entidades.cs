namespace Crv4.Api.Modulos.Seguridad;

public class Usuario
{
    public int Id { get; set; }
    public required string NombreUsuario { get; set; }
    public required string Nombre { get; set; }
    public required string PasswordHash { get; set; }
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public bool DebeCambiarPassword { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime? UltimoIngreso { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}

public class Rol
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }

    /// <summary>Los roles de sistema (Administrador) no se pueden borrar ni quedarse sin permisos.</summary>
    public bool EsSistema { get; set; }

    public List<RolPermiso> Permisos { get; set; } = [];
    public List<Usuario> Usuarios { get; set; } = [];
}

public class Permiso
{
    /// <summary>Código estable, por ejemplo "rubros.editar". Es la clave primaria.</summary>
    public required string Codigo { get; set; }
    public required string Modulo { get; set; }
    public required string Descripcion { get; set; }
}

public class RolPermiso
{
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public required string PermisoCodigo { get; set; }
    public Permiso Permiso { get; set; } = null!;
}

/// <summary>
/// Una sesión abierta. El token de renovación nunca se guarda: solo su hash SHA-256.
/// Cada renovación revoca la fila actual y crea otra con el mismo vencimiento absoluto.
/// </summary>
public class Sesion
{
    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public required string TokenHash { get; set; }
    public DateTime CreadaEn { get; set; }
    public DateTime ExpiraEn { get; set; }
    public DateTime? RevocadaEn { get; set; }
    public long? ReemplazadaPorId { get; set; }
    public string? Ip { get; set; }
    public string? Dispositivo { get; set; }
}
