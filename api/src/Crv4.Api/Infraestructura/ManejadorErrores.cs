using Crv4.Api.Comun;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crv4.Api.Infraestructura;

/// <summary>Convierte excepciones en respuestas ProblemDetails con un mensaje en castellano para la web.</summary>
public class ManejadorErrores(IProblemDetailsService problemas, ILogger<ManejadorErrores> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception error, CancellationToken ct)
    {
        var (estado, mensaje) = error switch
        {
            ErrorNegocio e => (e.Estado, e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Otro usuario modificó este dato. Recargá e intentá de nuevo."),
            DbUpdateException => (StatusCodes.Status409Conflict, "No se pudo guardar porque el dato está en uso o duplicado."),
            OperationCanceledException => (499, "Solicitud cancelada."),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Quedó registrado en el servidor."),
        };

        if (estado >= 500)
            log.LogError(error, "Error no controlado en {Metodo} {Ruta}", http.Request.Method, http.Request.Path);
        else if (error is DbUpdateException)
            log.LogWarning(error, "Error de base de datos en {Metodo} {Ruta}", http.Request.Method, http.Request.Path);

        http.Response.StatusCode = estado;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = error,
            ProblemDetails = new ProblemDetails { Status = estado, Title = mensaje },
        });
    }
}
