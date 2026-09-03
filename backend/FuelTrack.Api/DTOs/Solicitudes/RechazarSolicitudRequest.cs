using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Solicitudes;

public record RechazarSolicitudRequest(
    [Required, MaxLength(500)] string MotivoRechazo
);
