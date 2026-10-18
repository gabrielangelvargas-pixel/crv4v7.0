namespace Crv4.Api.Infraestructura;

/// <summary>
/// La web se abre desde Hostinger (internet) y llama a esta API en la red local.
/// Chrome y Edge piden permiso para eso (Private Network Access): en la consulta previa
/// mandan "Access-Control-Request-Private-Network: true" y esperan que el servidor lo acepte.
/// Solo se responde si el origen está permitido por CORS.
/// </summary>
public class AccesoRedPrivada(RequestDelegate siguiente, IConfiguration config)
{
    private readonly HashSet<string> _origenes =
        new(config.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [], StringComparer.OrdinalIgnoreCase);

    public Task InvokeAsync(HttpContext http)
    {
        if (HttpMethods.IsOptions(http.Request.Method) &&
            http.Request.Headers["Access-Control-Request-Private-Network"] == "true" &&
            _origenes.Contains(http.Request.Headers.Origin.ToString()))
        {
            http.Response.OnStarting(() =>
            {
                http.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
                return Task.CompletedTask;
            });
        }
        return siguiente(http);
    }
}
