namespace Crv4.Api.Modulos.Clientes;

/// <summary>Tabla zonas de crv4_datos.</summary>
public class Zona
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
}

/// <summary>Tabla fiscales de crv4_datos: la condición fiscal del cliente (consumidor final, responsable inscripto...).</summary>
public class CondicionFiscal
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
}
