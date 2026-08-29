namespace FuelTrack.Api.Models;

public class Inventario
{
    public int Id { get; set; }
    public decimal ExistenciaActual { get; set; }
    public decimal Disponibilidad { get; set; }
    public DateTime UltimaActualizacion { get; set; }

    public int TanqueId { get; set; }
    public Tanque Tanque { get; set; } = null!;
}
