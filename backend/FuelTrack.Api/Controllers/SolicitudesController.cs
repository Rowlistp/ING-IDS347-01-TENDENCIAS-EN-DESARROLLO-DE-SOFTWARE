// backend/FuelTrack.Api/Controllers/SolicitudesController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Solicitudes;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/solicitudes")]
[Authorize]
public sealed class SolicitudesController : ControllerBase
{
    private readonly AppDbContext _db;
    public SolicitudesController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<SolicitudDto>>> GetAll(CancellationToken ct)
        => throw new NotImplementedException();

    [HttpGet("{id:int}")]
    public Task<ActionResult<SolicitudDto>> GetById(int id, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Solicitante}")]
    public Task<ActionResult<SolicitudDto>> Create(CreateSolicitudRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost("{id:int}/aprobar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<SolicitudDto>> Aprobar(int id, AprobarSolicitudRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost("{id:int}/rechazar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<SolicitudDto>> Rechazar(int id, RechazarSolicitudRequest req, CancellationToken ct)
        => throw new NotImplementedException();
}
