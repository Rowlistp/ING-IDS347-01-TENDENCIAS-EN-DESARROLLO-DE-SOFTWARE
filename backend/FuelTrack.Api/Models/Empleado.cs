namespace FuelTrack.Api.Models;

public class Empleado
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public ICollection<SolicitudCombustible> Solicitudes { get; set; } = new List<SolicitudCombustible>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
