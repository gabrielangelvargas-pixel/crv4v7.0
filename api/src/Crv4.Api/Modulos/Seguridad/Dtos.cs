using System.ComponentModel.DataAnnotations;

namespace Crv4.Api.Modulos.Seguridad;

public record UsuarioDto(int Id, string NombreUsuario, string Nombre, int RolId, string Rol, bool Activo, bool DebeCambiarPassword, bool Bloqueado, DateTime? UltimoIngreso);

public record UsuarioCrear(
    [Required, MaxLength(50), RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "El usuario solo puede tener letras, números, punto, guion y guion bajo.")] string NombreUsuario,
    [Required, MaxLength(120)] string Nombre,
    [Required] int RolId,
    [Required, MaxLength(128)] string PasswordInicial);

public record UsuarioActualizar(
    [Required, MaxLength(120)] string Nombre,
    [Required] int RolId,
    bool Activo);

public record PasswordReinicio([Required, MaxLength(128)] string PasswordTemporal);

public record RolDto(int Id, string Nombre, string? Descripcion, bool EsSistema, int Usuarios, IReadOnlyList<string> Permisos);

public record RolGuardar(
    [Required, MaxLength(60)] string Nombre,
    [MaxLength(250)] string? Descripcion,
    [Required] IReadOnlyList<string> Permisos);

public record PermisoDto(string Codigo, string Modulo, string Descripcion);
