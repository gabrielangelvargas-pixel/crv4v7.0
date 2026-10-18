namespace Crv4.Api.Modulos.Seguridad;

// Tablas existentes de crv4_datos (compartidas con Sistema03 mientras convivan).

/// <summary>Tabla usuarios_sistema.</summary>
public class Usuario
{
    public int Id { get; set; }
    public required string NombreUsuario { get; set; }
    public required string PasswordHash { get; set; }
    public required string Nombre { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
    public bool DebeCambiarPassword { get; set; }
    public DateTime? UltimoIngreso { get; set; }
    public DateTime FechaAlta { get; set; }

    // Columnas agregadas por la 7.0 (con valor por defecto, Sistema03 no las necesita).
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }

    public List<UsuarioRol> Roles { get; set; } = [];
}

/// <summary>Tabla usuarios_roles: un usuario puede tener varios roles.</summary>
public class UsuarioRol
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
}

/// <summary>Tabla roles.</summary>
public class Rol
{
    public const string NombreAdministrador = "Administrador";

    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }

    /// <summary>El rol Administrador siempre tiene todos los permisos y no se edita ni se borra.</summary>
    public bool EsAdministrador => Nombre == NombreAdministrador;

    public List<RolPermiso> Permisos { get; set; } = [];
    public List<UsuarioRol> Usuarios { get; set; } = [];
}

/// <summary>Tabla permisos.</summary>
public class Permiso
{
    public int Id { get; set; }
    public required string Codigo { get; set; }
    public required string Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>Tabla roles_permisos.</summary>
public class RolPermiso
{
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public int PermisoId { get; set; }
    public Permiso Permiso { get; set; } = null!;
}

/// <summary>
/// Tabla nueva sesiones. El token de renovación nunca se guarda: solo su hash SHA-256.
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
