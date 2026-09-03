using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Inventario;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/inventario")]
[Authorize]
public sealed class InventarioController : ControllerBase
{
    private readonly AppDbContext _db;
    public InventarioController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<InventarioDto>>> GetAll(CancellationToken ct)
        => throw new NotImplementedException();

    [HttpGet("{tanqueId:int}")]
    public Task<ActionResult<InventarioDto>> GetByTanque(int tanqueId, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost("ajustes")]
    [Authorize(Roles = Roles.Administrador)]
    public Task<ActionResult<InventarioDto>> Ajustar(AjustarInventarioRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost("transferencias")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<TransferenciaResultDto>> Transferir(TransferirRequest req, CancellationToken ct)
        => throw new NotImplementedException();
}
