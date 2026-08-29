namespace FuelTrack.Api.Models;

public class Notificacion
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Destinatario { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string? ReferenciaEvento { get; set; }
}
