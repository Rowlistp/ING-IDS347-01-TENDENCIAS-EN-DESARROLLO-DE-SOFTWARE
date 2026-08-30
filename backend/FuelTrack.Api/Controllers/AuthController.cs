using System.Security.Claims;
using FuelTrack.Api.DTOs.Auth;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(request, GetClientIp(), cancellationToken);

        return result is null
            ? Unauthorized(new { code = "INVALID_CREDENTIALS", message = "Credenciales inválidas." })
            : Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _auth.RefreshAsync(
            request.RefreshToken,
            GetClientIp(),
            cancellationToken);

        return result is null
            ? Unauthorized(new { code = "INVALID_REFRESH_TOKEN", message = "Refresh token inválido o expirado." })
            : Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _auth.LogoutAsync(request.RefreshToken, GetClientIp(), cancellationToken);
        return NoContent();
    }

    // Fase 1: reset administrativo. El flujo de recuperación por correo queda para
    // la fase de integraciones, cuando exista SMTP.
    [Authorize(Roles = Roles.Administrador)]
    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var adminId))
            return Unauthorized();

        var updated = await _auth.ResetPasswordAsync(
            request.UsuarioId,
            request.NuevaContrasena,
            adminId,
            GetClientIp(),
            cancellationToken);

        return updated ? NoContent() : NotFound();
    }

    private string? GetClientIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    private bool TryGetCurrentUserId(out int userId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
