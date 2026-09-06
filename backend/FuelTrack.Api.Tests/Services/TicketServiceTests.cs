using System.Net;
using System.Security.Cryptography;
using System.Text;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Tickets;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;

namespace FuelTrack.Api.Tests.Services;

[TestClass]
public sealed class TicketServiceTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private TicketService _service = null!;
    private TicketQrService _qr = null!;
    private int _actorId;
    private int _employeeId;
    private int _vehicleId;
    private int _departmentId;
    private int _fuelTypeId;

    [TestInitialize]
    public async Task Setup()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);
        await _db.Database.EnsureCreatedAsync();

        using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var options = Options.Create(new TicketOptions
        {
            Prefix = "COM",
            SigningPrivateKeyPkcs8Base64 = Convert.ToBase64String(signingKey.ExportPkcs8PrivateKey()),
            SigningPublicKeySpkiBase64 = Convert.ToBase64String(signingKey.ExportSubjectPublicKeyInfo())
        });
        _qr = new TicketQrService(options);
        _service = new TicketService(
            _db,
            new TicketNumberService(_db),
            _qr,
            new TicketPdfService(),
            new AuditService(_db),
            options);

        var department = new Departamento { Nombre = "Operaciones", Activo = true };
        var fuelType = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        var employee = new Empleado
        {
            Codigo = "EMP-QR",
            NombreCompleto = "Ada Pruebas",
            Cedula = "00100000001",
            Cargo = "Analista",
            Correo = "ada@example.test",
            Telefono = "+18095550101",
            Activo = true,
            Departamento = department
        };
        var vehicle = new Vehiculo
        {
            Placa = "QR00001",
            Ficha = "F-QR-1",
            Marca = "Prueba",
            Modelo = "Seguro",
            Año = 2026,
            Tipo = "Sedan",
            CapacidadTanque = 20,
            Activo = true,
            Departamento = department
        };
        var actor = new Usuario
        {
            NombreUsuario = "ticket-test-actor",
            PasswordHash = "test-only",
            Activo = true
        };
        _db.AddRange(department, fuelType, employee, vehicle, actor);
        await _db.SaveChangesAsync();
        _actorId = actor.Id;
        _employeeId = employee.Id;
        _vehicleId = vehicle.Id;
        _departmentId = department.Id;
        _fuelTypeId = fuelType.Id;
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [TestMethod]
    public async Task Create_FromApprovedRequest_CopiesAuthoritativeDataAndProtectsToken()
    {
        var requestId = await AddRequestAsync();

        var created = await _service.CreateAsync(
            new CreateTicketRequest { SolicitudId = requestId },
            _actorId,
            "127.0.0.1",
            default);

        Assert.AreNotEqual(Guid.Empty, created.Ticket.Id);
        Assert.AreEqual("COM", created.Ticket.Prefijo);
        StringAssert.StartsWith(created.Ticket.Codigo, $"COM-{DateTime.UtcNow.Year}-");
        Assert.AreEqual(25.5m, created.Ticket.CantidadAutorizada);
        Assert.AreEqual(_employeeId, created.Ticket.EmpleadoId);
        Assert.AreEqual(_vehicleId, created.Ticket.VehiculoId);
        Assert.AreEqual(_departmentId, created.Ticket.DepartamentoId);
        Assert.AreEqual(_fuelTypeId, created.Ticket.TipoCombustibleId);

        var stored = await _db.Tickets.SingleAsync();
        Assert.AreEqual(64, stored.HashSeguridad.Length);
        Assert.AreEqual(64, stored.TokenValidacion.Length);
        Assert.AreEqual(86, stored.FirmaDigital.Length);
        Assert.IsTrue(stored.QrCodePng.Length > 100);
        Assert.IsFalse(created.QrPayload.Contains(stored.TokenValidacion, StringComparison.Ordinal));
        Assert.AreEqual(1, await _db.Auditorias.CountAsync(item => item.Evento == "TICKET_CREADO"));
    }

    [TestMethod]
    [DataRow(EstadoSolicitud.Pendiente)]
    [DataRow(EstadoSolicitud.Rechazada)]
    public async Task Create_FromRequestNotApproved_ReturnsConflict(EstadoSolicitud state)
    {
        var requestId = await AddRequestAsync(state);

        var exception = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => _service.CreateAsync(
            new CreateTicketRequest { SolicitudId = requestId }, _actorId, null, default));

        Assert.AreEqual((int)HttpStatusCode.Conflict, exception.StatusCode);
        Assert.AreEqual("SOLICITUD_NO_APROBADA", exception.Code);
    }

    [TestMethod]
    public async Task Create_WhenRequestDoesNotExist_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => _service.CreateAsync(
            new CreateTicketRequest { SolicitudId = int.MaxValue }, _actorId, null, default));

        Assert.AreEqual((int)HttpStatusCode.NotFound, exception.StatusCode);
        Assert.AreEqual("SOLICITUD_NO_ENCONTRADA", exception.Code);
    }

    [TestMethod]
    public async Task Create_WhenRequestRelationsDoNotMatch_ReturnsBadRequest()
    {
        var requestId = await AddRequestAsync();
        var otherDepartment = new Departamento { Nombre = "Otro", Activo = true };
        _db.Departamentos.Add(otherDepartment);
        await _db.SaveChangesAsync();
        var vehicle = await _db.Vehiculos.SingleAsync(item => item.Id == _vehicleId);
        vehicle.DepartamentoId = otherDepartment.Id;
        await _db.SaveChangesAsync();

        var exception = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => CreateAsync(requestId));

        Assert.AreEqual((int)HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.AreEqual("RELACIONES_SOLICITUD_INVALIDAS", exception.Code);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(0.0)]
    public async Task Create_WithInvalidAuthorizedQuantity_ReturnsBadRequest(double? quantity)
    {
        var requestId = await AddRequestAsync(quantity: quantity is null ? null : (decimal)quantity.Value);
        var exception = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => _service.CreateAsync(
            new CreateTicketRequest { SolicitudId = requestId }, _actorId, null, default));

        Assert.AreEqual((int)HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.AreEqual("CANTIDAD_AUTORIZADA_INVALIDA", exception.Code);
    }

    [TestMethod]
    public async Task Create_WithMissingOrPastExpiration_ReturnsBadRequest()
    {
        var missing = await AddRequestAsync(hasExpiration: false);
        var missingException = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => _service.CreateAsync(
            new CreateTicketRequest { SolicitudId = missing }, _actorId, null, default));
        Assert.AreEqual("VENCIMIENTO_REQUERIDO", missingException.Code);

        var past = await AddRequestAsync(expiration: DateTime.UtcNow.AddMinutes(-1));
        var pastException = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => _service.CreateAsync(
            new CreateTicketRequest { SolicitudId = past }, _actorId, null, default));
        Assert.AreEqual("VENCIMIENTO_INVALIDO", pastException.Code);
    }

    [TestMethod]
    public async Task Create_WhenUsableTicketExists_ReturnsConflict()
    {
        var requestId = await AddRequestAsync();
        await CreateAsync(requestId);

        var exception = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => CreateAsync(requestId));

        Assert.AreEqual("TICKET_UTILIZABLE_EXISTENTE", exception.Code);
        Assert.AreEqual(1, await _db.Tickets.CountAsync());
    }

    [TestMethod]
    public async Task Create_AfterPreviousTicketIsExpired_AllowsReissue()
    {
        var requestId = await AddRequestAsync();
        var first = await CreateAsync(requestId);
        var stored = await _db.Tickets.SingleAsync(item => item.Id == first.Ticket.Id);
        stored.FechaVencimiento = DateTime.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        var second = await CreateAsync(requestId);

        Assert.AreNotEqual(first.Ticket.Id, second.Ticket.Id);
        Assert.AreNotEqual(first.Ticket.NumeroSecuencial, second.Ticket.NumeroSecuencial);
        Assert.AreEqual(EstadoTicket.Vencido, stored.Estado);
    }

    [TestMethod]
    public async Task Validate_WithAuthenticQr_ReturnsTicketDataWithoutConsuming()
    {
        var created = await CreateAsync(await AddRequestAsync());

        var result = await _service.ValidateAsync(created.QrPayload, default);

        Assert.IsTrue(result.Valido);
        Assert.AreEqual(created.Ticket.Id, result.Ticket!.Id);
        Assert.AreEqual(EstadoTicket.Creado, (await _db.Tickets.SingleAsync()).Estado);
    }

    [TestMethod]
    [DataRow("ticketId")]
    [DataRow("numero")]
    [DataRow("empleadoId")]
    [DataRow("vehiculoId")]
    [DataRow("cantidad")]
    [DataRow("emision")]
    [DataRow("expiracion")]
    [DataRow("token")]
    public async Task Validate_WhenCanonicalFieldIsModified_ReturnsInvalid(string field)
    {
        var created = await CreateAsync(await AddRequestAsync());
        var tampered = TamperCanonicalField(created.QrPayload, field);

        var result = await _service.ValidateAsync(tampered, default);

        Assert.IsFalse(result.Valido);
        Assert.AreEqual("QR_INVALIDO", result.Codigo);
    }

    [TestMethod]
    public async Task Validate_WhenEnvelopeHashOrSignatureIsModified_ReturnsInvalid()
    {
        var created = await CreateAsync(await AddRequestAsync());
        var parts = created.QrPayload.Split('.');

        var hash = parts.ToArray();
        hash[2] = ToggleLastCharacter(hash[2]);
        Assert.IsFalse((await _service.ValidateAsync(string.Join('.', hash), default)).Valido);

        var signature = parts.ToArray();
        signature[3] = ToggleLastCharacter(signature[3]);
        Assert.IsFalse((await _service.ValidateAsync(string.Join('.', signature), default)).Valido);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not-a-qr")]
    [DataRow("FTQR2.a.b.c")]
    public async Task Validate_WithMalformedPayload_ReturnsInvalid(string payload)
    {
        var result = await _service.ValidateAsync(payload, default);
        Assert.IsFalse(result.Valido);
        Assert.AreEqual("QR_INVALIDO", result.Codigo);
    }

    [TestMethod]
    public async Task Validate_WhenStoredHashTokenOrSignatureChanges_ReturnsMismatch()
    {
        var created = await CreateAsync(await AddRequestAsync());
        var stored = await _db.Tickets.SingleAsync();

        stored.HashSeguridad = new string('A', 64);
        await _db.SaveChangesAsync();
        Assert.AreEqual("QR_NO_COINCIDE", (await _service.ValidateAsync(created.QrPayload, default)).Codigo);

        stored.HashSeguridad = created.QrPayload.Split('.')[2];
        stored.TokenValidacion = new string('B', 64);
        await _db.SaveChangesAsync();
        Assert.AreEqual("QR_NO_COINCIDE", (await _service.ValidateAsync(created.QrPayload, default)).Codigo);

        stored.TokenValidacion = TicketQrService.HashToken(
            ParseQr(created.QrPayload).Token);
        stored.FirmaDigital = "firma-alterada";
        await _db.SaveChangesAsync();
        Assert.AreEqual("QR_NO_COINCIDE", (await _service.ValidateAsync(created.QrPayload, default)).Codigo);
    }

    [TestMethod]
    [DataRow(EstadoTicket.Consumido, "TICKET_CONSUMIDO")]
    [DataRow(EstadoTicket.Anulado, "TICKET_ANULADO")]
    [DataRow(EstadoTicket.Vencido, "TICKET_VENCIDO")]
    public async Task Validate_WithTerminalState_ReturnsInvalid(EstadoTicket state, string expectedCode)
    {
        var created = await CreateAsync(await AddRequestAsync());
        var stored = await _db.Tickets.SingleAsync();
        stored.Estado = state;
        await _db.SaveChangesAsync();

        var result = await _service.ValidateAsync(created.QrPayload, default);

        Assert.IsFalse(result.Valido);
        Assert.AreEqual(expectedCode, result.Codigo);
    }

    [TestMethod]
    public async Task GetById_WhenDateExpired_ReportsEffectiveExpiredState()
    {
        var created = await CreateAsync(await AddRequestAsync());
        var stored = await _db.Tickets.SingleAsync();
        stored.FechaVencimiento = DateTime.UtcNow.AddSeconds(-1);
        await _db.SaveChangesAsync();

        var response = await _service.GetByIdAsync(created.Ticket.Id, default);

        Assert.AreEqual(EstadoTicket.Vencido, response!.Estado);
    }

    [TestMethod]
    public async Task Validate_WhenExpirationPassedButStoredStateIsActive_ReturnsExpiredAndUpdatesState()
    {
        var created = await CreateAsync(await AddRequestAsync());
        var stored = await _db.Tickets.SingleAsync();
        var pastExpiration = DateTime.UtcNow.AddMinutes(-1);
        var regenerated = _qr.Generate(
            stored.Id,
            stored.NumeroSecuencial,
            stored.Prefijo,
            stored.SolicitudId!.Value,
            stored.EmpleadoId,
            stored.VehiculoId,
            stored.DepartamentoId,
            stored.TipoCombustibleId,
            stored.CantidadAutorizada,
            stored.FechaCreacion,
            pastExpiration);
        stored.FechaVencimiento = regenerated.Data.FechaExpiracionUtc;
        stored.HashSeguridad = regenerated.PayloadHash;
        stored.TokenValidacion = regenerated.TokenHash;
        stored.FirmaDigital = regenerated.Signature;
        stored.QrCodePng = regenerated.Png;
        stored.Estado = EstadoTicket.Creado;
        await _db.SaveChangesAsync();

        var result = await _service.ValidateAsync(regenerated.Payload, default);

        Assert.IsFalse(result.Valido);
        Assert.AreEqual("TICKET_VENCIDO", result.Codigo);
        Assert.AreEqual(EstadoTicket.Vencido, stored.Estado);
        Assert.AreEqual(created.Ticket.Id, stored.Id);
    }

    [TestMethod]
    public async Task Cancel_ActiveTicket_IsAuditedAndIdempotent()
    {
        var created = await CreateAsync(await AddRequestAsync());

        var cancelled = await _service.CancelAsync(
            created.Ticket.Id, "Error administrativo", _actorId, "127.0.0.1", default);
        var repeated = await _service.CancelAsync(
            created.Ticket.Id, "Segundo motivo ignorado", _actorId, "127.0.0.1", default);

        Assert.AreEqual(EstadoTicket.Anulado, cancelled.Estado);
        Assert.AreEqual("Error administrativo", repeated.MotivoAnulacion);
        Assert.AreEqual(1, await _db.Auditorias.CountAsync(item => item.Evento == "TICKET_ANULADO"));
    }

    [TestMethod]
    public async Task Cancel_ConsumedTicket_ReturnsConflict()
    {
        var created = await CreateAsync(await AddRequestAsync());
        (await _db.Tickets.SingleAsync()).Estado = EstadoTicket.Consumido;
        await _db.SaveChangesAsync();

        var exception = await Assert.ThrowsExactlyAsync<TicketDomainException>(() => _service.CancelAsync(
            created.Ticket.Id, "No procede consumo", _actorId, null, default));

        Assert.AreEqual("TICKET_CONSUMIDO", exception.Code);
    }

    [TestMethod]
    public async Task PrepareSend_CreatesPendingNotificationsWithoutTransportAndIsIdempotent()
    {
        var created = await CreateAsync(await AddRequestAsync());

        var sent = await _service.PrepareSendAsync(created.Ticket.Id, _actorId, "127.0.0.1", default);
        var repeated = await _service.PrepareSendAsync(created.Ticket.Id, _actorId, "127.0.0.1", default);

        Assert.AreEqual(2, sent.NotificacionesPendientes);
        Assert.AreEqual(0, repeated.NotificacionesPendientes);
        Assert.AreEqual(2, await _db.Notificaciones.CountAsync(item => item.Estado == "PENDIENTE"));
        CollectionAssert.AreEquivalent(
            new[] { "EMAIL", "SMS" },
            await _db.Notificaciones.Select(item => item.Canal).ToArrayAsync());
        Assert.AreEqual(EstadoTicket.Pendiente, (await _db.Tickets.SingleAsync()).Estado);
        Assert.AreEqual(1, await _db.Auditorias.CountAsync(item => item.Evento == "TICKET_PREPARADO_ENVIO"));
    }

    [TestMethod]
    public async Task PrepareSend_UsesExistingQueueEvenWhenTicketIsCreated()
    {
        var created = await CreateAsync(await AddRequestAsync());
        _db.Notificaciones.Add(new Notificacion
        {
            Tipo = "TICKET_EMITIDO", Canal = "EMAIL", Estado = "PENDIENTE",
            Destinatario = "ada@example.test", ReferenciaEvento = created.Ticket.Id.ToString("D"), FechaHora = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        var first = await _service.PrepareSendAsync(created.Ticket.Id, _actorId, null, default);
        var second = await _service.PrepareSendAsync(created.Ticket.Id, _actorId, null, default);
        Assert.AreEqual(1, first.NotificacionesPendientes);
        Assert.AreEqual(0, second.NotificacionesPendientes);
        Assert.AreEqual(2, await _db.Notificaciones.CountAsync());
        Assert.AreEqual(EstadoTicket.Pendiente, second.Ticket.Estado);
    }

    [TestMethod]
    public async Task GeneratePdf_ReturnsPdfWithSameStoredQrAndAudits()
    {
        var created = await CreateAsync(await AddRequestAsync());

        var result = await _service.GeneratePdfAsync(
            created.Ticket.Id, _actorId, "127.0.0.1", default);

        Assert.IsTrue(result.Content.Length > 1000);
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("%PDF"), result.Content[..4]);
        StringAssert.Contains(result.FileName, created.Ticket.Codigo);
        Assert.IsTrue((await _db.Tickets.SingleAsync()).QrCodePng.Length > 100);
        Assert.AreEqual(1, await _db.Auditorias.CountAsync(item => item.Evento == "TICKET_PDF_GENERADO"));
    }

    [TestMethod]
    public async Task QrGeneration_UsesFresh256BitTokenEveryTime()
    {
        var first = await CreateAsync(await AddRequestAsync());
        var second = await CreateAsync(await AddRequestAsync());

        var firstToken = ParseQr(first.QrPayload).Token;
        var secondToken = ParseQr(second.QrPayload).Token;
        Assert.AreEqual(32, WebEncoders.Base64UrlDecode(firstToken).Length);
        Assert.AreEqual(32, WebEncoders.Base64UrlDecode(secondToken).Length);
        Assert.AreNotEqual(firstToken, secondToken);
    }

    [TestMethod]
    public void QrValidation_WithPublicKeyOnly_DoesNotRequirePrivateKey()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privateOptions = Options.Create(new TicketOptions
        {
            SigningPrivateKeyPkcs8Base64 = Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
            SigningPublicKeySpkiBase64 = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo())
        });
        var generated = new TicketQrService(privateOptions).Generate(
            Guid.NewGuid(), 1, "COM", 1, 1, 1, 1, 1, 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        var publicOnly = new TicketQrService(Options.Create(new TicketOptions
        {
            SigningPublicKeySpkiBase64 = privateOptions.Value.SigningPublicKeySpkiBase64
        }));

        Assert.IsTrue(publicOnly.TryValidate(generated.Payload, out _, out _, out _));
        Assert.ThrowsExactly<InvalidOperationException>(() => publicOnly.Generate(
            Guid.NewGuid(), 2, "COM", 2, 1, 1, 1, 1, 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
    }

    private async Task<int> AddRequestAsync(
        EstadoSolicitud state = EstadoSolicitud.Aprobada,
        decimal? quantity = 25.5m,
        DateTime? expiration = null,
        bool hasExpiration = true)
    {
        var request = new SolicitudCombustible
        {
            Estado = state,
            CantidadSolicitada = 25.5m,
            CantidadAutorizada = quantity,
            TipoSolicitud = "Manual",
            FechaSolicitud = DateTime.UtcNow,
            FechaVencimiento = hasExpiration ? expiration ?? DateTime.UtcNow.AddDays(2) : null,
            EmpleadoId = _employeeId,
            VehiculoId = _vehicleId,
            DepartamentoId = _departmentId,
            TipoCombustibleId = _fuelTypeId
        };
        _db.SolicitudesCombustible.Add(request);
        await _db.SaveChangesAsync();
        return request.Id;
    }

    private Task<TicketCreationResult> CreateAsync(int requestId)
        => _service.CreateAsync(
            new CreateTicketRequest { SolicitudId = requestId },
            _actorId,
            "127.0.0.1",
            default);

    private TicketQrData ParseQr(string payload)
    {
        Assert.IsTrue(_qr.TryValidate(payload, out var data, out _, out _));
        return data!;
    }

    private static string TamperCanonicalField(string payload, string field)
    {
        var parts = payload.Split('.');
        var canonical = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(parts[1]));
        var lines = canonical.Split('\n');
        var index = Array.FindIndex(lines, line => line.StartsWith(field + "=", StringComparison.Ordinal));
        Assert.IsGreaterThan(0, index);
        lines[index] += "x";
        parts[1] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(string.Join('\n', lines)));
        return string.Join('.', parts);
    }

    private static string ToggleLastCharacter(string value)
        => value[..^1] + (value[^1] == 'A' ? 'B' : 'A');
}
