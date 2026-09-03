using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.TiposCombustible;

public record SaveTipoCombustibleRequest(
    [Required, MaxLength(50)] string Nombre,
    bool Activo = true
);
