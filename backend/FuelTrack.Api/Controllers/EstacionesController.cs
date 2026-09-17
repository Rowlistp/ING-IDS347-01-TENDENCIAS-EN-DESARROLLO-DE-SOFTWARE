using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Estaciones;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController, Route("api/v1/estaciones")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Despachador},{Roles.Auditor},{Roles.Consulta}")]
public sealed class EstacionesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EstacionDto>>> GetAll(CancellationToken ct)
    {
        var list = await db.Estaciones
            .AsNoTracking()
            .Where(e => e.Activo)
            .OrderBy(e => e.Nombre)
            .Select(e => new EstacionDto(e.Id, e.Nombre, e.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EstacionDto>> GetById(int id, CancellationToken ct)
    {
        var e = await db.Estaciones
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return e is null ? NotFound() : Ok(new EstacionDto(e.Id, e.Nombre, e.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<EstacionDto>> Create(
        SaveEstacionRequest req, CancellationToken ct)
    {
        var entity = new Estacion { Nombre = req.Nombre, Activo = req.Activo };
        db.Estaciones.Add(entity);
        await db.SaveChangesAsync(ct);
        var dto = new EstacionDto(entity.Id, entity.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<EstacionDto>> Update(
        int id, SaveEstacionRequest req, CancellationToken ct)
    {
        var entity = await db.Estaciones.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Nombre = req.Nombre;
        entity.Activo = req.Activo;
        await db.SaveChangesAsync(ct);
        return Ok(new EstacionDto(entity.Id, entity.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await db.Estaciones.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await db.Despachos.AnyAsync(d => d.EstacionId == id, ct))
            return Conflict(new
            {
                code = "ESTACION_CON_DESPACHOS",
                message = "No se puede desactivar la estación porque tiene despachos asociados."
            });

        entity.Activo = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
