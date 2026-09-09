using System.ComponentModel.DataAnnotations;
using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Solicitudes;

public record CreateSolicitudRecurrenteRequest(
    [Required, Range(0.0001, 999999.9999)] decimal CantidadSolicitada,
    [Required] Periodicidad Periodicidad,
    [Required] DateOnly FechaInicio,
    DateOnly? FechaFin,
    [Required] int EmpleadoId,
    [Required] int VehiculoId,
    [Required] int DepartamentoId,
    [Required] int TipoCombustibleId
);
