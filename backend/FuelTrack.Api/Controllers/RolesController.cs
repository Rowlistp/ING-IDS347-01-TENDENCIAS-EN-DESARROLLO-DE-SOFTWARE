using FuelTrack.Api.DTOs.Roles;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/v1/roles")]
public sealed class RolesController(RoleService roles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await roles.GetAllAsync(cancellationToken));
}
