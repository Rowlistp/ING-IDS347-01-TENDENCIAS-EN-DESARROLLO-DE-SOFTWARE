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
                new
                {
                    Motivo = usuario is not null && !usuario.Activo
                        ? "UsuarioInactivo"
                        : "CredencialesInvalidas"
                },
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
        var now = DateTime.UtcNow;

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = refreshHash,
            UsuarioId = usuario.Id,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenDays),
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
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return null;

        var tokenHash = TokenService.HashRefreshToken(rawRefreshToken);
        var now = DateTime.UtcNow;

        await using var transaction = await _db.Database
            .BeginTransactionAsync(cancellationToken);

        var stored = await _db.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is null || stored.RevokedAtUtc is not null || stored.ExpiresAtUtc <= now)
            return null;

        var usuario = await _db.Usuarios
            .Include(u => u.UsuarioRoles)
            .ThenInclude(ur => ur.Rol)
            .SingleOrDefaultAsync(u => u.Id == stored.UsuarioId, cancellationToken);

        if (usuario is null || !usuario.Activo)
            return null;

        var roles = usuario.UsuarioRoles
            .Select(ur => ur.Rol.Nombre)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var newRawRefreshToken = _tokens.CreateRefreshToken();
        var newHash = TokenService.HashRefreshToken(newRawRefreshToken);

        // Reclama el refresh token mediante una actualizacion condicional atomica.
        // Si otra solicitud ya lo uso, rowsAffected sera 0 y esta solicitud falla.
        var rowsAffected = await _db.RefreshTokens
            .Where(t =>
                t.Id == stored.Id &&
                t.TokenHash == tokenHash &&
                t.RevokedAtUtc == null &&
                t.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAtUtc, now)
                    .SetProperty(t => t.RevokedByIp, ip)
                    .SetProperty(t => t.ReplacedByTokenHash, newHash),
                cancellationToken);

        if (rowsAffected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = newHash,
            UsuarioId = usuario.Id,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ip
        });

        var (accessToken, accessExpires) = _tokens.CreateAccessToken(usuario, roles);

        await _db.SaveChangesAsync(cancellationToken);

        // La auditoria queda dentro de la misma transaccion de rotacion.
        await _audit.WriteAsync(
            "TOKEN_RENOVADO",
            "Usuario",
            usuario.Id.ToString(),
            usuario.Id,
            ip,
            null,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRawRefreshToken,
            AccessTokenExpiresAtUtc = accessExpires,
            UsuarioId = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            Roles = roles
        };
    }

    public async Task<bool> LogoutAsync(
        string rawRefreshToken,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return false;

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
            .Where(t =>
                t.UsuarioId == usuarioId &&
                t.RevokedAtUtc == null &&
                t.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
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
