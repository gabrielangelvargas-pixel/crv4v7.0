namespace Crv4.Api.Modulos.Rubros;

/// <summary>Tabla rubros de crv4_datos. Los productos se relacionan por <see cref="Codigo"/> (columna IdRubro).</summary>
public class Rubro
{
    public int Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? ImagenPrincipal { get; set; }
    public int? IdPadre { get; set; }
    public Rubro? Padre { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime? FechaModificacion { get; set; }
    public long? IdCategoriaWeb { get; set; }
}
