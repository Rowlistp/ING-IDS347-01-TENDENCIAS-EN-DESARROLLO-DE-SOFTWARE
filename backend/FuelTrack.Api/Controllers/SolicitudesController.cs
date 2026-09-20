using System.Security.Claims;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Solicitudes;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/solicitudes")]
[Authorize]
public sealed class SolicitudesController : ControllerBase
{
    private static readonly string[] OperationalRoles =
        [Roles.Administrador, Roles.Supervisor, Roles.Despachador, Roles.Auditor, Roles.Consulta];

    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    public SolicitudesController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<List<SolicitudDto>>> GetAll(CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var actorId))
            return Unauthorized();
        var ownerFilter = OwnerFilter(actorId);

        var list = await _db.SolicitudesCombustible
            .AsNoTracking()
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .Where(s => ownerFilter == null || s.Empleado.UsuarioId == ownerFilter)
            .ToListAsync(ct);
        return Ok(list.ConvertAll(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitudDto>> GetById(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var actorId))
            return Unauthorized();
        var ownerFilter = OwnerFilter(actorId);

        var s = await _db.SolicitudesCombustible
            .AsNoTracking()
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .Where(s => ownerFilter == null || s.Empleado.UsuarioId == ownerFilter)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        return s is null ? NotFound() : Ok(ToDto(s));
    }

    private int? OwnerFilter(int actorId)
        => User is not null && OperationalRoles.Any(User.IsInRole) ? null : actorId;

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        return User is not null && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Solicitante}")]
    public async Task<ActionResult<SolicitudDto>> Create(CreateSolicitudRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var actorId)) return Unauthorized();
        {
            var ownerId = OwnerFilter(actorId);
            if (ownerId.HasValue)
            {
                var empleadoEsPropio = await _db.Empleados.AnyAsync(
                    e => e.Id == req.EmpleadoId && e.UsuarioId == ownerId.Value, ct);
                if (!empleadoEsPropio)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { code = "SOLICITANTE_EMPLEADO_NO_AUTORIZADO", message = "No tiene autorización para crear solicitudes a nombre de otro empleado." });
            }
        }

        var empleado = await _db.Empleados
    .FirstOrDefaultAsync(e => e.Id == req.EmpleadoId, ct);

if (empleado is null)
    return BadRequest(new
    {
        code = "EMPLEADO_NOT_FOUND",
        message = "El empleado no existe."
    });

if (!empleado.Activo)
    return BadRequest(new
    {
        code = "EMPLEADO_INACTIVO",
        message = "No se puede crear una solicitud para un empleado inactivo."
    });


var vehiculo = await _db.Vehiculos
    .FirstOrDefaultAsync(v => v.Id == req.VehiculoId, ct);

if (vehiculo is null)
    return BadRequest(new
    {
        code = "VEHICULO_NOT_FOUND",
        message = "El vehículo no existe."
    });

if (!vehiculo.Activo)
    return BadRequest(new
    {
        code = "VEHICULO_INACTIVO",
        message = "No se puede crear una solicitud para un vehículo inactivo."
    });


var departamentoId = req.DepartamentoId > 0 ? req.DepartamentoId : empleado.DepartamentoId;
var departamento = await _db.Departamentos
    .FirstOrDefaultAsync(d => d.Id == departamentoId, ct);

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
        message = "No se puede crear una solicitud para un departamento inactivo."
    });


var tipoCombustible = await _db.TiposCombustible
    .FirstOrDefaultAsync(t => t.Id == req.TipoCombustibleId, ct);

if (tipoCombustible is null)
    return BadRequest(new
    {
        code = "TIPO_COMBUSTIBLE_NOT_FOUND",
        message = "El tipo de combustible no existe."
    });

if (!tipoCombustible.Activo)
    return BadRequest(new
    {
        code = "TIPO_COMBUSTIBLE_INACTIVO",
        message = "No se puede crear una solicitud con un tipo de combustible inactivo."
    });
        if (empleado.DepartamentoId != departamento.Id || vehiculo.DepartamentoId != departamento.Id)
            return BadRequest(new { code = "SOLICITUD_RELACIONES_INVALIDAS", message = "El empleado y el vehículo deben pertenecer al departamento de la solicitud." });

        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = req.CantidadSolicitada,
            EmpleadoId = req.EmpleadoId,
            VehiculoId = req.VehiculoId,
            DepartamentoId = empleado.DepartamentoId,
            TipoCombustibleId = req.TipoCombustibleId,
            FechaVencimiento = req.FechaVencimiento,
            TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente,
            FechaSolicitud = DateTime.UtcNow
        };
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync(ct);
        await RegistrarAuditoriaAsync("SOLICITUD_CREADA", solicitud, actorId, ct);
        await transaction.CommitAsync(ct);

        await _db.Entry(solicitud).Reference(s => s.Empleado).LoadAsync(ct);
        await _db.Entry(solicitud).Reference(s => s.Vehiculo).LoadAsync(ct);
        await _db.Entry(solicitud).Reference(s => s.Departamento).LoadAsync(ct);
        await _db.Entry(solicitud).Reference(s => s.TipoCombustible).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = solicitud.Id }, ToDto(solicitud));
    }

    [HttpPost("{id:int}/aprobar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<SolicitudDto>> Aprobar(int id, AprobarSolicitudRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var actorId)) return Unauthorized();
        var solicitud = await _db.SolicitudesCombustible
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (solicitud is null) return NotFound();
        if (solicitud.Estado != EstadoSolicitud.Pendiente)
            return Conflict(new { code = "SOLICITUD_YA_PROCESADA", message = "La solicitud ya fue procesada." });

        if (!solicitud.Empleado.Activo || !solicitud.Vehiculo.Activo || !solicitud.Departamento.Activo || !solicitud.TipoCombustible.Activo)
            return Conflict(new { code = "SOLICITUD_CATALOGO_INACTIVO", message = "La solicitud contiene un empleado, vehículo, departamento o combustible inactivo." });
        if (solicitud.Empleado.DepartamentoId != solicitud.DepartamentoId || solicitud.Vehiculo.DepartamentoId != solicitud.DepartamentoId)
            return Conflict(new { code = "SOLICITUD_RELACIONES_INVALIDAS", message = "El empleado y el vehículo deben pertenecer al departamento de la solicitud." });
        if (req.CantidadAutorizada > solicitud.CantidadSolicitada)
            return BadRequest(new { code = "CANTIDAD_AUTORIZADA_EXCEDIDA", message = "No se puede autorizar más combustible del solicitado." });

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var updated = await _db.SolicitudesCombustible.Where(s => s.Id == id && s.Estado == EstadoSolicitud.Pendiente)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Estado, EstadoSolicitud.Aprobada)
                .SetProperty(s => s.CantidadAutorizada, req.CantidadAutorizada), ct);
        if (updated == 0)
            return Conflict(new { code = "SOLICITUD_YA_PROCESADA", message = "La solicitud ya fue procesada por otro usuario." });
        await _db.Entry(solicitud).ReloadAsync(ct);
        await RegistrarAuditoriaAsync("SOLICITUD_APROBADA", solicitud, actorId, ct);
        await transaction.CommitAsync(ct);

        return Ok(ToDto(solicitud));
    }

    [HttpPost("{id:int}/rechazar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<SolicitudDto>> Rechazar(int id, RechazarSolicitudRequest req, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var actorId)) return Unauthorized();
        var solicitud = await _db.SolicitudesCombustible
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (solicitud is null) return NotFound();
        if (solicitud.Estado != EstadoSolicitud.Pendiente)
            return Conflict(new { code = "SOLICITUD_YA_PROCESADA", message = "La solicitud ya fue procesada." });

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var updated = await _db.SolicitudesCombustible.Where(s => s.Id == id && s.Estado == EstadoSolicitud.Pendiente)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Estado, EstadoSolicitud.Rechazada)
                .SetProperty(s => s.MotivoRechazo, req.MotivoRechazo), ct);
        if (updated == 0)
            return Conflict(new { code = "SOLICITUD_YA_PROCESADA", message = "La solicitud ya fue procesada por otro usuario." });
        await _db.Entry(solicitud).ReloadAsync(ct);
        await RegistrarAuditoriaAsync("SOLICITUD_RECHAZADA", solicitud, actorId, ct);
        await transaction.CommitAsync(ct);

        return Ok(ToDto(solicitud));
    }

    private Task RegistrarAuditoriaAsync(string evento, SolicitudCombustible solicitud, int actorId, CancellationToken ct)
        => _audit.WriteAsync(evento, "SolicitudCombustible", solicitud.Id.ToString(), actorId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            new { solicitud.EmpleadoId, solicitud.VehiculoId, solicitud.DepartamentoId, solicitud.Estado, solicitud.CantidadSolicitada, solicitud.CantidadAutorizada, solicitud.MotivoRechazo }, ct);

    private static SolicitudDto ToDto(SolicitudCombustible s) => new(
        s.Id,
        s.CantidadSolicitada,
        s.CantidadAutorizada,
        s.TipoSolicitud,
        s.Estado,
        s.FechaSolicitud,
        s.FechaVencimiento,
        s.MotivoRechazo,
        s.EmpleadoId, s.Empleado.NombreCompleto,
        s.VehiculoId, s.Vehiculo.Placa,
        s.DepartamentoId, s.Departamento.Nombre,
        s.TipoCombustibleId, s.TipoCombustible.Nombre);
}
