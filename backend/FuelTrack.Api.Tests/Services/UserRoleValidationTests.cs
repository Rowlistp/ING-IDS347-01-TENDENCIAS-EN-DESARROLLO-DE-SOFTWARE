using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Users;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Services;

[TestClass]
public sealed class UserRoleValidationTests
{
    [TestMethod]
    [DataRow("empty")]
    [DataRow("duplicate")]
    [DataRow("unknown")]
    public async Task CreateUser_RejectsInvalidRoleAssignments(string caseName)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var role = new Rol { Nombre = Roles.Consulta };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        List<int> roleIds = caseName switch
        {
            "empty" => [],
            "duplicate" => [role.Id, role.Id],
            _ => [int.MaxValue]
        };
        var users = new UserService(db, new PasswordService(), new AuditService(db));

        await Assert.ThrowsExactlyAsync<UserValidationException>(() => users.CreateAsync(
            new CreateUserRequest
            {
                NombreUsuario = $"roles-{caseName}",
                Contrasena = "Clave-Roles-123!",
                RolIds = roleIds
            },
            1,
            "127.0.0.1"));
    }
}
