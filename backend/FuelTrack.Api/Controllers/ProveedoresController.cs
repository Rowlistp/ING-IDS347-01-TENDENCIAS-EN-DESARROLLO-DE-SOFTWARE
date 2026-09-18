using System.Security.Claims;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Proveedores;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/proveedores")]
[Authorize]
public sealed class ProveedoresController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public ProveedoresController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private bool TryGetCurrentUserId(out int userId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    [HttpGet]
    public async Task<ActionResult<List<ProveedorDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Proveedores
            .AsNoTracking()
            .Select(p => new ProveedorDto(p.Id, p.Rnc, p.Nombre, p.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProveedorDto>> GetById(int id, CancellationToken ct)
    {
        var p = await _db.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? NotFound() : Ok(new ProveedorDto(p.Id, p.Rnc, p.Nombre, p.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<ProveedorDto>> Create(
        SaveProveedorRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        if (await _db.Proveedores.AnyAsync(p => p.Rnc == req.Rnc, ct))
            return Conflict(new { code = "RNC_DUPLICADO",
                message = "Ya existe un proveedor con ese RNC." });

        var entity = new Proveedor { Rnc = req.Rnc, Nombre = req.Nombre, Activo = req.Activo };
        _db.Proveedores.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("PROVEEDOR_CREADO", "Proveedor", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), new { entity.Rnc, entity.Nombre, entity.Activo }, ct);

        var dto = new ProveedorDto(entity.Id, entity.Rnc, entity.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<ProveedorDto>> Update(
        int id, SaveProveedorRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Proveedores.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.Proveedores.AnyAsync(p => p.Rnc == req.Rnc && p.Id != id, ct))
            return Conflict(new { code = "RNC_DUPLICADO",
                message = "Ya existe un proveedor con ese RNC." });

        entity.Rnc = req.Rnc;
        entity.Nombre = req.Nombre;
        entity.Activo = req.Activo;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("PROVEEDOR_ACTUALIZADO", "Proveedor", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), new { entity.Rnc, entity.Nombre, entity.Activo }, ct);

        return Ok(new ProveedorDto(entity.Id, entity.Rnc, entity.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Proveedores.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.RecepcionesCombustible.AnyAsync(r => r.ProveedorId == id, ct))
            return Conflict(new
            {
                code = "PROVEEDOR_CON_RECEPCIONES",
                message = "No se puede desactivar el proveedor porque tiene recepciones de combustible registradas."
            });

        entity.Activo = false;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("PROVEEDOR_DESACTIVADO", "Proveedor", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), null, ct);

        return NoContent();
    }
}
