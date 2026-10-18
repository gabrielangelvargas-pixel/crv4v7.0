namespace Crv4.Api.Modulos.Productos;

/// <summary>Tabla productos de crv4_datos. La clave es el código (IdProducto), no un número.</summary>
public class Producto
{
    public required string Codigo { get; set; }
    public required string IdProveedor { get; set; }
    public string? RefProveedor { get; set; }
    public required string IdRubro { get; set; }
    public required string Descripcion { get; set; }
    public decimal PrecioCompra { get; set; }
    /// <summary>Porcentaje de descuento sobre el precio de compra.</summary>
    public decimal Descuento { get; set; }
    /// <summary>Calculado por la base: PrecioCompra * (1 - Descuento / 100).</summary>
    public decimal? PrecioCosto { get; private set; }
    public decimal? PrecioVenta { get; set; }
    public decimal Utilidad { get; set; }
    public decimal? PrecioOferta { get; set; }
    public DateTime? FechaInicioOferta { get; set; }
    public DateTime? FechaFinOferta { get; set; }
    public DateTime FechaAlta { get; set; }
    public bool Activo { get; set; } = true;
    public string? ImagenPrincipal { get; set; }
}
