namespace FuelTrack.Api.DTOs.Cierres;

public sealed record CierreDiarioResponse(
    int Id,
    DateOnly Fecha,
    int TotalDespachos,
    decimal TotalVolumenDespachado,
    decimal TotalInventarioFinal,
    decimal TotalDiferencias,
    int CreadoPorId,
    string CreadoPorNombre,
    DateTime CreadoEn,
    bool PdfDisponible,
    IReadOnlyList<CierreDiarioDetalleResponse> Detalles);
