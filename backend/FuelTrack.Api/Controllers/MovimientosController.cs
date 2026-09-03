using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Movimientos;
using FuelTrack.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/inventario/movimientos")]
[Authorize]
public sealed class MovimientosController : ControllerBase
{
    private readonly AppDbContext _db;
    public MovimientosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MovimientoDto>>> GetAll(
        [FromQuery] int? tanqueId, CancellationToken ct)
    {
        var query = _db.MovimientosInventario
            .AsNoTracking()
            .Include(m => m.Tanque)
            .Include(m => m.Usuario)
            .AsQueryable();

        if (tanqueId.HasValue)
            query = query.Where(m => m.TanqueId == tanqueId.Value);

        var list = await query.ToListAsync(ct);
        return Ok(list.ConvertAll(ToDto));
    }

    private static MovimientoDto ToDto(MovimientoInventario m) => new(
        m.Id,
        m.Tipo,
        m.Volumen,
        m.FechaHora,
        m.ReferenciaOperacion,
        m.Observaciones,
        m.TanqueId, m.Tanque.Identificacion,
        m.UsuarioId, m.Usuario.NombreUsuario);
}
