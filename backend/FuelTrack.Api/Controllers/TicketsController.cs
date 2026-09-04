using System.Security.Claims;
using FuelTrack.Api.DTOs.Tickets;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/tickets")]
[Authorize]
public sealed class TicketsController(TicketService tickets) : ControllerBase
{
    private const string OperationalRoles =
        $"{Roles.Administrador},{Roles.Supervisor},{Roles.Despachador},{Roles.Auditor},{Roles.Consulta}";
    private const string ManagementRoles = $"{Roles.Administrador},{Roles.Supervisor}";

    [HttpGet]
    [Authorize(Roles = OperationalRoles)]
    public async Task<ActionResult<IReadOnlyCollection<TicketResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await tickets.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = OperationalRoles)]
    public async Task<ActionResult<TicketResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await tickets.GetByIdAsync(id, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost]
    [Authorize(Roles = ManagementRoles)]
    public async Task<ActionResult<TicketResponse>> Create(
        CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var actorId))
            return Unauthorized();

        try
        {
            var created = await tickets.CreateAsync(
                request,
                actorId,
                GetClientIp(),
                cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Ticket.Id }, created.Ticket);
        }
        catch (TicketDomainException exception)
        {
            return DomainError<TicketResponse>(exception);
        }
    }

    [HttpPost("validar")]
    [Authorize(Roles = OperationalRoles)]
    public async Task<ActionResult<TicketValidationResponse>> Validate(
        ValidateTicketRequest request,
        CancellationToken cancellationToken)
        => Ok(await tickets.ValidateAsync(request.QrPayload, cancellationToken));

    [HttpPost("{id:guid}/anular")]
    [Authorize(Roles = ManagementRoles)]
    public async Task<ActionResult<TicketResponse>> Cancel(
        Guid id,
        CancelTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var actorId))
            return Unauthorized();

        try
        {
            return Ok(await tickets.CancelAsync(
                id,
                request.Motivo,
                actorId,
                GetClientIp(),
                cancellationToken));
        }
        catch (TicketDomainException exception)
        {
            return DomainError<TicketResponse>(exception);
        }
    }

    [HttpPost("{id:guid}/enviar")]
    [Authorize(Roles = ManagementRoles)]
    public async Task<ActionResult<SendTicketResponse>> Send(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var actorId))
            return Unauthorized();

        try
        {
            return Ok(await tickets.PrepareSendAsync(
                id,
                actorId,
                GetClientIp(),
                cancellationToken));
        }
        catch (TicketDomainException exception)
        {
            return DomainError<SendTicketResponse>(exception);
        }
    }

    [HttpGet("{id:guid}/pdf")]
    [Authorize(Roles = OperationalRoles)]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var actorId))
            return Unauthorized();

        try
        {
            var result = await tickets.GeneratePdfAsync(
                id,
                actorId,
                GetClientIp(),
                cancellationToken);
            return File(result.Content, "application/pdf", result.FileName);
        }
        catch (TicketDomainException exception)
        {
            return StatusCode(exception.StatusCode, new { code = exception.Code, message = exception.Message });
        }
    }

    private ActionResult<T> DomainError<T>(TicketDomainException exception)
        => StatusCode(exception.StatusCode, new { code = exception.Code, message = exception.Message });

    private string? GetClientIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    private bool TryGetCurrentUserId(out int userId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
