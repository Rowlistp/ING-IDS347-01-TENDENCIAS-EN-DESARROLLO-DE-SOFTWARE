using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Recepciones;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/recepciones")]
[Authorize]
public sealed class RecepcionesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly RecepcionPdfService _pdf;
    public RecepcionesController(AppDbContext db, AuditService audit, RecepcionPdfService pdf)
    {
        _db = db;
        _audit = audit;
        _pdf = pdf;
    }

    [HttpGet]
    public async Task<ActionResult<List<RecepcionDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.RecepcionesCombustible
            .AsNoTracking()
            .Include(r => r.Proveedor)
            .Include(r => r.Tanque)
            .ToListAsync(ct);
        return Ok(list.ConvertAll(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecepcionDto>> GetById(int id, CancellationToken ct)
    {
        var r = await _db.RecepcionesCombustible
            .AsNoTracking()
            .Include(r => r.Proveedor)
            .Include(r => r.Tanque)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        return r is null ? NotFound() : Ok(ToDto(r));
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<ActionResult> GetPdf(int id, CancellationToken ct)
    {
        var pdf = await _pdf.GetPdfAsync(id, ct);
        return pdf is null ? NotFound() : File(pdf, "application/pdf", $"comprobante-recepcion-REC-{id:D5}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf");
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<RecepcionDto>> Create(CreateRecepcionRequest req, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == req.ProveedorId, ct);
        if (proveedor is null)
            return BadRequest(new { code = "PROVEEDOR_NOT_FOUND", message = "El proveedor no existe." });
        if (!proveedor.Activo)
            return BadRequest(new { code = "PROVEEDOR_INACTIVO", message = "El proveedor no está activo." });

        var tanque = await _db.Tanques
            .Include(t => t.Inventario)
            .Include(t => t.TipoCombustible)
            .FirstOrDefaultAsync(t => t.Id == req.TanqueId, ct);

        if (tanque is null)
            return BadRequest(new { code = "TANQUE_NOT_FOUND", message = "El tanque no existe." });
        if (!tanque.Activo)
            return BadRequest(new { code = "TANQUE_INACTIVO", message = "El tanque no está activo." });
        if (!tanque.TipoCombustible.Activo)
            return Conflict(new { code = "TIPO_COMBUSTIBLE_INACTIVO", message = "El tipo de combustible del tanque no está activo." });

        if (tanque.Inventario is null)
            return Conflict(new { code = "INVENTARIO_NO_DISPONIBLE", message = "El tanque no tiene inventario inicializado." });
        if (tanque.Inventario.ExistenciaActual + req.VolumenRecibido > tanque.Capacidad)
            return Conflict(new { code = "CAPACIDAD_EXCEDIDA", message = "La recepción supera el espacio disponible del tanque. Actualice los saldos." });

        var recepcion = new RecepcionCombustible
        {
            NumeroFactura = req.NumeroFactura,
            VolumenRecibido = req.VolumenRecibido,
            Fecha = req.Fecha,
            ProveedorId = req.ProveedorId,
            TanqueId = req.TanqueId
        };

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.RecepcionesCombustible.Add(recepcion);

            _db.MovimientosInventario.Add(new MovimientoInventario
            {
                Tipo = TipoMovimiento.Entrada,
                Volumen = req.VolumenRecibido,
                FechaHora = DateTime.UtcNow,
                ReferenciaOperacion = req.NumeroFactura,
                TanqueId = req.TanqueId,
                UsuarioId = usuarioId
            });

            tanque.Inventario!.ExistenciaActual += req.VolumenRecibido;
            tanque.Inventario.Disponibilidad += req.VolumenRecibido;
            tanque.Inventario.UltimaActualizacion = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            await _audit.WriteAsync("RECEPCION_REGISTRADA", "RecepcionCombustible", recepcion.Id.ToString(), usuarioId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                new { req.ProveedorId, req.TanqueId, req.VolumenRecibido, req.NumeroFactura }, ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Conflict(new { code = "INVENTARIO_MODIFICADO", message = "El inventario cambió durante la recepción. Actualice los saldos y vuelva a intentarlo." });
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        await _db.Entry(recepcion).Reference(r => r.Proveedor).LoadAsync(ct);
        await _db.Entry(recepcion).Reference(r => r.Tanque).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = recepcion.Id }, ToDto(recepcion));
    }

    private static RecepcionDto ToDto(RecepcionCombustible r) => new(
        r.Id,
        r.NumeroFactura,
        r.VolumenRecibido,
        r.Fecha,
        r.ProveedorId, r.Proveedor.Nombre,
        r.TanqueId, r.Tanque.Identificacion);
}
