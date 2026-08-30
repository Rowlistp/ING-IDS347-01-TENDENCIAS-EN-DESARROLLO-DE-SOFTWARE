using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Users;

public sealed class UpdateUserRequest
{
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    public List<int> RolIds { get; set; } = [];
}
