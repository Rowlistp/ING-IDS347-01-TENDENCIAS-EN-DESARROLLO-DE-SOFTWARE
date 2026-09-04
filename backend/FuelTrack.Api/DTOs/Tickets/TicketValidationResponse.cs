namespace FuelTrack.Api.DTOs.Tickets;

public sealed record TicketValidationResponse(
    bool Valido,
    string Codigo,
    string Mensaje,
    TicketResponse? Ticket)
{
    public static TicketValidationResponse Invalid(string code, string message)
        => new(false, code, message, null);

    public static TicketValidationResponse Valid(TicketResponse ticket)
        => new(true, "TICKET_VALIDO", "El ticket es válido.", ticket);
}
