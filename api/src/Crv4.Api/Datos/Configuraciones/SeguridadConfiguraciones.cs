using Crv4.Api.Modulos.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crv4.Api.Datos.Configuraciones;

// Las tablas existentes se marcan ExcludeFromMigrations: EF las usa pero nunca las crea ni las modifica.
// Cualquier cambio sobre ellas va como SQL explícito dentro de una migración.

public class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios_sistema", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdUsuario");
        b.Property(x => x.Nombre).HasColumnName("NombreCompleto");
        b.Property(x => x.UltimoIngreso).HasColumnName("UltimoLogin");
        b.Property(x => x.FechaAlta).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
        b.HasIndex(x => x.NombreUsuario).IsUnique();
    }
}

public class UsuarioRolConfiguracion : IEntityTypeConfiguration<UsuarioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioRol> b)
    {
        b.ToTable("usuarios_roles", t => t.ExcludeFromMigrations());
        b.HasKey(x => new { x.UsuarioId, x.RolId });
        b.Property(x => x.UsuarioId).HasColumnName("IdUsuario");
        b.Property(x => x.RolId).HasColumnName("IdRol");
        b.HasOne(x => x.Usuario).WithMany(x => x.Roles).HasForeignKey(x => x.UsuarioId);
        b.HasOne(x => x.Rol).WithMany(x => x.Usuarios).HasForeignKey(x => x.RolId);
    }
}

public class RolConfiguracion : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> b)
    {
        b.ToTable("roles", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdRol");
        b.Property(x => x.Nombre).HasColumnName("NombreRol");
        b.Property(x => x.FechaAlta).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
        b.Ignore(x => x.EsAdministrador);
    }
}

public class PermisoConfiguracion : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> b)
    {
        b.ToTable("permisos", t => t.ExcludeFromMigrations());
        b.Property(x => x.Id).HasColumnName("IdPermiso");
        b.HasIndex(x => x.Codigo).IsUnique();
    }
}

public class RolPermisoConfiguracion : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> b)
    {
        b.ToTable("roles_permisos", t => t.ExcludeFromMigrations());
        b.HasKey(x => new { x.RolId, x.PermisoId });
        b.Property(x => x.RolId).HasColumnName("IdRol");
        b.Property(x => x.PermisoId).HasColumnName("IdPermiso");
        b.HasOne(x => x.Rol).WithMany(x => x.Permisos).HasForeignKey(x => x.RolId);
        b.HasOne(x => x.Permiso).WithMany().HasForeignKey(x => x.PermisoId);
    }
}

/// <summary>Tabla nueva de la 7.0: la crea la migración.</summary>
public class SesionConfiguracion : IEntityTypeConfiguration<Sesion>
{
    public void Configure(EntityTypeBuilder<Sesion> b)
    {
        b.ToTable("sesiones");
        b.Property(x => x.Id).HasColumnName("IdSesion");
        b.Property(x => x.UsuarioId).HasColumnName("IdUsuario");
        b.Property(x => x.TokenHash).HasMaxLength(64).IsFixedLength();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.UsuarioId, x.RevocadaEn });
        b.Property(x => x.Ip).HasMaxLength(45);
        b.Property(x => x.Dispositivo).HasMaxLength(250);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
