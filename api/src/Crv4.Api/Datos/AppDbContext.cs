using Crv4.Api.Modulos.Clientes;
using Crv4.Api.Modulos.Productos;
using Crv4.Api.Modulos.Rubros;
using Crv4.Api.Modulos.Seguridad;
using Crv4.Api.Modulos.Sistema;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Datos;

/// <summary>
/// Trabaja sobre la base existente crv4_datos (la misma de Sistema03). Las fechas se guardan en hora local
/// del servidor, igual que los datos que ya hay.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    public DbSet<Rubro> Rubros => Set<Rubro>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Numerador> Numeradores => Set<Numerador>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
    }
}
