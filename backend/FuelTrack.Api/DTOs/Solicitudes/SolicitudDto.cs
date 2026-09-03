using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Solicitudes;

public record SolicitudDto(
    int Id,
    decimal CantidadSolicitada,
    decimal? CantidadAutorizada,
    string TipoSolicitud,
    EstadoSolicitud Estado,
    DateTime FechaSolicitud,
    DateTime? FechaVencimiento,
    string? MotivoRechazo,
    int EmpleadoId,
    string EmpleadoNombre,
    int VehiculoId,
    string VehiculoPlaca,
    int DepartamentoId,
    string DepartamentoNombre,
    int TipoCombustibleId,
    string TipoCombustibleNombre
);
