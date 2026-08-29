namespace FuelTrack.Api.Models;

public class Despacho
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public decimal GalonesServidos { get; set; }
    public string? Observaciones { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int OperadorId { get; set; }
    public Usuario Operador { get; set; } = null!;

    public int EstacionId { get; set; }
    public Estacion Estacion { get; set; } = null!;
}
