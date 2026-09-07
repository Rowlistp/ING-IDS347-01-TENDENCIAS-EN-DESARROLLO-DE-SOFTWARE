using FuelTrack.Api.Notifications;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController, Route("api/v1/tickets/descargar")]
public sealed class TicketDownloadsController(TicketDeliveryLinkService links) : ControllerBase
{
    [HttpGet("{token}"), AllowAnonymous]
    public async Task<IActionResult> Download(string token, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        try
        {
            var result = await links.DownloadAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return File(result.Content, "application/pdf", result.FileName);
        }
        catch (TicketDomainException e) { return StatusCode(e.StatusCode, new { code = e.Code, message = e.Message }); }
    }
}
