using System.ComponentModel.DataAnnotations;
using FuelTrack.Api.Security;

namespace FuelTrack.Api.DTOs.Users;

public sealed class CreateUserRequest
{
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    [MinLength(PasswordService.MinimumLength)]
    public string Contrasena { get; set; } = string.Empty;

    public List<int> RolIds { get; set; } = [];
}
