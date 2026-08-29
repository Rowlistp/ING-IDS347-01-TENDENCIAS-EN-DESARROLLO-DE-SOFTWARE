using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Auth;

public sealed class ResetPasswordRequest
{
    [Range(1, int.MaxValue)]
    public int UsuarioId { get; set; }

    [Required]
    public string NuevaContrasena { get; set; } = string.Empty;
}
