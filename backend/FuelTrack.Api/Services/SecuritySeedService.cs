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
        foreach (var roleName in Roles.Todos)
        {
            if (!await _db.Roles.AnyAsync(r => r.Nombre == roleName, cancellationToken))
                _db.Roles.Add(new Rol { Nombre = roleName });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var adminUsername = _configuration["BootstrapAdmin:Username"];
        var adminPassword = _configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == adminUsername, cancellationToken))
            return;

        var adminRole = await _db.Roles
            .SingleAsync(r => r.Nombre == Roles.Administrador, cancellationToken);

        var admin = new Usuario
        {
            NombreUsuario = adminUsername.Trim(),
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
