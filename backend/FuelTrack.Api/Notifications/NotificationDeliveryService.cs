using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuelTrack.Api.Notifications;

public sealed class NotificationDeliveryService(AppDbContext db, IOptions<NotificationOptions> configured)
{
    private readonly NotificationOptions options = configured.Value;
    public async Task<List<Notificacion>> ClaimAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var rows = await db.Notificaciones.FromSqlInterpolated($"""
            SELECT * FROM "Notificaciones"
            WHERE (("Estado" = 'PENDIENTE' AND ("ProximoIntentoUtc" IS NULL OR "ProximoIntentoUtc" <= {now}))
              OR ("Estado" = 'PROCESANDO' AND "BloqueadaHastaUtc" <= {now}))
              AND (("Canal" = 'EMAIL' AND {options.Smtp.Enabled}) OR ("Canal" = 'SMS' AND {options.Sms.Enabled}))
            ORDER BY "Id" LIMIT {options.BatchSize} FOR UPDATE SKIP LOCKED
            """).ToListAsync(ct);
        foreach (var n in rows)
        {
            n.Estado = "PROCESANDO"; n.ReservaId = Guid.NewGuid(); n.BloqueadaHastaUtc = now.AddSeconds(options.LockSeconds);
            n.UltimoIntentoUtc = now; n.Intentos++; n.IntentosTotales++;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
        return rows;
    }

    public async Task CompleteAsync(Notificacion claim, DeliveryResult result, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        Ticket? ticket = null;
        // Same ordering as F4: Ticket before notification. Serialize the all-channels transition.
        if (claim.Tipo == "TICKET_EMITIDO" && Guid.TryParse(claim.ReferenciaEvento, out var id))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Tickets\" WHERE \"Id\" = {id} FOR UPDATE", ct);
            ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == id, ct);
        }
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Notificaciones\" WHERE \"Id\" = {claim.Id} FOR UPDATE", ct);
        var n = await db.Notificaciones.SingleAsync(n => n.Id == claim.Id, ct);
        if (n.Estado != "PROCESANDO" || n.ReservaId != claim.ReservaId || n.BloqueadaHastaUtc <= DateTime.UtcNow) return;
        n.BloqueadaHastaUtc = null; n.ReservaId = null;
        if (result.Success)
        {
            n.Estado = "ENVIADA"; n.EnviadaEnUtc = DateTime.UtcNow; n.UltimoError = null;
            n.ProveedorMensajeId = result.ProviderId;
            db.Auditorias.Add(NotificationQueue.Audit("NOTIFICACION_ENVIADA", n));
        }
        else
        {
            // Never persist exception messages, payloads, auth headers or provider response bodies.
            n.UltimoError = System.Text.RegularExpressions.Regex.IsMatch(result.Code, "^[A-Z0-9_]{1,80}$") ? result.Code : "INTEGRACION_ERROR";
            n.Estado = result.Transient && n.Intentos < options.MaxAttempts ? "PENDIENTE" : "FALLIDA";
            n.ProximoIntentoUtc = n.Estado == "PENDIENTE" ? DateTime.UtcNow.AddSeconds(options.RetrySeconds(n.Intentos)) : null;
            db.Auditorias.Add(NotificationQueue.Audit("NOTIFICACION_FALLIDA", n));
            if (n.Estado == "FALLIDA")
            {
                db.Auditorias.Add(NotificationQueue.Audit("INTEGRACION_FALLIDA", n));
                var alert = NotificationQueue.New("INTEGRACION_FALLIDA", n.Id.ToString(), "INTERNO", "OPERACIONES", $"Notificación {n.Id}, canal {n.Canal}: requiere revisión en API.");
                alert.Estado = "ENVIADA";
                await NotificationQueue.EnqueueAsync(db, alert, ct);
            }
        }
        await db.SaveChangesAsync(ct);
        if (ticket?.Estado == EstadoTicket.Pendiente && ticket.FechaVencimiento > DateTime.UtcNow &&
            !await db.Notificaciones.AnyAsync(other => other.Tipo == "TICKET_EMITIDO" && other.ReferenciaEvento == claim.ReferenciaEvento && other.Estado != "ENVIADA", ct))
        {
            ticket.Estado = EstadoTicket.Enviado;
            db.Auditorias.Add(new Auditoria { Evento = "TICKET_ENVIADO", EntidadAfectada = "Ticket", IdentificadorRegistro = ticket.Id.ToString(), FechaHora = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }
}
