namespace FuelTrack.Api.DTOs.Reportes;

public sealed record ReportePageResponse(
    string Tipo,
    int Total,
    int Pagina,
    int TamanoPagina,
    IReadOnlyList<object> Items);
