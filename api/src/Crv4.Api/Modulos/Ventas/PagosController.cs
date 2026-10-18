using System.ComponentModel.DataAnnotations;
using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Ventas;

public record PagoDetalleGuardar(
    int IdMedioPago,
    int? IdCuentaCobro,
    [Range(0.01d, 1_000_000_000d, ErrorMessage = "Todos los importes tienen que ser mayores a cero.")] decimal Importe,
    [MaxLength(50)] string? NumeroOperacion,
    [MaxLength(50)] string? Autorizacion,
    int? Cuotas,
    [MaxLength(100)] string? TitularTarjeta,
    [MaxLength(4)] string? Ultimos4,
    string? Observaciones);

public record PagoGuardar(
    int IdFactura,
    [MaxLength(255)] string? Observaciones,
    /// <summary>Si el cobro no coincide con el saldo, agrega un renglón de redondeo a la venta por la diferencia.</summary>
    bool AjustarImporte,
    List<PagoDetalleGuardar> Detalles);

public record PagoDto(
    int Id, int IdFactura, int IdCliente, DateTime Fecha, decimal ImporteTotal, decimal PagadoVenta, decimal SaldoVenta,
    decimal SaldoCliente, int IdEstadoVenta, string EstadoVenta);

public record MedioPagoDto(int Id, string Nombre, bool RequiereCuentaDestino, bool RequiereNumeroOperacion, bool RequiereAutorizacion, bool RequiereCuotas);

public record CuentaCobroDto(int Id, string Nombre, string? Titular, string? Banco, string? Alias);

[ApiController]
[Route("api/pagos")]
public class PagosController(AppDbContext db, TimeProvider reloj) : ControllerBase
{
    /// <summary>Tope del ajuste por redondeo, igual que en Sistema03.</summary>
    public const decimal RedondeoMaximo = 100m;

    [HttpPost]
    [TienePermiso(Permisos.VentasEditar)]
    public async Task<ActionResult<PagoDto>> Registrar(PagoGuardar datos, CancellationToken ct)
    {
        if (datos.Detalles is not { Count: > 0 })
            throw new ErrorNegocio("El pago tiene que tener al menos un medio de pago.");

        var factura = await db.Facturas.Include(f => f.Detalles).Include(f => f.Pagos).Include(f => f.Cliente)
            .FirstOrDefaultAsync(f => f.Id == datos.IdFactura, ct) ?? throw new NoEncontrado($"No existe la venta {datos.IdFactura}.");
        if (factura.IdEstado == EstadoVenta.Anulada)
            throw new ErrorNegocio("No se puede cobrar una venta anulada.");
        await ValidarMediosAsync(datos.Detalles, ct);

        var total = VentasController.Total(factura.Detalles, factura.DescuentoBono);
        var pagado = factura.Pagos.Sum(p => p.ImporteAplicado);
        var saldo = Math.Max(total - pagado, 0);
        var importe = datos.Detalles.Sum(d => d.Importe);
        var ajuste = datos.AjustarImporte ? Math.Round(importe - saldo, 2) : 0m;
        if (!datos.AjustarImporte && importe > saldo)
            throw new ErrorNegocio($"El pago supera el saldo pendiente de la venta ({saldo:C}).");
        if (datos.AjustarImporte && ajuste == 0)
            throw new ErrorNegocio("No hay diferencia para ajustar.");
        if (Math.Abs(ajuste) > RedondeoMaximo)
            throw new ErrorNegocio($"El redondeo no puede superar {RedondeoMaximo:C}.");

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        if (ajuste != 0)
        {
            factura.Detalles.Add(new FacturaDetalle
            {
                IdProducto = VentasController.ProductoRedondeo, Descripcion = $"Redondeo {ajuste:+0.00;-0.00}", PrecioCosto = 0,
                PrecioVenta = ajuste, Cantidad = 1, SubtotalBrutoVenta = ajuste, DescuentoProveedores = 0, SubtotalNetoVenta = ajuste,
            });
            saldo = Math.Max(total + ajuste - pagado, 0);
        }

        var pago = new Pago
        {
            IdCliente = factura.IdCliente,
            ImporteTotal = importe,
            Observaciones = Limpio(datos.Observaciones),
            Detalles = datos.Detalles.Select(d => new PagoDetalle
            {
                IdMedioPago = d.IdMedioPago, IdCuentaCobro = d.IdCuentaCobro, Importe = d.Importe,
                NumeroOperacion = Limpio(d.NumeroOperacion), Autorizacion = Limpio(d.Autorizacion), Cuotas = d.Cuotas,
                TitularTarjeta = Limpio(d.TitularTarjeta), Ultimos4 = Limpio(d.Ultimos4), Observaciones = Limpio(d.Observaciones),
                UsuarioAlta = User.FindFirst(Claims.Nombre)?.Value,
            }).ToList(),
            Facturas = [new PagoFactura { IdFactura = factura.Id, ImporteAplicado = importe }],
        };
        db.Pagos.Add(pago);

        // El redondeo cambia el total de la venta: si la venta sumaba a la cuenta del cliente, la cuenta cambia igual.
        var ajusteCuenta = factura.IdEstado is EstadoVenta.Pendiente or EstadoVenta.Parcial ? ajuste : 0;
        factura.Cliente!.SaldoCuenta = Math.Max((factura.Cliente.SaldoCuenta ?? 0) + ajusteCuenta - importe, 0);
        var saldoNuevo = Math.Max(saldo - importe, 0);
        factura.IdEstado = saldoNuevo == 0 ? EstadoVenta.Pagada : EstadoVenta.Parcial;
        factura.FechaCobro = saldoNuevo == 0 ? reloj.GetLocalNow().DateTime : null;
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        var estado = await db.EstadosVenta.AsNoTracking().Where(e => e.Id == factura.IdEstado).Select(e => e.Nombre).FirstAsync(ct);
        return Created($"/api/pagos/{pago.Id}", new PagoDto(
            pago.Id, factura.Id, factura.IdCliente, pago.Fecha, importe, pagado + importe, saldoNuevo,
            factura.Cliente.SaldoCuenta ?? 0, factura.IdEstado, estado));
    }

    [HttpGet("/api/medios-pago")]
    [TienePermiso(Permisos.VentasVer)]
    public async Task<IReadOnlyList<MedioPagoDto>> MediosPago(CancellationToken ct) =>
        await db.MediosPago.AsNoTracking().Where(m => m.Activo).OrderBy(m => m.Id)
            .Select(m => new MedioPagoDto(m.Id, m.Nombre, m.RequiereCuentaDestino, m.RequiereNumeroOperacion, m.RequiereAutorizacion, m.RequiereCuotas))
            .ToListAsync(ct);

    [HttpGet("/api/cuentas-cobro")]
    [TienePermiso(Permisos.VentasVer)]
    public async Task<IReadOnlyList<CuentaCobroDto>> CuentasCobro(CancellationToken ct) =>
        await db.CuentasCobro.AsNoTracking().Where(c => c.Activo).OrderBy(c => c.Nombre)
            .Select(c => new CuentaCobroDto(c.Id, c.Nombre, c.Titular, c.Banco, c.Alias)).ToListAsync(ct);

    private async Task ValidarMediosAsync(List<PagoDetalleGuardar> detalles, CancellationToken ct)
    {
        var idsMedios = detalles.Select(d => d.IdMedioPago).Distinct().ToList();
        var medios = await db.MediosPago.AsNoTracking().Where(m => idsMedios.Contains(m.Id)).ToDictionaryAsync(m => m.Id, ct);
        var idsCuentas = detalles.Where(d => d.IdCuentaCobro is not null).Select(d => d.IdCuentaCobro!.Value).Distinct().ToList();
        var cuentas = await db.CuentasCobro.AsNoTracking().Where(c => idsCuentas.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        foreach (var d in detalles)
        {
            if (!medios.TryGetValue(d.IdMedioPago, out var medio) || !medio.Activo)
                throw new ErrorNegocio("El medio de pago elegido no existe o está inactivo.");
            if (medio.RequiereCuentaDestino && d.IdCuentaCobro is null)
                throw new ErrorNegocio($"{medio.Nombre} necesita la cuenta de destino.");
            if (d.IdCuentaCobro is { } idCuenta && (!cuentas.TryGetValue(idCuenta, out var cuenta) || !cuenta.Activo))
                throw new ErrorNegocio("La cuenta de cobro elegida no existe o está inactiva.");
            if (medio.RequiereNumeroOperacion && string.IsNullOrWhiteSpace(d.NumeroOperacion))
                throw new ErrorNegocio($"{medio.Nombre} necesita el número de operación.");
            if (medio.RequiereAutorizacion && string.IsNullOrWhiteSpace(d.Autorizacion))
                throw new ErrorNegocio($"{medio.Nombre} necesita la autorización.");
            if (medio.RequiereCuotas && d.Cuotas is not > 0)
                throw new ErrorNegocio($"{medio.Nombre} necesita la cantidad de cuotas.");
        }
    }

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
