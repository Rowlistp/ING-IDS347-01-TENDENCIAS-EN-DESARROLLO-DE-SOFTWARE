using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Tickets;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// Herramienta local/CI; nunca forma parte de los endpoints o del despliegue API.
var connection = Environment.GetEnvironmentVariable("FUELTRACK_TEST_CONNECTION") ?? throw new Exception("Falta base de pruebas.");
var cs = new Npgsql.NpgsqlConnectionStringBuilder(connection);
if (cs.Host is not ("127.0.0.1" or "localhost") || cs.Database?.Contains("test", StringComparison.OrdinalIgnoreCase) != true)
    throw new Exception("El harness solo admite PostgreSQL local de pruebas.");
var flutter = Environment.GetEnvironmentVariable("FLUTTER_BIN") ?? "flutter";
var root = Directory.GetCurrentDirectory();
var temp = Directory.CreateTempSubdirectory("fueltrack-mobile-e2e-").FullName;
var fixtureFile = Path.Combine(temp, "fixture.json");
using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
var ticketOptions = Options.Create(new TicketOptions { SigningPrivateKeyPkcs8Base64 = Convert.ToBase64String(key.ExportPkcs8PrivateKey()), SigningPublicKeySpkiBase64 = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()) });
var jwt = Options.Create(new JwtOptions { Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)), Issuer = "FuelTrack.E2E", Audience = "FuelTrack.MobileE2E" });
var dbOptions = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
await using var db = new AppDbContext(dbOptions);
await db.Database.MigrateAsync();
var actor = new Usuario { NombreUsuario = "mobile-e2e-dispatcher", PasswordHash = "fixture-not-password", Activo = true };
actor.UsuarioRoles.Add(new UsuarioRol { Rol = new Rol { Nombre = Roles.Despachador } });
var department = new Departamento { Nombre = "E2E" };
var fuel = new TipoCombustible { Nombre = "Diesel E2E" };
var employee = new Empleado { Codigo = "E2E", Cedula = "E2E", NombreCompleto = "Operador E2E", Departamento = department, Activo = true };
var vehicle = new Vehiculo { Placa = "E2E", Ficha = "E2E", Departamento = department, Activo = true };
var tank = new Tanque { Identificacion = "Tanque E2E", Capacidad = 100, TipoCombustible = fuel };
var station = new Estacion { Nombre = "Estación E2E" };
db.AddRange(actor, employee, vehicle, tank, station);
db.Inventarios.Add(new Inventario { Tanque = tank, ExistenciaActual = 50, Disponibilidad = 50, UltimaActualizacion = DateTime.UtcNow });
var requests = Enumerable.Range(0, 2).Select(_ => new SolicitudCombustible { Empleado = employee, Vehiculo = vehicle, Departamento = department,
    TipoCombustible = fuel, Estado = EstadoSolicitud.Aprobada, CantidadAutorizada = 10, CantidadSolicitada = 10,
    TipoSolicitud = "Manual", FechaSolicitud = DateTime.UtcNow, FechaVencimiento = DateTime.UtcNow.AddDays(1) }).ToArray();
db.SolicitudesCombustible.AddRange(requests);
await db.SaveChangesAsync();
var tickets = new TicketService(db, new TicketNumberService(db), new TicketQrService(ticketOptions), new TicketPdfService(), new AuditService(db), ticketOptions);
var valid = await tickets.CreateAsync(new CreateTicketRequest { SolicitudId = requests[0].Id }, actor.Id, null, default);
var rollback = await tickets.CreateAsync(new CreateTicketRequest { SolicitudId = requests[1].Id }, actor.Id, null, default);
// Fallo controlado después de preparar las escrituras, solo en la base efímera.
await db.Database.ExecuteSqlRawAsync("""
    CREATE FUNCTION e2e_reject_audit() RETURNS trigger AS $$ BEGIN
      IF NEW."Evento" = 'DESPACHO_REGISTRADO' AND NEW."DatosRelevantes"->>'TicketId' = 'REPLACE_TICKET'
      THEN RAISE EXCEPTION 'controlled E2E rollback'; END IF;
      RETURN NEW; END; $$ LANGUAGE plpgsql;
    CREATE TRIGGER e2e_reject_audit BEFORE INSERT ON "Auditorias" FOR EACH ROW EXECUTE FUNCTION e2e_reject_audit();
    """.Replace("REPLACE_TICKET", rollback.Ticket.Id.ToString("D")));
var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
var apiUrl = $"http://127.0.0.1:{port}";
await File.WriteAllTextAsync(fixtureFile, JsonSerializer.Serialize(new { apiUrl = apiUrl + "/api/v1", token = new TokenService(jwt).CreateAccessToken(actor, [Roles.Despachador]).Token,
    qrPayload = valid.QrPayload, rollbackQr = rollback.QrPayload, rollbackId = rollback.Ticket.Id, tanqueId = tank.Id, estacionId = station.Id }));
if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(fixtureFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
var apiStart = new ProcessStartInfo("dotnet") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
apiStart.ArgumentList.Add(Path.Combine(root, "backend/FuelTrack.Api/bin/Release/net10.0/FuelTrack.Api.dll"));
foreach (var pair in new Dictionary<string, string> {
    ["ASPNETCORE_ENVIRONMENT"] = "Testing", ["ASPNETCORE_URLS"] = apiUrl, ["ConnectionStrings__DefaultConnection"] = connection,
    ["Jwt__Key"] = jwt.Value.Key, ["Jwt__Issuer"] = jwt.Value.Issuer, ["Jwt__Audience"] = jwt.Value.Audience,
    ["Tickets__SigningPrivateKeyPkcs8Base64"] = ticketOptions.Value.SigningPrivateKeyPkcs8Base64,
    ["Tickets__SigningPublicKeySpkiBase64"] = ticketOptions.Value.SigningPublicKeySpkiBase64,
    ["Logging__LogLevel__Default"] = "Critical" }) apiStart.Environment[pair.Key] = pair.Value;
using var api = Process.Start(apiStart)!;
var output = api.StandardOutput.ReadToEndAsync(); var errors = api.StandardError.ReadToEndAsync();
try
{
    using var client = new HttpClient();
    var ready = false;
    for (var attempt = 0; attempt < 60; attempt++)
    {
        if (api.HasExited) throw new Exception("API E2E no inició: " + await errors);
        try { ready = (await client.GetAsync(apiUrl + "/api/v1/tickets")).StatusCode == HttpStatusCode.Unauthorized; } catch (HttpRequestException) { }
        if (ready) break;
        await Task.Delay(500);
    }
    if (!ready) throw new Exception("Timeout API E2E.");
    var start = new ProcessStartInfo(flutter) { WorkingDirectory = Path.Combine(root, "mobile") };
    foreach (var arg in new[] { "test", "e2e/api_e2e_test.dart", "--dart-define=E2E_FIXTURE=" + fixtureFile }) start.ArgumentList.Add(arg);
    using var test = Process.Start(start)!; await test.WaitForExitAsync();
    if (test.ExitCode != 0) throw new Exception("Flutter E2E falló.");
    db.ChangeTracker.Clear();
    if (await db.Despachos.CountAsync() != 1 || await db.MovimientosInventario.CountAsync() != 1 ||
        (await db.Inventarios.SingleAsync()).ExistenciaActual != 45 ||
        (await db.Tickets.SingleAsync(t => t.Id == rollback.Ticket.Id)).Estado != EstadoTicket.Creado ||
        await db.Auditorias.CountAsync(a => a.Evento == "DESPACHO_REGISTRADO") != 1)
        throw new Exception("Persistencia E2E/rollback inconsistente.");
    Console.WriteLine("E2E PASS: Flutter → API → PostgreSQL; consumo único, stock 45, movimiento, auditoría y rollback íntegro.");
}
finally
{
    if (!api.HasExited) { api.Kill(entireProcessTree: true); await api.WaitForExitAsync(); }
    File.Delete(fixtureFile); Directory.Delete(temp);
}
