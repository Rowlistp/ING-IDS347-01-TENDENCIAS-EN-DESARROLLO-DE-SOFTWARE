using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Auth;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuelTrack.Api.Tests.Services;

[TestClass]
public sealed class AuthServiceRefreshTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private PasswordService _passwords = null!;
    private TokenService _tokens = null!;
    private AuditService _audit = null!;
    private AuthService _auth = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "FuelTrack.Tests",
            Audience = "FuelTrack.TestClients",
            Key = "TEST-ONLY-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });

        _passwords = new PasswordService();
        _tokens = new TokenService(jwtOptions);
        _audit = new AuditService(_db);
        _auth = new AuthService(_db, _passwords, _tokens, _audit, jwtOptions);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [TestMethod]
    public async Task Refresh_RotatesToken_AndOldTokenCannotBeUsedAgain()
    {
        var role = new Rol { Nombre = Roles.Administrador };
        var user = new Usuario
        {
            NombreUsuario = "admin",
            PasswordHash = _passwords.Hash("Admin-Fuerte-123!"),
            Activo = true
        };
        user.UsuarioRoles.Add(new UsuarioRol { Usuario = user, Rol = role });

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var rawRefresh = _tokens.CreateRefreshToken();
        var oldHash = TokenService.HashRefreshToken(rawRefresh);

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = oldHash,
            UsuarioId = user.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1"
        });
        await _db.SaveChangesAsync();

        var first = await _auth.RefreshAsync(rawRefresh, "127.0.0.1");

        Assert.IsNotNull(first);
        Assert.AreNotEqual(rawRefresh, first.RefreshToken);

        _db.ChangeTracker.Clear();

        var oldStored = await _db.RefreshTokens.SingleAsync(t => t.TokenHash == oldHash);
        Assert.IsNotNull(oldStored.RevokedAtUtc);
        Assert.AreEqual(
            TokenService.HashRefreshToken(first.RefreshToken),
            oldStored.ReplacedByTokenHash);

        var second = await _auth.RefreshAsync(rawRefresh, "127.0.0.1");

        Assert.IsNull(second);
    }

    [TestMethod]
    public async Task Login_WithDisabledUser_ReturnsNull()
    {
        var user = new Usuario
        {
            NombreUsuario = "bloqueado",
            PasswordHash = _passwords.Hash("Clave-Fuerte-123!"),
            Activo = false
        };

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var result = await _auth.LoginAsync(
            new LoginRequest
            {
                NombreUsuario = "bloqueado",
                Contrasena = "Clave-Fuerte-123!"
            },
            "127.0.0.1");

        Assert.IsNull(result);

        var audit = await _db.Auditorias.SingleAsync();
        Assert.AreEqual("LOGIN_FALLIDO", audit.Evento);
    }

    [TestMethod]
    public async Task ResetPassword_ChangesHashRevokesSessionsAndAudits()
    {
        var admin = new Usuario
        {
            Id = 99,
            NombreUsuario = "admin-reset",
            PasswordHash = _passwords.Hash("Clave-Admin-123!"),
            Activo = true
        };
        var user = new Usuario
        {
            NombreUsuario = "usuario-reset",
            PasswordHash = _passwords.Hash("Clave-Anterior-123!"),
            Activo = true
        };
        _db.Usuarios.AddRange(admin, user);
        await _db.SaveChangesAsync();
        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = TokenService.HashRefreshToken(_tokens.CreateRefreshToken()),
            UsuarioId = user.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        var changed = await _auth.ResetPasswordAsync(
            user.Id,
            "Clave-Nueva-456!",
            99,
            "127.0.0.1");

        Assert.IsTrue(changed);
        _db.ChangeTracker.Clear();
        var storedUser = await _db.Usuarios.SingleAsync(item => item.Id == user.Id);
        Assert.IsTrue(_passwords.Verify("Clave-Nueva-456!", storedUser.PasswordHash));
        Assert.IsFalse(_passwords.Verify("Clave-Anterior-123!", storedUser.PasswordHash));
        Assert.AreEqual(2, storedUser.SecurityVersion);
        Assert.IsNotNull((await _db.RefreshTokens.SingleAsync()).RevokedAtUtc);
        Assert.IsTrue(await _db.Auditorias.AnyAsync(item => item.Evento == "PASSWORD_RESET_ADMIN"));
    }

    [TestMethod]
    public async Task Refresh_TwoConcurrentRequests_OnlyOneSessionSucceeds()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"fueltrack-refresh-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Cache=Shared;Default Timeout=10";
        var rawToken = _tokens.CreateRefreshToken();

        try
        {
            await using (var setupDb = CreateFileContext(connectionString))
            {
                await setupDb.Database.EnsureCreatedAsync();
                var role = new Rol { Nombre = Roles.Administrador };
                var user = new Usuario
                {
                    NombreUsuario = "admin-concurrent",
                    PasswordHash = _passwords.Hash("Clave-Concurrente-123!"),
                    Activo = true
                };
                user.UsuarioRoles.Add(new UsuarioRol { Usuario = user, Rol = role });
                setupDb.Usuarios.Add(user);
                await setupDb.SaveChangesAsync();
                setupDb.RefreshTokens.Add(new RefreshToken
                {
                    TokenHash = TokenService.HashRefreshToken(rawToken),
                    UsuarioId = user.Id,
                    CreatedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                });
                await setupDb.SaveChangesAsync();
            }

            await using var firstDb = CreateFileContext(connectionString);
            await using var secondDb = CreateFileContext(connectionString);
            var firstAuth = CreateAuthService(firstDb);
            var secondAuth = CreateAuthService(secondDb);
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task<AuthResponse?> RotateAsync(AuthService service)
            {
                await gate.Task;
                return await service.RefreshAsync(rawToken, "127.0.0.1");
            }

            var firstTask = RotateAsync(firstAuth);
            var secondTask = RotateAsync(secondAuth);
            gate.SetResult();
            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.AreEqual(1, results.Count(result => result is not null));
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static AppDbContext CreateFileContext(string connectionString)
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options);

    private static AuthService CreateAuthService(AppDbContext context)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "FuelTrack.Tests",
            Audience = "FuelTrack.TestClients",
            Key = "TEST-ONLY-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });
        var passwords = new PasswordService();
        var tokens = new TokenService(options);
        return new AuthService(context, passwords, tokens, new AuditService(context), options);
    }
}
