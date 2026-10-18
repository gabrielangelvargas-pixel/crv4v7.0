using Crv4.Api.Modulos.Rubros;
using Crv4.Api.Modulos.Sistema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crv4.Api.Datos.Configuraciones;

public class RubroConfiguracion : IEntityTypeConfiguration<Rubro>
{
    public void Configure(EntityTypeBuilder<Rubro> b)
    {
        b.ToTable("rubros");
        b.Property(x => x.Nombre).HasMaxLength(100);
        b.HasIndex(x => x.Nombre).IsUnique();
        b.Property(x => x.Descripcion).HasMaxLength(500);
    }
}

public class NumeradorConfiguracion : IEntityTypeConfiguration<Numerador>
{
    public void Configure(EntityTypeBuilder<Numerador> b)
    {
        b.ToTable("numeradores");
        b.HasKey(x => x.Clave);
        b.Property(x => x.Clave).HasMaxLength(40);
        b.Property(x => x.Descripcion).HasMaxLength(200);

        b.HasData(
            new Numerador { Clave = Numeradores.Ventas, UltimoNumero = 0, Descripcion = "Número de venta" },
            new Numerador { Clave = Numeradores.Clientes, UltimoNumero = 0, Descripcion = "Código de cliente" });
    }
}
