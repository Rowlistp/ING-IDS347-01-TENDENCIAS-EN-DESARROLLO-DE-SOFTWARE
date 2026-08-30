using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Users;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Services;

public sealed class UserService
{
    private readonly AppDbContext _db;
    private readonly PasswordService _passwords;
    private readonly AuditService _audit;

    public UserService(AppDbContext db, PasswordService passwords, AuditService audit)
    {
        _db = db;
        _passwords = passwords;
        _audit = audit;
    }

    public async Task<IReadOnlyCollection<UserResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.UsuarioRoles)
            .ThenInclude(ur => ur.Rol)
            .OrderBy(u => u.NombreUsuario)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                NombreUsuario = u.NombreUsuario,
                Activo = u.Activo,
                Roles = u.UsuarioRoles.Select(ur => ur.Rol.Nombre).OrderBy(r => r).ToArray()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UserResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.UsuarioRoles)
            .ThenInclude(ur => ur.Rol)
            .Where(u => u.Id == id)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                NombreUsuario = u.NombreUsuario,
                Activo = u.Activo,
                Roles = u.UsuarioRoles.Select(ur => ur.Rol.Nombre).OrderBy(r => r).ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        int actorUsuarioId,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var username = request.NombreUsuario.Trim();

        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == username, cancellationToken))
            throw new InvalidOperationException("Ya existe un usuario con ese nombre.");

        var roles = await ResolveRolesAsync(request.RolIds, cancellationToken);

        var usuario = new Usuario
        {
            NombreUsuario = username,
            PasswordHash = _passwords.Hash(request.Contrasena),
            Activo = true
        };

        foreach (var rol in roles)
            usuario.UsuarioRoles.Add(new UsuarioRol { Usuario = usuario, Rol = rol });

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            "USUARIO_CREADO",
            "Usuario",
            usuario.Id.ToString(),
            actorUsuarioId,
            ip,
            new { usuario.NombreUsuario, Roles = roles.Select(r => r.Nombre) },
            cancellationToken);

        return (await GetByIdAsync(usuario.Id, cancellationToken))!;
    }

    public async Task<UserResponse?> UpdateAsync(
        int id,
        UpdateUserRequest request,
        int actorUsuarioId,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.UsuarioRoles)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (usuario is null)
            return null;

        var username = request.NombreUsuario.Trim();

        if (await _db.Usuarios.AnyAsync(
            u => u.Id != id && u.NombreUsuario == username,
            cancellationToken))
        {
            throw new InvalidOperationException("Ya existe un usuario con ese nombre.");
        }

        var roles = await ResolveRolesAsync(request.RolIds, cancellationToken);

        usuario.NombreUsuario = username;
        usuario.SecurityVersion++;
        _db.UsuarioRoles.RemoveRange(usuario.UsuarioRoles);
        usuario.UsuarioRoles = roles
            .Select(rol => new UsuarioRol { UsuarioId = id, RolId = rol.Id })
            .ToList();

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            "USUARIO_MODIFICADO",
            "Usuario",
            id.ToString(),
            actorUsuarioId,
            ip,
            new { usuario.NombreUsuario, Roles = roles.Select(r => r.Nombre) },
            cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> SetStatusAsync(
        int id,
        bool activo,
        int actorUsuarioId,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _db.Usuarios
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (usuario is null)
            return false;

        if (usuario.Activo == activo)
            return true;

        usuario.Activo = activo;
        usuario.SecurityVersion++;

        if (!activo)
        {
            var tokens = await _db.RefreshTokens
                .Where(t => t.UsuarioId == id && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.RevokedAtUtc = DateTime.UtcNow;
                token.RevokedByIp = ip;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            activo ? "USUARIO_ACTIVADO" : "USUARIO_DESACTIVADO",
            "Usuario",
            id.ToString(),
            actorUsuarioId,
            ip,
            null,
            cancellationToken);

        return true;
    }

    private async Task<List<Rol>> ResolveRolesAsync(
        IReadOnlyCollection<int> rolIds,
        CancellationToken cancellationToken)
    {
        if (rolIds.Count == 0)
            return [];

        var distinctIds = rolIds.Distinct().ToArray();

        var roles = await _db.Roles
            .Where(r => distinctIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        if (roles.Count != distinctIds.Length)
            throw new InvalidOperationException("Uno o más roles no existen.");

        return roles;
    }
}
