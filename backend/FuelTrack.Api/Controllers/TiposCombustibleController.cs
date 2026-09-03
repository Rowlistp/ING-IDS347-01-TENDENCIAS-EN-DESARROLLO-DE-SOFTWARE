using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.TiposCombustible;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
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

    public TiposCombustibleController(AppDbContext db) => _db = db;

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
        if (await _db.TiposCombustible.AnyAsync(t => t.Nombre == req.Nombre, ct))
            return Conflict(new { code = "NOMBRE_DUPLICADO",
                message = "Ya existe un tipo de combustible con ese nombre." });

        var entity = new TipoCombustible { Nombre = req.Nombre, Activo = req.Activo };
        _db.TiposCombustible.Add(entity);
        await _db.SaveChangesAsync(ct);
        var dto = new TipoCombustibleDto(entity.Id, entity.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TipoCombustibleDto>> Update(
        int id, SaveTipoCombustibleRequest req, CancellationToken ct)
    {
        var entity = await _db.TiposCombustible.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.TiposCombustible.AnyAsync(t => t.Nombre == req.Nombre && t.Id != id, ct))
            return Conflict(new { code = "NOMBRE_DUPLICADO",
                message = "Ya existe un tipo de combustible con ese nombre." });

        entity.Nombre = req.Nombre;
        entity.Activo = req.Activo;
        await _db.SaveChangesAsync(ct);
        return Ok(new TipoCombustibleDto(entity.Id, entity.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.TiposCombustible.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
