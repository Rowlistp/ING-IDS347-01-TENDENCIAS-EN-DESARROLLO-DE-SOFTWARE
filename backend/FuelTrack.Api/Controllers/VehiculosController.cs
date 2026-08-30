using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Vehiculos;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
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

    public VehiculosController(AppDbContext db) => _db = db;

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
        if (!await _db.Departamentos.AnyAsync(d => d.Id == req.DepartamentoId, ct))
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND",
                message = "El departamento no existe." });

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
        var entity = await _db.Vehiculos
            .Include(v => v.Departamento)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null) return NotFound();

        if (!await _db.Departamentos.AnyAsync(d => d.Id == req.DepartamentoId, ct))
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND",
                message = "El departamento no existe." });

        if (await _db.Vehiculos.AnyAsync(v => v.Placa == req.Placa && v.Id != id, ct))
            return Conflict(new { code = "PLACA_DUPLICADA",
                message = "La placa ya está registrada." });

        if (await _db.Vehiculos.AnyAsync(v => v.Ficha == req.Ficha && v.Id != id, ct))
            return Conflict(new { code = "FICHA_DUPLICADA",
                message = "La ficha ya está registrada." });

        entity.Placa           = req.Placa;
        entity.Ficha           = req.Ficha;
        entity.Marca           = req.Marca;
        entity.Modelo          = req.Modelo;
        entity.Año             = req.Año;
        entity.Tipo            = req.Tipo;
        entity.CapacidadTanque = req.CapacidadTanque;
        entity.Odometro        = req.Odometro;
        entity.DepartamentoId  = req.DepartamentoId;
        await _db.SaveChangesAsync(ct);

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
        var entity = await _db.Vehiculos.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
