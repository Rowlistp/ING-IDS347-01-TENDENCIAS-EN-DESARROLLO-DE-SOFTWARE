using System.Security.Claims;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Empleados;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Notifications;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
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
    private readonly AuditService _audit;

    public EmpleadosController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private bool TryGetCurrentUserId(out int userId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private bool CanReadAllDetails() =>
        User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.Supervisor) || User.IsInRole(Roles.Auditor);

    [HttpGet("opciones")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Auditor},{Roles.Consulta}")]
    public async Task<IActionResult> GetOptions(CancellationToken ct) =>
        Ok(await _db.Empleados.AsNoTracking().OrderBy(e => e.NombreCompleto)
            .Select(e => new { e.Id, e.NombreCompleto }).ToListAsync(ct));

    [HttpGet]
    public async Task<ActionResult<List<EmpleadoDto>>> GetAll(CancellationToken ct)
    {
        var canReadAll = CanReadAllDetails();
        if (!canReadAll && !User.IsInRole(Roles.Solicitante)) return Forbid();
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        var list = await _db.Empleados
            .AsNoTracking()
            .Where(e => canReadAll || e.UsuarioId == userId)
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
        var canReadAll = CanReadAllDetails();
        if (!canReadAll && !User.IsInRole(Roles.Solicitante)) return Forbid();
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        var e = await _db.Empleados
            .AsNoTracking()
            .Where(e => canReadAll || e.UsuarioId == userId)
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
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var departamento = await _db.Departamentos
    .FirstOrDefaultAsync(d => d.Id == req.DepartamentoId, ct);

if (departamento is null)
    return BadRequest(new
    {
        code = "DEPARTAMENTO_NOT_FOUND",
        message = "El departamento no existe."
    });

if (!departamento.Activo)
    return BadRequest(new
    {
        code = "DEPARTAMENTO_INACTIVO",
        message = "No se puede asignar un empleado a un departamento inactivo."
    });

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
            Telefono       = NotificationOptions.NormalizePhone(req.Telefono),
            DepartamentoId = req.DepartamentoId,
            Activo         = req.Activo,
            UsuarioId      = req.UsuarioId
        };
        _db.Empleados.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(entity).Reference(e => e.Departamento).LoadAsync(ct);
        if (entity.UsuarioId.HasValue)
            await _db.Entry(entity).Reference(e => e.Usuario).LoadAsync(ct);

        await _audit.WriteAsync("EMPLEADO_CREADO", "Empleado", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            new { entity.Codigo, entity.DepartamentoId, entity.Activo }, ct);

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
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Empleados
            .Include(e => e.Departamento)
            .Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null) return NotFound();

        var departamento = await _db.Departamentos
    .FirstOrDefaultAsync(d => d.Id == req.DepartamentoId, ct);

if (departamento is null)
    return BadRequest(new
    {
        code = "DEPARTAMENTO_NOT_FOUND",
        message = "El departamento no existe."
    });

if (!departamento.Activo)
    return BadRequest(new
    {
        code = "DEPARTAMENTO_INACTIVO",
        message = "No se puede asignar un empleado a un departamento inactivo."
    });

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

        if (entity.Activo && req.Activo == false)
        {
            if (!User.IsInRole(Roles.Administrador))
                return StatusCode(StatusCodes.Status403Forbidden, new { code = "DESACTIVACION_NO_AUTORIZADA", message = "Solo un administrador puede desactivar este registro." });
            var conflict = await ValidateDeactivationAsync(id, ct);
            if (conflict is not null) return conflict;
        }

        entity.Codigo         = req.Codigo;
        entity.NombreCompleto = req.NombreCompleto;
        entity.Cedula         = req.Cedula;
        entity.Cargo          = req.Cargo;
        entity.Correo         = req.Correo;
        entity.Telefono       = NotificationOptions.NormalizePhone(req.Telefono);
        entity.DepartamentoId = req.DepartamentoId;
        entity.Activo         = req.Activo;
        entity.UsuarioId      = req.UsuarioId;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("EMPLEADO_ACTUALIZADO", "Empleado", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            new { entity.Codigo, entity.DepartamentoId, entity.Activo }, ct);

        await _db.Entry(entity)
    .Reference(e => e.Departamento)
    .LoadAsync(ct);
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
        if (!TryGetCurrentUserId(out var usuarioId)) return Unauthorized();

        var entity = await _db.Empleados.FindAsync([id], ct);
        if (entity is null) return NotFound();

        var conflict = await ValidateDeactivationAsync(id, ct);
        if (conflict is not null) return conflict;

        entity.Activo = false;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("EMPLEADO_DESACTIVADO", "Empleado", entity.Id.ToString(), usuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), null, ct);

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

    private async Task<ConflictObjectResult?> ValidateDeactivationAsync(int id, CancellationToken ct)
    {
        if (await _db.SolicitudesCombustible.AnyAsync(s => s.EmpleadoId == id &&
            (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.Aprobada), ct))
            return Conflict(new
            {
                code = "EMPLEADO_CON_SOLICITUDES_ACTIVAS",
                message = "No se puede desactivar el empleado porque tiene solicitudes pendientes o aprobadas."
            });

        return null;
    }
}
