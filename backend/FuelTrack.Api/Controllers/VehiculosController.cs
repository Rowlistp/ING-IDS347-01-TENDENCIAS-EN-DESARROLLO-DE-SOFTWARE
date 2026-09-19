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
            .Select(v => new VehiculoDto(
                v.Id, v.Placa, v.Ficha, v.Marca, v.Modelo, v.Año,
                v.Tipo, v.CapacidadTanque, v.Odometro,
                v.DepartamentoId, v.Departamento.Nombre, v.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehiculoDto>> GetById(int id, CancellationToken ct)
    {
        var v = await _db.Vehiculos
            .AsNoTracking()
            .Include(x => x.Departamento)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return NotFound();
        return Ok(new VehiculoDto(
            v.Id, v.Placa, v.Ficha, v.Marca, v.Modelo, v.Año,
            v.Tipo, v.CapacidadTanque, v.Odometro,
            v.DepartamentoId, v.Departamento.Nombre, v.Activo));
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
            Activo          = true
        };
        _db.Vehiculos.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(entity).Reference(v => v.Departamento).LoadAsync(ct);

        await _audit.WriteAsync("VEHICULO_CREADO", "Vehiculo", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            new { entity.Placa, entity.DepartamentoId, entity.Activo }, ct);

        var dto = new VehiculoDto(
            entity.Id, entity.Placa, entity.Ficha, entity.Marca, entity.Modelo, entity.Año,
            entity.Tipo, entity.CapacidadTanque, entity.Odometro,
            entity.DepartamentoId, entity.Departamento.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<VehiculoDto>> Update(
        int id, SaveVehiculoRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Vehiculos
            .Include(v => v.Departamento)
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

        if (await _db.Vehiculos.AnyAsync(v => v.Placa == req.Placa && v.Id != id, ct))
            return Conflict(new { code = "PLACA_DUPLICADA",
                message = "La placa ya está registrada." });

        if (await _db.Vehiculos.AnyAsync(v => v.Ficha == req.Ficha && v.Id != id, ct))
            return Conflict(new { code = "FICHA_DUPLICADA",
                message = "La ficha ya está registrada." });

        if (entity.Activo && req.Activo == false && await _db.SolicitudesCombustible.AnyAsync(s => s.VehiculoId == id &&
            (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.Aprobada), ct))
            return Conflict(new
            {
                code = "VEHICULO_CON_SOLICITUDES_ACTIVAS",
                message = "No se puede desactivar el vehículo porque tiene solicitudes pendientes o aprobadas."
            });

        entity.Placa           = req.Placa;
        entity.Ficha           = req.Ficha;
        entity.Marca           = req.Marca;
        entity.Modelo          = req.Modelo;
        entity.Año             = req.Año;
        entity.Tipo            = req.Tipo;
        entity.CapacidadTanque = req.CapacidadTanque;
        entity.Odometro        = req.Odometro;
        entity.DepartamentoId  = req.DepartamentoId;
        if (req.Activo.HasValue) entity.Activo = req.Activo.Value;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("VEHICULO_ACTUALIZADO", "Vehiculo", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            new { entity.Placa, entity.DepartamentoId, entity.Activo }, ct);

        if (entity.Departamento.Id != req.DepartamentoId)
            await _db.Entry(entity).Reference(v => v.Departamento).LoadAsync(ct);

        return Ok(new VehiculoDto(
            entity.Id, entity.Placa, entity.Ficha, entity.Marca, entity.Modelo, entity.Año,
            entity.Tipo, entity.CapacidadTanque, entity.Odometro,
            entity.DepartamentoId, entity.Departamento.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Vehiculos.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.SolicitudesCombustible.AnyAsync(s => s.VehiculoId == id &&
            (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.Aprobada), ct))
            return Conflict(new
            {
                code = "VEHICULO_CON_SOLICITUDES_ACTIVAS",
                message = "No se puede desactivar el vehículo porque tiene solicitudes pendientes o aprobadas."
            });

        entity.Activo = false;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("VEHICULO_DESACTIVADO", "Vehiculo", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), null, ct);

        return NoContent();
    }
}
