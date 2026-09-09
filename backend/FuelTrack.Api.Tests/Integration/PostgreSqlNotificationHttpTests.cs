using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Auth;
using FuelTrack.Api.DTOs.Dispatch;
using FuelTrack.Api.DTOs.Tickets;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Notifications;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using FuelTrack.Api.Tests.Notifications;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FuelTrack.Api.Tests.Integration;

public sealed partial class PostgreSqlSecurityTests
{
    [TestMethod]
    [TestCategory("NotificationTransport")]
    public async Task Notifications_RestSmtpSmsLinkDispatchInventoryReport_EndToEnd()
    {
        var smtpPort = Environment.GetEnvironmentVariable("FUELTRACK_SMTP_PORT");
        var mailUrl = Environment.GetEnvironmentVariable("FUELTRACK_MAILPIT_URL");
        Assert.IsFalse(string.IsNullOrEmpty(smtpPort), "FUELTRACK_SMTP_PORT is required; run the full integration script.");
        Assert.IsFalse(string.IsNullOrEmpty(mailUrl), "FUELTRACK_MAILPIT_URL is required.");
        var fixture = await SeedDispatchAsync();
        await using (var db = CreateContext())
        {
            var actor = await db.Usuarios.SingleAsync(); actor.PasswordHash = new PasswordService().Hash("Testing-F9-Strong-123!");
            db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = actor.Id, Rol = new Rol { Nombre = Roles.Administrador } });
            await db.SaveChangesAsync();
        }
        await using var sms = new LocalGateway();
        var configuration = new NotificationOptions { Smtp = new() { Port = int.Parse(smtpPort!) }, Sms = new() { BaseUrl = sms.Url } };
        Environment.SetEnvironmentVariable("Jwt__Key", "TEST-JWT-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        try
        {
            await using var factory = new NotificationWebFactory(_connectionString, configuration, fixture.Options.Value);
            using var api = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await api.GetAsync("/api/v1/notificaciones")).StatusCode);
            var login = await api.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { NombreUsuario = "postgres-ticket-actor", Contrasena = "Testing-F9-Strong-123!" });
            Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
            var credentials = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
            api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
            Assert.AreEqual(HttpStatusCode.OK, (await api.GetAsync("/api/v1/solicitudes")).StatusCode);
            // Exercise HTTP creation/approval/emission too. The signed fixture supplies the scan input
            // for the transport→dispatch leg, without regenerating QR crypto or exposing raw QR in REST.
            var source = fixture.Ticket.Ticket;
            var requestResponse = await api.PostAsJsonAsync("/api/v1/solicitudes", new { cantidadSolicitada = 10,
                empleadoId = source.EmpleadoId, vehiculoId = source.VehiculoId, departamentoId = source.DepartamentoId,
                tipoCombustibleId = source.TipoCombustibleId, fechaVencimiento = DateTime.UtcNow.AddDays(1) });
            Assert.AreEqual(HttpStatusCode.Created, requestResponse.StatusCode);
            using var requestJson = JsonDocument.Parse(await requestResponse.Content.ReadAsStringAsync());
            var requestId = requestJson.RootElement.GetProperty("id").GetInt32();
            Assert.AreEqual(HttpStatusCode.OK, (await api.PostAsJsonAsync($"/api/v1/solicitudes/{requestId}/aprobar", new { cantidadAutorizada = 10 })).StatusCode);
            Assert.AreEqual(HttpStatusCode.Created, (await api.PostAsJsonAsync("/api/v1/tickets", new CreateTicketRequest { SolicitudId = requestId })).StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, (await api.GetAsync("/api/v1/tickets")).StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, (await api.GetAsync($"/api/v1/tickets/{fixture.Ticket.Ticket.Id}")).StatusCode);
            for (int i = 0; i < 3; i++) Assert.AreEqual(HttpStatusCode.OK, (await api.PostAsync($"/api/v1/tickets/{fixture.Ticket.Ticket.Id}/enviar", null)).StatusCode);

            var worker = new NotificationDeliveryWorker(factory.Services.GetRequiredService<IServiceScopeFactory>(),
                factory.Services.GetRequiredService<IOptions<NotificationOptions>>(), NullLogger<NotificationDeliveryWorker>.Instance);
            // Two independent worker cycles concurrently; only one transport per channel.
            sms.DelayMs = 1000;
            var deliveries = Task.WhenAll(worker.RunOnceAsync(default), worker.RunOnceAsync(default));
            using (var wait = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                while (sms.Requests.IsEmpty) await Task.Delay(10, wait.Token);
            // External HTTP is still waiting: no Ticket/Inventory transaction may be held for it.
            await using (var locks = CreateContext())
            {
                await using var tx = await locks.Database.BeginTransactionAsync();
                await locks.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '500ms'");
                await locks.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Tickets\" WHERE \"Id\" = {fixture.Ticket.Ticket.Id} FOR UPDATE");
                await locks.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Inventarios\" WHERE \"TanqueId\" = {fixture.Tank} FOR UPDATE");
            }
            await deliveries;
            var ticket = await api.GetFromJsonAsync<TicketResponse>($"/api/v1/tickets/{fixture.Ticket.Ticket.Id}");
            Assert.AreEqual(EstadoTicket.Enviado, ticket!.Estado);
            Assert.AreEqual(1, sms.Requests.Count);
            using var payload = JsonDocument.Parse(sms.Requests.Single().Body);
            var body = payload.RootElement.GetProperty("message").GetString()!;
            StringAssert.Contains(body, ticket.Codigo); Assert.IsFalse(body.Contains("FTQR1"));
            var url = body.Split("Descarga segura: ")[1]; var raw = url.Split('/').Last();
            using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            var download = await anonymous.GetAsync(new Uri(url).AbsolutePath);
            Assert.AreEqual(HttpStatusCode.OK, download.StatusCode); Assert.AreEqual("application/pdf", download.Content.Headers.ContentType!.MediaType);
            Assert.IsTrue((await download.Content.ReadAsByteArrayAsync()).AsSpan().StartsWith("%PDF"u8));
            Assert.IsTrue(download.Headers.CacheControl!.NoStore);
            Assert.IsFalse(factory.Logs.Lines.Any(line => line.Contains(raw) || line.Contains("fixture-key-only")));

            using var mailbox = new HttpClient { BaseAddress = new Uri(mailUrl!) };
            using var list = JsonDocument.Parse(await mailbox.GetStringAsync("/api/v1/messages"));
            var messages = list.RootElement.GetProperty("messages").EnumerateArray().Where(m => m.GetProperty("Subject").GetString()!.Contains(ticket.Codigo)).ToArray();
            Assert.AreEqual(1, messages.Length);
            var mailId = messages[0].GetProperty("ID").GetString();
            using var mime = await MimeMessage.LoadAsync(new MemoryStream(await mailbox.GetByteArrayAsync($"/api/v1/message/{mailId}/raw")));
            Assert.AreEqual("postgres@example.test", mime.To.Mailboxes.Single().Address);
            StringAssert.Contains(mime.TextBody, ticket.EmpleadoNombre); StringAssert.Contains(mime.TextBody, ticket.VehiculoPlaca);
            Assert.AreEqual("application/pdf", ((MimePart)mime.Attachments.Single()).ContentType.MimeType);

            var validation = await api.PostAsJsonAsync("/api/v1/tickets/validar", new ValidateTicketRequest { QrPayload = fixture.Ticket.QrPayload });
            Assert.IsTrue((await validation.Content.ReadFromJsonAsync<TicketValidationResponse>())!.Valido);
            var dispatch = await api.PostAsJsonAsync("/api/v1/despachos", fixture.Request());
            Assert.AreEqual(HttpStatusCode.Created, dispatch.StatusCode);
            Assert.AreEqual(5m, (await dispatch.Content.ReadFromJsonAsync<DispatchResponse>())!.InventarioRestante);
            Assert.AreEqual(HttpStatusCode.OK, (await api.GetAsync("/api/v1/inventario")).StatusCode);
            var report = await api.GetAsync("/api/v1/reportes?tipo=despachos"); Assert.AreEqual(HttpStatusCode.OK, report.StatusCode);
            StringAssert.Contains(await report.Content.ReadAsStringAsync(), ticket.Codigo);
            Assert.AreEqual(HttpStatusCode.Gone, (await anonymous.GetAsync(new Uri(url).AbsolutePath)).StatusCode);

            var notifications = await api.GetAsync("/api/v1/notificaciones?estado=ENVIADA&canal=EMAIL&tipo=TICKET_EMITIDO&pagina=1&tamanoPagina=10");
            Assert.AreEqual(HttpStatusCode.OK, notifications.StatusCode);
            Assert.AreEqual(HttpStatusCode.BadRequest, (await api.GetAsync("/api/v1/notificaciones?pagina=0")).StatusCode);
            int failedId;
            await using (var db = CreateContext())
            {
                var failed = NotificationQueue.New("TEST", "retry", "EMAIL", "ops@example.test"); failed.Estado = "FALLIDA"; failed.Intentos = 4; failed.IntentosTotales = 4;
                db.Notificaciones.Add(failed); await db.SaveChangesAsync(); failedId = failed.Id;
            }
            Assert.AreEqual(HttpStatusCode.OK, (await api.PostAsync($"/api/v1/notificaciones/{failedId}/reintentar", null)).StatusCode);
            Assert.AreEqual(HttpStatusCode.Conflict, (await api.PostAsync($"/api/v1/notificaciones/{failedId}/reintentar", null)).StatusCode);
            await using var verify = CreateContext(); var retry = await verify.Notificaciones.SingleAsync(n => n.Id == failedId);
            Assert.AreEqual(4, retry.IntentosTotales); Assert.AreEqual(0, retry.Intentos);
            Assert.AreEqual(1, await verify.Auditorias.CountAsync(a => a.Evento == "NOTIFICACION_REPROGRAMADA"));
        }
        finally { Environment.SetEnvironmentVariable("Jwt__Key", null); }
    }

    [TestMethod, TestCategory("NotificationTransport")]
    public async Task Notifications_RealProviderFailureExhaustsRetriesAndCreatesInternalAlert()
    {
        var fixture = await QueuedTicketAsync();
        await using var sms = new LocalGateway { Status = 500, Response = "sensitive-provider-body-must-not-persist" };
        var configuration = new NotificationOptions { Smtp = new() { Port = int.Parse(Environment.GetEnvironmentVariable("FUELTRACK_SMTP_PORT")!) }, Sms = new() { BaseUrl = sms.Url } };
        Environment.SetEnvironmentVariable("Jwt__Key", "TEST-JWT-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        try
        {
            await using var factory = new NotificationWebFactory(_connectionString, configuration, fixture.Options.Value);
            using var api = factory.CreateClient();
            var worker = new NotificationDeliveryWorker(factory.Services.GetRequiredService<IServiceScopeFactory>(), factory.Services.GetRequiredService<IOptions<NotificationOptions>>(), NullLogger<NotificationDeliveryWorker>.Instance);
            for (var attempt = 0; attempt < 4; attempt++)
            {
                await worker.RunOnceAsync(default);
                await using var db = CreateContext();
                await db.Database.ExecuteSqlRawAsync("UPDATE \"Notificaciones\" SET \"ProximoIntentoUtc\" = now() - interval '1 second' WHERE \"Estado\" = 'PENDIENTE'");
            }
            await worker.RunOnceAsync(default); // Exhausted failures must not be sent again.
            Assert.AreEqual(4, sms.Requests.Count);
            await using var verify = CreateContext();
            var failed = await verify.Notificaciones.SingleAsync(n => n.Canal == "SMS");
            Assert.AreEqual("FALLIDA", failed.Estado); Assert.AreEqual("SMS_HTTP_500", failed.UltimoError);
            Assert.AreEqual(EstadoTicket.Pendiente, (await verify.Tickets.SingleAsync()).Estado);
            Assert.AreEqual(1, await verify.Notificaciones.CountAsync(n => n.Canal == "INTERNO"));
            Assert.AreEqual(1, await verify.Auditorias.CountAsync(a => a.Evento == "INTEGRACION_FALLIDA"));
            Assert.IsFalse(factory.Logs.Lines.Any(line => line.Contains("sensitive-provider-body") || line.Contains("fixture-key-only")));
        }
        finally { Environment.SetEnvironmentVariable("Jwt__Key", null); }
    }
}
