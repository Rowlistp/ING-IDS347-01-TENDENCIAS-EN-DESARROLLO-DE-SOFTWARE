using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Estaciones;

public record SaveEstacionRequest(
    [Required, MaxLength(100)] string Nombre,
    bool Activo = true
);
