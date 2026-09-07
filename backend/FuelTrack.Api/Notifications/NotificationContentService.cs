using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Notifications;

public sealed class NotificationContentService(AppDbContext db, TicketPdfService pdf, TicketDeliveryLinkService links)
{
    public async Task<OutgoingNotification> ComposeAsync(Notificacion notification, CancellationToken ct)
    {
        var key = notification.ClaveIdempotencia ?? $"LEGACY:{notification.Id}";
        if (notification.Tipo != "TICKET_EMITIDO")
            return new(notification.Id, key, notification.Destinatario, $"FuelTrack — {notification.Tipo}", notification.Mensaje ?? notification.Tipo);
        if (!Guid.TryParse(notification.ReferenciaEvento, out var id)) throw new TicketDomainException(404, "TICKET_NO_ENCONTRADO", "Referencia inválida.");
        var ticket = await db.Tickets.AsNoTracking().Include(t => t.Empleado).Include(t => t.Vehiculo)
            .Include(t => t.Departamento).Include(t => t.TipoCombustible).SingleOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new TicketDomainException(404, "TICKET_NO_ENCONTRADO", "Ticket inexistente.");
        if (ticket.FechaVencimiento <= DateTime.UtcNow || ticket.Estado is EstadoTicket.Vencido or EstadoTicket.Consumido or EstadoTicket.Anulado)
            throw new TicketDomainException(410, "TICKET_NO_DISPONIBLE", "Ticket no disponible para entrega.");
        var response = TicketService.ToResponse(ticket);
        var summary = $"FuelTrack\nTicket {response.Codigo}\n{response.CantidadAutorizada:0.####} galones {response.TipoCombustibleNombre}\nVence: {response.FechaVencimiento:O}";
        if (notification.Canal == "SMS")
            return new(notification.Id, key, notification.Destinatario, response.Codigo, summary + "\nDescarga segura: " + await links.CreateAsync(ticket, ct));
        return new(notification.Id, key, notification.Destinatario, $"FuelTrack — Ticket {response.Codigo}",
            summary + $"\nEmpleado: {response.EmpleadoNombre}\nVehículo: {response.VehiculoPlaca}\nEstado: {response.Estado}\nPresente el PDF con QR al despachador. La validación es en línea; visualizar no consume el ticket.",
            pdf.Generate(response, ticket.QrCodePng), $"ticket-{response.Codigo}.pdf");
    }
}
