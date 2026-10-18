namespace Crv4.Api.Modulos.Clientes;

/// <summary>Tabla clientes de crv4_datos. El número (IdCliente) lo asigna el numerador "clientes".</summary>
public class Cliente
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Direccion { get; set; }
    public int IdZona { get; set; }
    public int IdFiscal { get; set; }
    public string? NumeroFiscal { get; set; }
    public string? Celular1 { get; set; }
    public string? Celular2 { get; set; }
    public string? TelefonoFijo { get; set; }
    public decimal? SaldoCuenta { get; set; }
    public DateTime FechaAlta { get; set; }
}
