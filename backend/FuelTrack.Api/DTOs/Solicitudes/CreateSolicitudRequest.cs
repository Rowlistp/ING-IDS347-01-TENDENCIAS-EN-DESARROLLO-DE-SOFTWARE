using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Solicitudes;

public record CreateSolicitudRequest(
    [Required, Range(0.0001, 999999.9999)] decimal CantidadSolicitada,
    [Required] int EmpleadoId,
    [Required] int VehiculoId,
    [Required] int DepartamentoId,
    [Required] int TipoCombustibleId,
    DateTime? FechaVencimiento
);
