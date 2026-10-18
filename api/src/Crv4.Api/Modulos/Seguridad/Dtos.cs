using System.ComponentModel.DataAnnotations;

namespace Crv4.Api.Modulos.Seguridad;

public record RolResumen(int Id, string Nombre);

public record UsuarioDto(int Id, string NombreUsuario, string Nombre, string? Email, IReadOnlyList<RolResumen> Roles, bool Activo,
    bool DebeCambiarPassword, bool Bloqueado, DateTime? UltimoIngreso);

public record UsuarioCrear(
    [Required, MaxLength(50), RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "El usuario solo puede tener letras, números, punto, guion y guion bajo.")] string NombreUsuario,
    [Required, MaxLength(120)] string Nombre,
    [EmailAddress, MaxLength(150)] string? Email,
    [Required, MinLength(1, ErrorMessage = "Elegí al menos un rol.")] IReadOnlyList<int> RolIds,
    [Required, MaxLength(128)] string PasswordInicial);

public record UsuarioActualizar(
    [Required, MaxLength(120)] string Nombre,
    [EmailAddress, MaxLength(150)] string? Email,
    [Required, MinLength(1, ErrorMessage = "Elegí al menos un rol.")] IReadOnlyList<int> RolIds,
    bool Activo);

public record PasswordReinicio([Required, MaxLength(128)] string PasswordTemporal);

public record RolDto(int Id, string Nombre, string? Descripcion, bool Activo, bool EsAdministrador, int Usuarios, IReadOnlyList<string> Permisos);

public record RolGuardar(
    [Required, MaxLength(50)] string Nombre,
    [MaxLength(255)] string? Descripcion,
    bool Activo,
    [Required] IReadOnlyList<string> Permisos);

public record PermisoDto(string Codigo, string Modulo, string Descripcion);
