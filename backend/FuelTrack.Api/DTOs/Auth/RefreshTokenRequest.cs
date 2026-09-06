using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public string RefreshToken { get; set; } = string.Empty;
}
