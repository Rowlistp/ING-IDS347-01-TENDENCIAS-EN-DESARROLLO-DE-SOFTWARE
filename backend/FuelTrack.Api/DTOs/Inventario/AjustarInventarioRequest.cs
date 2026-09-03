using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Inventario;

public record AjustarInventarioRequest(
    [Required] int TanqueId,
    decimal Volumen,
    [Required, MaxLength(500)] string Observaciones
);
