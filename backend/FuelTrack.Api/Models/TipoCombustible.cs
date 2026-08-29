namespace FuelTrack.Api.Models;

public class TipoCombustible
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<Tanque> Tanques { get; set; } = new List<Tanque>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<SolicitudCombustible> Solicitudes { get; set; } = new List<SolicitudCombustible>();
}
