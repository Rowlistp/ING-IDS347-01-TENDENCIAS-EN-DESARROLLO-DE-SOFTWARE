using System.ComponentModel.DataAnnotations;
using FuelTrack.Api.Security;

namespace FuelTrack.Api.DTOs.Auth;

public sealed class ResetPasswordRequest
{
    [Range(1, int.MaxValue)]
    public int UsuarioId { get; set; }

    [Required]
    [MinLength(PasswordService.MinimumLength)]
    public string NuevaContrasena { get; set; } = string.Empty;
}
