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
            PasswordHash = _passwords.Hash("Admin-123!"),
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
            PasswordHash = _passwords.Hash("Clave-123!"),
            Activo = false
        };

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var result = await _auth.LoginAsync(
            new LoginRequest
            {
                NombreUsuario = "bloqueado",
                Contrasena = "Clave-123!"
            },
            "127.0.0.1");

        Assert.IsNull(result);

        var audit = await _db.Auditorias.SingleAsync();
        Assert.AreEqual("LOGIN_FALLIDO", audit.Evento);
    }
}
