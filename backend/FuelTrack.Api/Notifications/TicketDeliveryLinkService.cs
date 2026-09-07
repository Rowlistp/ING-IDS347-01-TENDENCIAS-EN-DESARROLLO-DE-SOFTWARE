using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Services;

namespace FuelTrack.Api.Notifications;

public sealed class TicketDeliveryLinkService(AppDbContext db, TicketPdfService pdf, IOptions<NotificationOptions> options)
{
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
    public async Task<string> CreateAsync(Ticket ticket, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (ticket.FechaVencimiento <= now || ticket.Estado is EstadoTicket.Consumido or EstadoTicket.Anulado)
            throw new TicketDomainException(410, "LINK_REVOCADO", "El ticket no está disponible.");
        var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        db.TicketDeliveryLinks.Add(new TicketDeliveryLink { Id = Guid.NewGuid(), TicketId = ticket.Id,
            TokenHash = Hash(raw), CreadoEnUtc = now,
            ExpiraEnUtc = new[] { now.AddHours(options.Value.TicketLinkHours), ticket.FechaVencimiento }.Min() });
        await db.SaveChangesAsync(ct);
        return options.Value.PublicBaseUrl.TrimEnd('/') + "/api/v1/tickets/descargar/" + raw;
    }
    public async Task<(string FileName, byte[] Content)> DownloadAsync(string token, string? ip, CancellationToken ct)
    {
        if (token.Length != 43 || token.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_'))
            throw new TicketDomainException(404, "LINK_INVALIDO", "Enlace inválido.");
        var hash = Hash(token);
        var link = await db.TicketDeliveryLinks.Include(l => l.Ticket).ThenInclude(t => t.Empleado)
            .Include(l => l.Ticket).ThenInclude(t => t.Vehiculo).Include(l => l.Ticket).ThenInclude(t => t.Departamento)
            .Include(l => l.Ticket).ThenInclude(t => t.TipoCombustible).SingleOrDefaultAsync(l => l.TokenHash == hash, ct)
            ?? throw new TicketDomainException(404, "LINK_INVALIDO", "Enlace inválido.");
        var now = DateTime.UtcNow;
        var code = link.RevocadoEnUtc != null || link.Ticket.Estado is EstadoTicket.Consumido or EstadoTicket.Anulado ? "LINK_REVOCADO"
            : link.ExpiraEnUtc <= now || link.Ticket.FechaVencimiento <= now || link.Ticket.Estado == EstadoTicket.Vencido ? "LINK_EXPIRADO" : "OK";
        db.Auditorias.Add(new Auditoria { Evento = "LINK_TICKET_DESCARGADO", EntidadAfectada = "TicketDeliveryLink",
            IdentificadorRegistro = link.Id.ToString(), DireccionIp = ip, FechaHora = now,
            DatosRelevantes = System.Text.Json.JsonSerializer.Serialize(new { link.TicketId, DeliveryLinkId = link.Id, Resultado = code }) });
        if (code == "OK") link.UltimoAccesoUtc = now;
        await db.SaveChangesAsync(ct);
        if (code != "OK") throw new TicketDomainException(410, code, "Enlace no disponible.");
        var response = TicketService.ToResponse(link.Ticket);
        return ($"ticket-{response.Codigo}.pdf", pdf.Generate(response, link.Ticket.QrCodePng));
    }
}
