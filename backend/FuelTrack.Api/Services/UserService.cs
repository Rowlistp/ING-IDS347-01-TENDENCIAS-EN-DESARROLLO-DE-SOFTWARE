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
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var username = request.NombreUsuario.Trim();
        PasswordService.ValidatePolicy(request.Contrasena);

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

        await transaction.CommitAsync(cancellationToken);

        return (await GetByIdAsync(usuario.Id, cancellationToken))!;
    }

    public async Task<UserResponse?> UpdateAsync(
        int id,
        UpdateUserRequest request,
        int actorUsuarioId,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await AcquireAdministratorGuardAsync(cancellationToken);

        var usuario = await _db.Usuarios
            .Include(u => u.UsuarioRoles)
            .ThenInclude(ur => ur.Rol)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (usuario is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var username = request.NombreUsuario.Trim();

        if (await _db.Usuarios.AnyAsync(
            u => u.Id != id && u.NombreUsuario == username,
            cancellationToken))
        {
            throw new InvalidOperationException("Ya existe un usuario con ese nombre.");
        }

        var roles = await ResolveRolesAsync(request.RolIds, cancellationToken);
        var isAdministrator = usuario.UsuarioRoles.Any(
            ur => ur.Rol.Nombre == Roles.Administrador);
        var remainsAdministrator = roles.Any(
            rol => rol.Nombre == Roles.Administrador);

        if (isAdministrator && !remainsAdministrator)
        {
            if (actorUsuarioId == id)
            {
                throw new AdministrativeLockoutException(
                    "Un administrador no puede retirar su propio rol Administrador.");
            }

            await EnsureAnotherActiveAdministratorAsync(id, cancellationToken);
        }

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

        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> SetStatusAsync(
        int id,
        bool activo,
        int actorUsuarioId,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await AcquireAdministratorGuardAsync(cancellationToken);

        var usuario = await _db.Usuarios
            .Include(u => u.UsuarioRoles)
            .ThenInclude(ur => ur.Rol)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (usuario is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (usuario.Activo == activo)
        {
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        if (!activo && usuario.UsuarioRoles.Any(ur => ur.Rol.Nombre == Roles.Administrador))
            await EnsureAnotherActiveAdministratorAsync(id, cancellationToken);

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

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private async Task EnsureAnotherActiveAdministratorAsync(
        int excludedUserId,
        CancellationToken cancellationToken)
    {
        var anotherAdministratorExists = await _db.Usuarios
            .AsNoTracking()
            .AnyAsync(
                u => u.Id != excludedUserId &&
                    u.Activo &&
                    u.UsuarioRoles.Any(ur => ur.Rol.Nombre == Roles.Administrador),
                cancellationToken);

        if (!anotherAdministratorExists)
        {
            throw new AdministrativeLockoutException(
                "La operación dejaría el sistema sin un administrador activo.");
        }
    }

    private async Task AcquireAdministratorGuardAsync(CancellationToken cancellationToken)
    {
        if (_db.Database.IsNpgsql())
        {
            await _db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(2026083001)",
                cancellationToken);
        }
    }

    private async Task<List<Rol>> ResolveRolesAsync(
        IReadOnlyCollection<int> rolIds,
        CancellationToken cancellationToken)
    {
        if (rolIds.Count == 0)
            throw new UserValidationException("ROLE_REQUIRED", "Debe asignar al menos un rol.");

        var distinctIds = rolIds.Distinct().ToArray();

        if (distinctIds.Length != rolIds.Count)
            throw new UserValidationException("DUPLICATE_ROLE", "No se permiten roles duplicados.");

        var roles = await _db.Roles
            .Where(r => distinctIds.Contains(r.Id) && Roles.Todos.Contains(r.Nombre))
            .ToListAsync(cancellationToken);

        if (roles.Count != distinctIds.Length)
            throw new UserValidationException("INVALID_ROLE", "Uno o más roles no son válidos.");

        return roles;
    }
}
