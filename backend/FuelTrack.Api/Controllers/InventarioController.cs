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
        return Ok(list.ConvertAll(ToDto));
    }

    [HttpGet("{tanqueId:int}")]
    public async Task<ActionResult<InventarioDto>> GetByTanque(int tanqueId, CancellationToken ct)
    {
        var inv = await _db.Inventarios
            .AsNoTracking()
            .Include(i => i.Tanque)
            .FirstOrDefaultAsync(i => i.TanqueId == tanqueId, ct);
        return inv is null ? NotFound() : Ok(ToDto(inv));
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

        return Ok(ToDto(inventario));
    }

    [HttpPost("transferencias")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<TransferenciaResultDto>> Transferir(TransferirRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    private static InventarioDto ToDto(Inventario i) => new(
        i.Id,
        i.ExistenciaActual,
        i.Disponibilidad,
        i.UltimaActualizacion,
        i.TanqueId, i.Tanque.Identificacion, i.Tanque.Capacidad);
}
