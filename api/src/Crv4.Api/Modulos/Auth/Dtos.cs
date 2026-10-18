using System.ComponentModel.DataAnnotations;

namespace Crv4.Api.Modulos.Auth;

public record LoginSolicitud(
    [Required(ErrorMessage = "Ingresá el usuario."), MaxLength(50)] string Usuario,
    [Required(ErrorMessage = "Ingresá la contraseña."), MaxLength(128)] string Password);

public record RenovarSolicitud([Required] string TokenRenovacion);

public record CambiarPasswordSolicitud(
    [Required] string PasswordActual,
    [Required, MaxLength(128)] string PasswordNueva);

public record UsuarioSesion(int Id, string NombreUsuario, string Nombre, IReadOnlyList<string> Roles, IReadOnlyList<string> Permisos, bool DebeCambiarPassword);

public record SesionRespuesta(string TokenAcceso, DateTime AccesoExpiraEn, string TokenRenovacion, DateTime SesionExpiraEn, UsuarioSesion Usuario);
