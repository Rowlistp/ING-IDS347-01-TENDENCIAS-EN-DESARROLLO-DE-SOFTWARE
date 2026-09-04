using System.ComponentModel.DataAnnotations;
using FuelTrack.Api.Security;

namespace FuelTrack.Api.DTOs.Auth;

public sealed class LoginRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "El nombre de usuario no puede contener solo espacios.")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    [StringLength(PasswordService.MaximumLength, MinimumLength = 1)]
    public string Contrasena { get; set; } = string.Empty;
}
