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
[Route("api/v1/solicitudes-recurrentes")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
public sealed class SolicitudesRecurrentesController : ControllerBase
{
    private readonly AppDbContext _db;
    public SolicitudesRecurrentesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<SolicitudRecurrenteDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.SolicitudesRecurrentes
            .AsNoTracking()
            .Include(s => s.Empleado).Include(s => s.Vehiculo)
            .Include(s => s.Departamento).Include(s => s.TipoCombustible)
            .ToListAsync(ct);
        return Ok(list.ConvertAll(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitudRecurrenteDto>> GetById(int id, CancellationToken ct)
    {
        var s = await FindAsync(id, ct);
        return s is null ? NotFound() : Ok(ToDto(s));
    }

    [HttpPost]
    public async Task<ActionResult<SolicitudRecurrenteDto>> Create(
        CreateSolicitudRecurrenteRequest req, CancellationToken ct)
    {
        var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == req.EmpleadoId, ct);
        if (empleado is null)
            return BadRequest(new { code = "EMPLEADO_NOT_FOUND", message = "El empleado no existe." });
        if (!empleado.Activo)
            return BadRequest(new { code = "EMPLEADO_INACTIVO", message = "No se puede crear una solicitud recurrente para un empleado inactivo." });

        var vehiculo = await _db.Vehiculos.FirstOrDefaultAsync(v => v.Id == req.VehiculoId, ct);
        if (vehiculo is null)
            return BadRequest(new { code = "VEHICULO_NOT_FOUND", message = "El vehículo no existe." });
        if (!vehiculo.Activo)
            return BadRequest(new { code = "VEHICULO_INACTIVO", message = "No se puede crear una solicitud recurrente para un vehículo inactivo." });

        if (req.DepartamentoId > 0)
        {
            var deptoReq = await _db.Departamentos.FirstOrDefaultAsync(d => d.Id == req.DepartamentoId, ct);
            if (deptoReq is null)
                return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND", message = "El departamento no existe." });

            if (req.DepartamentoId != empleado.DepartamentoId)
                return BadRequest(new { code = "DEPARTAMENTO_NO_COINCIDE", message = "El departamento no coincide con el departamento asignado al empleado." });
        }

        var departamento = await _db.Departamentos.FirstOrDefaultAsync(d => d.Id == empleado.DepartamentoId, ct);
        if (departamento is null)
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND", message = "El departamento no existe." });
        if (!departamento.Activo)
            return BadRequest(new { code = "DEPARTAMENTO_INACTIVO", message = "No se puede crear una solicitud recurrente para un departamento inactivo." });

        var tipoCombustible = await _db.TiposCombustible.FirstOrDefaultAsync(t => t.Id == req.TipoCombustibleId, ct);
        if (tipoCombustible is null)
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_NOT_FOUND", message = "El tipo de combustible no existe." });
        if (!tipoCombustible.Activo)
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_INACTIVO", message = "No se puede crear una solicitud recurrente con un tipo de combustible inactivo." });

        if (req.FechaFin.HasValue && req.FechaFin.Value <= req.FechaInicio)
            return BadRequest(new { code = "FECHA_FIN_INVALIDA", message = "FechaFin debe ser posterior a FechaInicio." });

        var solicitud = new SolicitudRecurrente
        {
            CantidadSolicitada = req.CantidadSolicitada,
            Periodicidad = req.Periodicidad,
            FechaInicio = req.FechaInicio,
            FechaFin = req.FechaFin,
            EmpleadoId = req.EmpleadoId,
            VehiculoId = req.VehiculoId,
            DepartamentoId = empleado.DepartamentoId,
            TipoCombustibleId = req.TipoCombustibleId,
            Activa = true
        };

        _db.SolicitudesRecurrentes.Add(solicitud);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(solicitud).Reference(s => s.Empleado).LoadAsync(ct);
        await _db.Entry(solicitud).Reference(s => s.Vehiculo).LoadAsync(ct);
        await _db.Entry(solicitud).Reference(s => s.Departamento).LoadAsync(ct);
        await _db.Entry(solicitud).Reference(s => s.TipoCombustible).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = solicitud.Id }, ToDto(solicitud));
    }

    [HttpPost("{id:int}/desactivar")]
    public async Task<ActionResult<SolicitudRecurrenteDto>> Desactivar(int id, CancellationToken ct)
    {
        var s = await FindAsync(id, ct);
        if (s is null) return NotFound();
        if (!s.Activa)
            return Conflict(new { code = "YA_DESACTIVADA", message = "La plantilla ya está desactivada." });

        s.Activa = false;
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(s));
    }

    [HttpPost("{id:int}/activar")]
    public async Task<ActionResult<SolicitudRecurrenteDto>> Activar(int id, CancellationToken ct)
    {
        var s = await FindAsync(id, ct);
        if (s is null) return NotFound();
        if (s.Activa)
            return Conflict(new { code = "YA_ACTIVA", message = "La plantilla ya está activa." });

        if (!s.Empleado.Activo)
            return Conflict(new { code = "EMPLEADO_INACTIVO", message = "No se puede activar la plantilla porque el empleado está inactivo." });
        if (!s.Vehiculo.Activo)
            return Conflict(new { code = "VEHICULO_INACTIVO", message = "No se puede activar la plantilla porque el vehículo está inactivo." });
        if (!s.Departamento.Activo)
            return Conflict(new { code = "DEPARTAMENTO_INACTIVO", message = "No se puede activar la plantilla porque el departamento está inactivo." });
        if (!s.TipoCombustible.Activo)
            return Conflict(new { code = "TIPO_COMBUSTIBLE_INACTIVO", message = "No se puede activar la plantilla porque el tipo de combustible está inactivo." });

        s.Activa = true;
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(s));
    }

    private Task<SolicitudRecurrente?> FindAsync(int id, CancellationToken ct) =>
        _db.SolicitudesRecurrentes
            .Include(s => s.Empleado).Include(s => s.Vehiculo)
            .Include(s => s.Departamento).Include(s => s.TipoCombustible)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    private static SolicitudRecurrenteDto ToDto(SolicitudRecurrente s) => new(
        s.Id, s.CantidadSolicitada, s.Periodicidad,
        s.FechaInicio, s.FechaFin, s.Activa, s.UltimaEjecucion,
        s.EmpleadoId, s.Empleado.NombreCompleto,
        s.VehiculoId, s.Vehiculo.Placa,
        s.DepartamentoId, s.Departamento.Nombre,
        s.TipoCombustibleId, s.TipoCombustible.Nombre);
}
