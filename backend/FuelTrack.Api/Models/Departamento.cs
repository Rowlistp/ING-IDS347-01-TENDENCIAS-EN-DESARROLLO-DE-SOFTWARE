namespace FuelTrack.Api.Models;

public class Departamento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
    public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    public ICollection<SolicitudCombustible> Solicitudes { get; set; } = new List<SolicitudCombustible>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
