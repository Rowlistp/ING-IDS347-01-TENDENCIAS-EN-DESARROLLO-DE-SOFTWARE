using System.Security.Claims;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Tanques;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/tanques")]
[Authorize]
public sealed class TanquesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public TanquesController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private bool TryGetCurrentUserId(out int userId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    [HttpGet]
    public async Task<ActionResult<List<TanqueDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Tanques
            .AsNoTracking()
            .Include(t => t.TipoCombustible)
            .Select(t => new TanqueDto(
                t.Id, t.Identificacion, t.Capacidad,
                t.Inventario != null ? t.Inventario.ExistenciaActual : 0m,
                t.NivelCritico, t.TipoCombustibleId, t.TipoCombustible.Nombre, t.Activo, t.TipoCombustible.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TanqueDto>> GetById(int id, CancellationToken ct)
    {
        var t = await _db.Tanques
            .AsNoTracking()
            .Include(x => x.TipoCombustible)
            .Include(x => x.Inventario)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        return Ok(new TanqueDto(
            t.Id, t.Identificacion, t.Capacidad, t.Inventario?.ExistenciaActual ?? 0m, t.NivelCritico,
            t.TipoCombustibleId, t.TipoCombustible.Nombre, t.Activo, t.TipoCombustible.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TanqueDto>> Create(SaveTanqueRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        if (!await _db.TiposCombustible.AnyAsync(t => t.Id == req.TipoCombustibleId, ct))
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_NOT_FOUND",
                message = "El tipo de combustible no existe." });

        if (!await _db.TiposCombustible.AnyAsync(t => t.Id == req.TipoCombustibleId && t.Activo, ct))
            return Conflict(new { code = "TIPO_COMBUSTIBLE_INACTIVO", message = "Seleccione un tipo de combustible activo." });

        if (await _db.Tanques.AnyAsync(t => t.Identificacion == req.Identificacion, ct))
            return Conflict(new { code = "IDENTIFICACION_DUPLICADA",
                message = "Ya existe un tanque con esa identificación." });

        var tanque = new Tanque
        {
            Identificacion    = req.Identificacion,
            Capacidad         = req.Capacidad,
            NivelActual       = 0,
            NivelCritico      = req.NivelCritico,
            TipoCombustibleId = req.TipoCombustibleId,
            Activo            = true
        };
        _db.Tanques.Add(tanque);

        var inventario = new Inventario
        {
            Tanque              = tanque,
            ExistenciaActual    = 0,
            Disponibilidad      = 0,
            UltimaActualizacion = DateTime.UtcNow
        };
        _db.Inventarios.Add(inventario);

        await _db.SaveChangesAsync(ct);
        await _db.Entry(tanque).Reference(t => t.TipoCombustible).LoadAsync(ct);

        await _audit.WriteAsync("TANQUE_CREADO", "Tanque", tanque.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            new { tanque.Identificacion, tanque.TipoCombustibleId, tanque.Activo }, ct);

        return CreatedAtAction(nameof(GetById), new { id = tanque.Id },
            new TanqueDto(tanque.Id, tanque.Identificacion, tanque.Capacidad, tanque.Inventario?.ExistenciaActual ?? 0m,
                tanque.NivelCritico, tanque.TipoCombustibleId, tanque.TipoCombustible.Nombre, tanque.Activo, tanque.TipoCombustible.Activo));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TanqueDto>> Update(int id, SaveTanqueRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var tanque = await _db.Tanques
            .Include(t => t.TipoCombustible)
            .Include(t => t.Inventario)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tanque is null) return NotFound();

        var tipoCombustible = await _db.TiposCombustible.FirstOrDefaultAsync(t => t.Id == req.TipoCombustibleId, ct);
        if (tipoCombustible is null)
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_NOT_FOUND",
                message = "El tipo de combustible no existe." });

        if (await _db.Tanques.AnyAsync(t => t.Identificacion == req.Identificacion && t.Id != id, ct))
            return Conflict(new { code = "IDENTIFICACION_DUPLICADA",
                message = "Ya existe un tanque con esa identificación." });

        if ((req.Activo ?? tanque.Activo) && !tipoCombustible.Activo)
            return Conflict(new { code = "TIPO_COMBUSTIBLE_INACTIVO",
                message = "No se puede activar el tanque porque su tipo de combustible está inactivo." });

        if (tanque.Activo && req.Activo == false && (tanque.Inventario?.ExistenciaActual ?? 0m) > 0)
            return Conflict(new { code = "TANQUE_CON_INVENTARIO",
                message = "No se puede desactivar el tanque porque tiene combustible en inventario." });

        var existencia = tanque.Inventario?.ExistenciaActual ?? 0m;
        if (req.Capacidad < existencia)
            return Conflict(new { code = "CAPACIDAD_EXCEDIDA", message = "La capacidad no puede ser menor que la existencia actual." });
        var tipoCambio = tanque.TipoCombustibleId != req.TipoCombustibleId;
        if (tipoCambio && existencia > 0)
            return Conflict(new { code = "TANQUE_CON_INVENTARIO", message = "Vacía el tanque antes de cambiar su tipo de combustible." });

        if (tanque.Activo && req.Activo == false && !User.IsInRole(Roles.Administrador))
            return StatusCode(StatusCodes.Status403Forbidden, new { code = "DESACTIVACION_NO_AUTORIZADA", message = "Solo un administrador puede desactivar este registro." });

        tanque.Identificacion    = req.Identificacion;
        tanque.Capacidad         = req.Capacidad;
        tanque.NivelCritico      = req.NivelCritico;
        tanque.TipoCombustibleId = req.TipoCombustibleId;
        if (req.Activo.HasValue) tanque.Activo = req.Activo.Value;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("TANQUE_ACTUALIZADO", "Tanque", tanque.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            new { tanque.Identificacion, tanque.TipoCombustibleId, tanque.Activo }, ct);

        if (tipoCambio)
            await _db.Entry(tanque).Reference(t => t.TipoCombustible).LoadAsync(ct);

        return Ok(new TanqueDto(tanque.Id, tanque.Identificacion, tanque.Capacidad, tanque.Inventario?.ExistenciaActual ?? 0m,
            tanque.NivelCritico, tanque.TipoCombustibleId, tanque.TipoCombustible.Nombre, tanque.Activo, tanque.TipoCombustible.Activo));
    }

    [HttpPut("{id:int}/activar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TanqueDto>> Activar(int id, CancellationToken ct)
    {
        var tanque = await _db.Tanques
            .Include(t => t.TipoCombustible)
            .Include(t => t.Inventario)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tanque is null) return NotFound();

        if (!tanque.TipoCombustible.Activo)
            return Conflict(new { code = "TIPO_COMBUSTIBLE_INACTIVO",
                message = "No se puede activar el tanque porque su tipo de combustible está inactivo." });

        tanque.Activo = true;
        await _db.SaveChangesAsync(ct);

        return Ok(new TanqueDto(tanque.Id, tanque.Identificacion, tanque.Capacidad, tanque.Inventario?.ExistenciaActual ?? 0m,
            tanque.NivelCritico, tanque.TipoCombustibleId, tanque.TipoCombustible.Nombre, tanque.Activo, tanque.TipoCombustible.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Tanques.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.Inventarios.AnyAsync(i => i.TanqueId == id && i.ExistenciaActual > 0, ct))
            return Conflict(new
            {
                code = "TANQUE_CON_INVENTARIO",
                message = "No se puede desactivar el tanque porque tiene combustible en inventario."
            });

        entity.Activo = false;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("TANQUE_DESACTIVADO", "Tanque", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), null, ct);

        return NoContent();
    }
}
