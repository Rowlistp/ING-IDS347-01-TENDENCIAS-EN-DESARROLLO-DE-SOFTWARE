using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Users;

public sealed class CreateUserRequest
{
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    public string Contrasena { get; set; } = string.Empty;

    public List<int> RolIds { get; set; } = [];
}
