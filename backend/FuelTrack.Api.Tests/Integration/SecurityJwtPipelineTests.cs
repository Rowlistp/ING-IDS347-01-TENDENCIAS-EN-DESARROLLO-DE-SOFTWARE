using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FuelTrack.Api.Tests.Integration;

[TestClass]
public sealed class SecurityJwtPipelineTests
{
    private JwtPipelineFactory _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            "TEST-JWT-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        _factory = new JwtPipelineFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        Environment.SetEnvironmentVariable("Jwt__Key", null);
    }

    [TestMethod]
    public async Task Users_WithInvalidJwt_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, "not-a-jwt");

        var response = await _client.GetAsync("/api/v1/usuarios");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Audit_WithoutJwt_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/audit");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Users_WithValidAdministratorJwt_Returns200()
    {
        var token = await CreateTokenAsync(Roles.Administrador);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        var response = await _client.GetAsync("/api/v1/usuarios");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Roles_WithAdministrator_ReturnsOnlyPersistedOfficialRoles()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Roles.AddRange(
                new Rol { Nombre = Roles.Administrador },
                new Rol { Nombre = Roles.Consulta },
                new Rol { Nombre = "RolInventado" });
            await db.SaveChangesAsync();
        }

        var token = await CreateTokenAsync(Roles.Administrador);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        var response = await _client.GetAsync("/api/v1/roles");
        var json = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(json, Roles.Administrador);
        StringAssert.Contains(json, Roles.Consulta);
        Assert.IsFalse(json.Contains("RolInventado", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Users_WithValidWrongRoleJwt_Returns403()
    {
        var token = await CreateTokenAsync(Roles.Supervisor);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        var response = await _client.GetAsync("/api/v1/usuarios");

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Audit_WithWrongRoleJwt_Returns403()
    {
        var token = await CreateTokenAsync(Roles.Solicitante);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        var response = await _client.GetAsync("/api/v1/audit");

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    [DataRow(Roles.Administrador)]
    [DataRow(Roles.Auditor)]
    public async Task Audit_WithAuthorizedRole_ReturnsSafeResponse(string role)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Auditorias.Add(new Auditoria
            {
                Evento = "PRUEBA_SEGURA",
                EntidadAfectada = "Usuario",
                IdentificadorRegistro = "7",
                FechaHora = DateTime.UtcNow,
                DireccionIp = "127.0.0.1",
                DatosRelevantes = "{\"PasswordHash\":\"no-exponer\"}"
            });
            await db.SaveChangesAsync();
        }

        var token = await CreateTokenAsync(role);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
        var response = await _client.GetAsync("/api/v1/audit");
        var json = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(json, "PRUEBA_SEGURA");
        Assert.IsFalse(json.Contains("PasswordHash", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("no-exponer", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Users_SelfDeactivationAsAdministrator_Returns400()
    {
        var (token, userId) = await CreateTokenWithUserIdAsync(Roles.Administrador);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/usuarios/{userId}/estado",
            new { activo = false });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task AccessToken_AfterUserIsDisabled_Returns401()
    {
        var (token, userId) = await CreateTokenWithUserIdAsync(Roles.Administrador);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Usuarios.SingleAsync(item => item.Id == userId);
            user.Activo = false;
            user.SecurityVersion++;
            await db.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
        var response = await _client.GetAsync("/api/v1/usuarios");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> CreateTokenAsync(string role)
        => (await CreateTokenWithUserIdAsync(role)).Token;

    private async Task<(string Token, int UserId)> CreateTokenWithUserIdAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedRole = new Rol { Nombre = $"{role}-{Guid.NewGuid():N}" };
        var user = new Usuario
        {
            NombreUsuario = $"jwt-{Guid.NewGuid():N}",
            PasswordHash = "test-only",
            Activo = true
        };
        user.UsuarioRoles.Add(new UsuarioRol { Usuario = user, Rol = storedRole });
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();

        // El claim usa el nombre RBAC oficial aunque el rol persistido tenga un sufijo
        // para aislar cada prueba en la misma base SQLite.
        var tokens = scope.ServiceProvider.GetRequiredService<TokenService>();
        var (token, _) = tokens.CreateAccessToken(user, [role]);
        return (token, user.Id);
    }
}

internal sealed class JwtPipelineFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "FuelTrack.Api",
                ["Jwt:Audience"] = "FuelTrack.Clients",
                ["Jwt:Key"] = "TEST-JWT-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
