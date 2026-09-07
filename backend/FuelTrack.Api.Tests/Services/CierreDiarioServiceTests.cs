using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace FuelTrack.Api.Tests.Services;

[TestClass]
public sealed class CierreDiarioServiceTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private CierreDiarioService _service = null!;
    private int _actorId;
    private int _tanqueId;
    private DateOnly _hoy;

    [TestInitialize]
    public async Task Setup()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        _service = new CierreDiarioService(_db, new AuditService(_db));
        _hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var rol = new Rol { Nombre = "Supervisor" };
        var actor = new Usuario { NombreUsuario = "supervisor1", PasswordHash = "x", Activo = true, SecurityVersion = 1 };
        _db.Roles.Add(rol);
        _db.Usuarios.Add(actor);
        await _db.SaveChangesAsync();
        _actorId = actor.Id;

        var tipo = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var tanque = new Tanque
        {
            Identificacion = "TQ-001", Capacidad = 5000m, NivelActual = 0m,
            NivelCritico = 100m, Activo = true, TipoCombustibleId = tipo.Id
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();
        _tanqueId = tanque.Id;

        var inventario = new Inventario
        {
            TanqueId = _tanqueId, ExistenciaActual = 1000m,
            Disponibilidad = 1000m, UltimaActualizacion = DateTime.UtcNow
        };
        _db.Inventarios.Add(inventario);
        await _db.SaveChangesAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task AgregarDespacho(decimal galones, DateOnly? fecha = null)
    {
        var tipo = await _db.TiposCombustible.FirstAsync();
        var empleado = new Empleado
        {
            Codigo = $"E-{Guid.NewGuid():N}", NombreCompleto = "Test", Cedula = $"{Guid.NewGuid():N}",
            Cargo = "Analista", Correo = "t@t.com", Telefono = "8090000000",
            DepartamentoId = 0, Activo = true
        };
        var depto = new Departamento { Nombre = "Test", Activo = true };
        var vehiculo = new Vehiculo
        {
            Placa = $"P{Guid.NewGuid():N}"[..7], Ficha = $"F{Guid.NewGuid():N}"[..5],
            Marca = "Toyota", Modelo = "Hilux", Año = 2022, Tipo = "Pickup",
            CapacidadTanque = 70m, Odometro = 0m, DepartamentoId = 0, Activo = true
        };
        _db.Departamentos.Add(depto);
        await _db.SaveChangesAsync();
        empleado.DepartamentoId = depto.Id;
        vehiculo.DepartamentoId = depto.Id;
        _db.Empleados.Add(empleado);
        _db.Vehiculos.Add(vehiculo);
        await _db.SaveChangesAsync();

        var estacion = new Estacion { Nombre = "Est-1", Activo = true };
        _db.Estaciones.Add(estacion);
        await _db.SaveChangesAsync();

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(), NumeroSecuencial = 1, Prefijo = "COM",
            FechaCreacion = DateTime.UtcNow, FechaVencimiento = DateTime.UtcNow.AddHours(8),
            Estado = EstadoTicket.Consumido, CantidadAutorizada = galones,
            HashSeguridad = "x", TokenValidacion = "x", FirmaDigital = "x", QrCodePng = [],
            TipoCombustibleId = tipo.Id, EmpleadoId = empleado.Id,
            VehiculoId = vehiculo.Id, DepartamentoId = depto.Id
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var inv = await _db.Inventarios.FirstAsync(i => i.TanqueId == _tanqueId);
        var despacho = new Despacho
        {
            Fecha = fecha ?? _hoy, Hora = TimeOnly.FromDateTime(DateTime.UtcNow),
            GalonesServidos = galones, TanqueId = _tanqueId, TicketId = ticket.Id,
            OperadorId = _actorId, EstacionId = estacion.Id,
            InventarioRestante = inv.ExistenciaActual - galones,
            DisponibilidadRestante = inv.Disponibilidad - galones
        };
        _db.Despachos.Add(despacho);
        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Salida, Volumen = -galones, TanqueId = _tanqueId,
            UsuarioId = _actorId, FechaHora = DateTime.UtcNow,
            ReferenciaOperacion = $"DESPACHO-TEST/TICKET-{ticket.Id:D}"
        });
        inv.ExistenciaActual -= galones;
        inv.Disponibilidad -= galones;
        await _db.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Generar_FechaFutura_Lanza400()
    {
        var manana = _hoy.AddDays(1);
        var ex = await Assert.ThrowsExactlyAsync<TicketDomainException>(
            () => _service.GenerarAsync(manana, _actorId, null, CancellationToken.None));
        Assert.AreEqual(400, ex.StatusCode);
        Assert.AreEqual("FECHA_FUTURA", ex.Code);
    }

    [TestMethod]
    public async Task Generar_SinDespachos_Lanza400()
    {
        var ex = await Assert.ThrowsExactlyAsync<TicketDomainException>(
            () => _service.GenerarAsync(_hoy, _actorId, null, CancellationToken.None));
        Assert.AreEqual(400, ex.StatusCode);
        Assert.AreEqual("SIN_DESPACHOS", ex.Code);
    }

    [TestMethod]
    public async Task Generar_ConDespachos_CreaEncabezadoYDetalle()
    {
        await AgregarDespacho(100m);
        var result = await _service.GenerarAsync(_hoy, _actorId, null, CancellationToken.None);

        Assert.AreEqual(_hoy, result.Fecha);
        Assert.AreEqual(1, result.TotalDespachos);
        Assert.AreEqual(100m, result.TotalVolumenDespachado);
        Assert.AreEqual(1, result.Detalles.Count);
        Assert.AreEqual(_tanqueId, result.Detalles[0].TanqueId);
        Assert.AreEqual(100m, result.Detalles[0].VolumenDespachado);
        Assert.IsTrue(result.PdfDisponible);
    }

    [TestMethod]
    public async Task Generar_InventarioInicialCorrecto()
    {
        // ExistenciaActual empieza en 1000, despachamos 100 → queda 900
        // InventarioInicial debe ser 1000
        await AgregarDespacho(100m);
        var result = await _service.GenerarAsync(_hoy, _actorId, null, CancellationToken.None);
        var detalle = result.Detalles[0];
        Assert.AreEqual(1000m, detalle.InventarioInicial);
        Assert.AreEqual(900m, detalle.InventarioFinal);
        Assert.AreEqual(0m, detalle.Diferencias);
    }

    [TestMethod]
    public async Task Generar_FechaRepetida_Lanza409()
    {
        await AgregarDespacho(50m);
        await _service.GenerarAsync(_hoy, _actorId, null, CancellationToken.None);

        var ex = await Assert.ThrowsExactlyAsync<TicketDomainException>(
            () => _service.GenerarAsync(_hoy, _actorId, null, CancellationToken.None));
        Assert.AreEqual(409, ex.StatusCode);
        Assert.AreEqual("CIERRE_YA_EXISTE", ex.Code);
    }

    [TestMethod]
    public async Task GetAll_DevuelveCierresOrdenados()
    {
        await AgregarDespacho(50m);
        await _service.GenerarAsync(_hoy, _actorId, null, CancellationToken.None);
        var lista = await _service.GetAllAsync(1, 20, CancellationToken.None);
        Assert.AreEqual(1, lista.Count);
        Assert.AreEqual(_hoy, lista[0].Fecha);
    }

    [TestMethod]
    public async Task GetById_RetornaNull_CuandoNoExiste()
    {
        var result = await _service.GetByIdAsync(999, CancellationToken.None);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetPdf_RetornaBytes_CuandoExiste()
    {
        await AgregarDespacho(50m);
        var cierre = await _service.GenerarAsync(_hoy, _actorId, null, CancellationToken.None);
        var pdf = await _service.GetPdfAsync(cierre.Id, CancellationToken.None);
        Assert.IsNotNull(pdf);
        Assert.IsTrue(pdf.Length > 0);
    }
}
