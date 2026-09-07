namespace FuelTrack.Api.Models;

public sealed class TicketDeliveryLink
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime CreadoEnUtc { get; set; }
    public DateTime ExpiraEnUtc { get; set; }
    public DateTime? RevocadoEnUtc { get; set; }
    public DateTime? UltimoAccesoUtc { get; set; }
}
