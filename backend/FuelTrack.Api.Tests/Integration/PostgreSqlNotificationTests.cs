using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Notifications;
using FuelTrack.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FuelTrack.Api.Tests.Integration;

public sealed partial class PostgreSqlSecurityTests
{
    private static IOptions<NotificationOptions> DeliveryOptions(int batch = 10) => Options.Create(new NotificationOptions
    { BatchSize = batch, Smtp = new() { Enabled = true }, Sms = new() { Enabled = true } });
    private async Task<DispatchFixture> QueuedTicketAsync()
    {
        var fixture = await SeedDispatchAsync();
        await using var db = CreateContext();
        await CreateTicketService(db, fixture.Options).PrepareSendAsync(fixture.Ticket.Ticket.Id, fixture.Actor, null, default);
        return fixture;
    }
    [TestMethod]
    public async Task Notification_ConcurrentClaimsAndRecoveryFenceStaleWorker()
    {
        await QueuedTicketAsync(); var options = DeliveryOptions(1);
        async Task<Notificacion> Claim()
        {
            await using var db = CreateContext(); return (await new NotificationDeliveryService(db, options).ClaimAsync(default)).Single();
        }
        var claims = await Task.WhenAll(Claim(), Claim());
        Assert.AreEqual(2, claims.Select(n => n.Id).Distinct().Count());
        await using (var db = CreateContext())
        {
            Assert.AreEqual(0, (await new NotificationDeliveryService(db, options).ClaimAsync(default)).Count);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Notificaciones\" SET \"BloqueadaHastaUtc\" = {DateTime.UtcNow.AddSeconds(-1)} WHERE \"Id\" = {claims[0].Id}");
        }
        var recovered = await Claim(); Assert.AreEqual(claims[0].Id, recovered.Id); Assert.AreNotEqual(claims[0].ReservaId, recovered.ReservaId);
        await using (var db = CreateContext()) await new NotificationDeliveryService(db, options).CompleteAsync(claims[0], DeliveryResult.Sent(), default);
        await using (var db = CreateContext()) Assert.AreEqual("PROCESANDO", (await db.Notificaciones.SingleAsync(n => n.Id == recovered.Id)).Estado);
        await using (var db = CreateContext()) await new NotificationDeliveryService(db, options).CompleteAsync(recovered, DeliveryResult.Sent(), default);
        await using (var db = CreateContext()) Assert.AreEqual("ENVIADA", (await db.Notificaciones.SingleAsync(n => n.Id == recovered.Id)).Estado);
    }
    [TestMethod]
    public async Task Notification_SkipLockedDoesNotWaitForAnotherClaim()
    {
        await QueuedTicketAsync();
        await using var lockDb = CreateContext(); await using var tx = await lockDb.Database.BeginTransactionAsync();
        var first = await lockDb.Notificaciones.OrderBy(n => n.Id).FirstAsync();
        await lockDb.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Notificaciones\" WHERE \"Id\" = {first.Id} FOR UPDATE");
        await using var other = CreateContext(); using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var batch = await new NotificationDeliveryService(other, DeliveryOptions()).ClaimAsync(deadline.Token);
        Assert.AreEqual(1, batch.Count); Assert.AreNotEqual(first.Id, batch[0].Id);
    }
    [TestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task Notification_AllChannelsRequiredAndSingleTicketAudit(bool concurrent)
    {
        var fixture = await QueuedTicketAsync(); var options = DeliveryOptions();
        List<Notificacion> claims;
        await using (var db = CreateContext()) claims = await new NotificationDeliveryService(db, options).ClaimAsync(default);
        async Task Complete(Notificacion n)
        { await using var db = CreateContext(); await new NotificationDeliveryService(db, options).CompleteAsync(n, DeliveryResult.Sent(), default); }
        if (concurrent) await Task.WhenAll(claims.Select(Complete));
        else
        {
            await Complete(claims[0]); await using var verify = CreateContext();
            Assert.AreEqual(EstadoTicket.Pendiente, (await verify.Tickets.SingleAsync()).Estado);
            await Complete(claims[1]);
        }
        await using var final = CreateContext();
        Assert.AreEqual(EstadoTicket.Enviado, (await final.Tickets.SingleAsync()).Estado);
        Assert.AreEqual(1, await final.Auditorias.CountAsync(a => a.Evento == "TICKET_ENVIADO"));
        var again = await CreateTicketService(final, fixture.Options).PrepareSendAsync(fixture.Ticket.Ticket.Id, fixture.Actor, null, default);
        Assert.AreEqual(2, await final.Notificaciones.CountAsync());
    }
    [TestMethod]
    public async Task Notification_RetryBackoffMaxAttemptsInternalAlertAndNoFalseEnviado()
    {
        var fixture = await QueuedTicketAsync(); var options = DeliveryOptions(1); options.Value.MaxAttempts = 2;
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            await using var db = CreateContext(); var service = new NotificationDeliveryService(db, options);
            var claim = (await service.ClaimAsync(default)).Single();
            await service.CompleteAsync(claim, DeliveryResult.Error("SMTP_CONEXION", true), default);
            db.ChangeTracker.Clear(); var updated = await db.Notificaciones.SingleAsync(n => n.Id == claim.Id);
            Assert.AreEqual(attempt, updated.Intentos); Assert.AreEqual(attempt, updated.IntentosTotales);
            if (attempt == 1)
            {
                Assert.IsTrue(updated.ProximoIntentoUtc > DateTime.UtcNow.AddSeconds(50));
                updated.ProximoIntentoUtc = DateTime.UtcNow.AddSeconds(-1); await db.SaveChangesAsync();
            }
            else Assert.AreEqual("FALLIDA", updated.Estado);
        }
        await using var verify = CreateContext();
        Assert.AreEqual(1, await verify.Notificaciones.CountAsync(n => n.Tipo == "INTEGRACION_FALLIDA" && n.Canal == "INTERNO"));
        Assert.AreEqual(1, await verify.Auditorias.CountAsync(a => a.Evento == "INTEGRACION_FALLIDA"));
        Assert.AreEqual(EstadoTicket.Pendiente, (await verify.Tickets.SingleAsync()).Estado);
        await CreateTicketService(verify, fixture.Options).PrepareSendAsync(fixture.Ticket.Ticket.Id, fixture.Actor, null, default);
        Assert.AreEqual(2, await verify.Notificaciones.CountAsync(n => n.Tipo == "TICKET_EMITIDO"));
    }
    [TestMethod]
    public async Task Notification_CompletionAuditFailureRollsBack()
    {
        await QueuedTicketAsync(); var options = DeliveryOptions(1);
        await using var db = CreateContext(); var service = new NotificationDeliveryService(db, options);
        var claim = (await service.ClaimAsync(default)).Single();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION reject_notification_audit() RETURNS trigger AS $$ BEGIN
            IF NEW."Evento" = 'NOTIFICACION_ENVIADA' THEN RAISE EXCEPTION 'controlled failure'; END IF; RETURN NEW; END $$ LANGUAGE plpgsql;
            CREATE TRIGGER reject_notification_audit BEFORE INSERT ON "Auditorias" FOR EACH ROW EXECUTE FUNCTION reject_notification_audit();
            """);
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => service.CompleteAsync(claim, DeliveryResult.Sent(), default));
        await using var verify = CreateContext(); Assert.AreEqual("PROCESANDO", (await verify.Notificaciones.SingleAsync(n => n.Id == claim.Id)).Estado);
    }

    [TestMethod]
    [DataRow(EstadoTicket.Consumido)] [DataRow(EstadoTicket.Anulado)] [DataRow(EstadoTicket.Vencido)]
    public async Task Notification_DeliveryCompletionNeverRevivesTerminalTicket(EstadoTicket state)
    {
        await QueuedTicketAsync(); var options = DeliveryOptions(); List<Notificacion> claims;
        await using (var db = CreateContext()) claims = await new NotificationDeliveryService(db, options).ClaimAsync(default);
        await using (var db = CreateContext()) { (await db.Tickets.SingleAsync()).Estado = state; await db.SaveChangesAsync(); }
        foreach (var claim in claims) { await using var db = CreateContext(); await new NotificationDeliveryService(db, options).CompleteAsync(claim, DeliveryResult.Sent(), default); }
        await using var verify = CreateContext(); Assert.AreEqual(state, (await verify.Tickets.SingleAsync()).Estado);
        Assert.AreEqual(0, await verify.Auditorias.CountAsync(a => a.Evento == "TICKET_ENVIADO"));
    }

    [TestMethod]
    public async Task Notification_DisabledChannelsRemainPendingWithoutAttempts()
    {
        await QueuedTicketAsync(); await using var db = CreateContext();
        Assert.AreEqual(0, (await new NotificationDeliveryService(db, Options.Create(new NotificationOptions())).ClaimAsync(default)).Count);
        Assert.IsTrue(await db.Notificaciones.AllAsync(n => n.Estado == "PENDIENTE" && n.Intentos == 0));
    }

    [TestMethod]
    public async Task Notification_UnknownTicketCannotAcquireSecureLink()
    {
        await using var db = CreateContext();
        db.TicketDeliveryLinks.Add(new TicketDeliveryLink { Id = Guid.NewGuid(), TicketId = Guid.NewGuid(), TokenHash = new string('a', 64), CreadoEnUtc = DateTime.UtcNow, ExpiraEnUtc = DateTime.UtcNow.AddHours(1) });
        var error = await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.AreEqual("23503", ((PostgresException)error.InnerException!).SqlState);
    }
    [TestMethod]
    [DataRow(25, 0)] [DataRow(23, 2)] [DataRow(-1, 2)]
    public async Task Notification_TicketRulesWindowAndTenRunsDedupe(int hours, int expected)
    {
        await SeedDispatchAsync(); var options = DeliveryOptions();
        await using (var db = CreateContext()) { (await db.Tickets.SingleAsync()).FechaVencimiento = DateTime.UtcNow.AddHours(hours); await db.SaveChangesAsync(); }
        for (int i = 0; i < 10; i++) { await using var db = CreateContext(); await new NotificationRuleService(db, options).RunOnceAsync(default); }
        await using var verify = CreateContext(); Assert.AreEqual(expected, await verify.Notificaciones.CountAsync());
        Assert.AreEqual(hours < 0 ? EstadoTicket.Vencido : EstadoTicket.Creado, (await verify.Tickets.SingleAsync()).Estado);
    }
    [TestMethod]
    [DataRow(EstadoTicket.Consumido)] [DataRow(EstadoTicket.Anulado)]
    public async Task Notification_RulesNeverReviveTerminalTickets(EstadoTicket state)
    {
        await SeedDispatchAsync(); await using var db = CreateContext(); var ticket = await db.Tickets.SingleAsync();
        ticket.Estado = state; ticket.FechaVencimiento = DateTime.UtcNow.AddHours(-1); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await new NotificationRuleService(db, DeliveryOptions()).RunOnceAsync(default);
        Assert.AreEqual(state, (await db.Tickets.SingleAsync()).Estado); Assert.AreEqual(0, await db.Notificaciones.CountAsync());
    }
    [TestMethod]
    [DataRow(101, 0)] [DataRow(100, 1)] [DataRow(50, 1)]
    public async Task Notification_LowInventoryBoundaryAndDedupe(int balance, int expected)
    {
        await SeedDispatchAsync(); var options = DeliveryOptions(); options.Value.Operations.Emails = ["ops@example.test"];
        await using (var db = CreateContext()) { (await db.Tanques.SingleAsync()).NivelCritico = 100; (await db.Inventarios.SingleAsync()).ExistenciaActual = balance; await db.SaveChangesAsync(); }
        async Task Run() { await using var db = CreateContext(); await new NotificationRuleService(db, options).RunOnceAsync(default); }
        await Task.WhenAll(Run(), Run());
        await using var verify = CreateContext(); Assert.AreEqual(expected, await verify.Notificaciones.CountAsync(n => n.Tipo == "INVENTARIO_BAJO"));
    }
    [TestMethod]
    public async Task Notification_AdjustmentOnlyAndConcurrentDedupe()
    {
        var fixture = await SeedDispatchAsync(); var options = DeliveryOptions(); options.Value.Operations.Emails = ["ops@example.test"];
        await using (var db = CreateContext())
        {
            foreach (var type in new[] { TipoMovimiento.Ajuste, TipoMovimiento.Entrada, TipoMovimiento.Salida, TipoMovimiento.Transferencia })
                db.MovimientosInventario.Add(new MovimientoInventario { Tipo = type, TanqueId = fixture.Tank, UsuarioId = fixture.Actor, Volumen = 1, FechaHora = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        async Task Run() { await using var db = CreateContext(); await new NotificationRuleService(db, options).RunOnceAsync(default); }
        await Task.WhenAll(Run(), Run());
        await using var verify = CreateContext(); var notification = await verify.Notificaciones.SingleAsync(n => n.Tipo == "AJUSTE_INVENTARIO");
        StringAssert.Contains(notification.Mensaje!, $"actor {fixture.Actor}");
    }
    [TestMethod]
    [DataRow("valid", "OK")] [DataRow("wrong", "LINK_INVALIDO")] [DataRow("expired", "LINK_EXPIRADO")]
    [DataRow("revoked", "LINK_REVOCADO")] [DataRow("consumed", "LINK_REVOCADO")]
    public async Task Notification_SecureLinksHashOnlyExpiryRevocationAndPdf(string scenario, string expected)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        await SeedDispatchAsync(); var options = DeliveryOptions(); options.Value.PublicBaseUrl = "https://tickets.example.test";
        string raw;
        await using (var db = CreateContext())
        {
            var service = new TicketDeliveryLinkService(db, new TicketPdfService(), options); var ticket = await db.Tickets.SingleAsync();
            raw = (await service.CreateAsync(ticket, default)).Split('/').Last(); var link = await db.TicketDeliveryLinks.SingleAsync();
            Assert.AreEqual(43, raw.Length); Assert.AreEqual(TicketDeliveryLinkService.Hash(raw), link.TokenHash); Assert.IsTrue(link.ExpiraEnUtc <= ticket.FechaVencimiento);
            if (scenario == "expired") link.ExpiraEnUtc = DateTime.UtcNow.AddSeconds(-1);
            if (scenario == "revoked") link.RevocadoEnUtc = DateTime.UtcNow;
            if (scenario == "consumed") ticket.Estado = EstadoTicket.Consumido;
            await db.SaveChangesAsync();
        }
        await using var execution = CreateContext(); var downloader = new TicketDeliveryLinkService(execution, new TicketPdfService(), options);
        if (scenario == "valid") Assert.IsTrue((await downloader.DownloadAsync(raw, "127.0.0.1", default)).Content.AsSpan().StartsWith("%PDF"u8));
        else
        {
            var error = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => downloader.DownloadAsync(scenario == "wrong" ? new string('z', 43) : raw, null, default));
            Assert.AreEqual(expected, error.Code);
        }
        Assert.IsFalse((await execution.Auditorias.ToListAsync()).Any(a => a.DatosRelevantes?.Contains(raw) == true));
    }
    [TestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task Notification_MigrationBackfillPreservesHistoryOrRejectsDuplicate(bool duplicate)
    {
        await using var db = CreateContext(); var migrator = db.GetService<IMigrator>();
        var previous = db.Database.GetMigrations().Reverse().Skip(1).First(); await migrator.MigrateAsync(previous);
        var count = duplicate ? 2 : 1;
        for (var i = 0; i < count; i++) await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "Notificaciones" ("Tipo", "Destinatario", "Estado", "FechaHora", "Canal", "ReferenciaEvento")
            VALUES ('TICKET_EMITIDO', 'history@example.test', 'PENDIENTE', now(), 'EMAIL', 'history-ticket')
            """);
        if (duplicate)
        {
            await Assert.ThrowsExactlyAsync<PostgresException>(() => migrator.MigrateAsync());
            await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync();
            Assert.AreEqual(2L, await ScalarLongAsync(connection, "SELECT count(*) FROM \"Notificaciones\""));
        }
        else
        {
            await migrator.MigrateAsync(); var row = await db.Notificaciones.SingleAsync();
            Assert.AreEqual("TICKET_EMITIDO:history-ticket:EMAIL:history@example.test", row.ClaveIdempotencia);
            Assert.AreEqual("PENDIENTE", row.Estado);
            await Assert.ThrowsExactlyAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("""
                INSERT INTO "Notificaciones" ("Tipo", "Destinatario", "Estado", "FechaHora", "Canal", "ClaveIdempotencia", "Intentos", "IntentosTotales")
                VALUES ('TICKET_EMITIDO', 'history@example.test', 'PENDIENTE', now(), 'EMAIL', 'TICKET_EMITIDO:history-ticket:EMAIL:history@example.test', 0, 0)
                """));
        }
    }
}
