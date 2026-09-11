using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Vehiculos;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class VehiculosControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private VehiculosController _controller = null!;

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
        _controller = new VehiculosController(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_db is not null) await _db.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }

    private async Task<Departamento> CrearDepartamentoAsync(string nombre = "Logística")
    {
        var dep = new Departamento { Nombre = nombre, Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();
        return dep;
    }

    private async Task<(Empleado empleado, Vehiculo vehiculo, TipoCombustible tipo)>
        CrearDependenciasAsync(Departamento dep)
    {
        var tipo = new TipoCombustible { Nombre = "Diesel", Activo = true };
        _db.TiposCombustible.Add(tipo);

        var empleado = new Empleado
        {
            Codigo = "EMP-V01", NombreCompleto = "Rosa Marte", Cedula = "002-0000001-1",
            Cargo = "Chofer", Correo = "rosa@test.com", Telefono = "809-100-0001",
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Empleados.Add(empleado);

        var vehiculo = new Vehiculo
        {
            Placa = "B100001", Ficha = "FV01", Marca = "Ford", Modelo = "Ranger",
            Año = 2021, Tipo = "Camioneta", CapacidadTanque = 80, Odometro = 0,
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Vehiculos.Add(vehiculo);
        await _db.SaveChangesAsync();
        return (empleado, vehiculo, tipo);
    }

    // ── GetAll ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_CuandoNoHayDatos()
    {
        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<VehiculoDto>;
        Assert.AreEqual(0, list!.Count);
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
        var dep = await CrearDepartamentoAsync();
        var veh = new Vehiculo
        {
            Placa = "C200001", Ficha = "FV02", Marca = "Nissan", Modelo = "Frontier",
            Año = 2023, Tipo = "Camioneta", CapacidadTanque = 70, Odometro = 0,
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Vehiculos.Add(veh);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(veh.Id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as VehiculoDto;
        Assert.AreEqual("C200001", dto!.Placa);
        Assert.AreEqual("Nissan", dto.Marca);
    }

    // ── Create ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Create_Returns201_ConDto()
    {
        var dep = await CrearDepartamentoAsync();
        var req = new SaveVehiculoRequest("D300001", "FV03", "Toyota", "Hilux",
            2022, "Camioneta", dep.Id, 60m);

        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as VehiculoDto;
        Assert.AreEqual("D300001", dto!.Placa);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoDepartamentoNoExiste()
    {
        var req = new SaveVehiculoRequest("E400001", "FV04", "Honda", "CRV",
            2020, "SUV", 999, 55m);
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Create_Returns409_CuandoPlacaDuplicada()
    {
        var dep = await CrearDepartamentoAsync();
        _db.Vehiculos.Add(new Vehiculo
        {
            Placa = "F500001", Ficha = "FV05", Marca = "KIA", Modelo = "Sportage",
            Año = 2021, Tipo = "SUV", CapacidadTanque = 55, Odometro = 0,
            Activo = true, DepartamentoId = dep.Id
        });
        await _db.SaveChangesAsync();

        var req = new SaveVehiculoRequest("F500001", "FV06", "KIA", "Picanto",
            2022, "Sedan", dep.Id, 40m);
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    // ── Deactivate ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Deactivate_Returns404_CuandoNoExiste()
    {
        var result = await _controller.Deactivate(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Deactivate_Returns204_SinSolicitudesActivas()
    {
        var dep = await CrearDepartamentoAsync();
        var veh = new Vehiculo
        {
            Placa = "G600001", Ficha = "FV07", Marca = "Hyundai", Modelo = "Tucson",
            Año = 2020, Tipo = "SUV", CapacidadTanque = 58, Odometro = 0,
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Vehiculos.Add(veh);
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(veh.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);

        await _db.Entry(veh).ReloadAsync();
        Assert.IsFalse(veh.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoTieneSolicitudPendiente()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, veh, tipo) = await CrearDependenciasAsync(dep);

        _db.SolicitudesCombustible.Add(new SolicitudCombustible
        {
            CantidadSolicitada = 40m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente, FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = emp.Id, VehiculoId = veh.Id,
            DepartamentoId = dep.Id, TipoCombustibleId = tipo.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(veh.Id, CancellationToken.None);
        var conflict = result as ConflictObjectResult;
        Assert.IsNotNull(conflict, "Esperaba 409 Conflict");

        var code = conflict.Value!.GetType().GetProperty("code")?.GetValue(conflict.Value)?.ToString();
        Assert.AreEqual("VEHICULO_CON_SOLICITUDES_ACTIVAS", code);

        await _db.Entry(veh).ReloadAsync();
        Assert.IsTrue(veh.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoTieneSolicitudAprobada()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, veh, tipo) = await CrearDependenciasAsync(dep);

        _db.SolicitudesCombustible.Add(new SolicitudCombustible
        {
            CantidadSolicitada = 50m, CantidadAutorizada = 50m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Aprobada, FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = emp.Id, VehiculoId = veh.Id,
            DepartamentoId = dep.Id, TipoCombustibleId = tipo.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(veh.Id, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result);
    }

    [TestMethod]
    public async Task Deactivate_Returns204_CuandoSoloTieneSolicitudRechazada()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, veh, tipo) = await CrearDependenciasAsync(dep);

        _db.SolicitudesCombustible.Add(new SolicitudCombustible
        {
            CantidadSolicitada = 40m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Rechazada, MotivoRechazo = "Cupo excedido",
            FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = emp.Id, VehiculoId = veh.Id,
            DepartamentoId = dep.Id, TipoCombustibleId = tipo.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(veh.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);
    }
}
