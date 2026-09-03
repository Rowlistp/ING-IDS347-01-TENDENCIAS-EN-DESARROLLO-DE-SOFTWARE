using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Recepciones;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
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
    public RecepcionesController(AppDbContext db) => _db = db;

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

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<RecepcionDto>> Create(CreateRecepcionRequest req, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        if (!await _db.Proveedores.AnyAsync(p => p.Id == req.ProveedorId, ct))
            return BadRequest(new { code = "PROVEEDOR_NOT_FOUND", message = "El proveedor no existe." });

        var tanque = await _db.Tanques
            .Include(t => t.Inventario)
            .FirstOrDefaultAsync(t => t.Id == req.TanqueId, ct);

        if (tanque is null)
            return BadRequest(new { code = "TANQUE_NOT_FOUND", message = "El tanque no existe." });
        if (!tanque.Activo)
            return BadRequest(new { code = "TANQUE_INACTIVO", message = "El tanque no está activo." });

        var recepcion = new RecepcionCombustible
        {
            NumeroFactura = req.NumeroFactura,
            VolumenRecibido = req.VolumenRecibido,
            Fecha = req.Fecha,
            ProveedorId = req.ProveedorId,
            TanqueId = req.TanqueId
        };
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
