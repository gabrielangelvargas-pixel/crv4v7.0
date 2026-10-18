using System.ComponentModel.DataAnnotations;
using Crv4.Api.Comun;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Seguridad;
using Crv4.Api.Modulos.Sistema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Modulos.Clientes;

public record ClienteDto(
    int Id, string Nombre, string? Direccion, int IdZona, string Zona, int IdFiscal, string CondicionFiscal,
    string? NumeroFiscal, string? Celular1, string? Celular2, string? TelefonoFijo, decimal SaldoCuenta, DateTime FechaAlta);

public record ClienteGuardar(
    /// <summary>Vacío en el alta: el número lo asigna el numerador.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "El código del cliente debe ser mayor a cero.")] int? Id,
    [Required(ErrorMessage = "Ingresá el nombre."), MaxLength(150, ErrorMessage = "El nombre tiene hasta 150 caracteres.")] string Nombre,
    [MaxLength(150, ErrorMessage = "La dirección tiene hasta 150 caracteres.")] string? Direccion,
    [Range(1, int.MaxValue, ErrorMessage = "Elegí la zona.")] int IdZona,
    [Range(1, int.MaxValue, ErrorMessage = "Elegí la condición fiscal.")] int IdFiscal,
    [MaxLength(13, ErrorMessage = "El CUIT/DNI tiene hasta 13 caracteres.")] string? NumeroFiscal,
    [MaxLength(10, ErrorMessage = "El celular tiene hasta 10 caracteres.")] string? Celular1,
    [MaxLength(10, ErrorMessage = "El celular tiene hasta 10 caracteres.")] string? Celular2,
    [MaxLength(15, ErrorMessage = "El teléfono fijo tiene hasta 15 caracteres.")] string? TelefonoFijo,
    [Range(0d, 99_999_999.99, ErrorMessage = "El saldo no puede ser negativo.")] decimal SaldoCuenta = 0);

public record OpcionDto(int Id, string Nombre);

[ApiController]
[Route("api/clientes")]
public class ClientesController(AppDbContext db, INumeradorServicio numerador) : ControllerBase
{
    [HttpGet]
    [TienePermiso(Permisos.ClientesVer)]
    public async Task<Paginado<ClienteDto>> Listar(
        [FromQuery] string? buscar, [FromQuery] int? idZona, [FromQuery] int? idFiscal, [FromQuery] bool? conSaldo,
        [FromQuery] int? pagina, [FromQuery] int? tamano, CancellationToken ct)
    {
        var (numero, cantidad) = Paginacion.Normalizar(pagina, tamano);
        var consulta = db.Clientes.AsNoTracking();
        if (idZona is not null)
            consulta = consulta.Where(c => c.IdZona == idZona);
        if (idFiscal is not null)
            consulta = consulta.Where(c => c.IdFiscal == idFiscal);
        if (conSaldo is true)
            consulta = consulta.Where(c => c.SaldoCuenta > 0);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            var esCodigo = int.TryParse(texto, out var codigo);
            consulta = consulta.Where(c => (esCodigo && c.Id == codigo) || c.Nombre.Contains(texto)
                || (c.Direccion != null && c.Direccion.Contains(texto))
                || (c.NumeroFiscal != null && c.NumeroFiscal.Contains(texto))
                || (c.Celular1 != null && c.Celular1.Contains(texto))
                || (c.Celular2 != null && c.Celular2.Contains(texto))
                || (c.TelefonoFijo != null && c.TelefonoFijo.Contains(texto)));
        }

        var total = await consulta.CountAsync(ct);
        var items = await Proyectar(consulta.OrderBy(c => c.Id).Skip((numero - 1) * cantidad).Take(cantidad)).ToListAsync(ct);
        return new Paginado<ClienteDto>(items, total, numero, cantidad);
    }

    [HttpGet("{id:int}")]
    [TienePermiso(Permisos.ClientesVer)]
    public async Task<ClienteDto> Obtener(int id, CancellationToken ct) =>
        await Proyectar(db.Clientes.AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new NoEncontrado("El cliente no existe.");

    [HttpPost]
    [TienePermiso(Permisos.ClientesCrear)]
    public async Task<ActionResult<ClienteDto>> Crear(ClienteGuardar datos, CancellationToken ct)
    {
        await ValidarAsync(datos, ct);
        int id;
        if (datos.Id is { } elegido)
        {
            if (await db.Clientes.AnyAsync(c => c.Id == elegido, ct))
                throw new ErrorNegocio($"Ya existe un cliente con el código {elegido}.", StatusCodes.Status409Conflict);
            id = elegido;
        }
        else
        {
            id = checked((int)await numerador.SiguienteAsync(Numeradores.Clientes, ct));
        }

        var cliente = new Cliente { Id = id, Nombre = "" };
        Copiar(datos, cliente);
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id }, await Obtener(id, ct));
    }

    [HttpPut("{id:int}")]
    [TienePermiso(Permisos.ClientesEditar)]
    public async Task<ClienteDto> Actualizar(int id, ClienteGuardar datos, CancellationToken ct)
    {
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NoEncontrado("El cliente no existe.");
        await ValidarAsync(datos, ct);
        var nuevoId = datos.Id ?? id;
        if (nuevoId != id && await db.Clientes.AnyAsync(c => c.Id == nuevoId, ct))
            throw new ErrorNegocio($"Ya existe un cliente con el código {nuevoId}.", StatusCodes.Status409Conflict);

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        Copiar(datos, cliente);
        await db.SaveChangesAsync(ct);
        if (nuevoId != id)
        {
            // Las facturas y los pagos siguen al cliente por la clave foránea (ON UPDATE CASCADE);
            // los bonos de descuento no tienen clave foránea y se mueven a mano, igual que en Sistema03.
            await db.Clientes.Where(c => c.Id == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.Id, nuevoId), ct);
            await db.Database.ExecuteSqlAsync($"UPDATE bonosdescuento SET IdCliente = {nuevoId} WHERE IdCliente = {id}", ct);
        }
        await transaccion.CommitAsync(ct);
        return await Obtener(nuevoId, ct);
    }

    [HttpDelete("{id:int}")]
    [TienePermiso(Permisos.ClientesEliminar)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        if (!await db.Clientes.AnyAsync(c => c.Id == id, ct))
            throw new NoEncontrado("El cliente no existe.");
        if (await TieneMovimientosAsync(id, ct))
            throw new ErrorNegocio("El cliente tiene ventas, pagos o bonos: no se puede eliminar.", StatusCodes.Status409Conflict);
        await db.Clientes.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    [HttpGet("/api/zonas")]
    [TienePermiso(Permisos.ClientesVer)]
    public async Task<IReadOnlyList<OpcionDto>> Zonas(CancellationToken ct) =>
        await db.Zonas.AsNoTracking().OrderBy(z => z.Nombre).Select(z => new OpcionDto(z.Id, z.Nombre)).ToListAsync(ct);

    [HttpGet("/api/condiciones-fiscales")]
    [TienePermiso(Permisos.ClientesVer)]
    public async Task<IReadOnlyList<OpcionDto>> CondicionesFiscales(CancellationToken ct) =>
        await db.CondicionesFiscales.AsNoTracking().OrderBy(f => f.Nombre).Select(f => new OpcionDto(f.Id, f.Nombre)).ToListAsync(ct);

    private async Task ValidarAsync(ClienteGuardar datos, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            throw new ErrorNegocio("Ingresá el nombre.");
        if (!await db.Zonas.AnyAsync(z => z.Id == datos.IdZona, ct))
            throw new ErrorNegocio("La zona elegida no existe.");
        if (!await db.CondicionesFiscales.AnyAsync(f => f.Id == datos.IdFiscal, ct))
            throw new ErrorNegocio("La condición fiscal elegida no existe.");
    }

    private async Task<bool> TieneMovimientosAsync(int id, CancellationToken ct) =>
        await db.Database.SqlQuery<int>($"""
            SELECT (SELECT COUNT(*) FROM facturas WHERE IdCliente = {id})
                 + (SELECT COUNT(*) FROM pagos WHERE IdCliente = {id})
                 + (SELECT COUNT(*) FROM bonosdescuento WHERE IdCliente = {id}) AS Value
            """).SingleAsync(ct) > 0;

    private static void Copiar(ClienteGuardar datos, Cliente cliente)
    {
        cliente.Nombre = datos.Nombre.Trim();
        cliente.Direccion = Limpio(datos.Direccion);
        cliente.IdZona = datos.IdZona;
        cliente.IdFiscal = datos.IdFiscal;
        cliente.NumeroFiscal = Limpio(datos.NumeroFiscal);
        cliente.Celular1 = Limpio(datos.Celular1);
        cliente.Celular2 = Limpio(datos.Celular2);
        cliente.TelefonoFijo = Limpio(datos.TelefonoFijo);
        cliente.SaldoCuenta = Math.Round(datos.SaldoCuenta, 2, MidpointRounding.AwayFromZero);
    }

    private static IQueryable<ClienteDto> Proyectar(IQueryable<Cliente> consulta) =>
        consulta.Select(c => new ClienteDto(
            c.Id, c.Nombre, c.Direccion, c.IdZona, c.Zona!.Nombre, c.IdFiscal, c.CondicionFiscal!.Nombre,
            c.NumeroFiscal, c.Celular1, c.Celular2, c.TelefonoFijo, c.SaldoCuenta ?? 0, c.FechaAlta));

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
