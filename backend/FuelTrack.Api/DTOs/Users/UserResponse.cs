namespace FuelTrack.Api.DTOs.Users;

public sealed class UserResponse
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}
