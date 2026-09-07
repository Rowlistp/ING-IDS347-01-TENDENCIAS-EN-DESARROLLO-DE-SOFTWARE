using FuelTrack.Api.DTOs.Dashboard;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController, Route("api/v1/dashboard")]
[Authorize(Roles = Roles.Administrador)]
public sealed class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet("resumen")]
    public async Task<ActionResult<DashboardResumenResponse>> GetResumen(CancellationToken ct)
        => Ok(await service.GetResumenAsync(ct));
}
