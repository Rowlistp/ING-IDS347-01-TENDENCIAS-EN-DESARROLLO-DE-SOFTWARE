using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Notifications;

public static class NotificationQueue
{
    public static Notificacion New(string type, string reference, string channel, string recipient, string? text = null, string? period = null)
        => new() { Tipo = type, ReferenciaEvento = reference, Canal = channel, Destinatario = recipient.Trim(),
            ClaveIdempotencia = $"{type}:{reference}:{period}{channel}:{recipient.Trim()}",
            Estado = "PENDIENTE", FechaHora = DateTime.UtcNow, ProximoIntentoUtc = DateTime.UtcNow, Mensaje = text };

    public static Task<int> EnqueueAsync(AppDbContext db, Notificacion n, CancellationToken ct)
        => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Notificaciones" ("Tipo", "ReferenciaEvento", "Canal", "Destinatario", "ClaveIdempotencia", "Estado", "FechaHora", "ProximoIntentoUtc", "Mensaje", "Intentos", "IntentosTotales")
            VALUES ({n.Tipo}, {n.ReferenciaEvento}, {n.Canal}, {n.Destinatario}, {n.ClaveIdempotencia}, {n.Estado}, {n.FechaHora}, {n.ProximoIntentoUtc}, {n.Mensaje}, 0, 0)
            ON CONFLICT ("ClaveIdempotencia") DO NOTHING
            """, ct);

    public static Auditoria Audit(string name, Notificacion n, int? actor = null) => new()
    {
        Evento = name, EntidadAfectada = "Notificacion", IdentificadorRegistro = n.Id.ToString(), UsuarioId = actor,
        FechaHora = DateTime.UtcNow, DatosRelevantes = System.Text.Json.JsonSerializer.Serialize(new { n.Id, n.Tipo, n.Canal, n.Intentos, n.Estado })
    };
}
