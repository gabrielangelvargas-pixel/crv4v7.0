namespace Crv4.Api.Modulos.Sistema;

/// <summary>
/// Contador por tipo de comprobante (ventas, clientes, etc.).
/// Se incrementa en la base con un UPDATE atómico, nunca con MAX(id)+1.
/// </summary>
public class Numerador
{
    public required string Clave { get; set; }
    public long UltimoNumero { get; set; }
    public string? Descripcion { get; set; }
}
