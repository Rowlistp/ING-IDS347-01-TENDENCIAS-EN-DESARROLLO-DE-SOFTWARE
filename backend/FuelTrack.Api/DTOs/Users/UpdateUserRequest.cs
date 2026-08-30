using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Users;

public sealed class UpdateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "El nombre de usuario no puede contener solo espacios.")]
    public string NombreUsuario { get; set; } = string.Empty;

    public List<int> RolIds { get; set; } = [];
}
