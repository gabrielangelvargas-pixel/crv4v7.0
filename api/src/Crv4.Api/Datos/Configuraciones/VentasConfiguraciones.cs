using Crv4.Api.Modulos.Ventas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crv4.Api.Datos.Configuraciones;

public class FacturaConfiguracion : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> b)
    {
        b.ToTable("facturas", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdFactura").ValueGeneratedNever();
        b.Property(x => x.Fecha).HasColumnName("FechaFactura").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
        b.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.IdFactura).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Pagos).WithOne().HasForeignKey(x => x.IdFactura).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.IdCliente).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Vendedor).WithMany().HasForeignKey(x => x.IdVendedor).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Estado).WithMany().HasForeignKey(x => x.IdEstado).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FacturaDetalleConfiguracion : IEntityTypeConfiguration<FacturaDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaDetalle> b)
    {
        b.ToTable("facturasdetalles", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.SubTotalCosto).HasComputedColumnSql("`PrecioCosto` * `Cantidad`", stored: true);
        b.Property(x => x.SubTotalVenta).HasComputedColumnSql("`PrecioVenta` * `Cantidad`", stored: true);
    }
}

public class EstadoVentaConfiguracion : IEntityTypeConfiguration<EstadoVenta>
{
    public void Configure(EntityTypeBuilder<EstadoVenta> b)
    {
        b.ToTable("estados", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdEstado");
        b.Property(x => x.Nombre).HasColumnName("NombreEstado");
    }
}

public class VendedorConfiguracion : IEntityTypeConfiguration<Vendedor>
{
    public void Configure(EntityTypeBuilder<Vendedor> b)
    {
        b.ToTable("vendedores", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdVendedor").ValueGeneratedNever();
        b.Property(x => x.Nombre).HasColumnName("NombreVendedor");
    }
}

public class MedioPagoConfiguracion : IEntityTypeConfiguration<MedioPago>
{
    public void Configure(EntityTypeBuilder<MedioPago> b)
    {
        b.ToTable("mediospago", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdMedioPago");
        b.Property(x => x.Nombre).HasColumnName("NombreMedioPago");
    }
}

public class CuentaCobroConfiguracion : IEntityTypeConfiguration<CuentaCobro>
{
    public void Configure(EntityTypeBuilder<CuentaCobro> b)
    {
        b.ToTable("cuentascobro", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdCuentaCobro");
        b.Property(x => x.Nombre).HasColumnName("NombreCuenta");
    }
}

public class PagoConfiguracion : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> b)
    {
        b.ToTable("pagos", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdPago");
        b.Property(x => x.Fecha).HasColumnName("FechaPago").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
        b.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.IdPago).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Facturas).WithOne().HasForeignKey(x => x.IdPago).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PagoDetalleConfiguracion : IEntityTypeConfiguration<PagoDetalle>
{
    public void Configure(EntityTypeBuilder<PagoDetalle> b)
    {
        b.ToTable("pagosdetalle", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdPagoDetalle");
    }
}

public class PagoFacturaConfiguracion : IEntityTypeConfiguration<PagoFactura>
{
    public void Configure(EntityTypeBuilder<PagoFactura> b)
    {
        b.ToTable("pagosfacturas", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdPagoFactura");
    }
}
