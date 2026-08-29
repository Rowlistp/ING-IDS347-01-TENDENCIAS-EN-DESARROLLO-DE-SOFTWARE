namespace FuelTrack.Api.Models;

public class Auditoria
{
    public long Id { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string EntidadAfectada { get; set; } = string.Empty;
    public string IdentificadorRegistro { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string? DireccionIp { get; set; }
    public string? DatosRelevantes { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
