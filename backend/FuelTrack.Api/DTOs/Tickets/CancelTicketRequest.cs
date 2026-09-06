using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Tickets;

public sealed class CancelTicketRequest
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Motivo { get; set; } = string.Empty;
}
