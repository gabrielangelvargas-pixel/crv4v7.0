using System.Threading.RateLimiting;
using Crv4.Api.Datos;
using Crv4.Api.Modulos.Auth;
using Crv4.Api.Modulos.Sistema;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Crv4.Api.Infraestructura;

public static class Servicios
{
    public const string PoliticaCors = "web";

    public static IServiceCollection AgregarCrv4(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<JwtOpciones>().Bind(config.GetSection(JwtOpciones.Seccion)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<SeguridadOpciones>().Bind(config.GetSection(SeguridadOpciones.Seccion)).ValidateDataAnnotations().ValidateOnStart();

        services.AddDbContext<AppDbContext>(o => ConfigurarBase(o, CadenaConexion(config, "Principal")));

        services.AddScoped<AuthServicio>();
        services.AddScoped<INumeradorServicio, NumeradorServicio>();
        services.AddSingleton<TokenServicio>();
        services.AddSingleton<PasswordServicio>();

        services.AddControllers();
        services.AddProblemDetails();
        services.AddExceptionHandler<ManejadorErrores>();

        AgregarAutenticacion(services);
        AgregarCors(services);
        AgregarLimites(services);
        return services;
    }

    public static void ConfigurarBase(DbContextOptionsBuilder o, string cadena) =>
        o.UseMySQL(cadena, my => my.MigrationsHistoryTable("__migraciones"))
         .UseSnakeCaseNamingConvention();

    public static string CadenaConexion(IConfiguration config, string nombre)
    {
        var cadena = config.GetConnectionString(nombre);
        if (string.IsNullOrWhiteSpace(cadena))
            throw new InvalidOperationException(
                $"Falta la cadena de conexión '{nombre}'. Configurala con la variable de entorno CRV4_ConnectionStrings__{nombre} " +
                "(ver deploy/servidor/Configurar-Secretos.ps1) o con 'dotnet user-secrets' en desarrollo.");
        return cadena;
    }

    private static void AgregarAutenticacion(IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOpciones>>((o, jwt) =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Emisor,
                    ValidAudience = jwt.Value.Audiencia,
                    IssuerSigningKey = TokenServicio.ClaveFirma(jwt.Value),
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = Claims.Nombre,
                    RoleClaimType = Claims.Rol,
                };
            });

        services.AddSingleton<IAuthorizationPolicyProvider, PoliticasPermisoProvider>();
        services.AddSingleton<IAuthorizationHandler, PermisoHandler>();
        services.AddSingleton<IAuthorizationHandler, CambioPasswordPendienteHandler>();
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                .AddRequirements(new CambioPasswordPendienteRequisito()).Build())
            // Todo endpoint sin atributo exige login: olvidarse un [Authorize] no deja nada abierto.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                .AddRequirements(new CambioPasswordPendienteRequisito()).Build());
    }

    private static void AgregarCors(IServiceCollection services)
    {
        // Se lee al construir la aplicación (no al registrar) para tomar también las variables de entorno.
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IConfiguration>((o, config) =>
        {
            var origenes = config.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
            o.AddPolicy(PoliticaCors, p => p
                .WithOrigins(origenes)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithHeaders("Authorization", "Content-Type")
                .SetPreflightMaxAge(TimeSpan.FromHours(1)));
        });
    }

    private static void AgregarLimites(IServiceCollection services)
    {
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(LimitesTasa.Login, http =>
            {
                var limite = http.RequestServices.GetRequiredService<IOptions<SeguridadOpciones>>().Value.SolicitudesLoginPorMinuto;
                return RateLimitPartition.GetFixedWindowLimiter(
                    http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
        });
    }
}
