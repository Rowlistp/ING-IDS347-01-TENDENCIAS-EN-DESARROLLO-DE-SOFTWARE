using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Solicitudes;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class SolicitudesRecurrentesControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private SolicitudesRecurrentesController _controller = null!;

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
        _controller = new SolicitudesRecurrentesController(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<(int empleadoId, int vehiculoId, int departamentoId, int tipoCombustibleId)>
        CrearDependenciasAsync()
    {
        var depto = new Departamento { Nombre = "TI", Activo = true };
        _db.Departamentos.Add(depto);
        await _db.SaveChangesAsync();

        var tipo = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        var empleado = new Empleado
        {
            Codigo = "E-001", NombreCompleto = "Juan Pérez", Cedula = "001-0000001-1",
            Cargo = "Analista", Correo = "juan@test.com", Telefono = "8091234567",
            DepartamentoId = depto.Id, Activo = true
        };
        var vehiculo = new Vehiculo
        {
            Placa = "A123456", Ficha = "F-001", Marca = "Toyota", Modelo = "Hilux",
            Año = 2022, Tipo = "Pickup", CapacidadTanque = 70m, Odometro = 0m,
            DepartamentoId = depto.Id, Activo = true
        };
        _db.TiposCombustible.Add(tipo);
        _db.Empleados.Add(empleado);
        _db.Vehiculos.Add(vehiculo);
        await _db.SaveChangesAsync();
        return (empleado.Id, vehiculo.Id, depto.Id, tipo.Id);
    }

    private CreateSolicitudRecurrenteRequest BuildRequest(int eId, int vId, int dId, int tId,
        Periodicidad p = Periodicidad.Semanal) =>
        new(50m, p, DateOnly.FromDateTime(DateTime.UtcNow), null, eId, vId, dId, tId);

    // ── CRUD ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_WhenNoData()
    {
        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(0, ((List<SolicitudRecurrenteDto>)ok.Value!).Count);
    }

    [TestMethod]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _controller.GetById(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Create_Returns201_ConDtoCompleto()
    {
        var (eId, vId, dId, tId) = await CrearDependenciasAsync();
        var req = BuildRequest(eId, vId, dId, tId, Periodicidad.Mensual);

        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;

        Assert.IsNotNull(created);
        var dto = (SolicitudRecurrenteDto)created.Value!;
        Assert.AreEqual(50m, dto.CantidadSolicitada);
        Assert.AreEqual(Periodicidad.Mensual, dto.Periodicidad);
        Assert.IsTrue(dto.Activa);
        Assert.IsNull(dto.UltimaEjecucion);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoEmpleadoNoExiste()
    {
        var (_, vId, dId, tId) = await CrearDependenciasAsync();
        var req = BuildRequest(999, vId, dId, tId);
        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(bad);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoFechaFinAnteriorAInicio()
    {
        var (eId, vId, dId, tId) = await CrearDependenciasAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var req = new CreateSolicitudRecurrenteRequest(50m, Periodicidad.Diaria, hoy, hoy.AddDays(-1), eId, vId, dId, tId);
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Desactivar_Returns200_CuandoActiva()
    {
        var (eId, vId, dId, tId) = await CrearDependenciasAsync();
        var created = (await _controller.Create(BuildRequest(eId, vId, dId, tId), CancellationToken.None))
            .Result as CreatedAtActionResult;
        var id = ((SolicitudRecurrenteDto)created!.Value!).Id;

        var result = await _controller.Desactivar(id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.IsFalse(((SolicitudRecurrenteDto)ok.Value!).Activa);
    }

    [TestMethod]
    public async Task Desactivar_Returns409_CuandoYaDesactivada()
    {
        var (eId, vId, dId, tId) = await CrearDependenciasAsync();
        var created = (await _controller.Create(BuildRequest(eId, vId, dId, tId), CancellationToken.None))
            .Result as CreatedAtActionResult;
        var id = ((SolicitudRecurrenteDto)created!.Value!).Id;

        await _controller.Desactivar(id, CancellationToken.None);
        var result = await _controller.Desactivar(id, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Activar_Returns200_CuandoDesactivada()
    {
        var (eId, vId, dId, tId) = await CrearDependenciasAsync();
        var created = (await _controller.Create(BuildRequest(eId, vId, dId, tId), CancellationToken.None))
            .Result as CreatedAtActionResult;
        var id = ((SolicitudRecurrenteDto)created!.Value!).Id;

        await _controller.Desactivar(id, CancellationToken.None);
        var result = await _controller.Activar(id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.IsTrue(((SolicitudRecurrenteDto)ok.Value!).Activa);
    }

    // ── Background service ────────────────────────────────────────────────

    [TestMethod]
    public async Task ProcesarPlantillas_GeneraSolicitud_CuandoFechaInicioLlegó()
    {
        var (eId, vId, dId, tId) = await CrearDependenciasAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        _db.SolicitudesRecurrentes.Add(new SolicitudRecurrente
        {
            CantidadSolicitada = 30m, Periodicidad = Periodicidad.Diaria,
            FechaInicio = hoy, Activa = true,
            EmpleadoId = eId, VehiculoId = vId, DepartamentoId = dId, TipoCombustibleId = tId
        });
        await _db.SaveChangesAsync();

        await EjecutarServicioAsync();

        Assert.AreEqual(1, await _db.SolicitudesCombustible.CountAsync());
        var sol = await _db.SolicitudesCombustible.FirstAsync();
        Assert.AreEqual("Automatica", sol.TipoSolicitud);
        Assert.AreEqual(30m, sol.CantidadSolicitada);

        var plantilla = await _db.SolicitudesRecurrentes.AsNoTracking().FirstAsync();
        Assert.AreEqual(hoy, plantilla.UltimaEjecucion);
    }

    [TestMethod]
    public async Task ProcesarPlantillas_NoGenera_CuandoFechaInicioEsFutura()
    {
        var (eId, vId, dId, tId) = await CrearDependenciasAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        _db.SolicitudesRecurrentes.Add(new SolicitudRecurrente
        {
            CantidadSolicitada = 30m, Periodicidad = Periodicidad.Diaria,
            FechaInicio = hoy.AddDays(5), Activa = true,
            EmpleadoId = eId, VehiculoId = vId, DepartamentoId = dId, TipoCombustibleId = tId
        });
        await _db.SaveChangesAsync();

        await EjecutarServicioAsync();

        Assert.AreEqual(0, await _db.SolicitudesCombustible.CountAsync());
    }

    [TestMethod]
    public async Task ProcesarPlantillas_NoGenera_CuandoPeriodicidadNoAlcanzada()
    {
        var (eId, vId, dId, tId) = await CrearDependenciasAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        _db.SolicitudesRecurrentes.Add(new SolicitudRecurrente
        {
            CantidadSolicitada = 30m, Periodicidad = Periodicidad.Semanal,
            FechaInicio = hoy.AddDays(-3), UltimaEjecucion = hoy.AddDays(-2),
            Activa = true,
            EmpleadoId = eId, VehiculoId = vId, DepartamentoId = dId, TipoCombustibleId = tId
        });
        await _db.SaveChangesAsync();

        await EjecutarServicioAsync();

        Assert.AreEqual(0, await _db.SolicitudesCombustible.CountAsync());
    }

    [TestMethod]
    public async Task ProcesarPlantillas_NoGenera_CuandoDesactivada()
    {
        var (eId, vId, dId, tId) = await CrearDependenciasAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        _db.SolicitudesRecurrentes.Add(new SolicitudRecurrente
        {
            CantidadSolicitada = 30m, Periodicidad = Periodicidad.Diaria,
            FechaInicio = hoy.AddDays(-10), Activa = false,
            EmpleadoId = eId, VehiculoId = vId, DepartamentoId = dId, TipoCombustibleId = tId
        });
        await _db.SaveChangesAsync();

        await EjecutarServicioAsync();

        Assert.AreEqual(0, await _db.SolicitudesCombustible.CountAsync());
    }

    [TestMethod]
    public async Task ProcesarPlantillas_GeneraSemanal_CuandoHanPasado7Dias()
    {
        var (eId, vId, dId, tId) = await CrearDependenciasAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        _db.SolicitudesRecurrentes.Add(new SolicitudRecurrente
        {
            CantidadSolicitada = 40m, Periodicidad = Periodicidad.Semanal,
            FechaInicio = hoy.AddDays(-14), UltimaEjecucion = hoy.AddDays(-7),
            Activa = true,
            EmpleadoId = eId, VehiculoId = vId, DepartamentoId = dId, TipoCombustibleId = tId
        });
        await _db.SaveChangesAsync();

        await EjecutarServicioAsync();

        Assert.AreEqual(1, await _db.SolicitudesCombustible.CountAsync());
    }

    private async Task EjecutarServicioAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IServiceScopeFactory>();
        var service = new SolicitudRecurrenteService(factory, NullLogger<SolicitudRecurrenteService>.Instance);
        await service.ProcesarPlantillasAsync(CancellationToken.None);
    }
}
