using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.Models;

public class Ticket
{
    public Guid Id { get; set; }
    public int NumeroSecuencial { get; set; }
    public string Prefijo { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public EstadoTicket Estado { get; set; }
    public decimal CantidadAutorizada { get; set; }
    public string HashSeguridad { get; set; } = string.Empty;
    // Contiene exclusivamente SHA-256(token QR), nunca el token en claro.
    public string TokenValidacion { get; set; } = string.Empty;
    public string FirmaDigital { get; set; } = string.Empty;
    public byte[] QrCodePng { get; set; } = [];
    public string? MotivoAnulacion { get; set; }

    public int TipoCombustibleId { get; set; }
    public TipoCombustible TipoCombustible { get; set; } = null!;

    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    public int? SolicitudId { get; set; }
    public SolicitudCombustible? Solicitud { get; set; }

    public Despacho? Despacho { get; set; }
}
