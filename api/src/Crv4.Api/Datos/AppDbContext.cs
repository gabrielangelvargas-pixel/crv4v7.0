using Crv4.Api.Modulos.Rubros;
using Crv4.Api.Modulos.Seguridad;
using Crv4.Api.Modulos.Sistema;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Datos;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    public DbSet<Rubro> Rubros => Set<Rubro>();
    public DbSet<Numerador> Numeradores => Set<Numerador>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todas las fechas se guardan en UTC con precisión de microsegundos.
        configurationBuilder.Properties<DateTime>().HavePrecision(6);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;
        foreach (var entrada in ChangeTracker.Entries())
        {
            if (entrada.State == EntityState.Added && entrada.Metadata.FindProperty("CreadoEn") is not null)
                entrada.Property("CreadoEn").CurrentValue = ahora;
            if ((entrada.State is EntityState.Added or EntityState.Modified) && entrada.Metadata.FindProperty("ActualizadoEn") is not null)
                entrada.Property("ActualizadoEn").CurrentValue = ahora;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
