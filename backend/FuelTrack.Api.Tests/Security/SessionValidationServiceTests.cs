using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Security;

[TestClass]
public sealed class SessionValidationServiceTests
{
    [TestMethod]
    public async Task IsValid_RequiresActiveUserAndCurrentSecurityVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options);
        await db.Database.EnsureCreatedAsync();
        var user = new Usuario
        {
            NombreUsuario = "session-user",
            PasswordHash = new PasswordService().Hash("Clave-Session-123!"),
            Activo = true,
            SecurityVersion = 3
        };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        var service = new SessionValidationService(db);

        Assert.IsTrue(await service.IsValidAsync(user.Id, 3));
        Assert.IsFalse(await service.IsValidAsync(user.Id, 2));

        user.Activo = false;
        await db.SaveChangesAsync();
        Assert.IsFalse(await service.IsValidAsync(user.Id, 3));
    }
}
