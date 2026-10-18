using Crv4.Api.Modulos.Clientes;
using Crv4.Api.Modulos.Productos;
using Crv4.Api.Modulos.Rubros;
using Crv4.Api.Modulos.Sistema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crv4.Api.Datos.Configuraciones;

public class RubroConfiguracion : IEntityTypeConfiguration<Rubro>
{
    public void Configure(EntityTypeBuilder<Rubro> b)
    {
        b.ToTable("rubros", t => t.ExcludeFromMigrations());
        b.Property(x => x.Codigo).HasColumnName("IdRubro");
        b.Property(x => x.Nombre).HasColumnName("NombreRubro");
        b.HasIndex(x => x.Codigo).IsUnique();
        b.HasOne(x => x.Padre).WithMany().HasForeignKey(x => x.IdPadre).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductoConfiguracion : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> b)
    {
        b.ToTable("productos", t => t.ExcludeFromMigrations());
        b.HasKey(x => x.Codigo);
        b.Property(x => x.Codigo).HasColumnName("IdProducto");
        b.Property(x => x.PrecioCompra).HasColumnName("precioCompra");
        b.Property(x => x.PrecioCosto).HasComputedColumnSql("`precioCompra` * (1 - (`Descuento` / 100))", stored: true);
        b.Property(x => x.Utilidad).HasPrecision(10, 3);
        b.Property(x => x.FechaAlta).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
    }
}

public class ClienteConfiguracion : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("clientes", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdCliente").ValueGeneratedNever();
        b.Property(x => x.Nombre).HasColumnName("NombreCliente");
        b.Property(x => x.Celular1).HasColumnName("NumCelular1");
        b.Property(x => x.Celular2).HasColumnName("NumCelular2");
        b.Property(x => x.TelefonoFijo).HasColumnName("NumFijo");
        b.Property(x => x.FechaAlta).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
    }
}

/// <summary>Tabla nueva de la 7.0: la crea la migración.</summary>
public class NumeradorConfiguracion : IEntityTypeConfiguration<Numerador>
{
    public void Configure(EntityTypeBuilder<Numerador> b)
    {
        b.ToTable("numeradores");
        b.HasKey(x => x.Clave);
        b.Property(x => x.Clave).HasMaxLength(40);
        b.Property(x => x.Descripcion).HasMaxLength(200);
        b.HasData(
            new Numerador { Clave = Numeradores.Ventas, UltimoNumero = 0, Descripcion = "Número de venta (facturas.IdFactura)" },
            new Numerador { Clave = Numeradores.Clientes, UltimoNumero = 0, Descripcion = "Número de cliente (clientes.IdCliente)" });
    }
}
