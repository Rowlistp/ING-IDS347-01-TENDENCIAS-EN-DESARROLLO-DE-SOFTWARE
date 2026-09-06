using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Dispatch;

public sealed record DispatchResponse(int DespachoId, Guid TicketId, string CodigoTicket,
    DateOnly Fecha, TimeOnly Hora, decimal GalonesServidos, int OperadorId, string Operador,
    int TanqueId, string TanqueIdentificacion, int EstacionId, string EstacionNombre,
    decimal InventarioRestante, decimal DisponibilidadRestante, EstadoTicket EstadoTicket, string? Observaciones);
