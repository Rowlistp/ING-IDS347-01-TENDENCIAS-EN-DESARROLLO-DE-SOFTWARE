using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Tickets;

public sealed class CreateTicketRequest
{
    [Range(1, int.MaxValue)]
    public int SolicitudId { get; set; }

    [StringLength(10, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "El prefijo solo admite letras y números.")]
    public string? Prefijo { get; set; }
}
