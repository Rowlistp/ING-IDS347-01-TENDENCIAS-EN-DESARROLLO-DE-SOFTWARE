using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Auth;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuelTrack.Api.Services;

public sealed class AuthService
{
    private readonly AppDbContext _db;
    private readonly PasswordService _passwords;
    private readonly TokenService _tokens;
    private readonly AuditService _audit;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AppDbContext db,
        PasswordService passwords,
        TokenService tokens,
        AuditService audit,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
        _audit = audit;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse?> LoginAsync(
        LoginRequest request,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var username = request.NombreUsuario.Trim();

        var usuario = await _db.Usuarios
            .Include(u => u.UsuarioRoles)
            .ThenInclude(ur => ur.Rol)
            .SingleOrDefaultAsync(u => u.NombreUsuario == username, cancellationToken);

        if (usuario is null || !usuario.Activo || !_passwords.Verify(request.Contrasena, usuario.PasswordHash))
        {
            await _audit.WriteAsync(
                "LOGIN_FALLIDO",
                "Usuario",
                username,
                usuario?.Id,
                ip,
                new { Motivo = usuario is not null && !usuario.Activo ? "UsuarioInactivo" : "CredencialesInvalidas" },
                cancellationToken);

            return null;
        }

        var roles = usuario.UsuarioRoles
            .Select(ur => ur.Rol.Nombre)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var (accessToken, accessExpires) = _tokens.CreateAccessToken(usuario, roles);
        var rawRefreshToken = _tokens.CreateRefreshToken();
        var refreshHash = TokenService.HashRefreshToken(rawRefreshToken);

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = refreshHash,
            UsuarioId = usuario.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ip
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            "LOGIN_EXITOSO",
            "Usuario",
            usuario.Id.ToString(),
            usuario.Id,
            ip,
            null,
            cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            AccessTokenExpiresAtUtc = accessExpires,
            UsuarioId = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            Roles = roles
        };
    }

    public async Task<AuthResponse?> RefreshAsync(
        string rawRefreshToken,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = TokenService.HashRefreshToken(rawRefreshToken);

        var stored = await _db.RefreshTokens
            .Include(t => t.Usuario)
                .ThenInclude(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is null || !stored.IsActive || !stored.Usuario.Activo)
            return null;

        var roles = stored.Usuario.UsuarioRoles
            .Select(ur => ur.Rol.Nombre)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var newRawRefreshToken = _tokens.CreateRefreshToken();
        var newHash = TokenService.HashRefreshToken(newRawRefreshToken);
        var now = DateTime.UtcNow;

        stored.RevokedAtUtc = now;
        stored.RevokedByIp = ip;
        stored.ReplacedByTokenHash = newHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = newHash,
            UsuarioId = stored.UsuarioId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ip
        });

        var (accessToken, accessExpires) = _tokens.CreateAccessToken(stored.Usuario, roles);

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            "TOKEN_RENOVADO",
            "Usuario",
            stored.UsuarioId.ToString(),
            stored.UsuarioId,
            ip,
            null,
            cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRawRefreshToken,
            AccessTokenExpiresAtUtc = accessExpires,
            UsuarioId = stored.Usuario.Id,
            NombreUsuario = stored.Usuario.NombreUsuario,
            Roles = roles
        };
    }

    public async Task<bool> LogoutAsync(
        string rawRefreshToken,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = TokenService.HashRefreshToken(rawRefreshToken);

        var stored = await _db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is null)
            return false;

        if (stored.RevokedAtUtc is null)
        {
            stored.RevokedAtUtc = DateTime.UtcNow;
            stored.RevokedByIp = ip;
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _audit.WriteAsync(
            "LOGOUT",
            "Usuario",
            stored.UsuarioId.ToString(),
            stored.UsuarioId,
            ip,
            null,
            cancellationToken);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(
        int usuarioId,
        string nuevaContrasena,
        int adminUsuarioId,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _db.Usuarios
            .SingleOrDefaultAsync(u => u.Id == usuarioId, cancellationToken);

        if (usuario is null)
            return false;

        usuario.PasswordHash = _passwords.Hash(nuevaContrasena);

        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevokedByIp = ip;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            "PASSWORD_RESET_ADMIN",
            "Usuario",
            usuarioId.ToString(),
            adminUsuarioId,
            ip,
            new { UsuarioObjetivoId = usuarioId },
            cancellationToken);

        return true;
    }
}
