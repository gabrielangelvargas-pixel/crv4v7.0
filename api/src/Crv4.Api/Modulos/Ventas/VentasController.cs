using System.ComponentModel.DataAnnotations;
using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Productos;
using Crv4.Api.Modulos.Seguridad;
using Crv4.Api.Modulos.Sistema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Ventas;

public record VentaResumenDto(
    int Id, DateTime Fecha, int IdCliente, string Cliente, int IdVendedor, string Vendedor, int IdEstado, string Estado,
    int Items, decimal Total, decimal Pagado);

public record VentaItemDto(
    int Id, string IdProducto, string? Descripcion, decimal PrecioCosto, decimal PrecioVenta, int Cantidad,
    decimal SubTotalCosto, decimal SubTotalVenta, string? IdProveedorVenta, decimal SubtotalBrutoVenta,
    decimal DescuentoProveedores, decimal SubtotalNetoVenta);

public record VentaDto(
    int Id, DateTime Fecha, int IdCliente, string Cliente, int IdVendedor, string Vendedor, int IdEstado, string Estado,
    decimal DescuentoBono, decimal SubtotalProveedoresBonificados, decimal DescuentoEscalon1, decimal DescuentoEscalon2,
    decimal DescuentoEscalon3, decimal DescuentoProveedores, DateTime? FechaFinalizacion, DateTime? FechaCobro,
    decimal TotalCosto, decimal TotalVenta, decimal Pagado, IReadOnlyList<VentaItemDto> Items);

public record VentaItemGuardar(
    [Required(ErrorMessage = "Falta el código del producto.")] string IdProducto,
    [Range(1, 100_000, ErrorMessage = "La cantidad tiene que ser mayor a cero.")] int Cantidad,
    /// <summary>Si no viene, se usa el precio del producto (o la oferta vigente).</summary>
    [Range(-1_000_000_000d, 1_000_000_000d)] decimal? PrecioVenta);

public record VentaGuardar(
    int IdCliente,
    int IdVendedor,
    int IdEstado,
    [Range(0d, 1_000_000_000d, ErrorMessage = "El descuento no puede ser negativo.")] decimal DescuentoBono,
    List<VentaItemGuardar> Items,
    bool UsarPrecioOfertaSinVigencia = false);

public record VentaEliminada(string Accion, string Mensaje);

public record OpcionVentaDto(int Id, string Nombre);

[ApiController]
[Route("api/ventas")]
public class VentasController(AppDbContext db, INumeradorServicio numerador, TimeProvider reloj) : ControllerBase
{
    /// <summary>Proveedores que participan del descuento por escalones, igual que en Sistema03.</summary>
    public static readonly HashSet<string> ProveedoresBonificados = ["02", "46"];

    /// <summary>Renglón que agrega el cobro cuando se ajusta el importe. No es un producto de la tabla productos.</summary>
    public const string ProductoRedondeo = "REDONDEO";

    private DateTime Ahora => reloj.GetLocalNow().DateTime;

    [HttpGet]
    [TienePermiso(Permisos.VentasVer)]
    public async Task<Paginado<VentaResumenDto>> Listar(
        [FromQuery] string? buscar, [FromQuery] int? idCliente, [FromQuery] int? mes, [FromQuery] int? anio,
        [FromQuery] int? pagina, [FromQuery] int? tamano, CancellationToken ct)
    {
        var (numero, cantidad) = Paginacion.Normalizar(pagina, tamano);
        var consulta = db.Facturas.AsNoTracking();
        // Las ventas de un cliente se ven todas juntas; si no, las del mes elegido.
        if (idCliente is > 0)
            consulta = consulta.Where(f => f.IdCliente == idCliente);
        else
        {
            var hoy = Ahora;
            var desde = new DateTime(anio is >= 2000 and <= 2100 ? anio.Value : hoy.Year, mes is >= 1 and <= 12 ? mes.Value : hoy.Month, 1);
            var hasta = desde.AddMonths(1);
            consulta = consulta.Where(f => f.Fecha >= desde && f.Fecha < hasta);
        }
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            var esNumero = int.TryParse(texto, out var id);
            consulta = consulta.Where(f => (esNumero && f.Id == id) || f.Cliente!.Nombre.Contains(texto));
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta.OrderByDescending(f => f.Id).Skip((numero - 1) * cantidad).Take(cantidad)
            .Select(f => new VentaResumenDto(
                f.Id, f.Fecha, f.IdCliente, f.Cliente!.Nombre, f.IdVendedor, f.Vendedor!.Nombre, f.IdEstado, f.Estado!.Nombre,
                f.Detalles.Count,
                f.Detalles.Sum(d => d.SubtotalNetoVenta ?? d.SubTotalVenta ?? 0) - f.DescuentoBono,
                f.Pagos.Sum(p => (decimal?)p.ImporteAplicado) ?? 0))
            .ToListAsync(ct);
        return new Paginado<VentaResumenDto>(items, total, numero, cantidad);
    }

    [HttpGet("{id:int}")]
    [TienePermiso(Permisos.VentasVer)]
    public async Task<VentaDto> Obtener(int id, CancellationToken ct) =>
        await db.Facturas.AsNoTracking().Where(f => f.Id == id)
            .Select(f => new VentaDto(
                f.Id, f.Fecha, f.IdCliente, f.Cliente!.Nombre, f.IdVendedor, f.Vendedor!.Nombre, f.IdEstado, f.Estado!.Nombre,
                f.DescuentoBono, f.SubtotalProveedoresBonificados, f.DescuentoEscalon1, f.DescuentoEscalon2, f.DescuentoEscalon3,
                f.DescuentoProveedores, f.FechaFinalizacion, f.FechaCobro,
                f.Detalles.Sum(d => d.SubTotalCosto ?? 0),
                f.Detalles.Sum(d => d.SubtotalNetoVenta ?? d.SubTotalVenta ?? 0) - f.DescuentoBono,
                f.Pagos.Sum(p => (decimal?)p.ImporteAplicado) ?? 0,
                f.Detalles.OrderBy(d => d.Id).Select(d => new VentaItemDto(
                    d.Id, d.IdProducto, d.Descripcion, d.PrecioCosto ?? 0, d.PrecioVenta ?? 0, d.Cantidad ?? 0,
                    d.SubTotalCosto ?? 0, d.SubTotalVenta ?? 0, d.IdProveedorVenta,
                    d.SubtotalBrutoVenta ?? d.SubTotalVenta ?? 0, d.DescuentoProveedores ?? 0,
                    d.SubtotalNetoVenta ?? d.SubTotalVenta ?? 0)).ToList()))
            .FirstOrDefaultAsync(ct)
        ?? throw new NoEncontrado($"No existe la venta {id}.");

    [HttpPost]
    [TienePermiso(Permisos.VentasCrear)]
    public async Task<ActionResult<VentaDto>> Crear(VentaGuardar datos, CancellationToken ct)
    {
        await ValidarAsync(datos, ct);
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        var detalles = await ArmarDetallesAsync(datos, ct);
        var factura = new Factura
        {
            Id = checked((int)await numerador.SiguienteAsync(Numeradores.Ventas, ct)),
            IdCliente = datos.IdCliente,
            IdVendedor = datos.IdVendedor,
            IdEstado = datos.IdEstado,
            DescuentoBono = datos.DescuentoBono,
            Detalles = detalles,
        };
        AplicarDescuentos(factura);
        db.Facturas.Add(factura);
        await AjustarSaldoAsync(factura.IdCliente, factura.IdEstado, Total(detalles, factura.DescuentoBono), ct);
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = factura.Id }, await Obtener(factura.Id, ct));
    }

    [HttpPut("{id:int}")]
    [TienePermiso(Permisos.VentasEditar)]
    public async Task<VentaDto> Actualizar(int id, VentaGuardar datos, CancellationToken ct)
    {
        await ValidarAsync(datos, ct);
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        var factura = await db.Facturas.Include(f => f.Detalles).FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NoEncontrado($"No existe la venta {id}.");

        // Se descuenta del saldo lo que sumaba la venta y después se suma el total nuevo.
        await AjustarSaldoAsync(factura.IdCliente, factura.IdEstado, -Total(factura.Detalles, factura.DescuentoBono), ct);
        db.FacturasDetalles.RemoveRange(factura.Detalles);
        factura.Detalles = await ArmarDetallesAsync(datos, ct);
        factura.IdCliente = datos.IdCliente;
        factura.IdVendedor = datos.IdVendedor;
        factura.IdEstado = datos.IdEstado;
        factura.DescuentoBono = datos.DescuentoBono;
        AplicarDescuentos(factura);
        await AjustarSaldoAsync(factura.IdCliente, factura.IdEstado, Total(factura.Detalles, factura.DescuentoBono), ct);
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return await Obtener(id, ct);
    }

    /// <summary>
    /// Como en Sistema03: la última venta se elimina (y su número se vuelve a usar); las anteriores se anulan y quedan
    /// con total 0 para no dejar huecos en la numeración.
    /// </summary>
    [HttpDelete("{id:int}")]
    [TienePermiso(Permisos.VentasAnular)]
    public async Task<VentaEliminada> Eliminar(int id, CancellationToken ct)
    {
        var factura = await db.Facturas.Include(f => f.Detalles).FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NoEncontrado($"No existe la venta {id}.");
        if (await db.Set<PagoFactura>().AnyAsync(p => p.IdFactura == id, ct))
            throw new ErrorNegocio("La venta tiene pagos registrados: no se puede eliminar ni anular.", StatusCodes.Status409Conflict);
        if (await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM bonosdescuento WHERE IdFacturaOrigen = {id} OR IdFacturaUso = {id}").SingleAsync(ct) > 0)
            throw new ErrorNegocio("La venta está vinculada a bonos de descuento: no se puede eliminar ni anular.", StatusCodes.Status409Conflict);

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        await AjustarSaldoAsync(factura.IdCliente, factura.IdEstado, -Total(factura.Detalles, factura.DescuentoBono), ct);
        var ultima = await db.Facturas.MaxAsync(f => f.Id, ct);
        if (ultima == id)
        {
            db.Facturas.Remove(factura);
            await db.SaveChangesAsync(ct);
            await db.Database.ExecuteSqlAsync(
                $"UPDATE numeradores SET UltimoNumero = UltimoNumero - 1 WHERE Clave = {Numeradores.Ventas} AND UltimoNumero = {id}", ct);
            await transaccion.CommitAsync(ct);
            return new VentaEliminada("eliminada", $"Venta {id} eliminada.");
        }

        db.FacturasDetalles.RemoveRange(factura.Detalles);
        factura.DescuentoBono = 0;
        factura.SubtotalProveedoresBonificados = factura.DescuentoEscalon1 = factura.DescuentoEscalon2 = factura.DescuentoEscalon3 = 0;
        factura.DescuentoProveedores = 0;
        factura.IdEstado = EstadoVenta.Anulada;
        factura.FechaCobro = null;
        factura.FechaFinalizacion = Ahora;
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return new VentaEliminada("anulada", $"Venta {id} anulada. El número se conserva con total 0.");
    }

    [HttpGet("/api/vendedores")]
    [TienePermiso(Permisos.VentasVer)]
    public async Task<IReadOnlyList<OpcionVentaDto>> Vendedores(CancellationToken ct) =>
        await db.Vendedores.AsNoTracking().OrderBy(v => v.Nombre).Select(v => new OpcionVentaDto(v.Id, v.Nombre)).ToListAsync(ct);

    [HttpGet("/api/estados-venta")]
    [TienePermiso(Permisos.VentasVer)]
    public async Task<IReadOnlyList<OpcionVentaDto>> Estados(CancellationToken ct) =>
        await db.EstadosVenta.AsNoTracking().OrderBy(e => e.Id).Select(e => new OpcionVentaDto(e.Id, e.Nombre)).ToListAsync(ct);

    private async Task ValidarAsync(VentaGuardar datos, CancellationToken ct)
    {
        if (!await db.Clientes.AnyAsync(c => c.Id == datos.IdCliente, ct))
            throw new ErrorNegocio("El cliente elegido no existe.");
        if (!await db.Vendedores.AnyAsync(v => v.Id == datos.IdVendedor, ct))
            throw new ErrorNegocio("El vendedor elegido no existe.");
        if (!await db.EstadosVenta.AnyAsync(e => e.Id == datos.IdEstado, ct))
            throw new ErrorNegocio("El estado elegido no existe.");
        if (datos.Items is not { Count: > 0 })
            throw new ErrorNegocio("La venta tiene que tener al menos un producto.");
        if (datos.Items.Any(i => i.PrecioVenta < 0 && i.IdProducto.Trim() != ProductoRedondeo))
            throw new ErrorNegocio("Los precios no pueden ser negativos.");
    }

    private async Task<List<FacturaDetalle>> ArmarDetallesAsync(VentaGuardar datos, CancellationToken ct)
    {
        var codigos = datos.Items.Select(i => i.IdProducto.Trim()).Where(c => c != ProductoRedondeo).Distinct().ToList();
        var productos = await db.Productos.AsNoTracking().Where(p => codigos.Contains(p.Codigo)).ToDictionaryAsync(p => p.Codigo, ct);
        var faltante = codigos.FirstOrDefault(c => !productos.ContainsKey(c));
        if (faltante is not null)
            throw new ErrorNegocio($"El producto {faltante} no existe.");

        var ahora = Ahora;
        return datos.Items.Select(item =>
        {
            var codigo = item.IdProducto.Trim();
            if (codigo == ProductoRedondeo)
            {
                // El redondeo que agregó un cobro se conserva tal cual al volver a guardar la venta.
                var importe = item.PrecioVenta ?? 0;
                return new FacturaDetalle
                {
                    IdProducto = codigo, Descripcion = $"Redondeo {importe:+0.00;-0.00}", PrecioCosto = 0, PrecioVenta = importe, Cantidad = 1,
                };
            }
            var p = productos[codigo];
            var oferta = OfertaVigente(p, datos.UsarPrecioOfertaSinVigencia, ahora);
            var descripcion = p.Descripcion.Trim();
            return new FacturaDetalle
            {
                IdProducto = p.Codigo,
                Descripcion = oferta && !descripcion.StartsWith("OFF...", StringComparison.OrdinalIgnoreCase) ? $"OFF... {descripcion}" : descripcion,
                PrecioCosto = p.PrecioCosto ?? 0,
                PrecioVenta = item.PrecioVenta ?? (oferta ? p.PrecioOferta : p.PrecioVenta) ?? 0,
                Cantidad = item.Cantidad,
                IdProveedorVenta = p.IdProveedor,
            };
        }).ToList();
    }

    public static bool OfertaVigente(Producto p, bool sinValidarFechas, DateTime ahora) =>
        (p.PrecioOferta ?? 0) > 0
        && (sinValidarFechas || ((p.FechaInicioOferta is null || p.FechaInicioOferta <= ahora) && (p.FechaFinOferta is null || p.FechaFinOferta >= ahora)));

    /// <summary>
    /// Descuento por escalones sobre lo vendido de los proveedores 02 y 46: 6% si supera $100.000, otro 5% (sobre el resto)
    /// si supera $200.000 y otro 5% si supera $400.000. Se reparte entre los renglones en proporción a su importe.
    /// </summary>
    public static void AplicarDescuentos(Factura factura)
    {
        var participan = factura.Detalles.Where(d => d.IdProveedorVenta is not null && ProveedoresBonificados.Contains(d.IdProveedorVenta)).ToList();
        var subtotal = Redondear(participan.Sum(Bruto));
        var resto = subtotal;
        var primero = subtotal > 100_000 ? Redondear(resto * 0.06m) : 0;
        resto -= primero;
        var segundo = subtotal > 200_000 ? Redondear(resto * 0.05m) : 0;
        resto -= segundo;
        var tercero = subtotal > 400_000 ? Redondear(resto * 0.05m) : 0;
        var descuento = Redondear(primero + segundo + tercero);

        var repartido = 0m;
        foreach (var d in factura.Detalles)
        {
            var bruto = Redondear(Bruto(d));
            var parte = 0m;
            if (subtotal > 0 && participan.Contains(d))
            {
                parte = d == participan[^1] ? Redondear(descuento - repartido) : Redondear(descuento * bruto / subtotal);
                repartido += parte;
            }
            d.SubtotalBrutoVenta = bruto;
            d.DescuentoProveedores = parte;
            d.SubtotalNetoVenta = Redondear(bruto - parte);
        }

        factura.SubtotalProveedoresBonificados = subtotal;
        factura.DescuentoEscalon1 = primero;
        factura.DescuentoEscalon2 = segundo;
        factura.DescuentoEscalon3 = tercero;
        factura.DescuentoProveedores = descuento;
    }

    public static decimal Total(IEnumerable<FacturaDetalle> detalles, decimal descuentoBono) =>
        Math.Max(detalles.Sum(d => d.SubtotalNetoVenta ?? d.SubtotalBrutoVenta ?? Bruto(d)) - descuentoBono, 0);

    private static decimal Bruto(FacturaDetalle d) => d.SubtotalBrutoVenta ?? (d.PrecioVenta ?? 0) * (d.Cantidad ?? 0);

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    /// <summary>Las ventas pendientes o parciales suman al saldo de la cuenta del cliente.</summary>
    private async Task AjustarSaldoAsync(int idCliente, int idEstado, decimal importe, CancellationToken ct)
    {
        if (idEstado is not (EstadoVenta.Pendiente or EstadoVenta.Parcial) || importe == 0)
            return;
        var cliente = await db.Clientes.FirstAsync(c => c.Id == idCliente, ct);
        cliente.SaldoCuenta = Math.Max((cliente.SaldoCuenta ?? 0) + importe, 0);
    }
}
