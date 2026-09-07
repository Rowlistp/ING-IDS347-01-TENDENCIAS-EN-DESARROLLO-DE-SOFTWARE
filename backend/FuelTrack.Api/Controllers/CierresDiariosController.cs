using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FuelTrack.Api.DTOs.Cierres;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController, Route("api/v1/cierres-diarios")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Auditor}")]
public sealed class CierresDiariosController(CierreDiarioService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CierreDiarioResponse>>> GetAll(
        CancellationToken ct,
        [Range(1, 100000)] int pagina = 1,
        [Range(1, 100)] int tamanoPagina = 20)
        => Ok(await service.GetAllAsync(pagina, tamanoPagina, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CierreDiarioResponse>> GetById(int id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<ActionResult> GetPdf(int id, CancellationToken ct)
    {
        var pdf = await service.GetPdfAsync(id, ct);
        return pdf is null ? NotFound() : File(pdf, "application/pdf", $"cierre-{id}.pdf");
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<CierreDiarioResponse>> Crear(
        [FromBody] CierreDiarioRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Unauthorized();
        try
        {
            var result = await service.GenerarAsync(
                request.Fecha, actorId,
                HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (TicketDomainException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }
}
