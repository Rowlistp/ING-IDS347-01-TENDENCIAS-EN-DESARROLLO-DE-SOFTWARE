using FuelTrack.Api.Data;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController, Route("api/v1/estaciones")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Despachador},{Roles.Auditor},{Roles.Consulta}")]
public sealed class EstacionesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await db.Estaciones.AsNoTracking().Where(e => e.Activo).OrderBy(e => e.Nombre)
            .Select(e => new { e.Id, e.Nombre, e.Activo }).ToListAsync(ct));
}
