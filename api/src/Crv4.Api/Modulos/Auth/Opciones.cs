using System.ComponentModel.DataAnnotations;

namespace Crv4.Api.Modulos.Auth;

public class JwtOpciones
{
    public const string Seccion = "Jwt";

    [Required] public string Emisor { get; set; } = "";
    [Required] public string Audiencia { get; set; } = "";

    /// <summary>Clave de firma. Llega por variable de entorno (CRV4_Jwt__Clave), nunca desde un archivo del repositorio.</summary>
    [Required, MinLength(32, ErrorMessage = "Jwt:Clave debe tener al menos 32 caracteres.")]
    public string Clave { get; set; } = "";

    /// <summary>Duración del token de acceso. Corta: la web lo renueva sola mientras la sesión siga viva.</summary>
    [Range(1, 120)] public int MinutosAcceso { get; set; } = 15;

    /// <summary>Duración máxima de una sesión desde el login. Al vencer hay que volver a ingresar.</summary>
    [Range(1, 72)] public int HorasSesion { get; set; } = 12;
}

public class SeguridadOpciones
{
    public const string Seccion = "Seguridad";

    [Range(1, 50)] public int IntentosFallidosMaximos { get; set; } = 5;
    [Range(1, 1440)] public int MinutosBloqueo { get; set; } = 15;
    [Range(6, 128)] public int LargoMinimoPassword { get; set; } = 8;

    /// <summary>Intentos de login o renovación por minuto desde una misma IP.</summary>
    [Range(1, 10000)] public int SolicitudesLoginPorMinuto { get; set; } = 10;
}
