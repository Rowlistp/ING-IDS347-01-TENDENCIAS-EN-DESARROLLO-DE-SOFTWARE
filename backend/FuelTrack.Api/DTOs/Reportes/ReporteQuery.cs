namespace FuelTrack.Api.DTOs.Reportes;

public sealed record ReporteQuery(
    string Tipo,
    DateOnly? FechaDesde,
    DateOnly? FechaHasta,
    int? TanqueId,
    int Pagina,
    int TamanoPagina);
