using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuelTrack.Api.Notifications;

public sealed class NotificationRuleService(AppDbContext db, IOptions<NotificationOptions> configured)
{
    private readonly NotificationOptions options = configured.Value;
    public async Task RunOnceAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddHours(options.TicketExpiringSoonHours);
        // Keyset batches prevent starvation and avoid loading the whole ticket history.
        int cursor = 0;
        while (true)
        {
            var candidates = await db.Tickets.AsNoTracking().Where(t => t.NumeroSecuencial > cursor && t.FechaVencimiento <= threshold &&
                t.Estado != EstadoTicket.Consumido && t.Estado != EstadoTicket.Anulado &&
                (t.Estado != EstadoTicket.Vencido || t.FechaVencimiento <= now) &&
                ((t.FechaVencimiento <= now && t.Estado != EstadoTicket.Vencido) ||
                 (t.Empleado.Correo != "" && !db.Notificaciones.Any(n => n.ReferenciaEvento == t.Id.ToString() && n.Canal == "EMAIL" && n.Tipo == (t.FechaVencimiento <= now ? "TICKET_VENCIDO" : "TICKET_PROXIMO_VENCER"))) ||
                 (t.Empleado.Telefono != null && t.Empleado.Telefono != "" && !db.Notificaciones.Any(n => n.ReferenciaEvento == t.Id.ToString() && n.Canal == "SMS" && n.Tipo == (t.FechaVencimiento <= now ? "TICKET_VENCIDO" : "TICKET_PROXIMO_VENCER")))))
                .OrderBy(t => t.NumeroSecuencial).Take(100).Select(t => new { t.Id, t.NumeroSecuencial }).ToListAsync(ct);
            if (candidates.Count == 0) break;
            foreach (var candidate in candidates)
            {
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Tickets\" WHERE \"Id\" = {candidate.Id} FOR UPDATE", ct);
                var ticket = await db.Tickets.Include(t => t.Empleado).SingleAsync(t => t.Id == candidate.Id, ct);
                if (ticket.Estado is EstadoTicket.Consumido or EstadoTicket.Anulado) { db.ChangeTracker.Clear(); continue; }
                var expired = ticket.FechaVencimiento <= now;
                if (expired) ticket.Estado = EstadoTicket.Vencido;
                var type = expired ? "TICKET_VENCIDO" : "TICKET_PROXIMO_VENCER";
                var message = $"FuelTrack {type}: {ticket.Prefijo}-{ticket.FechaCreacion.Year}-{ticket.NumeroSecuencial:000000}. Vencimiento UTC: {ticket.FechaVencimiento:O}.";
                if (!string.IsNullOrWhiteSpace(ticket.Empleado.Correo)) await NotificationQueue.EnqueueAsync(db, NotificationQueue.New(type, ticket.Id.ToString("D"), "EMAIL", ticket.Empleado.Correo, message), ct);
                if (!string.IsNullOrWhiteSpace(ticket.Empleado.Telefono)) await NotificationQueue.EnqueueAsync(db, NotificationQueue.New(type, ticket.Id.ToString("D"), "SMS", ticket.Empleado.Telefono, message), ct);
                await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); db.ChangeTracker.Clear();
            }
            cursor = candidates[^1].NumeroSecuencial;
        }
        if (options.Operations.Emails.Length + options.Operations.Phones.Length == 0) return;
        var period = (new DateTimeOffset(now).ToUnixTimeSeconds() / (options.LowInventoryPeriodHours * 3600L)).ToString() + ":";
        var low = await db.Inventarios.AsNoTracking().Where(i => i.Tanque.Activo && i.ExistenciaActual <= i.Tanque.NivelCritico)
            .Select(i => new { i.TanqueId, i.Tanque.Identificacion, i.ExistenciaActual, i.Tanque.NivelCritico }).ToListAsync(ct);
        foreach (var tank in low)
            await OperationsAsync("INVENTARIO_BAJO", tank.TanqueId.ToString(), $"Tanque {tank.Identificacion}: existencia {tank.ExistenciaActual}, nivel crítico {tank.NivelCritico}.", period, ct);
        var adjustments = await db.MovimientosInventario.AsNoTracking().Where(m => m.Tipo == TipoMovimiento.Ajuste &&
            !db.Notificaciones.Any(n => n.Tipo == "AJUSTE_INVENTARIO" && n.ReferenciaEvento == m.Id.ToString()))
            .OrderBy(m => m.Id).Take(500).Select(m => new { m.Id, m.Tanque.Identificacion, m.Volumen, m.FechaHora, m.UsuarioId, m.ReferenciaOperacion }).ToListAsync(ct);
        foreach (var movement in adjustments)
            await OperationsAsync("AJUSTE_INVENTARIO", movement.Id.ToString(), $"Ajuste {movement.Id}, tanque {movement.Identificacion}, volumen {movement.Volumen}, UTC {movement.FechaHora:O}, actor {movement.UsuarioId}, referencia {movement.ReferenciaOperacion}.", null, ct);
    }
    private async Task OperationsAsync(string type, string reference, string text, string? period, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        foreach (var email in options.Operations.Emails.Distinct()) await NotificationQueue.EnqueueAsync(db, NotificationQueue.New(type, reference, "EMAIL", email, text, period), ct);
        foreach (var phone in options.Operations.Phones.Distinct()) await NotificationQueue.EnqueueAsync(db, NotificationQueue.New(type, reference, "SMS", phone, text, period), ct);
        await tx.CommitAsync(ct);
    }
}
