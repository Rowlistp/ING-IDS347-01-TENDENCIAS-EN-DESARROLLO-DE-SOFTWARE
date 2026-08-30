using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Empleados;

public record SaveEmpleadoRequest(
    [Required, MaxLength(20)]               string Codigo,
    [Required, MaxLength(150)]              string NombreCompleto,
    [Required, MaxLength(20)]               string Cedula,
    [Required, MaxLength(100)]              string Cargo,
    [Required, MaxLength(150), EmailAddress] string Correo,
    [Required, MaxLength(20)]               string Telefono,
    [Required]                              int DepartamentoId,
    bool Activo = true
);
