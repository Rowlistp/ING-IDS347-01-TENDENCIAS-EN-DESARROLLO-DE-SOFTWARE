using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Departamentos;

public record SaveDepartamentoRequest(
    [Required, MaxLength(100)] string Nombre,
    bool Activo = true
);
