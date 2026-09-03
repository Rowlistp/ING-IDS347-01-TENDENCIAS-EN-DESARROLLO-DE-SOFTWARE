using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Tanques;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
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

    public TanquesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TanqueDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Tanques
            .AsNoTracking()
            .Include(t => t.TipoCombustible)
            .Select(t => new TanqueDto(
                t.Id, t.Identificacion, t.Capacidad, t.NivelActual, t.NivelCritico,
                t.TipoCombustibleId, t.TipoCombustible.Nombre, t.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TanqueDto>> GetById(int id, CancellationToken ct)
    {
        var t = await _db.Tanques
            .AsNoTracking()
            .Include(x => x.TipoCombustible)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        return Ok(new TanqueDto(
            t.Id, t.Identificacion, t.Capacidad, t.NivelActual, t.NivelCritico,
            t.TipoCombustibleId, t.TipoCombustible.Nombre, t.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TanqueDto>> Create(SaveTanqueRequest req, CancellationToken ct)
    {
        if (!await _db.TiposCombustible.AnyAsync(t => t.Id == req.TipoCombustibleId, ct))
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_NOT_FOUND",
                message = "El tipo de combustible no existe." });

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

        return CreatedAtAction(nameof(GetById), new { id = tanque.Id },
            new TanqueDto(tanque.Id, tanque.Identificacion, tanque.Capacidad, tanque.NivelActual,
                tanque.NivelCritico, tanque.TipoCombustibleId, tanque.TipoCombustible.Nombre, tanque.Activo));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TanqueDto>> Update(int id, SaveTanqueRequest req, CancellationToken ct)
    {
        var tanque = await _db.Tanques
            .Include(t => t.TipoCombustible)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tanque is null) return NotFound();

        if (!await _db.TiposCombustible.AnyAsync(t => t.Id == req.TipoCombustibleId, ct))
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_NOT_FOUND",
                message = "El tipo de combustible no existe." });

        if (await _db.Tanques.AnyAsync(t => t.Identificacion == req.Identificacion && t.Id != id, ct))
            return Conflict(new { code = "IDENTIFICACION_DUPLICADA",
                message = "Ya existe un tanque con esa identificación." });

        tanque.Identificacion    = req.Identificacion;
        tanque.Capacidad         = req.Capacidad;
        tanque.NivelCritico      = req.NivelCritico;
        tanque.TipoCombustibleId = req.TipoCombustibleId;
        await _db.SaveChangesAsync(ct);

        if (tanque.TipoCombustible.Id != req.TipoCombustibleId)
            await _db.Entry(tanque).Reference(t => t.TipoCombustible).LoadAsync(ct);

        return Ok(new TanqueDto(tanque.Id, tanque.Identificacion, tanque.Capacidad, tanque.NivelActual,
            tanque.NivelCritico, tanque.TipoCombustibleId, tanque.TipoCombustible.Nombre, tanque.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.Tanques.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
