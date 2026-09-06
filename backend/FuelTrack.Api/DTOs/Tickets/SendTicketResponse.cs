namespace FuelTrack.Api.DTOs.Tickets;

public sealed record SendTicketResponse(TicketResponse Ticket, int NotificacionesPendientes);
