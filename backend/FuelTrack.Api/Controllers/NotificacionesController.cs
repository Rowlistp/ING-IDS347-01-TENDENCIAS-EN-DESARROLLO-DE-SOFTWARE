using System.Security.Claims;
using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Notifications;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController, Route("api/v1/notificaciones")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Auditor}")]
public sealed class NotificacionesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(string? estado, string? canal, string? tipo, DateTimeOffset? fechaDesde, DateTimeOffset? fechaHasta,
        int pagina = 1, int tamanoPagina = 20, CancellationToken ct = default)
    {
        if (pagina < 1 || pagina > 1000000 || tamanoPagina is < 1 or > 100 || fechaDesde > fechaHasta)
            return BadRequest(new { code = "FILTRO_INVALIDO", message = "Revisa filtros y paginación." });
        var query = db.Notificaciones.AsNoTracking().Where(n => (estado == null || n.Estado == estado) &&
            (canal == null || n.Canal == canal) && (tipo == null || n.Tipo == tipo) &&
            (fechaDesde == null || n.FechaHora >= fechaDesde.Value.UtcDateTime) && (fechaHasta == null || n.FechaHora <= fechaHasta.Value.UtcDateTime));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(n => n.Id).Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync(ct);
        return Ok(new { total, pagina, tamanoPagina, elementos = items.Select(ToResponse) });
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var n = await db.Notificaciones.AsNoTracking().SingleOrDefaultAsync(n => n.Id == id, ct);
        return n is null ? NotFound(new { code = "NOTIFICACION_NO_ENCONTRADA" }) : Ok(ToResponse(n));
    }
    [HttpPost("{id:int}/reintentar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<IActionResult> Retry(int id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (db.Database.IsNpgsql()) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Notificaciones\" WHERE \"Id\" = {id} FOR UPDATE", ct);
        var n = await db.Notificaciones.SingleOrDefaultAsync(n => n.Id == id, ct);
        if (n is null) return NotFound(new { code = "NOTIFICACION_NO_ENCONTRADA" });
        if (n.Estado != "FALLIDA" || n.Canal == "INTERNO") return Conflict(new { code = "REINTENTO_NO_PERMITIDO", message = "Solo se reintentan entregas fallidas." });
        n.Estado = "PENDIENTE"; n.Intentos = 0; n.ProximoIntentoUtc = DateTime.UtcNow; n.UltimoError = null;
        n.BloqueadaHastaUtc = null; n.ReservaId = null;
        db.Auditorias.Add(NotificationQueue.Audit("NOTIFICACION_REPROGRAMADA", n, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)));
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(ToResponse(n));
    }
    private static object ToResponse(Notificacion n) => new { n.Id, n.Tipo, n.Canal, n.Destinatario, n.Estado, n.FechaHora, n.ReferenciaEvento,
        n.Intentos, n.IntentosTotales, n.ProximoIntentoUtc, n.UltimoIntentoUtc, n.EnviadaEnUtc, n.UltimoError, n.Mensaje };
}
