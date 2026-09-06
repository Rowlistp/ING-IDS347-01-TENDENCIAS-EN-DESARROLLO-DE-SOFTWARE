using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Tickets;
using FuelTrack.Api.DTOs.Dispatch;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
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

    [TestMethod]
    public async Task Tickets_WithoutJwt_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/tickets");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Tickets_WithSolicitanteRole_ReturnsEmptyOwnedList()
    {
        var token = await CreateTokenAsync(Roles.Solicitante);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        var response = await _client.GetAsync("/api/v1/tickets");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(0, (await response.Content.ReadFromJsonAsync<TicketResponse[]>())!.Length);
    }

    [TestMethod]
    public async Task Tickets_Solicitante_CanReadOnlyOwnedTicketsAndPdf()
    {
        var owner = await CreateTokenWithUserIdAsync(Roles.Solicitante);
        var other = await CreateTokenWithUserIdAsync(Roles.Solicitante);
        var ownRequest = await SeedApprovedTicketRequestAsync(owner.UserId);
        var otherRequest = await SeedApprovedTicketRequestAsync(other.UserId);
        var admin = await CreateTokenAsync(Roles.Administrador);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin);
        var ownResponse = await _client.PostAsJsonAsync("/api/v1/tickets", new CreateTicketRequest { SolicitudId = ownRequest });
        var foreignResponse = await _client.PostAsJsonAsync("/api/v1/tickets", new CreateTicketRequest { SolicitudId = otherRequest });
        Assert.AreEqual(HttpStatusCode.Created, ownResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, foreignResponse.StatusCode);
        var own = (await ownResponse.Content.ReadFromJsonAsync<TicketResponse>())!;
        var foreign = (await foreignResponse.Content.ReadFromJsonAsync<TicketResponse>())!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
        var list = (await _client.GetFromJsonAsync<TicketResponse[]>("/api/v1/tickets"))!;
        CollectionAssert.AreEqual(new[] { own.Id }, list.Select(item => item.Id).ToArray());
        Assert.AreEqual(HttpStatusCode.OK, (await _client.GetAsync($"/api/v1/tickets/{own.Id}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/tickets/{foreign.Id}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await _client.GetAsync($"/api/v1/tickets/{own.Id}/pdf")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/tickets/{foreign.Id}/pdf")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/v1/tickets/validar", new ValidateTicketRequest { QrPayload = "FTQR1.test" })).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/v1/tickets", new CreateTicketRequest { SolicitudId = ownRequest })).StatusCode);
    }

    [TestMethod]
    public async Task Tickets_WithAdministratorRole_Returns200()
    {
        var token = await CreateTokenAsync(Roles.Administrador);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        var response = await _client.GetAsync("/api/v1/tickets");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Tickets_ManagementEndpoints_WithAdministrator_ExecuteLifecycle()
    {
        var requestId = await SeedApprovedTicketRequestAsync();
        var token = await CreateTokenAsync(Roles.Administrador);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        var create = await _client.PostAsJsonAsync(
            "/api/v1/tickets",
            new CreateTicketRequest { SolicitudId = requestId });
        Assert.AreEqual(HttpStatusCode.Created, create.StatusCode);
        var ticket = await create.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.IsNotNull(ticket);

        var send = await _client.PostAsync($"/api/v1/tickets/{ticket.Id:D}/enviar", null);
        Assert.AreEqual(HttpStatusCode.OK, send.StatusCode);

        var pdf = await _client.GetAsync($"/api/v1/tickets/{ticket.Id:D}/pdf");
        Assert.AreEqual(HttpStatusCode.OK, pdf.StatusCode);
        Assert.AreEqual("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        CollectionAssert.AreEqual(
            System.Text.Encoding.ASCII.GetBytes("%PDF"),
            (await pdf.Content.ReadAsByteArrayAsync())[..4]);

        var cancel = await _client.PostAsJsonAsync(
            $"/api/v1/tickets/{ticket.Id:D}/anular",
            new CancelTicketRequest { Motivo = "Corrección de prueba" });
        Assert.AreEqual(HttpStatusCode.OK, cancel.StatusCode);

        var get = await _client.GetFromJsonAsync<TicketResponse>($"/api/v1/tickets/{ticket.Id:D}");
        Assert.AreEqual(EstadoTicket.Anulado, get!.Estado);
    }

    [TestMethod]
    public async Task Tickets_ValidateMalformedQr_ReturnsSafeInvalidResponse()
    {
        var token = await CreateTokenAsync(Roles.Despachador);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/tickets/validar",
            new ValidateTicketRequest { QrPayload = "alterado" });
        var validation = await response.Content.ReadFromJsonAsync<TicketValidationResponse>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsFalse(validation!.Valido);
        Assert.AreEqual("QR_INVALIDO", validation.Codigo);
    }

    [TestMethod]
    public async Task Tickets_ManagementActions_WithSolicitanteRole_Return403()
    {
        var token = await CreateTokenAsync(Roles.Solicitante);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
        var id = Guid.NewGuid();

        var send = await _client.PostAsync($"/api/v1/tickets/{id:D}/enviar", null);
        var cancel = await _client.PostAsJsonAsync(
            $"/api/v1/tickets/{id:D}/anular",
            new CancelTicketRequest { Motivo = "Sin autorización" });

        Assert.AreEqual(HttpStatusCode.Forbidden, send.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, cancel.StatusCode);
    }

    private async Task<string> CreateTokenAsync(string role)
        => (await CreateTokenWithUserIdAsync(role)).Token;

    [TestMethod]
    [DataRow(Roles.Administrador)]
    [DataRow(Roles.Supervisor)]
    [DataRow(Roles.Auditor)]
    [DataRow(Roles.Consulta)]
    [DataRow(Roles.Solicitante)]
    public async Task Dispatch_PostRequiresDispatcher(string role)
    {
        var token = await CreateTokenAsync(role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/despachos", new CreateDispatchRequest { QrPayload = "test", TanqueId = 1, EstacionId = 1, GalonesServidos = 5 });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Dispatch_NoSession_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/despachos", new CreateDispatchRequest());
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Dispatch_HttpLifecycle_ValidatesConsumesAndReconciles()
    {
        var requestId = await SeedApprovedTicketRequestAsync();
        var identity = await CreateTokenWithUserIdAsync(Roles.Despachador);
        string payload;
        Guid ticketId;
        int tankId, stationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.UsuarioRoles.Include(r => r.Rol).SingleAsync(r => r.UsuarioId == identity.UserId)).Rol.Nombre = Roles.Despachador;
            var request = await db.SolicitudesCombustible.SingleAsync(r => r.Id == requestId);
            var tank = new Tanque { Identificacion = "HTTP-TANK", TipoCombustibleId = request.TipoCombustibleId, Capacidad = 100 };
            var station = new Estacion { Nombre = "HTTP-STATION" };
            db.AddRange(tank, station);
            db.Inventarios.Add(new Inventario { Tanque = tank, ExistenciaActual = 50, Disponibilidad = 50, UltimaActualizacion = DateTime.UtcNow });
            await db.SaveChangesAsync();
            var created = await scope.ServiceProvider.GetRequiredService<FuelTrack.Api.Services.TicketService>().CreateAsync(
                new CreateTicketRequest { SolicitudId = requestId }, identity.UserId, null, default);
            payload = created.QrPayload; ticketId = created.Ticket.Id; tankId = tank.Id; stationId = station.Id;
        }
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", identity.Token);
        Assert.AreEqual(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/estaciones")).StatusCode);
        var validation = await _client.PostAsJsonAsync("/api/v1/tickets/validar", new ValidateTicketRequest { QrPayload = payload });
        Assert.IsTrue((await validation.Content.ReadFromJsonAsync<TicketValidationResponse>())!.Valido);
        var dispatchRequest = new CreateDispatchRequest { TicketId = ticketId, QrPayload = payload, TanqueId = tankId, EstacionId = stationId, GalonesServidos = 5 };
        var response = await _client.PostAsJsonAsync("/api/v1/despachos", dispatchRequest);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<DispatchResponse>())!;
        Assert.AreEqual(45m, result.InventarioRestante);
        Assert.AreEqual(EstadoTicket.Consumido, result.EstadoTicket);
        var query = await _client.GetFromJsonAsync<DispatchResponse[]>($"/api/v1/despachos?ticketId={ticketId}");
        Assert.AreEqual(result.DespachoId, query!.Single().DespachoId);
        Assert.AreEqual(HttpStatusCode.OK, (await _client.GetAsync($"/api/v1/despachos/{result.DespachoId}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/v1/despachos", dispatchRequest)).StatusCode);
        var other = await CreateTokenAsync(Roles.Despachador);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other);
        Assert.AreEqual(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/despachos/{result.DespachoId}")).StatusCode);
        Assert.AreEqual(0, (await _client.GetFromJsonAsync<DispatchResponse[]>("/api/v1/despachos"))!.Length);
    }

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

    private async Task<int> SeedApprovedTicketRequestAsync(int? ownerUserId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var department = new Departamento { Nombre = $"Tickets-{suffix}", Activo = true };
        var fuel = new TipoCombustible { Nombre = $"Fuel-{suffix}", Activo = true };
        var employee = new Empleado
        {
            Codigo = $"E-{suffix}",
            NombreCompleto = "API Ticket",
            UsuarioId = ownerUserId,
            Cedula = suffix,
            Cargo = "Prueba",
            Correo = $"{suffix}@example.test",
            Telefono = "+18095550103",
            Activo = true,
            Departamento = department
        };
        var vehicle = new Vehiculo
        {
            Placa = suffix,
            Ficha = $"F-{suffix}",
            Marca = "API",
            Modelo = "Ticket",
            Año = 2026,
            Tipo = "Prueba",
            CapacidadTanque = 30,
            Activo = true,
            Departamento = department
        };
        var request = new SolicitudCombustible
        {
            CantidadSolicitada = 12,
            CantidadAutorizada = 11,
            TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Aprobada,
            FechaSolicitud = DateTime.UtcNow,
            FechaVencimiento = DateTime.UtcNow.AddDays(1),
            Empleado = employee,
            Vehiculo = vehicle,
            Departamento = department,
            TipoCombustible = fuel
        };
        db.SolicitudesCombustible.Add(request);
        await db.SaveChangesAsync();
        return request.Id;
    }
}

internal sealed class JwtPipelineFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly string _ticketPrivateKey;
    private readonly string _ticketPublicKey;

    public JwtPipelineFactory()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        _ticketPrivateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey());
        _ticketPublicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "FuelTrack.Api",
                ["Jwt:Audience"] = "FuelTrack.Clients",
                ["Jwt:Key"] = "TEST-JWT-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                ["Tickets:Prefix"] = "COM",
                ["Tickets:SigningPrivateKeyPkcs8Base64"] = _ticketPrivateKey,
                ["Tickets:SigningPublicKeySpkiBase64"] = _ticketPublicKey
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
