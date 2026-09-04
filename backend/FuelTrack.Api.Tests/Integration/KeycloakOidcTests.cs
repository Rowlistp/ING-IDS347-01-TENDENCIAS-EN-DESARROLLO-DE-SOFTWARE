using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
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
[TestCategory("Keycloak")]
[DoNotParallelize]
public sealed class KeycloakOidcTests
{
    private string _baseUrl = null!;
    private KeycloakPipelineFactory _factory = null!;
    private HttpClient _api = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _baseUrl = Environment.GetEnvironmentVariable("FUELTRACK_KEYCLOAK_URL")?.TrimEnd('/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_baseUrl))
            Assert.Inconclusive("Defina FUELTRACK_KEYCLOAK_URL para ejecutar la integración OIDC real.");

        Environment.SetEnvironmentVariable("Jwt__Key", "TEST-JWT-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        _factory = new KeycloakPipelineFactory($"{_baseUrl}/realms/fueltrack", "fueltrack-api");
        _api = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://fueltrack.test")
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedLocalUsersAsync(db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _api?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
        Environment.SetEnvironmentVariable("Jwt__Key", null);
    }

    [TestMethod]
    public async Task Metadata_ExposesExpectedIssuer()
    {
        using var client = new HttpClient();
        var json = await client.GetStringAsync(
            $"{_baseUrl}/realms/fueltrack/.well-known/openid-configuration");
        using var document = JsonDocument.Parse(json);
        Assert.AreEqual(
            $"{_baseUrl}/realms/fueltrack",
            document.RootElement.GetProperty("issuer").GetString());
    }

    [TestMethod]
    public async Task PasswordGrant_IsDisabledForPublicClient()
    {
        using var client = new HttpClient();
        var response = await client.PostAsync(
            $"{_baseUrl}/realms/fueltrack/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "fueltrack-web",
                ["username"] = "keycloak-admin-local",
                ["password"] = "Keycloak-Test-123!"
            }));
        var json = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        StringAssert.Contains(json, "unauthorized_client");
    }

    [TestMethod]
    public async Task LocalAdministratorRole_AuthorizesAdminEndpoint()
    {
        var token = await AcquirePkceTokenAsync("keycloak-admin-local");
        _api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _api.GetAsync("/api/v1/roles");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ExternalRealmRole_DoesNotElevateLocalConsultaUser()
    {
        var token = await AcquirePkceTokenAsync("keycloak-consulta-local");
        _api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _api.GetAsync("/api/v1/roles");

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    [DataRow("keycloak-desconocido")]
    [DataRow("keycloak-inactivo-local")]
    public async Task UnknownOrInactiveLocalIdentity_IsRejected(string username)
    {
        var token = await AcquirePkceTokenAsync(username);
        _api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _api.GetAsync("/api/v1/roles");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task WrongAudience_IsRejected()
    {
        var token = await AcquirePkceTokenAsync("keycloak-admin-local");
        await using var factory = new KeycloakPipelineFactory(
            $"{_baseUrl}/realms/fueltrack",
            "otra-audiencia");
        using var client = factory.CreateClient();
        await SeedFactoryAsync(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/roles")).StatusCode);
    }

    [TestMethod]
    public async Task WrongIssuer_IsRejected()
    {
        var token = await AcquirePkceTokenAsync("keycloak-admin-local");
        await using var factory = new KeycloakPipelineFactory(
            $"{_baseUrl}/realms/master",
            "fueltrack-api");
        using var client = factory.CreateClient();
        await SeedFactoryAsync(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/roles")).StatusCode);
    }

    private async Task<string> AcquirePkceTokenAsync(string username)
    {
        const string verifier = "fueltrack-pkce-verifier-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        const string redirectUri = "http://localhost:5173/callback";

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false
        };
        using var client = new HttpClient(handler);
        var authorizationUrl = $"{_baseUrl}/realms/fueltrack/protocol/openid-connect/auth" +
            $"?client_id=fueltrack-web&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code&scope=openid&state=fueltrack-test" +
            $"&code_challenge={challenge}&code_challenge_method=S256";

        var loginPage = await client.GetAsync(authorizationUrl);
        var html = await loginPage.Content.ReadAsStringAsync();
        var cookies = loginPage.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0]);
        var actionMatch = Regex.Match(html, "<form[^>]+action=[\"'](?<action>[^\"']+)", RegexOptions.IgnoreCase);
        Assert.IsTrue(actionMatch.Success, "Keycloak no devolvió el formulario de login esperado.");
        var action = HttpUtility.HtmlDecode(actionMatch.Groups["action"].Value);

        var fields = new Dictionary<string, string>();
        foreach (Match input in Regex.Matches(html, "<input[^>]*>", RegexOptions.IgnoreCase))
        {
            var name = Regex.Match(input.Value, "name=[\"'](?<value>[^\"']+)", RegexOptions.IgnoreCase);
            if (!name.Success)
                continue;
            var value = Regex.Match(input.Value, "value=[\"'](?<value>[^\"']*)", RegexOptions.IgnoreCase);
            fields[name.Groups["value"].Value] = value.Success
                ? HttpUtility.HtmlDecode(value.Groups["value"].Value)
                : string.Empty;
        }
        fields["username"] = username;
        fields["password"] = "Keycloak-Test-123!";
        fields["credentialId"] = string.Empty;

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, action)
        {
            Content = new FormUrlEncodedContent(fields)
        };
        loginRequest.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        var login = await client.SendAsync(loginRequest);
        var loginError = await login.Content.ReadAsStringAsync();
        Assert.IsTrue(
            login.StatusCode is HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"Login OIDC inesperado: {(int)login.StatusCode}. {loginError}");
        var callback = login.Headers.Location!;
        var query = HttpUtility.ParseQueryString(callback.Query);
        var code = query["code"];
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(code),
            $"Keycloak no devolvió authorization code. Redirect: {callback}");

        var tokenResponse = await client.PostAsync(
            $"{_baseUrl}/realms/fueltrack/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = "fueltrack-web",
                ["redirect_uri"] = redirectUri,
                ["code"] = code!,
                ["code_verifier"] = verifier
            }));
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        Assert.IsTrue(tokenResponse.IsSuccessStatusCode, tokenJson);
        using var document = JsonDocument.Parse(tokenJson);
        return document.RootElement.GetProperty("access_token").GetString()!;
    }

    private static async Task SeedFactoryAsync(KeycloakPipelineFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedLocalUsersAsync(db);
    }

    private static async Task SeedLocalUsersAsync(AppDbContext db)
    {
        if (await db.Usuarios.AnyAsync())
            return;

        var admin = new Rol { Nombre = Roles.Administrador };
        var consulta = new Rol { Nombre = Roles.Consulta };
        var activeAdmin = new Usuario { NombreUsuario = "keycloak-admin-local", PasswordHash = "external-only", Activo = true };
        var activeConsulta = new Usuario { NombreUsuario = "keycloak-consulta-local", PasswordHash = "external-only", Activo = true };
        var inactive = new Usuario { NombreUsuario = "keycloak-inactivo-local", PasswordHash = "external-only", Activo = false };
        activeAdmin.UsuarioRoles.Add(new UsuarioRol { Usuario = activeAdmin, Rol = admin });
        activeConsulta.UsuarioRoles.Add(new UsuarioRol { Usuario = activeConsulta, Rol = consulta });
        inactive.UsuarioRoles.Add(new UsuarioRol { Usuario = inactive, Rol = consulta });
        db.Usuarios.AddRange(activeAdmin, activeConsulta, inactive);
        await db.SaveChangesAsync();
    }
}

internal sealed class KeycloakPipelineFactory(string authority, string audience) : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TEST-JWT-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                ["Authentication:Keycloak:Enabled"] = "true",
                ["Authentication:Keycloak:Authority"] = authority,
                ["Authentication:Keycloak:Audience"] = audience,
                ["Authentication:Keycloak:IdentityClaim"] = "preferred_username",
                ["Authentication:Keycloak:RequireHttpsMetadata"] = "false"
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
