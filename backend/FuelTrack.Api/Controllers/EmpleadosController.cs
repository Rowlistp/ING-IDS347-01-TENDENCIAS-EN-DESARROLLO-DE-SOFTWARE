using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Empleados;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
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
            .Include(e => e.Usuario)
            .Select(e => new EmpleadoDto(
                e.Id, e.Codigo, e.NombreCompleto, e.Cedula, e.Cargo,
                e.Correo, e.Telefono, e.DepartamentoId, e.Departamento.Nombre, e.Activo,
                e.UsuarioId, e.Usuario != null ? e.Usuario.NombreUsuario : null))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpleadoDto>> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Empleados
            .AsNoTracking()
            .Include(x => x.Departamento)
            .Include(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();
        return Ok(new EmpleadoDto(
            e.Id, e.Codigo, e.NombreCompleto, e.Cedula, e.Cargo,
            e.Correo, e.Telefono, e.DepartamentoId, e.Departamento.Nombre, e.Activo,
            e.UsuarioId, e.Usuario?.NombreUsuario));
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

        if (req.UsuarioId.HasValue)
        {
            var (ok, error) = await ValidateUsuarioVinculoAsync(req.UsuarioId, null, ct);
            if (!ok) return error!;
        }

        var entity = new Empleado
        {
            Codigo         = req.Codigo,
            NombreCompleto = req.NombreCompleto,
            Cedula         = req.Cedula,
            Cargo          = req.Cargo,
            Correo         = req.Correo,
            Telefono       = req.Telefono,
            DepartamentoId = req.DepartamentoId,
            Activo         = req.Activo,
            UsuarioId      = req.UsuarioId
        };
        _db.Empleados.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(entity).Reference(e => e.Departamento).LoadAsync(ct);
        if (entity.UsuarioId.HasValue)
            await _db.Entry(entity).Reference(e => e.Usuario).LoadAsync(ct);

        var dto = new EmpleadoDto(
            entity.Id, entity.Codigo, entity.NombreCompleto, entity.Cedula, entity.Cargo,
            entity.Correo, entity.Telefono, entity.DepartamentoId,
            entity.Departamento.Nombre, entity.Activo,
            entity.UsuarioId, entity.Usuario?.NombreUsuario);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<EmpleadoDto>> Update(
        int id, SaveEmpleadoRequest req, CancellationToken ct)
    {
        var entity = await _db.Empleados
            .Include(e => e.Departamento)
            .Include(e => e.Usuario)
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

        if (req.UsuarioId.HasValue)
        {
            var (ok, error) = await ValidateUsuarioVinculoAsync(req.UsuarioId, id, ct);
            if (!ok) return error!;
        }

        entity.Codigo         = req.Codigo;
        entity.NombreCompleto = req.NombreCompleto;
        entity.Cedula         = req.Cedula;
        entity.Cargo          = req.Cargo;
        entity.Correo         = req.Correo;
        entity.Telefono       = req.Telefono;
        entity.DepartamentoId = req.DepartamentoId;
        entity.Activo         = req.Activo;
        entity.UsuarioId      = req.UsuarioId;
        await _db.SaveChangesAsync(ct);

        if (entity.Departamento.Id != req.DepartamentoId)
            await _db.Entry(entity).Reference(e => e.Departamento).LoadAsync(ct);
        if (entity.UsuarioId.HasValue && (entity.Usuario == null || entity.Usuario.Id != entity.UsuarioId))
            await _db.Entry(entity).Reference(e => e.Usuario).LoadAsync(ct);
        else if (!entity.UsuarioId.HasValue)
            entity.Usuario = null;

        return Ok(new EmpleadoDto(
            entity.Id, entity.Codigo, entity.NombreCompleto, entity.Cedula, entity.Cargo,
            entity.Correo, entity.Telefono, entity.DepartamentoId,
            entity.Departamento.Nombre, entity.Activo,
            entity.UsuarioId, entity.Usuario?.NombreUsuario));
    }

    [HttpPut("{id:int}/vincular-usuario")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<EmpleadoDto>> VincularUsuario(
        int id, VincularUsuarioRequest req, CancellationToken ct)
    {
        var entity = await _db.Empleados
            .Include(e => e.Departamento)
            .Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null) return NotFound();

        if (req.UsuarioId.HasValue)
        {
            var (ok, error) = await ValidateUsuarioVinculoAsync(req.UsuarioId, id, ct);
            if (!ok) return error!;
            entity.UsuarioId = req.UsuarioId;
        }
        else
        {
            entity.UsuarioId = null;
            entity.Usuario = null;
        }

        await _db.SaveChangesAsync(ct);
        if (entity.UsuarioId.HasValue)
            await _db.Entry(entity).Reference(e => e.Usuario).LoadAsync(ct);

        return Ok(new EmpleadoDto(
            entity.Id, entity.Codigo, entity.NombreCompleto, entity.Cedula, entity.Cargo,
            entity.Correo, entity.Telefono, entity.DepartamentoId,
            entity.Departamento.Nombre, entity.Activo,
            entity.UsuarioId, entity.Usuario?.NombreUsuario));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.Empleados.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.SolicitudesCombustible.AnyAsync(s => s.EmpleadoId == id &&
            (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.Aprobada), ct))
            return Conflict(new
            {
                code = "EMPLEADO_CON_SOLICITUDES_ACTIVAS",
                message = "No se puede desactivar el empleado porque tiene solicitudes pendientes o aprobadas."
            });

        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<(bool ok, ActionResult? error)> ValidateUsuarioVinculoAsync(
        int? usuarioId, int? currentEmpleadoId, CancellationToken ct)
    {
        if (!usuarioId.HasValue)
            return (true, null);

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId.Value, ct);
        if (usuario is null)
            return (false, NotFound(new { code = "USUARIO_NOT_FOUND", message = "El usuario especificado no existe." }));

        if (!usuario.Activo)
            return (false, BadRequest(new { code = "USUARIO_INACTIVO", message = "No se puede vincular un usuario inactivo." }));

        var yaVinculado = await _db.Empleados.AnyAsync(
            e => e.UsuarioId == usuarioId.Value && (!currentEmpleadoId.HasValue || e.Id != currentEmpleadoId.Value),
            ct);
        if (yaVinculado)
            return (false, Conflict(new { code = "USUARIO_YA_VINCULADO", message = "El usuario ya está vinculado a otro empleado." }));

        return (true, null);
    }
}
