using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Inventario;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/inventario")]
[Authorize]
public sealed class InventarioController : ControllerBase
{
    private readonly AppDbContext _db;
    public InventarioController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<InventarioDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Inventarios
            .AsNoTracking()
            .Include(i => i.Tanque)
            .ToListAsync(ct);
        var consumos = await GetConsumosBatchAsync(ct);
        return Ok(list.ConvertAll(i =>
        {
            var (diario, mensual) = consumos.GetValueOrDefault(i.TanqueId);
            return ToDto(i, diario, mensual);
        }));
    }

    [HttpGet("{tanqueId:int}")]
    public async Task<ActionResult<InventarioDto>> GetByTanque(int tanqueId, CancellationToken ct)
    {
        var inv = await _db.Inventarios
            .AsNoTracking()
            .Include(i => i.Tanque)
            .FirstOrDefaultAsync(i => i.TanqueId == tanqueId, ct);
        if (inv is null) return NotFound();

        var (diario, mensual) = await GetConsumoAsync(tanqueId, ct);
        return Ok(ToDto(inv, diario, mensual));
    }

    [HttpPost("ajustes")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<InventarioDto>> Ajustar(AjustarInventarioRequest req, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        var tanque = await _db.Tanques
            .Include(t => t.Inventario)
            .FirstOrDefaultAsync(t => t.Id == req.TanqueId, ct);

        if (tanque is null)
            return BadRequest(new { code = "TANQUE_NOT_FOUND", message = "El tanque no existe." });
        if (!tanque.Activo)
            return BadRequest(new { code = "TANQUE_INACTIVO", message = "El tanque no está activo." });

        var inventario = tanque.Inventario!;
        if (inventario.ExistenciaActual + req.Volumen < 0)
            return Conflict(new { code = "INVENTARIO_INSUFICIENTE", message = "El ajuste dejaría el inventario en negativo." });

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Ajuste,
            Volumen = req.Volumen,
            FechaHora = DateTime.UtcNow,
            Observaciones = req.Observaciones,
            TanqueId = req.TanqueId,
            UsuarioId = usuarioId
        });

        inventario.ExistenciaActual += req.Volumen;
        inventario.Disponibilidad += req.Volumen;
        inventario.UltimaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _db.Entry(inventario).Reference(i => i.Tanque).LoadAsync(ct);

        var (diario, mensual) = await GetConsumoAsync(req.TanqueId, ct);
        return Ok(ToDto(inventario, diario, mensual));
    }

    [HttpPost("transferencias")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TransferenciaResultDto>> Transferir(TransferirRequest req, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        if (req.TanqueOrigenId == req.TanqueDestinoId)
            return BadRequest(new { code = "TANQUE_ORIGEN_IGUAL_DESTINO", message = "El tanque de origen y destino no pueden ser el mismo." });

        var tanqueOrigen = await _db.Tanques
            .Include(t => t.Inventario)
            .FirstOrDefaultAsync(t => t.Id == req.TanqueOrigenId, ct);
        if (tanqueOrigen is null)
            return BadRequest(new { code = "TANQUE_ORIGEN_NOT_FOUND", message = "El tanque de origen no existe." });
        if (!tanqueOrigen.Activo)
            return BadRequest(new { code = "TANQUE_ORIGEN_INACTIVO", message = "El tanque de origen no está activo." });

        var tanqueDestino = await _db.Tanques
            .Include(t => t.Inventario)
            .FirstOrDefaultAsync(t => t.Id == req.TanqueDestinoId, ct);
        if (tanqueDestino is null)
            return BadRequest(new { code = "TANQUE_DESTINO_NOT_FOUND", message = "El tanque de destino no existe." });
        if (!tanqueDestino.Activo)
            return BadRequest(new { code = "TANQUE_DESTINO_INACTIVO", message = "El tanque de destino no está activo." });

        var inventarioOrigen = tanqueOrigen.Inventario!;
        var inventarioDestino = tanqueDestino.Inventario!;

        if (inventarioOrigen.ExistenciaActual < req.Volumen)
            return Conflict(new { code = "INVENTARIO_INSUFICIENTE", message = "El tanque de origen no tiene suficiente combustible." });

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Transferencia,
            Volumen = -req.Volumen,
            FechaHora = DateTime.UtcNow,
            ReferenciaOperacion = $"HACIA-TANQUE-{req.TanqueDestinoId}",
            Observaciones = req.Observaciones,
            TanqueId = req.TanqueOrigenId,
            UsuarioId = usuarioId
        });

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Transferencia,
            Volumen = req.Volumen,
            FechaHora = DateTime.UtcNow,
            ReferenciaOperacion = $"DESDE-TANQUE-{req.TanqueOrigenId}",
            Observaciones = req.Observaciones,
            TanqueId = req.TanqueDestinoId,
            UsuarioId = usuarioId
        });

        inventarioOrigen.ExistenciaActual -= req.Volumen;
        inventarioOrigen.Disponibilidad -= req.Volumen;
        inventarioOrigen.UltimaActualizacion = DateTime.UtcNow;

        inventarioDestino.ExistenciaActual += req.Volumen;
        inventarioDestino.Disponibilidad += req.Volumen;
        inventarioDestino.UltimaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _db.Entry(inventarioOrigen).Reference(i => i.Tanque).LoadAsync(ct);
        await _db.Entry(inventarioDestino).Reference(i => i.Tanque).LoadAsync(ct);

        var (diarioO, mensualO) = await GetConsumoAsync(req.TanqueOrigenId, ct);
        var (diarioD, mensualD) = await GetConsumoAsync(req.TanqueDestinoId, ct);
        return Ok(new TransferenciaResultDto(ToDto(inventarioOrigen, diarioO, mensualO), ToDto(inventarioDestino, diarioD, mensualD)));
    }

    private async Task<(decimal Diario, decimal Mensual)> GetConsumoAsync(int tanqueId, CancellationToken ct)
    {
        var hoyUtc = DateTime.UtcNow.Date;
        var inicioMesUtc = new DateTime(hoyUtc.Year, hoyUtc.Month, 1);

        var movs = await _db.MovimientosInventario
            .Where(m => m.TanqueId == tanqueId && m.Tipo == TipoMovimiento.Salida && m.FechaHora >= inicioMesUtc)
            .Select(m => new { m.FechaHora, m.Volumen })
            .ToListAsync(ct);

        return (
            movs.Where(m => m.FechaHora.Date == hoyUtc).Sum(m => -m.Volumen),
            movs.Sum(m => -m.Volumen));
    }

    private async Task<Dictionary<int, (decimal Diario, decimal Mensual)>> GetConsumosBatchAsync(CancellationToken ct)
    {
        var hoyUtc = DateTime.UtcNow.Date;
        var inicioMesUtc = new DateTime(hoyUtc.Year, hoyUtc.Month, 1);

        var movs = await _db.MovimientosInventario
            .Where(m => m.Tipo == TipoMovimiento.Salida && m.FechaHora >= inicioMesUtc)
            .Select(m => new { m.TanqueId, m.FechaHora, m.Volumen })
            .ToListAsync(ct);

        return movs
            .GroupBy(m => m.TanqueId)
            .ToDictionary(
                g => g.Key,
                g => (
                    Diario: g.Where(m => m.FechaHora.Date == hoyUtc).Sum(m => -m.Volumen),
                    Mensual: g.Sum(m => -m.Volumen)));
    }

    private static InventarioDto ToDto(Inventario i, decimal consumoDiario = 0m, decimal consumoMensual = 0m) => new(
        i.Id,
        i.ExistenciaActual,
        i.Disponibilidad,
        i.UltimaActualizacion,
        i.TanqueId, i.Tanque.Identificacion, i.Tanque.Capacidad,
        consumoDiario, consumoMensual);
}
