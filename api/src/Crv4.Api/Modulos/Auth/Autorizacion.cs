using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Crv4.Api.Modulos.Auth;

/// <summary>
/// Exige un permiso del catálogo: <c>[TienePermiso(Permisos.RubrosEditar)]</c>.
/// Con varios permisos alcanza con tener cualquiera de ellos.
/// </summary>
public sealed class TienePermisoAttribute(params string[] permisos) : AuthorizeAttribute(Prefijo + string.Join(Separador, permisos))
{
    public const string Prefijo = "perm:";
    public const char Separador = '|';
}

/// <summary>Marca acciones que se pueden usar aunque el usuario deba cambiar su contraseña.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PermitirConCambioPendienteAttribute : Attribute;

public sealed class PermisoRequisito(IReadOnlyList<string> permisos) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permisos { get; } = permisos;
}

public sealed class PermisoHandler : AuthorizationHandler<PermisoRequisito>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermisoRequisito requisito)
    {
        if (requisito.Permisos.Any(p => context.User.HasClaim(Claims.Permiso, p)))
            context.Succeed(requisito);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Bloquea todo excepto lo marcado con <see cref="PermitirConCambioPendienteAttribute"/>
/// mientras el usuario tenga pendiente el cambio de contraseña.
/// </summary>
public sealed class CambioPasswordPendienteRequisito : IAuthorizationRequirement;

public sealed class CambioPasswordPendienteHandler : AuthorizationHandler<CambioPasswordPendienteRequisito>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CambioPasswordPendienteRequisito requisito)
    {
        var pendiente = context.User.HasClaim(Claims.DebeCambiarPassword, "1");
        var permitido = context.Resource is HttpContext http &&
                        http.GetEndpoint()?.Metadata.GetMetadata<PermitirConCambioPendienteAttribute>() is not null;
        if (!pendiente || permitido)
            context.Succeed(requisito);
        return Task.CompletedTask;
    }
}

public sealed class PoliticasPermisoProvider(IOptions<AuthorizationOptions> opciones) : DefaultAuthorizationPolicyProvider(opciones)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string nombre)
    {
        if (!nombre.StartsWith(TienePermisoAttribute.Prefijo, StringComparison.Ordinal))
            return await base.GetPolicyAsync(nombre);

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new CambioPasswordPendienteRequisito())
            .AddRequirements(new PermisoRequisito(nombre[TienePermisoAttribute.Prefijo.Length..].Split(TienePermisoAttribute.Separador)))
            .Build();
    }
}

public static class ClaimsPrincipalExtensiones
{
    public static int UsuarioId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(Claims.UsuarioId) ?? throw new InvalidOperationException("Usuario sin id."));

    public static long? SesionId(this ClaimsPrincipal user) =>
        long.TryParse(user.FindFirstValue(Claims.SesionId), out var id) ? id : null;
}
