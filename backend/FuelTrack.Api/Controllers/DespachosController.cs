using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FuelTrack.Api.DTOs.Dispatch;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController, Route("api/v1/despachos")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Despachador},{Roles.Auditor},{Roles.Consulta}")]
public sealed class DespachosController(DispatchService dispatches) : ControllerBase
{
    [HttpPost, Authorize(Roles = Roles.Despachador)]
    public async Task<ActionResult<DispatchResponse>> Create(CreateDispatchRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Unauthorized();
        try
        {
            var result = await dispatches.CreateAsync(request, actorId, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return CreatedAtAction(nameof(GetById), new { id = result.DespachoId }, result);
        }
        catch (TicketDomainException ex) { return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message }); }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DispatchResponse>>> GetAll(
        CancellationToken ct, [Range(1, 100000)] int pagina = 1, [Range(1, 100)] int tamanoPagina = 20, Guid? ticketId = null)
        => Ok(await dispatches.GetAllAsync(pagina, tamanoPagina, OperatorFilter(), ticketId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DispatchResponse>> GetById(int id, CancellationToken ct)
    {
        var result = await dispatches.GetByIdAsync(id, OperatorFilter(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    private int? OperatorFilter()
        => new[] { Roles.Administrador, Roles.Supervisor, Roles.Auditor, Roles.Consulta }.Any(User.IsInRole)
            ? null : int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
