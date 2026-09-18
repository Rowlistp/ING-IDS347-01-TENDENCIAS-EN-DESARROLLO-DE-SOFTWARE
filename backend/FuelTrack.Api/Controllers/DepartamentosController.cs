using System.Security.Claims;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Departamentos;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/departamentos")]
[Authorize]
public sealed class DepartamentosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public DepartamentosController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private bool TryGetCurrentUserId(out int userId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    [HttpGet]
    public async Task<ActionResult<List<DepartamentoDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Departamentos
            .AsNoTracking()
            .Select(d => new DepartamentoDto(d.Id, d.Nombre, d.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartamentoDto>> GetById(int id, CancellationToken ct)
    {
        var d = await _db.Departamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return d is null ? NotFound() : Ok(new DepartamentoDto(d.Id, d.Nombre, d.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<DepartamentoDto>> Create(
        SaveDepartamentoRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = new Departamento { Nombre = req.Nombre, Activo = req.Activo };
        _db.Departamentos.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("DEPARTAMENTO_CREADO", "Departamento", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), new { entity.Nombre, entity.Activo }, ct);

        var dto = new DepartamentoDto(entity.Id, entity.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<DepartamentoDto>> Update(
        int id, SaveDepartamentoRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Departamentos.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Nombre = req.Nombre;
        entity.Activo = req.Activo;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("DEPARTAMENTO_ACTUALIZADO", "Departamento", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), new { entity.Nombre, entity.Activo }, ct);

        return Ok(new DepartamentoDto(entity.Id, entity.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Departamentos.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.Empleados.AnyAsync(e => e.DepartamentoId == id && e.Activo, ct))
            return Conflict(new
            {
                code = "DEPARTAMENTO_CON_EMPLEADOS_ACTIVOS",
                message = "No se puede desactivar el departamento porque tiene empleados activos asignados."
            });

        if (await _db.Vehiculos.AnyAsync(v => v.DepartamentoId == id && v.Activo, ct))
            return Conflict(new
            {
                code = "DEPARTAMENTO_CON_VEHICULOS_ACTIVOS",
                message = "No se puede desactivar el departamento porque tiene vehículos activos asignados."
            });

        entity.Activo = false;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("DEPARTAMENTO_DESACTIVADO", "Departamento", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), null, ct);

        return NoContent();
    }
}
