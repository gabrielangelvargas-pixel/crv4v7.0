using Crv4.Api.Modulos.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crv4.Api.Datos.Configuraciones;

public class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.Property(x => x.NombreUsuario).HasMaxLength(50);
        b.HasIndex(x => x.NombreUsuario).IsUnique();
        b.Property(x => x.Nombre).HasMaxLength(120);
        b.Property(x => x.PasswordHash).HasMaxLength(200);
        b.HasOne(x => x.Rol).WithMany(x => x.Usuarios).HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RolConfiguracion : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> b)
    {
        b.ToTable("roles");
        b.Property(x => x.Nombre).HasMaxLength(60);
        b.HasIndex(x => x.Nombre).IsUnique();
        b.Property(x => x.Descripcion).HasMaxLength(250);

        b.HasData(new Rol { Id = RolesSistema.AdministradorId, Nombre = "Administrador", Descripcion = "Acceso completo", EsSistema = true });
    }
}

public class PermisoConfiguracion : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> b)
    {
        b.ToTable("permisos");
        b.HasKey(x => x.Codigo);
        b.Property(x => x.Codigo).HasMaxLength(60);
        b.Property(x => x.Modulo).HasMaxLength(60);
        b.Property(x => x.Descripcion).HasMaxLength(200);

        b.HasData(Permisos.Todos);
    }
}

public class RolPermisoConfiguracion : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> b)
    {
        b.ToTable("roles_permisos");
        b.HasKey(x => new { x.RolId, x.PermisoCodigo });
        b.HasOne(x => x.Rol).WithMany(x => x.Permisos).HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Permiso).WithMany().HasForeignKey(x => x.PermisoCodigo).OnDelete(DeleteBehavior.Cascade);

        b.HasData(Permisos.Todos.Select(p => new RolPermiso { RolId = RolesSistema.AdministradorId, PermisoCodigo = p.Codigo }));
    }
}

public class SesionConfiguracion : IEntityTypeConfiguration<Sesion>
{
    public void Configure(EntityTypeBuilder<Sesion> b)
    {
        b.ToTable("sesiones");
        b.Property(x => x.TokenHash).HasMaxLength(64).IsFixedLength();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.UsuarioId, x.RevocadaEn });
        b.Property(x => x.Ip).HasMaxLength(45);
        b.Property(x => x.Dispositivo).HasMaxLength(250);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
