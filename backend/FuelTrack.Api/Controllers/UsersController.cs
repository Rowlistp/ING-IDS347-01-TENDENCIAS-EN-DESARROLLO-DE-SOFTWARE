using System.Security.Claims;
using FuelTrack.Api.DTOs.Users;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/v1/usuarios")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await _users.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var actorId))
            return Unauthorized();

        try
        {
            var created = await _users.CreateAsync(
                request,
                actorId,
                GetClientIp(),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { code = "USER_CONFLICT", message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var actorId))
            return Unauthorized();

        try
        {
            var updated = await _users.UpdateAsync(
                id,
                request,
                actorId,
                GetClientIp(),
                cancellationToken);

            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { code = "USER_CONFLICT", message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var actorId))
            return Unauthorized();

        if (actorId == id && !request.Activo)
        {
            return BadRequest(new
            {
                code = "SELF_DEACTIVATION_BLOCKED",
                message = "Un administrador no puede desactivarse a sí mismo desde esta operación."
            });
        }

        var updated = await _users.SetStatusAsync(
            id,
            request.Activo,
            actorId,
            GetClientIp(),
            cancellationToken);

        return updated ? NoContent() : NotFound();
    }

    private string? GetClientIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    private bool TryGetCurrentUserId(out int userId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
