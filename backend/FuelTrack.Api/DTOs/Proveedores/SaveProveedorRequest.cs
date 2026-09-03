using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Proveedores;

public record SaveProveedorRequest(
    [Required, MaxLength(20)]  string Rnc,
    [Required, MaxLength(150)] string Nombre,
    bool Activo = true
);
