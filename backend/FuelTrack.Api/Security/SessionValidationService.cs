using FuelTrack.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Security;

public sealed class SessionValidationService
{
    private readonly AppDbContext _db;

    public SessionValidationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsValidAsync(
        int userId,
        int securityVersion,
        CancellationToken cancellationToken = default)
        => await _db.Usuarios
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.Id == userId &&
                    user.Activo &&
                    user.SecurityVersion == securityVersion,
                cancellationToken);
}
