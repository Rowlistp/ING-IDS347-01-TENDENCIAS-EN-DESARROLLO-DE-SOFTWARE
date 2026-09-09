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
    public int Intentos { get; set; }
    public int IntentosTotales { get; set; }
    public DateTime? ProximoIntentoUtc { get; set; }
    public DateTime? UltimoIntentoUtc { get; set; }
    public DateTime? EnviadaEnUtc { get; set; }
    public string? UltimoError { get; set; }
    public string? ProveedorMensajeId { get; set; }
    public string? ClaveIdempotencia { get; set; }
    public DateTime? BloqueadaHastaUtc { get; set; }
    public Guid? ReservaId { get; set; }
    public string? Mensaje { get; set; }
}
