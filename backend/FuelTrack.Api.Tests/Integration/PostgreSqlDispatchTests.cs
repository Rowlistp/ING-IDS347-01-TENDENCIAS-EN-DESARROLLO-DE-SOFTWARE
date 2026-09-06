using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Dispatch;
using FuelTrack.Api.DTOs.Tickets;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FuelTrack.Api.Tests.Integration;

public sealed partial class PostgreSqlSecurityTests
{
    private sealed record DispatchFixture(int Actor, int Tank, int Station, TicketCreationResult Ticket, IOptions<TicketOptions> Options)
    {
        public CreateDispatchRequest Request(decimal gallons = 5) => new()
        {
            TicketId = Ticket.Ticket.Id, QrPayload = Ticket.QrPayload, TanqueId = Tank,
            EstacionId = Station, GalonesServidos = gallons, Observaciones = "Despacho de prueba"
        };
    }

    private async Task<DispatchFixture> SeedDispatchAsync(int requests = 1)
    {
        var seeded = await SeedTicketRequestsAsync(requests);
        var options = CreateTicketOptions();
        await using var db = CreateContext();
        var role = new Rol { Nombre = Roles.Despachador };
        db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = seeded.ActorId, Rol = role });
        var tank = new Tanque { Identificacion = "DISPATCH-TANK", Activo = true, Capacidad = 100, TipoCombustibleId = (await db.TiposCombustible.SingleAsync()).Id };
        var station = new Estacion { Nombre = "Estación prueba", Activo = true };
        db.AddRange(tank, station);
        db.Inventarios.Add(new Inventario { Tanque = tank, ExistenciaActual = 10, Disponibilidad = 10, UltimaActualizacion = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var ticket = await CreateTicketService(db, options).CreateAsync(new CreateTicketRequest { SolicitudId = seeded.RequestIds.First() }, seeded.ActorId, null, default);
        return new DispatchFixture(seeded.ActorId, tank.Id, station.Id, ticket, options);
    }

    private static DispatchService Dispatch(AppDbContext db, DispatchFixture fixture)
        => new(db, CreateTicketService(db, fixture.Options), new AuditService(db));

    private async Task<DispatchFixture> SecondTicketAsync(DispatchFixture fixture)
    {
        await using var db = CreateContext();
        var id = await db.SolicitudesCombustible.Where(s => !db.Tickets.Any(t => t.SolicitudId == s.Id)).Select(s => s.Id).SingleAsync();
        return fixture with { Ticket = await CreateTicketService(db, fixture.Options).CreateAsync(new CreateTicketRequest { SolicitudId = id }, fixture.Actor, null, default) };
    }

    [TestMethod]
    public async Task Dispatch_DifferentTicketsCompeteForStock_NoNegativeInventory()
    {
        var first = await SeedDispatchAsync(2);
        var second = await SecondTicketAsync(first);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<string> Run(DispatchFixture fixture)
        {
            await gate.Task;
            await using var db = CreateContext();
            try { await Dispatch(db, fixture).CreateAsync(fixture.Request(7), fixture.Actor, null, default); return "CREATED"; }
            catch (TicketDomainException ex) { return ex.Code; }
        }
        var tasks = new[] { Run(first), Run(second) };
        gate.SetResult();
        CollectionAssert.AreEquivalent(new[] { "CREATED", "INVENTARIO_INSUFICIENTE" }, await Task.WhenAll(tasks));
        await using var verify = CreateContext();
        Assert.AreEqual(3m, (await verify.Inventarios.SingleAsync()).ExistenciaActual);
        Assert.AreEqual(3m, (await verify.Inventarios.SingleAsync()).Disponibilidad);
        Assert.AreEqual(1, await verify.Despachos.CountAsync());
        Assert.AreEqual(1, await verify.MovimientosInventario.CountAsync());
        Assert.AreEqual(1, await verify.Tickets.CountAsync(t => t.Estado == EstadoTicket.Consumido));
    }

    [TestMethod]
    public async Task Dispatch_ValidQrForDifferentTicket_IsRejected()
    {
        var first = await SeedDispatchAsync(2);
        var second = await SecondTicketAsync(first);
        var request = first.Request(); request.QrPayload = second.Ticket.QrPayload;
        await using var db = CreateContext();
        var error = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => Dispatch(db, first).CreateAsync(request, first.Actor, null, default));
        Assert.AreEqual("QR_NO_COINCIDE", error.Code);
        Assert.AreEqual(0, await db.Despachos.CountAsync());
    }

    [TestMethod]
    public async Task Dispatch_SignedExpiredTicket_IsRejectedWithoutStateJob()
    {
        var fixture = await SeedDispatchAsync();
        var request = fixture.Request();
        await using (var db = CreateContext())
        {
            var ticket = await db.Tickets.SingleAsync();
            ticket.FechaCreacion = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeSeconds()).UtcDateTime;
            ticket.FechaVencimiento = ticket.FechaCreacion.AddDays(1);
            var qr = new TicketQrService(fixture.Options).Generate(ticket.Id, ticket.NumeroSecuencial, ticket.Prefijo,
                ticket.SolicitudId!.Value, ticket.EmpleadoId, ticket.VehiculoId, ticket.DepartamentoId, ticket.TipoCombustibleId,
                ticket.CantidadAutorizada, ticket.FechaCreacion, ticket.FechaVencimiento);
            ticket.TokenValidacion = qr.TokenHash; ticket.HashSeguridad = qr.PayloadHash; ticket.FirmaDigital = qr.Signature;
            request.QrPayload = qr.Payload;
            await db.SaveChangesAsync();
        }
        await using var execution = CreateContext();
        var error = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => Dispatch(execution, fixture).CreateAsync(request, fixture.Actor, null, default));
        Assert.AreEqual("TICKET_VENCIDO", error.Code);
        Assert.AreEqual(0, await execution.Despachos.CountAsync());
    }

    [TestMethod]
    public async Task Dispatch_MigrationPreservesUniqueTicketAndRequiredRestrictedTank()
    {
        var fixture = await SeedDispatchAsync();
        await using var db = CreateContext();
        await Dispatch(db, fixture).CreateAsync(fixture.Request(), fixture.Actor, null, default);
        await using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT is_nullable FROM information_schema.columns WHERE table_name = 'Despachos' AND column_name = 'TanqueId'";
        Assert.AreEqual("NO", await command.ExecuteScalarAsync());
        command.CommandText = "SELECT delete_rule FROM information_schema.referential_constraints WHERE constraint_name = 'FK_Despachos_Tanques_TanqueId'";
        Assert.AreEqual("RESTRICT", await command.ExecuteScalarAsync());
        command.CommandText = "UPDATE \"Despachos\" SET \"TanqueId\" = 2147483647";
        var foreignKey = await Assert.ThrowsExactlyAsync<Npgsql.PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.AreEqual("23503", foreignKey.SqlState);
        command.CommandText = """
            INSERT INTO "Despachos" ("Fecha", "Hora", "GalonesServidos", "TicketId", "OperadorId", "EstacionId", "TanqueId", "InventarioRestante", "DisponibilidadRestante")
            SELECT "Fecha", "Hora", "GalonesServidos", "TicketId", "OperadorId", "EstacionId", "TanqueId", "InventarioRestante", "DisponibilidadRestante" FROM "Despachos"
            """;
        var duplicate = await Assert.ThrowsExactlyAsync<Npgsql.PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.AreEqual("IX_Despachos_TicketId", duplicate.ConstraintName);
    }

    [TestMethod]
    public async Task Dispatch_StaleTicketCancellation_CannotUndoConsumption()
    {
        var fixture = await SeedDispatchAsync();
        await using var stale = CreateContext();
        var ticket = await stale.Tickets.SingleAsync();
        await using (var execution = CreateContext())
            await Dispatch(execution, fixture).CreateAsync(fixture.Request(), fixture.Actor, null, default);
        ticket.Estado = EstadoTicket.Anulado;
        await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());
    }

    [TestMethod]
    [DataRow(5)]
    [DataRow(10)]
    public async Task Dispatch_AtomicHappyPathAndPartialConsume(int gallons)
    {
        var fixture = await SeedDispatchAsync();
        await using (var db = CreateContext())
        {
            var result = await Dispatch(db, fixture).CreateAsync(fixture.Request(gallons), fixture.Actor, "127.0.0.1", default);
            Assert.AreEqual(10m - gallons, result.InventarioRestante);
            Assert.AreEqual(EstadoTicket.Consumido, result.EstadoTicket);
        }
        await using var verify = CreateContext();
        Assert.AreEqual(1, await verify.Despachos.CountAsync());
        Assert.AreEqual(10m - gallons, (await verify.Inventarios.SingleAsync()).ExistenciaActual);
        Assert.AreEqual(10m - gallons, (await verify.Inventarios.SingleAsync()).Disponibilidad);
        var movement = await verify.MovimientosInventario.SingleAsync();
        Assert.AreEqual(TipoMovimiento.Salida, movement.Tipo);
        Assert.AreEqual(-(decimal)gallons, movement.Volumen);
        Assert.AreEqual(1, await verify.Auditorias.CountAsync(a => a.Evento == "DESPACHO_REGISTRADO"));
        var error = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => Dispatch(verify, fixture).CreateAsync(fixture.Request(1), fixture.Actor, null, default));
        Assert.AreEqual("TICKET_CONSUMIDO", error.Code);
    }

    [TestMethod]
    [DataRow("malformed", "QR_INVALIDO")]
    [DataRow("signature", "QR_INVALIDO")]
    [DataRow("token", "QR_NO_COINCIDE")]
    [DataRow("missingTicket", "TICKET_NO_ENCONTRADO")]
    [DataRow("expired", "TICKET_VENCIDO")]
    [DataRow("cancelled", "TICKET_ANULADO")]
    [DataRow("consumed", "TICKET_CONSUMIDO")]
    [DataRow("zero", "GALONES_INVALIDOS")]
    [DataRow("negative", "GALONES_INVALIDOS")]
    [DataRow("precision", "GALONES_INVALIDOS")]
    [DataRow("excess", "GALONES_EXCEDEN_AUTORIZACION")]
    [DataRow("missingTank", "TANQUE_NO_ENCONTRADO")]
    [DataRow("inactiveTank", "TANQUE_INACTIVO")]
    [DataRow("wrongFuel", "COMBUSTIBLE_INCORRECTO")]
    [DataRow("missingStock", "INVENTARIO_NO_ENCONTRADO")]
    [DataRow("insufficient", "INVENTARIO_INSUFICIENTE")]
    [DataRow("unavailable", "INVENTARIO_INSUFICIENTE")]
    [DataRow("missingStation", "ESTACION_NO_ENCONTRADA")]
    [DataRow("inactiveStation", "ESTACION_INACTIVA")]
    [DataRow("inactiveActor", "OPERADOR_INVALIDO")]
    [DataRow("wrongRole", "OPERADOR_NO_AUTORIZADO")]
    public async Task Dispatch_InvalidInput_NoPartialEffects(string scenario, string expected)
    {
        var fixture = await SeedDispatchAsync();
        var request = fixture.Request();
        await using (var db = CreateContext())
        {
            switch (scenario)
            {
                case "malformed": request.QrPayload = "invalid"; break;
                case "signature": request.QrPayload = request.QrPayload[..^8] + "AAAAAAAA"; break;
                case "token": (await db.Tickets.SingleAsync()).TokenValidacion = new string('0', 64); break;
                case "missingTicket": request.TicketId = Guid.NewGuid(); break;
                case "expired": (await db.Tickets.SingleAsync()).Estado = EstadoTicket.Vencido; break;
                case "cancelled": (await db.Tickets.SingleAsync()).Estado = EstadoTicket.Anulado; break;
                case "consumed": (await db.Tickets.SingleAsync()).Estado = EstadoTicket.Consumido; break;
                case "zero": request.GalonesServidos = 0; break;
                case "negative": request.GalonesServidos = -1; break;
                case "precision": request.GalonesServidos = 0.00001m; break;
                case "excess": request.GalonesServidos = 12; break;
                case "missingTank": request.TanqueId = int.MaxValue; break;
                case "inactiveTank": (await db.Tanques.SingleAsync()).Activo = false; break;
                case "wrongFuel": (await db.Tanques.SingleAsync()).TipoCombustible = new TipoCombustible { Nombre = "Otro" }; break;
                case "missingStock": db.Inventarios.Remove(await db.Inventarios.SingleAsync()); break;
                case "insufficient": (await db.Inventarios.SingleAsync()).ExistenciaActual = 1; break;
                case "unavailable": (await db.Inventarios.SingleAsync()).Disponibilidad = 1; break;
                case "missingStation": request.EstacionId = int.MaxValue; break;
                case "inactiveStation": (await db.Estaciones.SingleAsync()).Activo = false; break;
                case "inactiveActor": (await db.Usuarios.SingleAsync()).Activo = false; break;
                case "wrongRole": db.UsuarioRoles.Remove(await db.UsuarioRoles.SingleAsync()); break;
            }
            await db.SaveChangesAsync();
        }
        await using var execution = CreateContext();
        var error = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => Dispatch(execution, fixture).CreateAsync(request, fixture.Actor, null, default));
        Assert.AreEqual(expected, error.Code);
        await using var verify = CreateContext();
        Assert.AreEqual(0, await verify.Despachos.CountAsync());
        Assert.AreEqual(0, await verify.MovimientosInventario.CountAsync());
        Assert.AreEqual(0, await verify.Auditorias.CountAsync(a => a.Evento == "DESPACHO_REGISTRADO"));
        if (scenario != "missingStock")
            Assert.AreEqual(scenario == "insufficient" ? 1m : 10m, (await verify.Inventarios.SingleAsync()).ExistenciaActual);
    }

    [TestMethod]
    public async Task Dispatch_ConcurrentSameTicket_OneSuccessOneDecrement()
    {
        var fixture = await SeedDispatchAsync();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<string> Run()
        {
            await gate.Task;
            await using var db = CreateContext();
            try { await Dispatch(db, fixture).CreateAsync(fixture.Request(), fixture.Actor, null, default); return "CREATED"; }
            catch (TicketDomainException ex) { return ex.Code; }
        }
        var tasks = new[] { Run(), Run() };
        gate.SetResult();
        CollectionAssert.AreEquivalent(new[] { "CREATED", "TICKET_CONSUMIDO" }, await Task.WhenAll(tasks));
        await using var verify = CreateContext();
        Assert.AreEqual(1, await verify.Despachos.CountAsync());
        Assert.AreEqual(1, await verify.MovimientosInventario.CountAsync());
        Assert.AreEqual(5m, (await verify.Inventarios.SingleAsync()).ExistenciaActual);
    }

    [TestMethod]
    public async Task Dispatch_AuditFailure_RollsBackEveryWrite()
    {
        var fixture = await SeedDispatchAsync();
        await using (var db = CreateContext())
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION reject_dispatch_audit() RETURNS trigger AS $$ BEGIN
                IF NEW."Evento" = 'DESPACHO_REGISTRADO' THEN RAISE EXCEPTION 'controlled audit failure'; END IF;
                RETURN NEW; END; $$ LANGUAGE plpgsql;
                CREATE TRIGGER reject_dispatch_audit BEFORE INSERT ON "Auditorias" FOR EACH ROW EXECUTE FUNCTION reject_dispatch_audit();
                """);
            await Assert.ThrowsExactlyAsync<DbUpdateException>(() => Dispatch(db, fixture).CreateAsync(fixture.Request(), fixture.Actor, null, default));
        }
        await using var verify = CreateContext();
        Assert.AreEqual(0, await verify.Despachos.CountAsync());
        Assert.AreEqual(0, await verify.MovimientosInventario.CountAsync());
        Assert.AreEqual(EstadoTicket.Creado, (await verify.Tickets.SingleAsync()).Estado);
        Assert.AreEqual(10m, (await verify.Inventarios.SingleAsync()).ExistenciaActual);
    }

    [TestMethod]
    public async Task Dispatch_StaleInventoryWriter_CannotOverwriteStock()
    {
        var fixture = await SeedDispatchAsync();
        await using var stale = CreateContext();
        var inventory = await stale.Inventarios.SingleAsync();
        await using (var execution = CreateContext())
            await Dispatch(execution, fixture).CreateAsync(fixture.Request(), fixture.Actor, null, default);
        inventory.ExistenciaActual += 20;
        await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());
        await using var verify = CreateContext();
        Assert.AreEqual(5m, (await verify.Inventarios.SingleAsync()).ExistenciaActual);
    }
}
