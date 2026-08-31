using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Auth;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FuelTrack.Api.Tests.Integration;

[TestClass]
[TestCategory("PostgreSQL")]
public sealed class PostgreSqlSecurityTests
{
    private string _connectionString = null!;
    private bool _canDestroyDatabase;

    [TestInitialize]
    public async Task Setup()
    {
        _connectionString = Environment.GetEnvironmentVariable("FUELTRACK_TEST_CONNECTION") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            Assert.Inconclusive(
                "Defina FUELTRACK_TEST_CONNECTION apuntando exclusivamente a una base PostgreSQL de pruebas.");
        }

        var databaseName = new NpgsqlConnectionStringBuilder(_connectionString).Database;
        if (string.IsNullOrWhiteSpace(databaseName) ||
            !databaseName.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Fail(
                "La prueba destructiva solo acepta una base cuyo nombre contenga 'test'. Nunca use la base de desarrollo.");
        }

        _canDestroyDatabase = true;

        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (!_canDestroyDatabase)
            return;

        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task MigrationsFromZero_CreateSecuritySchemaAndConstraints()
    {
        await using var db = CreateContext();
        var expectedMigrations = db.Database.GetMigrations().ToArray();
        var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        CollectionAssert.AreEqual(expectedMigrations, appliedMigrations);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        Assert.AreEqual(1L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'RefreshTokens'"));
        Assert.AreEqual(1L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_RefreshTokens_TokenHash'"));
        Assert.AreEqual(1L, await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_Roles_Nombre'"));

        await using var foreignKeyCommand = connection.CreateCommand();
        foreignKeyCommand.CommandText =
            "SELECT delete_rule FROM information_schema.referential_constraints " +
            "WHERE constraint_name = 'FK_RefreshTokens_Usuarios_UsuarioId'";
        Assert.AreEqual("RESTRICT", await foreignKeyCommand.ExecuteScalarAsync());
    }

    [TestMethod]
    public async Task AuditTable_AllowsInsertAndRejectsUpdateAndDelete()
    {
        long auditId;
        await using (var db = CreateContext())
        {
            var entry = new Auditoria
            {
                Evento = "APPEND_ONLY_TEST",
                EntidadAfectada = "Auditoria",
                IdentificadorRegistro = "test",
                FechaHora = DateTime.UtcNow
            };
            db.Auditorias.Add(entry);
            await db.SaveChangesAsync();
            auditId = entry.Id;
        }

        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            await using var update = connection.CreateCommand();
            update.CommandText = "UPDATE \"Auditorias\" SET \"Evento\" = 'ALTERADO' WHERE \"Id\" = @id";
            update.Parameters.AddWithValue("id", auditId);
            var exception = await Assert.ThrowsExactlyAsync<PostgresException>(() => update.ExecuteNonQueryAsync());
            Assert.AreEqual("55000", exception.SqlState);
        }

        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            await using var delete = connection.CreateCommand();
            delete.CommandText = "DELETE FROM \"Auditorias\" WHERE \"Id\" = @id";
            delete.Parameters.AddWithValue("id", auditId);
            var exception = await Assert.ThrowsExactlyAsync<PostgresException>(() => delete.ExecuteNonQueryAsync());
            Assert.AreEqual("55000", exception.SqlState);
        }

        await using var verification = CreateContext();
        Assert.AreEqual("APPEND_ONLY_TEST", (await verification.Auditorias.SingleAsync()).Evento);
    }

    [TestMethod]
    public async Task UserCreation_RollsBackWhenAuditInsertFails()
    {
        int roleId;
        await using (var setup = CreateContext())
        {
            var role = new Rol { Nombre = Roles.Consulta };
            setup.Roles.Add(role);
            await setup.SaveChangesAsync();
            roleId = role.Id;
        }

        await using (var db = CreateContext())
        {
            var users = new UserService(db, new PasswordService(), new AuditService(db));
            await Assert.ThrowsExactlyAsync<DbUpdateException>(() => users.CreateAsync(
                new FuelTrack.Api.DTOs.Users.CreateUserRequest
                {
                    NombreUsuario = "atomic-user",
                    Contrasena = "Clave-Atomica-123!",
                    RolIds = [roleId]
                },
                int.MaxValue,
                "127.0.0.1"));
        }

        await using var verification = CreateContext();
        Assert.IsFalse(await verification.Usuarios.AnyAsync(user => user.NombreUsuario == "atomic-user"));
        Assert.IsFalse(await verification.Auditorias.AnyAsync());
    }

    [TestMethod]
    public async Task RoleSeed_IsIdempotentConcurrentAndUniquenessIsEnforced()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BootstrapAdmin:Username"] = "bootstrap-postgres",
                ["BootstrapAdmin:Password"] = "Bootstrap-Seguro-123!"
            })
            .Build();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task SeedAsync()
        {
            await gate.Task;
            await using var context = CreateContext();
            var seed = new SecuritySeedService(context, new PasswordService(), configuration);
            await seed.SeedAsync();
        }

        var attempts = Enumerable.Range(0, 8).Select(_ => SeedAsync()).ToArray();
        gate.SetResult();
        await Task.WhenAll(attempts);

        await using var db = CreateContext();
        Assert.AreEqual(Roles.Todos.Length, await db.Roles.CountAsync());
        CollectionAssert.AreEquivalent(
            Roles.Todos,
            await db.Roles.Select(role => role.Nombre).ToArrayAsync());
        Assert.AreEqual(
            1,
            await db.Usuarios.CountAsync(user => user.NombreUsuario == "bootstrap-postgres"));
        Assert.AreEqual(
            1,
            await db.UsuarioRoles.CountAsync(
                userRole => userRole.Usuario.NombreUsuario == "bootstrap-postgres" &&
                    userRole.Rol.Nombre == Roles.Administrador));

        db.Roles.Add(new Rol { Nombre = Roles.Administrador });
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [TestMethod]
    public async Task LoginRefreshRotationAndReuse_RunAgainstPostgreSql()
    {
        await using var db = CreateContext();
        var services = CreateAuthServices(db);
        var adminRole = new Rol { Nombre = Roles.Administrador };
        var user = new Usuario
        {
            NombreUsuario = "postgres-admin",
            PasswordHash = services.Passwords.Hash("Clave-Postgres-123!"),
            Activo = true
        };
        user.UsuarioRoles.Add(new UsuarioRol { Usuario = user, Rol = adminRole });
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();

        var login = await services.Auth.LoginAsync(
            new LoginRequest
            {
                NombreUsuario = user.NombreUsuario,
                Contrasena = "Clave-Postgres-123!"
            },
            "127.0.0.1");
        Assert.IsNotNull(login);

        var rotated = await services.Auth.RefreshAsync(login.RefreshToken, "127.0.0.1");
        Assert.IsNotNull(rotated);
        Assert.AreNotEqual(login.RefreshToken, rotated.RefreshToken);
        Assert.IsNull(await services.Auth.RefreshAsync(login.RefreshToken, "127.0.0.1"));

        db.ChangeTracker.Clear();
        var originalHash = TokenService.HashRefreshToken(login.RefreshToken);
        var original = await db.RefreshTokens.SingleAsync(token => token.TokenHash == originalHash);
        Assert.IsNotNull(original.RevokedAtUtc);
        Assert.AreEqual(
            TokenService.HashRefreshToken(rotated.RefreshToken),
            original.ReplacedByTokenHash);
    }

    [TestMethod]
    public async Task DisablingUser_RevokesRefreshTokenAgainstPostgreSql()
    {
        await using var db = CreateContext();
        var services = CreateAuthServices(db);
        var admin = new Usuario
        {
            NombreUsuario = "postgres-actor",
            PasswordHash = services.Passwords.Hash("Clave-Actor-123!"),
            Activo = true
        };
        var user = new Usuario
        {
            NombreUsuario = "postgres-disabled",
            PasswordHash = services.Passwords.Hash("Clave-Usuario-123!"),
            Activo = true
        };
        db.Usuarios.AddRange(admin, user);
        await db.SaveChangesAsync();

        var login = await services.Auth.LoginAsync(
            new LoginRequest
            {
                NombreUsuario = user.NombreUsuario,
                Contrasena = "Clave-Usuario-123!"
            },
            "127.0.0.1");
        Assert.IsNotNull(login);

        var users = new UserService(db, services.Passwords, services.Audit);
        Assert.IsTrue(await users.SetStatusAsync(
            user.Id,
            false,
            admin.Id,
            "127.0.0.1"));
        Assert.IsNull(await services.Auth.RefreshAsync(login.RefreshToken, "127.0.0.1"));

        db.ChangeTracker.Clear();
        var hash = TokenService.HashRefreshToken(login.RefreshToken);
        Assert.IsNotNull((await db.RefreshTokens.SingleAsync(token => token.TokenHash == hash)).RevokedAtUtc);
    }

    [TestMethod]
    public async Task ConcurrentAdministratorDeactivation_LeavesOneActiveAdministrator()
    {
        int firstId;
        int secondId;
        await using (var setupDb = CreateContext())
        {
            var adminRole = new Rol { Nombre = Roles.Administrador };
            var first = new Usuario
            {
                NombreUsuario = "postgres-admin-one",
                PasswordHash = "test-only",
                Activo = true
            };
            var second = new Usuario
            {
                NombreUsuario = "postgres-admin-two",
                PasswordHash = "test-only",
                Activo = true
            };
            first.UsuarioRoles.Add(new UsuarioRol { Usuario = first, Rol = adminRole });
            second.UsuarioRoles.Add(new UsuarioRol { Usuario = second, Rol = adminRole });
            setupDb.Usuarios.AddRange(first, second);
            await setupDb.SaveChangesAsync();
            firstId = first.Id;
            secondId = second.Id;
        }

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<bool> TryDeactivateAsync(int userId)
        {
            await gate.Task;
            await using var context = CreateContext();
            var users = new UserService(context, new PasswordService(), new AuditService(context));

            try
            {
                return await users.SetStatusAsync(userId, false, userId, "127.0.0.1");
            }
            catch (AdministrativeLockoutException)
            {
                return false;
            }
        }

        var firstTask = TryDeactivateAsync(firstId);
        var secondTask = TryDeactivateAsync(secondId);
        gate.SetResult();
        var results = await Task.WhenAll(firstTask, secondTask);
        Assert.AreEqual(1, results.Count(result => result));

        await using var verificationDb = CreateContext();
        Assert.AreEqual(
            1,
            await verificationDb.Usuarios.CountAsync(
                user => user.Activo &&
                    user.UsuarioRoles.Any(role => role.Rol.Nombre == Roles.Administrador)));
    }

    private AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options);

    private static async Task<long> ScalarLongAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static AuthServices CreateAuthServices(AppDbContext db)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "FuelTrack.PostgreSqlTests",
            Audience = "FuelTrack.TestClients",
            Key = "TEST-ONLY-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });
        var passwords = new PasswordService();
        var tokens = new TokenService(options);
        var audit = new AuditService(db);
        return new AuthServices(
            passwords,
            audit,
            new AuthService(db, passwords, tokens, audit, options));
    }

    private sealed record AuthServices(
        PasswordService Passwords,
        AuditService Audit,
        AuthService Auth);
}
