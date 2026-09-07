using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Reportes;

public sealed record SolicitudReporteDto(
    int Id,
    DateTime FechaSolicitud,
    string Empleado,
    string Vehiculo,
    string Departamento,
    string TipoCombustible,
    decimal CantidadSolicitada,
    decimal? CantidadAutorizada,
    EstadoSolicitud Estado);
