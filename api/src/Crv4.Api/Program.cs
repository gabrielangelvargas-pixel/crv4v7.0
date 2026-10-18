using Crv4.Api.Comandos;
using Crv4.Api.Infraestructura;
using Microsoft.Extensions.Logging.EventLog;

var builder = WebApplication.CreateBuilder(args);

// Las claves (MySQL, JWT, certificado) nunca están en el repositorio. En el servidor se leen de
// C:\CRV4\config\crv4.secretos.json (lo crea deploy/servidor/Configurar-Secretos.ps1, solo lo lee el servicio).
// También se aceptan variables de entorno con prefijo CRV4_ (por ejemplo CRV4_ConnectionStrings__Principal)
// y, en desarrollo, 'dotnet user-secrets'.
var archivoSecretos = Environment.GetEnvironmentVariable("CRV4_SECRETOS")
                      ?? (OperatingSystem.IsWindows() ? @"C:\CRV4\config\crv4.secretos.json" : null);
if (archivoSecretos is not null)
    builder.Configuration.AddJsonFile(archivoSecretos, optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables(prefix: "CRV4_");

builder.Host.UseWindowsService(o => o.ServiceName = "CRV4Api");
if (OperatingSystem.IsWindows())
#pragma warning disable CA1416 // solo se ejecuta en Windows
    builder.Services.Configure<EventLogSettings>(o => o.SourceName = "CRV4Api");
#pragma warning restore CA1416
builder.Services.AgregarCrv4(builder.Configuration);

var app = builder.Build();

if (Comandos.EsComando(args))
    return await Comandos.EjecutarAsync(app, args);

if (!await Migraciones.VerificarAsync(app.Services, app.Logger))
    return 2;

app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();

app.UseMiddleware<AccesoRedPrivada>();
app.UseCors(Servicios.PoliticaCors);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
return 0;

public partial class Program;
