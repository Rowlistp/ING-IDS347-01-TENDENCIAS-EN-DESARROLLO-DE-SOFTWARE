namespace FuelTrack.Api.DTOs.Audit;

public sealed class AuditEntryResponse
{
    public long Id { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string EntidadAfectada { get; set; } = string.Empty;
    public string IdentificadorRegistro { get; set; } = string.Empty;
    public DateTime FechaHoraUtc { get; set; }
    public string? DireccionIp { get; set; }
    public int? UsuarioId { get; set; }
}
