using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Reportes;

public sealed record TicketReporteDto(
    Guid Id,
    string Codigo,
    DateTime FechaCreacion,
    DateTime FechaVencimiento,
    EstadoTicket Estado,
    decimal CantidadAutorizada,
    string Empleado,
    string Vehiculo,
    string Departamento);
