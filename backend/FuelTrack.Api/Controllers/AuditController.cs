using System.ComponentModel.DataAnnotations;
using FuelTrack.Api.DTOs.Audit;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Auditor}")]
[Route("api/v1/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly AuditService _audit;

    public AuditController(AuditService audit)
    {
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<AuditPageResponse>> GetAll(
        [FromQuery, Range(1, 1_000_000)] int pagina = 1,
        [FromQuery, Range(1, 100)] int tamanoPagina = 50,
        CancellationToken cancellationToken = default)
        => Ok(await _audit.GetPageAsync(pagina, tamanoPagina, cancellationToken));
}
