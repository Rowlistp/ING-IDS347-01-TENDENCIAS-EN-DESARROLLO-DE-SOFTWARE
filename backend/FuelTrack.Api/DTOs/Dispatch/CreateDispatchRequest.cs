using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Dispatch;

public sealed class CreateDispatchRequest
{
    public Guid TicketId { get; set; }
    [Required, StringLength(8192)] public string QrPayload { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int TanqueId { get; set; }
    [Range(1, int.MaxValue)] public int EstacionId { get; set; }
    public decimal GalonesServidos { get; set; }
    [StringLength(500)] public string? Observaciones { get; set; }
}
