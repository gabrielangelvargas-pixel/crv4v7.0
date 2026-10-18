namespace Crv4.Api.Modulos.Rubros;

public class Rubro
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}
