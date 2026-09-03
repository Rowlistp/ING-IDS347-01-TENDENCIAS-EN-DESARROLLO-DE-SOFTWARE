using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Inventario;

public record TransferirRequest(
    [Required] int TanqueOrigenId,
    [Required] int TanqueDestinoId,
    [Required, Range(0.0001, 999999.9999)] decimal Volumen,
    [MaxLength(500)] string? Observaciones
);
