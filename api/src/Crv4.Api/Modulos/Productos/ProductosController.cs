using System.ComponentModel.DataAnnotations;
using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Productos;

/// <summary>
/// Los costos (compra, descuento, costo y utilidad) solo los ve quien puede crear o editar productos,
/// y la referencia del proveedor solo el rol Administrador, igual que en Sistema03.
/// </summary>
public record ProductoDto(
    string Codigo, string Descripcion, string IdRubro, string Rubro, string IdProveedor, string Proveedor,
    string? ReferenciaProveedor, decimal? PrecioCompra, decimal? Descuento, decimal? PrecioCosto, decimal? Utilidad,
    decimal PrecioVenta, decimal? PrecioOferta, DateTime? FechaInicioOferta, DateTime? FechaFinOferta,
    DateTime FechaAlta, bool Activo, string? ImagenPrincipal);

public record ProductoGuardar(
    [Required(ErrorMessage = "Ingresá el código."), MaxLength(13, ErrorMessage = "El código tiene hasta 13 caracteres.")] string Codigo,
    [MaxLength(45, ErrorMessage = "La referencia del proveedor tiene hasta 45 caracteres.")] string? ReferenciaProveedor,
    [Required(ErrorMessage = "Ingresá la descripción."), MaxLength(100, ErrorMessage = "La descripción tiene hasta 100 caracteres.")] string Descripcion,
    [Required(ErrorMessage = "Elegí el rubro.")] string IdRubro,
    [Required(ErrorMessage = "Elegí el proveedor.")] string IdProveedor,
    [Range(0, 9_999_999_999, ErrorMessage = "El precio de compra no puede ser negativo.")] decimal PrecioCompra,
    [Range(-100, 100, ErrorMessage = "El descuento va de -100 a 100 (negativo es recargo).")] decimal Descuento,
    decimal Utilidad,
    /// <summary>Si viene, manda sobre la utilidad: la utilidad se recalcula a partir de este precio.</summary>
    [Range(0, 9_999_999_999, ErrorMessage = "El precio de venta no puede ser negativo.")] decimal? PrecioVenta,
    [Range(0, 9_999_999_999, ErrorMessage = "El precio de oferta no puede ser negativo.")] decimal? PrecioOferta,
    DateTime? FechaInicioOferta,
    DateTime? FechaFinOferta,
    [MaxLength(255)] string? ImagenPrincipal,
    bool Activo = true);

public record ProveedorDto(string Codigo, string Nombre);

[ApiController]
[Route("api/productos")]
public class ProductosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [TienePermiso(Permisos.ProductosVer)]
    public async Task<Paginado<ProductoDto>> Listar(
        [FromQuery] string? buscar, [FromQuery] string? idRubro, [FromQuery] string? idProveedor, [FromQuery] bool? activo,
        [FromQuery] int? pagina, [FromQuery] int? tamano, CancellationToken ct)
    {
        var (numero, cantidad) = Paginacion.Normalizar(pagina, tamano);
        var consulta = db.Productos.AsNoTracking();
        if (activo is not null)
            consulta = consulta.Where(p => p.Activo == activo);
        if (!string.IsNullOrWhiteSpace(idRubro))
        {
            // Un rubro incluye los productos de sus subrubros y nietos.
            var codigos = await RubroYDescendientesAsync(idRubro.Trim(), ct);
            consulta = consulta.Where(p => codigos.Contains(p.IdRubro));
        }
        if (!string.IsNullOrWhiteSpace(idProveedor))
            consulta = consulta.Where(p => p.IdProveedor == idProveedor.Trim());
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(p => p.Codigo.Contains(texto) || p.Descripcion.Contains(texto)
                || (p.RefProveedor != null && p.RefProveedor.Contains(texto)));
        }

        var total = await consulta.CountAsync(ct);
        var items = await Proyectar(consulta.OrderBy(p => p.Codigo).Skip((numero - 1) * cantidad).Take(cantidad)).ToListAsync(ct);
        return new Paginado<ProductoDto>(items, total, numero, cantidad);
    }

    [HttpGet("{codigo}")]
    [TienePermiso(Permisos.ProductosVer)]
    public async Task<ProductoDto> Obtener(string codigo, CancellationToken ct) =>
        await Proyectar(db.Productos.AsNoTracking().Where(p => p.Codigo == codigo)).FirstOrDefaultAsync(ct)
        ?? throw new NoEncontrado("El producto no existe.");

    [HttpPost]
    [TienePermiso(Permisos.ProductosCrear)]
    public async Task<ActionResult<ProductoDto>> Crear(ProductoGuardar datos, CancellationToken ct)
    {
        var codigo = datos.Codigo.Trim();
        await ValidarAsync(datos, codigo, null, ct);
        var producto = new Producto { Codigo = codigo, IdProveedor = "", IdRubro = "", Descripcion = "" };
        Copiar(datos, producto);
        if (!EsAdministrador) producto.RefProveedor = null;
        db.Productos.Add(producto);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { codigo }, await Obtener(codigo, ct));
    }

    [HttpPut("{codigo}")]
    [TienePermiso(Permisos.ProductosEditar)]
    public async Task<ProductoDto> Actualizar(string codigo, ProductoGuardar datos, CancellationToken ct)
    {
        var producto = await db.Productos.FirstOrDefaultAsync(p => p.Codigo == codigo, ct) ?? throw new NoEncontrado("El producto no existe.");
        var nuevoCodigo = datos.Codigo.Trim();
        await ValidarAsync(datos, nuevoCodigo, codigo, ct);
        // Las ventas guardan el código del producto sin clave foránea: cambiarlo las dejaría sueltas.
        if (nuevoCodigo != codigo && await TieneVentasAsync(codigo, ct))
            throw new ErrorNegocio("El producto ya tiene ventas: no se puede cambiar el código.", StatusCodes.Status409Conflict);
        var referencia = producto.RefProveedor;
        Copiar(datos, producto);
        // Quien no ve la referencia del proveedor la manda vacía: se conserva la que había.
        if (!EsAdministrador) producto.RefProveedor = referencia;
        await db.SaveChangesAsync(ct);

        if (nuevoCodigo != codigo)
        {
            await db.Productos.Where(p => p.Codigo == codigo).ExecuteUpdateAsync(s => s.SetProperty(p => p.Codigo, nuevoCodigo), ct);
        }
        return await Obtener(nuevoCodigo, ct);
    }

    [HttpDelete("{codigo}")]
    [TienePermiso(Permisos.ProductosEliminar)]
    public async Task<IActionResult> Eliminar(string codigo, CancellationToken ct)
    {
        if (!await db.Productos.AnyAsync(p => p.Codigo == codigo, ct))
            throw new NoEncontrado("El producto no existe.");
        if (await TieneVentasAsync(codigo, ct))
            throw new ErrorNegocio("El producto tiene ventas. Desactivalo en lugar de eliminarlo.", StatusCodes.Status409Conflict);
        await db.Productos.Where(p => p.Codigo == codigo).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    [HttpGet("/api/proveedores")]
    [TienePermiso(Permisos.ProductosVer)]
    public async Task<IReadOnlyList<ProveedorDto>> Proveedores(CancellationToken ct) =>
        await db.Proveedores.AsNoTracking().OrderBy(p => p.Nombre).ThenBy(p => p.Codigo)
            .Select(p => new ProveedorDto(p.Codigo, p.Nombre)).ToListAsync(ct);

    /// <summary>Precio de venta = costo + utilidad %. Si se carga el precio de venta, la utilidad se calcula a partir de él.</summary>
    public static (decimal precioVenta, decimal utilidad) CalcularPrecios(decimal precioCompra, decimal descuento, decimal utilidad, decimal? precioVenta)
    {
        var costo = precioCompra - precioCompra * descuento / 100m;
        if (precioVenta is not { } venta)
            return (Math.Round(Math.Max(costo + costo * utilidad / 100m, 0), 2, MidpointRounding.AwayFromZero), utilidad);
        var ventaRedondeada = Math.Round(venta, 2, MidpointRounding.AwayFromZero);
        var utilidadCalculada = costo <= 0 ? 0 : Math.Round((ventaRedondeada - costo) / costo * 100m, 3, MidpointRounding.AwayFromZero);
        return (ventaRedondeada, utilidadCalculada);
    }

    private static void Copiar(ProductoGuardar datos, Producto producto)
    {
        var (precioVenta, utilidad) = CalcularPrecios(datos.PrecioCompra, datos.Descuento, datos.Utilidad, datos.PrecioVenta);
        producto.RefProveedor = Limpio(datos.ReferenciaProveedor);
        producto.Descripcion = datos.Descripcion.Trim();
        producto.IdRubro = datos.IdRubro.Trim();
        producto.IdProveedor = datos.IdProveedor.Trim();
        producto.PrecioCompra = datos.PrecioCompra;
        producto.Descuento = datos.Descuento;
        producto.Utilidad = utilidad;
        producto.PrecioVenta = precioVenta;
        producto.PrecioOferta = datos.PrecioOferta;
        producto.FechaInicioOferta = datos.FechaInicioOferta;
        producto.FechaFinOferta = datos.FechaFinOferta;
        producto.Activo = datos.Activo;
        producto.ImagenPrincipal = Limpio(datos.ImagenPrincipal);
    }

    private async Task ValidarAsync(ProductoGuardar datos, string codigo, string? codigoActual, CancellationToken ct)
    {
        if (codigo != codigoActual && await db.Productos.AnyAsync(p => p.Codigo == codigo, ct))
            throw new ErrorNegocio("Ya existe un producto con ese código.", StatusCodes.Status409Conflict);
        if (!await db.Rubros.AnyAsync(r => r.Codigo == datos.IdRubro.Trim(), ct))
            throw new ErrorNegocio("El rubro elegido no existe.");
        if (!await db.Proveedores.AnyAsync(p => p.Codigo == datos.IdProveedor.Trim(), ct))
            throw new ErrorNegocio("El proveedor elegido no existe.");
        if (datos.PrecioVenta is null && datos.Utilidad < 0)
            throw new ErrorNegocio("La utilidad no puede ser negativa.");
        if (datos.FechaInicioOferta is { } inicio && datos.FechaFinOferta is { } fin && fin < inicio)
            throw new ErrorNegocio("La fecha de fin de la oferta no puede ser anterior a la de inicio.");
    }

    private async Task<bool> TieneVentasAsync(string codigo, CancellationToken ct) =>
        await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM facturasdetalles WHERE IdProducto = {codigo}").SingleAsync(ct) > 0;

    private async Task<List<string>> RubroYDescendientesAsync(string codigo, CancellationToken ct)
    {
        var rubros = await db.Rubros.AsNoTracking().Select(r => new { r.Id, r.Codigo, r.IdPadre }).ToListAsync(ct);
        var elegido = rubros.FirstOrDefault(r => r.Codigo == codigo);
        if (elegido is null) return [];
        var incluidos = new HashSet<int> { elegido.Id };
        var pendientes = new Queue<int>([elegido.Id]);
        while (pendientes.TryDequeue(out var padre))
            foreach (var hijo in rubros.Where(r => r.IdPadre == padre && incluidos.Add(r.Id)))
                pendientes.Enqueue(hijo.Id);
        return rubros.Where(r => incluidos.Contains(r.Id)).Select(r => r.Codigo).ToList();
    }

    private IQueryable<ProductoDto> Proyectar(IQueryable<Producto> consulta)
    {
        var verCostos = User.HasClaim(Claims.Permiso, Permisos.ProductosCrear) || User.HasClaim(Claims.Permiso, Permisos.ProductosEditar);
        var verReferencia = EsAdministrador;
        return consulta.Select(p => new ProductoDto(
            p.Codigo, p.Descripcion, p.IdRubro, p.Rubro!.Nombre, p.IdProveedor, p.Proveedor!.Nombre,
            verReferencia ? p.RefProveedor : null,
            verCostos ? p.PrecioCompra : null,
            verCostos ? p.Descuento : null,
            verCostos ? p.PrecioCosto ?? 0 : null,
            verCostos ? p.Utilidad : null,
            p.PrecioVenta ?? 0, p.PrecioOferta, p.FechaInicioOferta, p.FechaFinOferta, p.FechaAlta, p.Activo, p.ImagenPrincipal));
    }

    private bool EsAdministrador => User.HasClaim(Claims.Rol, Rol.NombreAdministrador);

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
