using System.ComponentModel.DataAnnotations;
using FuelTrack.Api.DTOs.Reportes;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController, Route("api/v1/reportes")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Auditor}")]
public sealed class ReportesController(ReporteService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReportePageResponse>> Get(
        CancellationToken ct,
        [Required] string tipo,
        DateOnly? fechaDesde = null,
        DateOnly? fechaHasta = null,
        int? tanqueId = null,
        [Range(1, 100000)] int pagina = 1,
        [Range(1, 100)] int tamanoPagina = 20)
    {
        try
        {
            var q = new ReporteQuery(tipo, fechaDesde, fechaHasta, tanqueId, pagina, tamanoPagina);
            return Ok(await service.GetAsync(q, ct));
        }
        catch (TicketDomainException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpGet("exportar")]
    public async Task<ActionResult> Exportar(
        CancellationToken ct,
        [Required] string tipo,
        [Required] string formato,
        DateOnly? fechaDesde = null,
        DateOnly? fechaHasta = null,
        int? tanqueId = null)
    {
        try
        {
            var q = new ReporteQuery(tipo, fechaDesde, fechaHasta, tanqueId, 1, int.MaxValue);
            var bytes = await service.ExportarAsync(q, formato, ct);
            var (contentType, ext) = formato switch
            {
                "csv"   => ("text/csv; charset=utf-8", "csv"),
                "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
                "pdf"   => ("application/pdf", "pdf"),
                _       => ("application/octet-stream", "bin")
            };
            return File(bytes, contentType, $"reporte-{tipo}-{DateTime.UtcNow:yyyyMMdd}.{ext}");
        }
        catch (TicketDomainException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }
}
