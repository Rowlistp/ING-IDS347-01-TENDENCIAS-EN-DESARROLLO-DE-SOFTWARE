using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Auth;

public sealed class LoginRequest
{
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    public string Contrasena { get; set; } = string.Empty;
}
