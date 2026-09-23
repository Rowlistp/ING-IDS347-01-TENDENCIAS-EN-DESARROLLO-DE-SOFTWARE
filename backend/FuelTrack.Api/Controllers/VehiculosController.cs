using System.Security.Claims;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Vehiculos;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/vehiculos")]
[Authorize]
public sealed class VehiculosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public VehiculosController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private bool TryGetCurrentUserId(out int userId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    [HttpGet]
    public async Task<ActionResult<List<VehiculoDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Vehiculos
            .AsNoTracking()
            .Include(v => v.Departamento)
            .Include(v => v.TipoCombustible)
            .Select(v => new VehiculoDto(
                v.Id, v.Placa, v.Ficha, v.Marca, v.Modelo, v.Año,
                v.Tipo, v.CapacidadTanque, v.Odometro,
                v.DepartamentoId, v.Departamento.Nombre, v.Activo,
                v.TipoCombustibleId, v.TipoCombustible != null ? v.TipoCombustible.Nombre : null))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehiculoDto>> GetById(int id, CancellationToken ct)
    {
        var v = await _db.Vehiculos
            .AsNoTracking()
            .Include(x => x.Departamento)
            .Include(x => x.TipoCombustible)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return NotFound();
        return Ok(ToDto(v));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<VehiculoDto>> Create(
        SaveVehiculoRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

       var departamento = await _db.Departamentos
    .FirstOrDefaultAsync(d => d.Id == req.DepartamentoId, ct);

if (departamento is null)
    return BadRequest(new
    {
        code = "DEPARTAMENTO_NOT_FOUND",
        message = "El departamento no existe."
    });

if (!departamento.Activo)
    return BadRequest(new
    {
        code = "DEPARTAMENTO_INACTIVO",
        message = "No se puede asignar un vehículo a un departamento inactivo."
    });

        if (!req.TipoCombustibleId.HasValue)
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_REQUERIDO",
                message = "El tipo de combustible del vehículo es obligatorio." });

        var errorCombustible = await ValidateTipoCombustibleAsync(req.TipoCombustibleId.Value, ct);
        if (errorCombustible is not null) return errorCombustible;

        if (await _db.Vehiculos.AnyAsync(v => v.Placa == req.Placa, ct))
            return Conflict(new { code = "PLACA_DUPLICADA",
                message = "La placa ya está registrada." });

        if (await _db.Vehiculos.AnyAsync(v => v.Ficha == req.Ficha, ct))
            return Conflict(new { code = "FICHA_DUPLICADA",
                message = "La ficha ya está registrada." });

        var entity = new Vehiculo
        {
            Placa           = req.Placa,
            Ficha           = req.Ficha,
            Marca           = req.Marca,
            Modelo          = req.Modelo,
            Año             = req.Año,
            Tipo            = req.Tipo,
            CapacidadTanque = req.CapacidadTanque,
            Odometro        = req.Odometro,
            DepartamentoId  = req.DepartamentoId,
            TipoCombustibleId = req.TipoCombustibleId,
            Activo          = true
        };
        _db.Vehiculos.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(entity).Reference(v => v.Departamento).LoadAsync(ct);
        await _db.Entry(entity).Reference(v => v.TipoCombustible).LoadAsync(ct);

        await _audit.WriteAsync("VEHICULO_CREADO", "Vehiculo", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            new { entity.Placa, entity.DepartamentoId, entity.TipoCombustibleId, entity.Activo }, ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<VehiculoDto>> Update(
        int id, SaveVehiculoRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Vehiculos
            .Include(v => v.Departamento)
            .Include(v => v.TipoCombustible)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null) return NotFound();

        var departamento = await _db.Departamentos
    .FirstOrDefaultAsync(d => d.Id == req.DepartamentoId, ct);

if (departamento is null)
    return BadRequest(new
    {
        code = "DEPARTAMENTO_NOT_FOUND",
        message = "El departamento no existe."
    });

if (!departamento.Activo)
    return BadRequest(new
    {
        code = "DEPARTAMENTO_INACTIVO",
        message = "No se puede asignar un vehículo a un departamento inactivo."
    });

        // Omitido en PUT = conservar el combustible actual (igual que Activo).
        if (req.TipoCombustibleId.HasValue && req.TipoCombustibleId != entity.TipoCombustibleId)
        {
            var errorCombustible = await ValidateTipoCombustibleAsync(req.TipoCombustibleId.Value, ct);
            if (errorCombustible is not null) return errorCombustible;
        }

        if (await _db.Vehiculos.AnyAsync(v => v.Placa == req.Placa && v.Id != id, ct))
            return Conflict(new { code = "PLACA_DUPLICADA",
                message = "La placa ya está registrada." });

        if (await _db.Vehiculos.AnyAsync(v => v.Ficha == req.Ficha && v.Id != id, ct))
            return Conflict(new { code = "FICHA_DUPLICADA",
                message = "La ficha ya está registrada." });

        if (entity.Activo && req.Activo == false)
        {
            if (!User.IsInRole(Roles.Administrador))
                return StatusCode(StatusCodes.Status403Forbidden, new { code = "DESACTIVACION_NO_AUTORIZADA", message = "Solo un administrador puede desactivar este registro." });
            var conflict = await ValidateDeactivationAsync(id, ct);
            if (conflict is not null) return conflict;
        }

        entity.Placa           = req.Placa;
        entity.Ficha           = req.Ficha;
        entity.Marca           = req.Marca;
        entity.Modelo          = req.Modelo;
        entity.Año             = req.Año;
        entity.Tipo            = req.Tipo;
        entity.CapacidadTanque = req.CapacidadTanque;
        entity.Odometro        = req.Odometro;
        entity.DepartamentoId  = req.DepartamentoId;
        if (req.TipoCombustibleId.HasValue) entity.TipoCombustibleId = req.TipoCombustibleId;
        if (req.Activo.HasValue) entity.Activo = req.Activo.Value;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("VEHICULO_ACTUALIZADO", "Vehiculo", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            new { entity.Placa, entity.DepartamentoId, entity.TipoCombustibleId, entity.Activo }, ct);

        if (entity.Departamento.Id != req.DepartamentoId)
            await _db.Entry(entity).Reference(v => v.Departamento).LoadAsync(ct);
        if (entity.TipoCombustibleId.HasValue && entity.TipoCombustible?.Id != entity.TipoCombustibleId)
            await _db.Entry(entity).Reference(v => v.TipoCombustible).LoadAsync(ct);

        return Ok(ToDto(entity));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Vehiculos.FindAsync([id], ct);
        if (entity is null) return NotFound();

        var conflict = await ValidateDeactivationAsync(id, ct);
        if (conflict is not null) return conflict;

        entity.Activo = false;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("VEHICULO_DESACTIVADO", "Vehiculo", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), null, ct);

        return NoContent();
    }

    private static VehiculoDto ToDto(Vehiculo v) => new(
        v.Id, v.Placa, v.Ficha, v.Marca, v.Modelo, v.Año,
        v.Tipo, v.CapacidadTanque, v.Odometro,
        v.DepartamentoId, v.Departamento.Nombre, v.Activo,
        v.TipoCombustibleId, v.TipoCombustible?.Nombre);

    private async Task<ActionResult?> ValidateTipoCombustibleAsync(int tipoCombustibleId, CancellationToken ct)
    {
        var tipo = await _db.TiposCombustible.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tipoCombustibleId, ct);
        if (tipo is null)
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_NOT_FOUND", message = "El tipo de combustible no existe." });
        if (!tipo.Activo)
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_INACTIVO", message = "No se puede asignar un tipo de combustible inactivo al vehículo." });
        return null;
    }

    private async Task<ConflictObjectResult?> ValidateDeactivationAsync(int id, CancellationToken ct)
    {
        if (await _db.SolicitudesCombustible.AnyAsync(s => s.VehiculoId == id &&
            (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.Aprobada), ct))
            return Conflict(new
            {
                code = "VEHICULO_CON_SOLICITUDES_ACTIVAS",
                message = "No se puede desactivar el vehículo porque tiene solicitudes pendientes o aprobadas."
            });

        return null;
    }
}
