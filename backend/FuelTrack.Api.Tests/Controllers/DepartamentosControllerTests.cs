using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Departamentos;
using FuelTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class DepartamentosControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private DepartamentosController _controller = null!;

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
        _controller = new DepartamentosController(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_db is not null) await _db.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }

    // ── GetAll ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_CuandoNoHayDatos()
    {
        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<DepartamentoDto>;
        Assert.AreEqual(0, list!.Count);
    }

    [TestMethod]
    public async Task GetAll_ReturnsDepartamentos_CuandoExisten()
    {
        _db.Departamentos.AddRange(
            new Departamento { Nombre = "TI", Activo = true },
            new Departamento { Nombre = "Logística", Activo = false });
        await _db.SaveChangesAsync();

        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<DepartamentoDto>;
        Assert.AreEqual(2, list!.Count);
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
        var dep = new Departamento { Nombre = "Operaciones", Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(dep.Id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as DepartamentoDto;
        Assert.AreEqual("Operaciones", dto!.Nombre);
        Assert.IsTrue(dto.Activo);
    }

    // ── Create ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Create_Returns201_ConDto()
    {
        var req = new SaveDepartamentoRequest("Finanzas");
        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as DepartamentoDto;
        Assert.AreEqual("Finanzas", dto!.Nombre);
        Assert.IsTrue(dto.Activo);
    }

    // ── Update ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Update_Returns404_CuandoNoExiste()
    {
        var req = new SaveDepartamentoRequest("RRHH");
        var result = await _controller.Update(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Update_Returns200_ConDtoActualizado()
    {
        var dep = new Departamento { Nombre = "Ventas", Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();

        var req = new SaveDepartamentoRequest("Comercial", false);
        var result = await _controller.Update(dep.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as DepartamentoDto;
        Assert.AreEqual("Comercial", dto!.Nombre);
        Assert.IsFalse(dto.Activo);
    }

    // ── Deactivate ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Deactivate_Returns404_CuandoNoExiste()
    {
        var result = await _controller.Deactivate(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Deactivate_Returns204_CuandoNoHayDependientesActivos()
    {
        var dep = new Departamento { Nombre = "Archivo", Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(dep.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);

        await _db.Entry(dep).ReloadAsync();
        Assert.IsFalse(dep.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns204_CuandoDependientesInactivos()
    {
        // empleado y vehículo inactivos no deben bloquear la desactivación
        var dep = new Departamento { Nombre = "Obsoleto", Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();

        _db.Empleados.Add(new Empleado
        {
            Codigo = "EMP-001", NombreCompleto = "Juan Pérez", Cedula = "001-0000001-1",
            Cargo = "Analista", Correo = "juan@test.com", Telefono = "809-000-0000",
            Activo = false, DepartamentoId = dep.Id
        });
        _db.Vehiculos.Add(new Vehiculo
        {
            Placa = "A000001", Ficha = "F001", Marca = "Toyota", Modelo = "Hilux",
            Año = 2020, Tipo = "Camioneta", CapacidadTanque = 60, Odometro = 0,
            Activo = false, DepartamentoId = dep.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(dep.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoHayEmpleadosActivos()
    {
        var dep = new Departamento { Nombre = "TI", Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();

        _db.Empleados.Add(new Empleado
        {
            Codigo = "EMP-002", NombreCompleto = "María López", Cedula = "001-0000002-2",
            Cargo = "Desarrolladora", Correo = "maria@test.com", Telefono = "809-000-0001",
            Activo = true, DepartamentoId = dep.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(dep.Id, CancellationToken.None);
        var conflict = result as ConflictObjectResult;
        Assert.IsNotNull(conflict, "Esperaba 409 Conflict");

        var body = conflict.Value!;
        var code = body.GetType().GetProperty("code")?.GetValue(body)?.ToString();
        Assert.AreEqual("DEPARTAMENTO_CON_EMPLEADOS_ACTIVOS", code);

        // el departamento NO debe haberse desactivado
        await _db.Entry(dep).ReloadAsync();
        Assert.IsTrue(dep.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoHayVehiculosActivos()
    {
        var dep = new Departamento { Nombre = "Logística", Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();

        _db.Vehiculos.Add(new Vehiculo
        {
            Placa = "B000002", Ficha = "F002", Marca = "Ford", Modelo = "Ranger",
            Año = 2022, Tipo = "Camioneta", CapacidadTanque = 80, Odometro = 0,
            Activo = true, DepartamentoId = dep.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(dep.Id, CancellationToken.None);
        var conflict = result as ConflictObjectResult;
        Assert.IsNotNull(conflict, "Esperaba 409 Conflict");

        var body = conflict.Value!;
        var code = body.GetType().GetProperty("code")?.GetValue(body)?.ToString();
        Assert.AreEqual("DEPARTAMENTO_CON_VEHICULOS_ACTIVOS", code);

        await _db.Entry(dep).ReloadAsync();
        Assert.IsTrue(dep.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoHayAmbosActivos()
    {
        // cuando hay tanto empleados como vehículos activos, el empleado tiene prioridad
        var dep = new Departamento { Nombre = "Operaciones", Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();

        _db.Empleados.Add(new Empleado
        {
            Codigo = "EMP-003", NombreCompleto = "Carlos Ruiz", Cedula = "001-0000003-3",
            Cargo = "Operador", Correo = "carlos@test.com", Telefono = "809-000-0002",
            Activo = true, DepartamentoId = dep.Id
        });
        _db.Vehiculos.Add(new Vehiculo
        {
            Placa = "C000003", Ficha = "F003", Marca = "Nissan", Modelo = "Frontier",
            Año = 2021, Tipo = "Camioneta", CapacidadTanque = 70, Odometro = 0,
            Activo = true, DepartamentoId = dep.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(dep.Id, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result);
    }
}
