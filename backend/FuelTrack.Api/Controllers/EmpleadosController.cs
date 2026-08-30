using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Empleados;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/empleados")]
[Authorize]
public sealed class EmpleadosController : ControllerBase
{
    private readonly AppDbContext _db;

    public EmpleadosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<EmpleadoDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Empleados
            .AsNoTracking()
            .Include(e => e.Departamento)
            .Select(e => new EmpleadoDto(
                e.Id, e.Codigo, e.NombreCompleto, e.Cedula, e.Cargo,
                e.Correo, e.Telefono, e.DepartamentoId, e.Departamento.Nombre, e.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpleadoDto>> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Empleados
            .AsNoTracking()
            .Include(x => x.Departamento)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();
        return Ok(new EmpleadoDto(
            e.Id, e.Codigo, e.NombreCompleto, e.Cedula, e.Cargo,
            e.Correo, e.Telefono, e.DepartamentoId, e.Departamento.Nombre, e.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<EmpleadoDto>> Create(
        SaveEmpleadoRequest req, CancellationToken ct)
    {
        if (!await _db.Departamentos.AnyAsync(d => d.Id == req.DepartamentoId, ct))
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND",
                message = "El departamento no existe." });

        if (await _db.Empleados.AnyAsync(e => e.Codigo == req.Codigo, ct))
            return Conflict(new { code = "CODIGO_DUPLICADO",
                message = "El código de empleado ya existe." });

        if (await _db.Empleados.AnyAsync(e => e.Cedula == req.Cedula, ct))
            return Conflict(new { code = "CEDULA_DUPLICADA",
                message = "La cédula ya está registrada." });

        var entity = new Empleado
        {
            Codigo         = req.Codigo,
            NombreCompleto = req.NombreCompleto,
            Cedula         = req.Cedula,
            Cargo          = req.Cargo,
            Correo         = req.Correo,
            Telefono       = req.Telefono,
            DepartamentoId = req.DepartamentoId,
            Activo         = req.Activo
        };
        _db.Empleados.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(entity).Reference(e => e.Departamento).LoadAsync(ct);

        var dto = new EmpleadoDto(
            entity.Id, entity.Codigo, entity.NombreCompleto, entity.Cedula, entity.Cargo,
            entity.Correo, entity.Telefono, entity.DepartamentoId,
            entity.Departamento.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<EmpleadoDto>> Update(
        int id, SaveEmpleadoRequest req, CancellationToken ct)
    {
        var entity = await _db.Empleados
            .Include(e => e.Departamento)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null) return NotFound();

        if (!await _db.Departamentos.AnyAsync(d => d.Id == req.DepartamentoId, ct))
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND",
                message = "El departamento no existe." });

        if (await _db.Empleados.AnyAsync(e => e.Codigo == req.Codigo && e.Id != id, ct))
            return Conflict(new { code = "CODIGO_DUPLICADO",
                message = "El código de empleado ya existe." });

        if (await _db.Empleados.AnyAsync(e => e.Cedula == req.Cedula && e.Id != id, ct))
            return Conflict(new { code = "CEDULA_DUPLICADA",
                message = "La cédula ya está registrada." });

        entity.Codigo         = req.Codigo;
        entity.NombreCompleto = req.NombreCompleto;
        entity.Cedula         = req.Cedula;
        entity.Cargo          = req.Cargo;
        entity.Correo         = req.Correo;
        entity.Telefono       = req.Telefono;
        entity.DepartamentoId = req.DepartamentoId;
        entity.Activo         = req.Activo;
        await _db.SaveChangesAsync(ct);

        if (entity.Departamento.Id != req.DepartamentoId)
            await _db.Entry(entity).Reference(e => e.Departamento).LoadAsync(ct);

        return Ok(new EmpleadoDto(
            entity.Id, entity.Codigo, entity.NombreCompleto, entity.Cedula, entity.Cargo,
            entity.Correo, entity.Telefono, entity.DepartamentoId,
            entity.Departamento.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.Empleados.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
