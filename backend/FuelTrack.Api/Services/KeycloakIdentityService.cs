using System.Security.Claims;
using FuelTrack.Api.Data;
using FuelTrack.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Services;

public sealed class KeycloakIdentityService(AppDbContext db)
{
    public async Task<ClaimsPrincipal?> ResolveAsync(
        ClaimsPrincipal externalPrincipal,
        string identityClaim,
        CancellationToken cancellationToken)
    {
        var externalIdentity = externalPrincipal.FindFirstValue(identityClaim)?.Trim();
        if (string.IsNullOrWhiteSpace(externalIdentity))
            return null;

        var localUser = await db.Usuarios
            .AsNoTracking()
            .Include(user => user.UsuarioRoles)
            .ThenInclude(userRole => userRole.Rol)
            .SingleOrDefaultAsync(
                user => user.NombreUsuario == externalIdentity && user.Activo,
                cancellationToken);

        if (localUser is null)
            return null;

        var localRoles = localUser.UsuarioRoles
            .Select(userRole => userRole.Rol.Nombre)
            .Where(Roles.Todos.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (localRoles.Length == 0)
            return null;

        // Los claims de autorización externos nunca se copian como roles locales.
        var safeExternalClaims = externalPrincipal.Claims.Where(claim =>
            claim.Type != ClaimTypes.NameIdentifier &&
            claim.Type != ClaimTypes.Name &&
            claim.Type != ClaimTypes.Role &&
            claim.Type != "role" &&
            claim.Type != "roles");

        var identity = new ClaimsIdentity(
            safeExternalClaims,
            AuthenticationSchemes.Keycloak,
            ClaimTypes.Name,
            ClaimTypes.Role);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, localUser.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, localUser.NombreUsuario));
        identity.AddClaim(new Claim("authentication_source", "keycloak"));

        foreach (var role in localRoles)
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        return new ClaimsPrincipal(identity);
    }
}
