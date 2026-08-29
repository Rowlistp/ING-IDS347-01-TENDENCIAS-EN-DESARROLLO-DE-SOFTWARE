namespace FuelTrack.Api.Models;

public class Estacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<Despacho> Despachos { get; set; } = new List<Despacho>();
}
