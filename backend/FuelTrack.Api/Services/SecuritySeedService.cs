using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Services;

public sealed class SecuritySeedService
{
    private readonly AppDbContext _db;
    private readonly PasswordService _passwords;
    private readonly IConfiguration _configuration;

    public SecuritySeedService(
        AppDbContext db,
        PasswordService passwords,
        IConfiguration configuration)
    {
        _db = db;
        _passwords = passwords;
        _configuration = configuration;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (_db.Database.IsNpgsql())
        {
            foreach (var roleName in Roles.Todos)
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO \"Roles\" (\"Nombre\") VALUES ({roleName}) ON CONFLICT (\"Nombre\") DO NOTHING",
                    cancellationToken);
            }
        }
        else
        {
            foreach (var roleName in Roles.Todos)
            {
                if (!await _db.Roles.AnyAsync(r => r.Nombre == roleName, cancellationToken))
                    _db.Roles.Add(new Rol { Nombre = roleName });
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        var adminUsername = _configuration["BootstrapAdmin:Username"];
        var adminPassword = _configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        var normalizedUsername = adminUsername.Trim();
        PasswordService.ValidatePolicy(adminPassword);

        if (_db.Database.IsNpgsql())
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            var passwordHash = _passwords.Hash(adminPassword);
            var created = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"Usuarios\" (\"NombreUsuario\", \"PasswordHash\", \"Activo\", \"SecurityVersion\") VALUES ({normalizedUsername}, {passwordHash}, TRUE, 1) ON CONFLICT (\"NombreUsuario\") DO NOTHING",
                cancellationToken);

            if (created == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"UsuarioRoles\" (\"UsuarioId\", \"RolId\") SELECT u.\"Id\", r.\"Id\" FROM \"Usuarios\" u CROSS JOIN \"Roles\" r WHERE u.\"NombreUsuario\" = {normalizedUsername} AND r.\"Nombre\" = {Roles.Administrador} ON CONFLICT (\"UsuarioId\", \"RolId\") DO NOTHING",
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (await _db.Usuarios.AnyAsync(
            u => u.NombreUsuario == normalizedUsername,
            cancellationToken))
        {
            return;
        }

        var adminRole = await _db.Roles
            .SingleAsync(r => r.Nombre == Roles.Administrador, cancellationToken);

        var admin = new Usuario
        {
            NombreUsuario = normalizedUsername,
            PasswordHash = _passwords.Hash(adminPassword),
            Activo = true
        };

        admin.UsuarioRoles.Add(new UsuarioRol
        {
            Usuario = admin,
            Rol = adminRole
        });

        _db.Usuarios.Add(admin);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
