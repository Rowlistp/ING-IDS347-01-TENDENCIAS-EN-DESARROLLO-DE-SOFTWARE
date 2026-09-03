using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Movimientos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/inventario/movimientos")]
[Authorize]
public sealed class MovimientosController : ControllerBase
{
    private readonly AppDbContext _db;
    public MovimientosController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<MovimientoDto>>> GetAll([FromQuery] int? tanqueId, CancellationToken ct)
        => throw new NotImplementedException();
}
