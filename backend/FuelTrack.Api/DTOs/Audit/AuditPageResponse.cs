namespace FuelTrack.Api.DTOs.Audit;

public sealed class AuditPageResponse
{
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int Total { get; set; }
    public IReadOnlyCollection<AuditEntryResponse> Elementos { get; set; } = [];
}
