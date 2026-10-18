using Crv4.Api.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Crv4.Api.Datos;

/// <summary>
/// Solo para 'dotnet ef migrations add / script'. Generar una migración no se conecta a la base,
/// por eso alcanza con una cadena ficticia si no hay una configurada.
/// </summary>
public class FabricaDisenio : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable("CRV4_ConnectionStrings__Migraciones")
                     ?? "server=localhost;database=crv4_datos;user=diseno";
        var opciones = new DbContextOptionsBuilder<AppDbContext>();
        Servicios.ConfigurarBase(opciones, cadena);
        return new AppDbContext(opciones.Options);
    }
}
