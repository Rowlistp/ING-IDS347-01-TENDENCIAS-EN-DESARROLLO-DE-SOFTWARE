using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.Models;

public class SolicitudRecurrente
{
    public int Id { get; set; }
    public decimal CantidadSolicitada { get; set; }
    public Periodicidad Periodicidad { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public bool Activa { get; set; } = true;
    public DateOnly? UltimaEjecucion { get; set; }

    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    public int TipoCombustibleId { get; set; }
    public TipoCombustible TipoCombustible { get; set; } = null!;
}
