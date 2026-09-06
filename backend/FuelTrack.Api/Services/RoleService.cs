using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Roles;
using FuelTrack.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Services;

public sealed class RoleService(AppDbContext db)
{
    public async Task<IReadOnlyCollection<RoleResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await db.Roles
            .AsNoTracking()
            .Where(role => Roles.Todos.Contains(role.Nombre))
            .OrderBy(role => role.Nombre)
            .Select(role => new RoleResponse { Id = role.Id, Nombre = role.Nombre })
            .ToListAsync(cancellationToken);
}
