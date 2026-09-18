using System.Security.Claims;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.TiposCombustible;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/tipos-combustible")]
[Authorize]
public sealed class TiposCombustibleController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public TiposCombustibleController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private bool TryGetCurrentUserId(out int userId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    [HttpGet]
    public async Task<ActionResult<List<TipoCombustibleDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.TiposCombustible
            .AsNoTracking()
            .Select(t => new TipoCombustibleDto(t.Id, t.Nombre, t.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TipoCombustibleDto>> GetById(int id, CancellationToken ct)
    {
        var t = await _db.TiposCombustible
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? NotFound() : Ok(new TipoCombustibleDto(t.Id, t.Nombre, t.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TipoCombustibleDto>> Create(
        SaveTipoCombustibleRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        if (await _db.TiposCombustible.AnyAsync(t => t.Nombre == req.Nombre, ct))
            return Conflict(new { code = "NOMBRE_DUPLICADO",
                message = "Ya existe un tipo de combustible con ese nombre." });

        var entity = new TipoCombustible { Nombre = req.Nombre, Activo = req.Activo };
        _db.TiposCombustible.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("TIPO_COMBUSTIBLE_CREADO", "TipoCombustible", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), new { entity.Nombre, entity.Activo }, ct);

        var dto = new TipoCombustibleDto(entity.Id, entity.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TipoCombustibleDto>> Update(
        int id, SaveTipoCombustibleRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.TiposCombustible.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.TiposCombustible.AnyAsync(t => t.Nombre == req.Nombre && t.Id != id, ct))
            return Conflict(new { code = "NOMBRE_DUPLICADO",
                message = "Ya existe un tipo de combustible con ese nombre." });

        entity.Nombre = req.Nombre;
        entity.Activo = req.Activo;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("TIPO_COMBUSTIBLE_ACTUALIZADO", "TipoCombustible", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), new { entity.Nombre, entity.Activo }, ct);

        return Ok(new TipoCombustibleDto(entity.Id, entity.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.TiposCombustible.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.Tanques.AnyAsync(t => t.TipoCombustibleId == id && t.Activo, ct))
            return Conflict(new
            {
                code = "TIPO_COMBUSTIBLE_CON_TANQUES_ACTIVOS",
                message = "No se puede desactivar el tipo de combustible porque tiene tanques activos asignados."
            });

        if (await _db.SolicitudesCombustible.AnyAsync(s => s.TipoCombustibleId == id &&
            (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.Aprobada), ct))
            return Conflict(new
            {
                code = "TIPO_COMBUSTIBLE_CON_SOLICITUDES_ACTIVAS",
                message = "No se puede desactivar el tipo de combustible porque tiene solicitudes pendientes o aprobadas."
            });

        if (await _db.Tickets.AnyAsync(t => t.TipoCombustibleId == id &&
            t.Estado != EstadoTicket.Vencido &&
            t.Estado != EstadoTicket.Consumido &&
            t.Estado != EstadoTicket.Anulado, ct))
            return Conflict(new
            {
                code = "TIPO_COMBUSTIBLE_CON_TICKETS_ACTIVOS",
                message = "No se puede desactivar el tipo de combustible porque tiene tickets activos."
            });

        entity.Activo = false;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("TIPO_COMBUSTIBLE_DESACTIVADO", "TipoCombustible", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), null, ct);

        return NoContent();
    }
}
