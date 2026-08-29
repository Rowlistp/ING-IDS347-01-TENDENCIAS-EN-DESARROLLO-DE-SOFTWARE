using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Auth;

public sealed class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
