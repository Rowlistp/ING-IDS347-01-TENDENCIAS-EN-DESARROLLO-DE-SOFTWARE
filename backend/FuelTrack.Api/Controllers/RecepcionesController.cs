using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Recepciones;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/recepciones")]
[Authorize]
public sealed class RecepcionesController : ControllerBase
{
    private readonly AppDbContext _db;
    public RecepcionesController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<RecepcionDto>>> GetAll(CancellationToken ct)
        => throw new NotImplementedException();

    [HttpGet("{id:int}")]
    public Task<ActionResult<RecepcionDto>> GetById(int id, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<RecepcionDto>> Create(CreateRecepcionRequest req, CancellationToken ct)
        => throw new NotImplementedException();
}
