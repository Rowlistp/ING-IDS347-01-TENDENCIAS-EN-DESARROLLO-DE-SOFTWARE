using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Tanques;

public record SaveTanqueRequest(
    [Required, MaxLength(50)]        string Identificacion,
    [Range(0.0001, 999999.9999)]     decimal Capacidad,
    [Range(0, 999999.9999)]          decimal NivelCritico,
    [Required]                        int TipoCombustibleId
);
