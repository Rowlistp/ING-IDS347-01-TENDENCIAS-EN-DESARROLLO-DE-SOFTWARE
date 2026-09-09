using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Solicitudes;

public record SolicitudRecurrenteDto(
    int Id,
    decimal CantidadSolicitada,
    Periodicidad Periodicidad,
    DateOnly FechaInicio,
    DateOnly? FechaFin,
    bool Activa,
    DateOnly? UltimaEjecucion,
    int EmpleadoId, string EmpleadoNombre,
    int VehiculoId, string VehiculoPlaca,
    int DepartamentoId, string DepartamentoNombre,
    int TipoCombustibleId, string TipoCombustibleNombre
);
