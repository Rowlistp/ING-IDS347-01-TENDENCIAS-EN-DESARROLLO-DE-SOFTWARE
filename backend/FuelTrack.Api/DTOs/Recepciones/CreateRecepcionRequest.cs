using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Recepciones;

public record CreateRecepcionRequest(
    [Required] int ProveedorId,
    [Required] int TanqueId,
    [Required, MaxLength(100)] string NumeroFactura,
    [Required, Range(0.0001, 999999.9999)] decimal VolumenRecibido,
    [Required] DateTime Fecha
);
