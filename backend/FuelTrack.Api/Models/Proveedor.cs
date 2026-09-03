namespace FuelTrack.Api.Models;

public class Proveedor
{
    public int Id { get; set; }
    public string Rnc { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<RecepcionCombustible> Recepciones { get; set; } = new List<RecepcionCombustible>();
}
