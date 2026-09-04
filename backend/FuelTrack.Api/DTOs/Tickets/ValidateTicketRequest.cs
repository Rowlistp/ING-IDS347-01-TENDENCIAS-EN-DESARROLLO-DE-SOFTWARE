using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Tickets;

public sealed class ValidateTicketRequest
{
    [Required]
    [StringLength(8192, MinimumLength = 1)]
    public string QrPayload { get; set; } = string.Empty;
}
