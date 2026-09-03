using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Proveedores;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
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

    public ProveedoresController(AppDbContext db) => _db = db;

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
        if (await _db.Proveedores.AnyAsync(p => p.Rnc == req.Rnc, ct))
            return Conflict(new { code = "RNC_DUPLICADO",
                message = "Ya existe un proveedor con ese RNC." });

        var entity = new Proveedor { Rnc = req.Rnc, Nombre = req.Nombre, Activo = req.Activo };
        _db.Proveedores.Add(entity);
        await _db.SaveChangesAsync(ct);
        var dto = new ProveedorDto(entity.Id, entity.Rnc, entity.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<ProveedorDto>> Update(
        int id, SaveProveedorRequest req, CancellationToken ct)
    {
        var entity = await _db.Proveedores.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.Proveedores.AnyAsync(p => p.Rnc == req.Rnc && p.Id != id, ct))
            return Conflict(new { code = "RNC_DUPLICADO",
                message = "Ya existe un proveedor con ese RNC." });

        entity.Rnc = req.Rnc;
        entity.Nombre = req.Nombre;
        entity.Activo = req.Activo;
        await _db.SaveChangesAsync(ct);
        return Ok(new ProveedorDto(entity.Id, entity.Rnc, entity.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.Proveedores.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
