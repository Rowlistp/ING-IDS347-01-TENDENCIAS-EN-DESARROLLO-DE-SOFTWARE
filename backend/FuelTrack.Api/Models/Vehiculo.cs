namespace FuelTrack.Api.Models;

public class Vehiculo
{
    public int Id { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Ficha { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Año { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal CapacidadTanque { get; set; }
    public decimal Odometro { get; set; }
    public bool Activo { get; set; } = true;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    public ICollection<SolicitudCombustible> Solicitudes { get; set; } = new List<SolicitudCombustible>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
