// backend/FuelTrack.Api/Controllers/SolicitudesController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Solicitudes;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/solicitudes")]
[Authorize]
public sealed class SolicitudesController : ControllerBase
{
    private readonly AppDbContext _db;
    public SolicitudesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<SolicitudDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.SolicitudesCombustible
            .AsNoTracking()
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .ToListAsync(ct);
        return Ok(list.ConvertAll(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitudDto>> GetById(int id, CancellationToken ct)
    {
        var s = await _db.SolicitudesCombustible
            .AsNoTracking()
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        return s is null ? NotFound() : Ok(ToDto(s));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Solicitante}")]
    public async Task<ActionResult<SolicitudDto>> Create(CreateSolicitudRequest req, CancellationToken ct)
    {
        if (!await _db.Empleados.AnyAsync(e => e.Id == req.EmpleadoId, ct))
            return BadRequest(new { code = "EMPLEADO_NOT_FOUND", message = "El empleado no existe." });
        if (!await _db.Vehiculos.AnyAsync(v => v.Id == req.VehiculoId, ct))
            return BadRequest(new { code = "VEHICULO_NOT_FOUND", message = "El vehículo no existe." });
        if (!await _db.Departamentos.AnyAsync(d => d.Id == req.DepartamentoId, ct))
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND", message = "El departamento no existe." });
        if (!await _db.TiposCombustible.AnyAsync(t => t.Id == req.TipoCombustibleId, ct))
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_NOT_FOUND", message = "El tipo de combustible no existe." });

        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = req.CantidadSolicitada,
            EmpleadoId = req.EmpleadoId,
            VehiculoId = req.VehiculoId,
            DepartamentoId = req.DepartamentoId,
            TipoCombustibleId = req.TipoCombustibleId,
            FechaVencimiento = req.FechaVencimiento,
            TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente,
            FechaSolicitud = DateTime.UtcNow
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync(ct);

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
        var solicitud = await _db.SolicitudesCombustible
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (solicitud is null) return NotFound();
        if (solicitud.Estado != EstadoSolicitud.Pendiente)
            return Conflict(new { code = "SOLICITUD_YA_PROCESADA" });

        solicitud.Estado = EstadoSolicitud.Aprobada;
        solicitud.CantidadAutorizada = req.CantidadAutorizada;
        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(solicitud));
    }

    [HttpPost("{id:int}/rechazar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<SolicitudDto>> Rechazar(int id, RechazarSolicitudRequest req, CancellationToken ct)
    {
        var solicitud = await _db.SolicitudesCombustible
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (solicitud is null) return NotFound();
        if (solicitud.Estado != EstadoSolicitud.Pendiente)
            return Conflict(new { code = "SOLICITUD_YA_PROCESADA" });

        solicitud.Estado = EstadoSolicitud.Rechazada;
        solicitud.MotivoRechazo = req.MotivoRechazo;
        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(solicitud));
    }

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
