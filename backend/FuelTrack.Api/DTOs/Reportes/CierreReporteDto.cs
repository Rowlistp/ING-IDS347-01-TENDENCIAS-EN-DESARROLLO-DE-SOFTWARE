namespace FuelTrack.Api.DTOs.Reportes;

public sealed record CierreReporteDto(
    int Id,
    DateOnly Fecha,
    int TotalDespachos,
    decimal VolumenDespachado,
    decimal InventarioFinal,
    decimal Diferencias,
    string CreadoPor);
