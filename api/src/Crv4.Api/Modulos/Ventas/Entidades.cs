using Crv4.Api.Modulos.Clientes;

namespace Crv4.Api.Modulos.Ventas;

/// <summary>Tabla facturas de crv4_datos: una venta. El número lo asigna el numerador "ventas".</summary>
public class Factura
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int IdEstado { get; set; }
    public int IdVendedor { get; set; }
    public int IdCliente { get; set; }
    public decimal DescuentoBono { get; set; }
    /// <summary>Lo vendido de los proveedores que participan del descuento por escalones (02 y 46).</summary>
    public decimal SubtotalProveedoresBonificados { get; set; }
    public decimal DescuentoEscalon1 { get; set; }
    public decimal DescuentoEscalon2 { get; set; }
    public decimal DescuentoEscalon3 { get; set; }
    public decimal DescuentoProveedores { get; set; }
    public int? IdBonoAplicado { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
    public DateTime? FechaCobro { get; set; }
    public List<FacturaDetalle> Detalles { get; set; } = [];
    public List<PagoFactura> Pagos { get; set; } = [];
    public Cliente? Cliente { get; set; }
    public Vendedor? Vendedor { get; set; }
    public EstadoVenta? Estado { get; set; }
}

/// <summary>Tabla facturasdetalles. SubTotalCosto y SubTotalVenta los calcula la base.</summary>
public class FacturaDetalle
{
    public int Id { get; set; }
    public int IdFactura { get; set; }
    public required string IdProducto { get; set; }
    public string? IdProveedorVenta { get; set; }
    public string? Descripcion { get; set; }
    public decimal? PrecioCosto { get; set; }
    public decimal? PrecioVenta { get; set; }
    public int? Cantidad { get; set; }
    public decimal? SubTotalCosto { get; private set; }
    public decimal? SubTotalVenta { get; private set; }
    public decimal? SubtotalBrutoVenta { get; set; }
    public decimal? DescuentoProveedores { get; set; }
    public decimal? SubtotalNetoVenta { get; set; }
}

/// <summary>Tabla estados: 1 Pendiente, 2 Parcial, 3 Pagada, 4 Anulada.</summary>
public class EstadoVenta
{
    public const int Pendiente = 1;
    public const int Parcial = 2;
    public const int Pagada = 3;
    public const int Anulada = 4;

    public int Id { get; set; }
    public required string Nombre { get; set; }
}

public class Vendedor
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
}

public class MedioPago
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; }
    public bool RequiereCuentaDestino { get; set; }
    public bool RequiereNumeroOperacion { get; set; }
    public bool RequiereAutorizacion { get; set; }
    public bool RequiereCuotas { get; set; }
}

public class CuentaCobro
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Titular { get; set; }
    public string? Banco { get; set; }
    public string? Alias { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Tabla pagos: un cobro a un cliente, repartido en medios (pagosdetalle) y aplicado a ventas (pagosfacturas).</summary>
public class Pago
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int IdCliente { get; set; }
    public decimal ImporteTotal { get; set; }
    public string? Observaciones { get; set; }
    public List<PagoDetalle> Detalles { get; set; } = [];
    public List<PagoFactura> Facturas { get; set; } = [];
}

public class PagoDetalle
{
    public int Id { get; set; }
    public int IdPago { get; set; }
    public int IdMedioPago { get; set; }
    public int? IdCuentaCobro { get; set; }
    public decimal Importe { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? Autorizacion { get; set; }
    public int? Cuotas { get; set; }
    public string? TitularTarjeta { get; set; }
    public string? Ultimos4 { get; set; }
    public string? Observaciones { get; set; }
    public string? UsuarioAlta { get; set; }
}

public class PagoFactura
{
    public int Id { get; set; }
    public int IdPago { get; set; }
    public int IdFactura { get; set; }
    public decimal ImporteAplicado { get; set; }
}
