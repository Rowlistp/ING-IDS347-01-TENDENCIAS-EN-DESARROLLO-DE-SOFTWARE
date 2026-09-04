using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Tickets;

public sealed record TicketResponse(
    Guid Id,
    string Codigo,
    int NumeroSecuencial,
    string Prefijo,
    DateTime FechaCreacion,
    DateTime FechaVencimiento,
    EstadoTicket Estado,
    decimal CantidadAutorizada,
    int EmpleadoId,
    string EmpleadoNombre,
    int VehiculoId,
    string VehiculoPlaca,
    int DepartamentoId,
    string DepartamentoNombre,
    int TipoCombustibleId,
    string TipoCombustibleNombre,
    int? SolicitudId,
    bool QrDisponible,
    string? MotivoAnulacion);
