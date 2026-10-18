namespace Crv4.Api.Comun;

public record Paginado<T>(IReadOnlyList<T> Items, int Total, int Pagina, int TamanoPagina);

public static class Paginacion
{
    public const int TamanoPorDefecto = 50;
    public const int TamanoMaximo = 200;

    public static (int pagina, int tamano) Normalizar(int? pagina, int? tamano) =>
        (Math.Max(1, pagina ?? 1), Math.Clamp(tamano ?? TamanoPorDefecto, 1, TamanoMaximo));
}
