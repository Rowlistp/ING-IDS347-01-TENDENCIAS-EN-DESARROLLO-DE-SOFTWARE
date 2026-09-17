namespace FuelTrack.Api.DTOs.Reportes;

public sealed record ReporteQuery(
    string Tipo,
    DateOnly? FechaDesde,
    DateOnly? FechaHasta,
    int? TanqueId,
    int? EmpleadoId,
    int? VehiculoId,
    int? DepartamentoId,
    int Pagina,
    int TamanoPagina);
