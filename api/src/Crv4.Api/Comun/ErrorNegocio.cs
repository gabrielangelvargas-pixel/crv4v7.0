namespace Crv4.Api.Comun;

/// <summary>Error esperado (validación, regla de negocio). Se devuelve al usuario con su mensaje.</summary>
public class ErrorNegocio(string mensaje, int estado = StatusCodes.Status400BadRequest) : Exception(mensaje)
{
    public int Estado { get; } = estado;
}

public class NoEncontrado(string mensaje) : ErrorNegocio(mensaje, StatusCodes.Status404NotFound);
