using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Estaciones;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class EstacionesControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private EstacionesController _controller = null!;
    private int _seq = 0;

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
        _controller = new EstacionesController(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_db is not null) await _db.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }

    // ── Helper: crea un despacho histórico asociado a una estación ────────────

    private async Task CreateDespachoAsync(int estacionId)
    {
        var n = ++_seq;

        var actor = new Usuario
        {
            NombreUsuario = $"operador{n}", PasswordHash = "x", Activo = true, SecurityVersion = 1
        };
        var tipo = new TipoCombustible { Nombre = $"Diesel{n}", Activo = true };
        var tanque = new Tanque
        {
            Identificacion = $"TQ-{n}", Capacidad = 5000m, NivelActual = 0m,
            NivelCritico = 100m, Activo = true, TipoCombustible = tipo
        };
        var depto = new Departamento { Nombre = $"Depto{n}", Activo = true };
        var empleado = new Empleado
        {
            Codigo = $"E{n}", NombreCompleto = "Test", Cedula = $"C{n}",
            Cargo = "Analista", Correo = $"e{n}@test.com", Telefono = "8090000000",
            DepartamentoId = 0, Departamento = depto, Activo = true
        };
        var vehiculo = new Vehiculo
        {
            Placa = $"P{n}", Ficha = $"F{n}", Marca = "Toyota", Modelo = "Hilux",
            Año = 2022, Tipo = "Pickup", CapacidadTanque = 70m, Odometro = 0m,
            DepartamentoId = 0, Departamento = depto, Activo = true
        };

        _db.Usuarios.Add(actor);
        _db.TiposCombustible.Add(tipo);
        _db.Tanques.Add(tanque);
        _db.Departamentos.Add(depto);
        _db.Empleados.Add(empleado);
        _db.Vehiculos.Add(vehiculo);
        await _db.SaveChangesAsync();

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(), NumeroSecuencial = n, Prefijo = "COM",
            FechaCreacion = DateTime.UtcNow, FechaVencimiento = DateTime.UtcNow.AddHours(8),
            Estado = EstadoTicket.Consumido, CantidadAutorizada = 10m,
            HashSeguridad = "x", TokenValidacion = "x", FirmaDigital = "x", QrCodePng = [],
            TipoCombustibleId = tipo.Id, EmpleadoId = empleado.Id,
            VehiculoId = vehiculo.Id, DepartamentoId = depto.Id
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        _db.Despachos.Add(new Despacho
        {
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow), Hora = TimeOnly.FromDateTime(DateTime.UtcNow),
            GalonesServidos = 10m, TanqueId = tanque.Id, TicketId = ticket.Id,
            OperadorId = actor.Id, EstacionId = estacionId,
            InventarioRestante = 0m, DisponibilidadRestante = 0m
        });
        await _db.SaveChangesAsync();
    }

    // ── GetAll ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_CuandoNoHayDatos()
    {
        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<EstacionDto>;
        Assert.AreEqual(0, list!.Count);
    }

    [TestMethod]
    public async Task GetAll_ReturnsActivasEInactivas_CuandoExisten()
    {
        _db.Estaciones.AddRange(
            new Estacion { Nombre = "Estación Norte", Activo = true },
            new Estacion { Nombre = "Estación Inactiva", Activo = false });
        await _db.SaveChangesAsync();

        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<EstacionDto>;
        Assert.AreEqual(2, list!.Count);
        Assert.IsTrue(list.Any(e => e.Nombre == "Estación Norte" && e.Activo));
        Assert.IsTrue(list.Any(e => e.Nombre == "Estación Inactiva" && !e.Activo));
    }

    // ── GetById ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetById_Returns404_CuandoNoExiste()
    {
        var result = await _controller.GetById(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task GetById_ReturnsDto_CuandoExiste()
    {
        var est = new Estacion { Nombre = "Estación Sur", Activo = true };
        _db.Estaciones.Add(est);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(est.Id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as EstacionDto;
        Assert.AreEqual("Estación Sur", dto!.Nombre);
        Assert.IsTrue(dto.Activo);
    }

    // ── Create ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Create_Returns201_ConDto()
    {
        var req = new SaveEstacionRequest("Estación Este");
        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as EstacionDto;
        Assert.AreEqual("Estación Este", dto!.Nombre);
        Assert.IsTrue(dto.Activo);
    }

    // ── Update ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Update_Returns404_CuandoNoExiste()
    {
        var req = new SaveEstacionRequest("Estación X");
        var result = await _controller.Update(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Update_Returns200_ConDtoActualizado()
    {
        var est = new Estacion { Nombre = "Estación Vieja", Activo = true };
        _db.Estaciones.Add(est);
        await _db.SaveChangesAsync();

        var req = new SaveEstacionRequest("Estación Nueva", false);
        var result = await _controller.Update(est.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as EstacionDto;
        Assert.AreEqual("Estación Nueva", dto!.Nombre);
        Assert.IsFalse(dto.Activo);
    }

    [TestMethod]
    public async Task Update_Returns409_CuandoDesactivaConDespachosAsociados()
    {
        var est = new Estacion { Nombre = "Estación Con Historial", Activo = true };
        _db.Estaciones.Add(est);
        await _db.SaveChangesAsync();

        await CreateDespachoAsync(est.Id);

        var req = new SaveEstacionRequest("Estación Con Historial", false);
        var result = await _controller.Update(est.Id, req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;
        Assert.IsNotNull(conflict, "Esperaba 409 Conflict");

        var body = conflict.Value!;
        var code = body.GetType().GetProperty("code")?.GetValue(body)?.ToString();
        Assert.AreEqual("ESTACION_CON_DESPACHOS", code);

        await _db.Entry(est).ReloadAsync();
        Assert.IsTrue(est.Activo);
    }

    [TestMethod]
    public async Task Update_PermiteEditarNombre_SinDesactivar_AunConDespachos()
    {
        var est = new Estacion { Nombre = "Estación Con Historial", Activo = true };
        _db.Estaciones.Add(est);
        await _db.SaveChangesAsync();

        await CreateDespachoAsync(est.Id);

        var req = new SaveEstacionRequest("Estación Renombrada", true);
        var result = await _controller.Update(est.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as EstacionDto;
        Assert.AreEqual("Estación Renombrada", dto!.Nombre);
        Assert.IsTrue(dto.Activo);
    }

    [TestMethod]
    public async Task Update_PermiteReactivar_EstacionDesactivada()
    {
        var est = new Estacion { Nombre = "Estación Reactivable", Activo = false };
        _db.Estaciones.Add(est);
        await _db.SaveChangesAsync();

        var req = new SaveEstacionRequest("Estación Reactivable", true);
        var result = await _controller.Update(est.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as EstacionDto;
        Assert.IsTrue(dto!.Activo);
    }

    // ── Deactivate ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Deactivate_Returns404_CuandoNoExiste()
    {
        var result = await _controller.Deactivate(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Deactivate_Returns204_CuandoNoTieneDespachos()
    {
        var est = new Estacion { Nombre = "Estación Nueva", Activo = true };
        _db.Estaciones.Add(est);
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(est.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);

        await _db.Entry(est).ReloadAsync();
        Assert.IsFalse(est.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoTieneDespachosHistoricos()
    {
        var est = new Estacion { Nombre = "Estación Con Historial", Activo = true };
        _db.Estaciones.Add(est);
        await _db.SaveChangesAsync();

        await CreateDespachoAsync(est.Id);

        var result = await _controller.Deactivate(est.Id, CancellationToken.None);
        var conflict = result as ConflictObjectResult;
        Assert.IsNotNull(conflict, "Esperaba 409 Conflict");

        var body = conflict.Value!;
        var code = body.GetType().GetProperty("code")?.GetValue(body)?.ToString();
        Assert.AreEqual("ESTACION_CON_DESPACHOS", code);

        await _db.Entry(est).ReloadAsync();
        Assert.IsTrue(est.Activo);
    }
}
