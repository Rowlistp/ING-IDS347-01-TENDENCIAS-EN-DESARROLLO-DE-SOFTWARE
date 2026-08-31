using System.ComponentModel.DataAnnotations;
using FuelTrack.Api.Security;

namespace FuelTrack.Api.DTOs.Users;

public sealed class CreateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "El nombre de usuario no puede contener solo espacios.")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    [StringLength(PasswordService.MaximumLength, MinimumLength = PasswordService.MinimumLength)]
    public string Contrasena { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "Debe asignar al menos un rol.")]
    public List<int> RolIds { get; set; } = [];
}
