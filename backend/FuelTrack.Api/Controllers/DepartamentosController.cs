using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Departamentos;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
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

    public DepartamentosController(AppDbContext db) => _db = db;

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
        var entity = new Departamento { Nombre = req.Nombre, Activo = req.Activo };
        _db.Departamentos.Add(entity);
        await _db.SaveChangesAsync(ct);
        var dto = new DepartamentoDto(entity.Id, entity.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<DepartamentoDto>> Update(
        int id, SaveDepartamentoRequest req, CancellationToken ct)
    {
        var entity = await _db.Departamentos.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Nombre = req.Nombre;
        entity.Activo = req.Activo;
        await _db.SaveChangesAsync(ct);
        return Ok(new DepartamentoDto(entity.Id, entity.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.Departamentos.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
