namespace FuelTrack.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public int SecurityVersion { get; set; } = 1;

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public Empleado? Empleado { get; set; }
    public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
    public ICollection<Despacho> DespachosOperados { get; set; } = new List<Despacho>();
    public ICollection<Auditoria> Auditorias { get; set; } = new List<Auditoria>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
