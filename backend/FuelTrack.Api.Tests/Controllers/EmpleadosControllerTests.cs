using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Empleados;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class EmpleadosControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private EmpleadosController _controller = null!;

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
        _controller = new EmpleadosController(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_db is not null) await _db.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }

    private async Task<Departamento> CrearDepartamentoAsync(string nombre = "TI")
    {
        var dep = new Departamento { Nombre = nombre, Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();
        return dep;
    }

    private async Task<(Empleado empleado, Vehiculo vehiculo, TipoCombustible tipo)>
        CrearDependenciasAsync(Departamento dep)
    {
        var tipo = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        _db.TiposCombustible.Add(tipo);

        var empleado = new Empleado
        {
            Codigo = "EMP-001", NombreCompleto = "Ana Torres", Cedula = "001-0000001-1",
            Cargo = "Analista", Correo = "ana@test.com", Telefono = "809-000-0001",
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Empleados.Add(empleado);

        var vehiculo = new Vehiculo
        {
            Placa = "A000001", Ficha = "F001", Marca = "Toyota", Modelo = "Corolla",
            Año = 2022, Tipo = "Sedan", CapacidadTanque = 50, Odometro = 0,
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
        var list = ok!.Value as List<EmpleadoDto>;
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
        var emp = new Empleado
        {
            Codigo = "EMP-001", NombreCompleto = "Luis Pérez", Cedula = "001-0000002-2",
            Cargo = "Gerente", Correo = "luis@test.com", Telefono = "809-000-0002",
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Empleados.Add(emp);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(emp.Id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as EmpleadoDto;
        Assert.AreEqual("EMP-001", dto!.Codigo);
        Assert.AreEqual("Luis Pérez", dto.NombreCompleto);
    }

    // ── Create ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Create_Returns201_ConDto()
    {
        var dep = await CrearDepartamentoAsync();
        var req = new SaveEmpleadoRequest("EMP-001", "María González", "001-0000003-3",
            "Analista", "maria@test.com", "809-000-0003", dep.Id);

        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as EmpleadoDto;
        Assert.AreEqual("EMP-001", dto!.Codigo);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoDepartamentoNoExiste()
    {
        var req = new SaveEmpleadoRequest("EMP-001", "Nombre", "001-0000004-4",
            "Cargo", "email@test.com", "809-000-0004", 999);
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Create_Returns409_CuandoCodigoDuplicado()
    {
        var dep = await CrearDepartamentoAsync();
        _db.Empleados.Add(new Empleado
        {
            Codigo = "EMP-001", NombreCompleto = "Existe", Cedula = "001-0000005-5",
            Cargo = "Cargo", Correo = "e1@test.com", Telefono = "809-000-0005",
            Activo = true, DepartamentoId = dep.Id
        });
        await _db.SaveChangesAsync();

        var req = new SaveEmpleadoRequest("EMP-001", "Otro", "001-0000006-6",
            "Cargo", "e2@test.com", "809-000-0006", dep.Id);
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    // ── Update ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Update_Returns404_CuandoNoExiste()
    {
        var dep = await CrearDepartamentoAsync();
        var req = new SaveEmpleadoRequest("EMP-001", "Nombre", "001-0000007-7",
            "Cargo", "e@test.com", "809-000-0007", dep.Id);
        var result = await _controller.Update(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
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
        var emp = new Empleado
        {
            Codigo = "EMP-010", NombreCompleto = "Pedro Soto", Cedula = "001-0000010-0",
            Cargo = "Operador", Correo = "pedro@test.com", Telefono = "809-000-0010",
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Empleados.Add(emp);
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(emp.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);

        await _db.Entry(emp).ReloadAsync();
        Assert.IsFalse(emp.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoTieneSolicitudPendiente()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, veh, tipo) = await CrearDependenciasAsync(dep);

        _db.SolicitudesCombustible.Add(new SolicitudCombustible
        {
            CantidadSolicitada = 20m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente, FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = emp.Id, VehiculoId = veh.Id,
            DepartamentoId = dep.Id, TipoCombustibleId = tipo.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(emp.Id, CancellationToken.None);
        var conflict = result as ConflictObjectResult;
        Assert.IsNotNull(conflict, "Esperaba 409 Conflict");

        var code = conflict.Value!.GetType().GetProperty("code")?.GetValue(conflict.Value)?.ToString();
        Assert.AreEqual("EMPLEADO_CON_SOLICITUDES_ACTIVAS", code);

        await _db.Entry(emp).ReloadAsync();
        Assert.IsTrue(emp.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoTieneSolicitudAprobada()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, veh, tipo) = await CrearDependenciasAsync(dep);

        _db.SolicitudesCombustible.Add(new SolicitudCombustible
        {
            CantidadSolicitada = 30m, CantidadAutorizada = 30m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Aprobada, FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = emp.Id, VehiculoId = veh.Id,
            DepartamentoId = dep.Id, TipoCombustibleId = tipo.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(emp.Id, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result);
    }

    [TestMethod]
    public async Task Deactivate_Returns204_CuandoSoloTieneSolicitudRechazada()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, veh, tipo) = await CrearDependenciasAsync(dep);

        _db.SolicitudesCombustible.Add(new SolicitudCombustible
        {
            CantidadSolicitada = 20m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Rechazada, MotivoRechazo = "Sin cupo",
            FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = emp.Id, VehiculoId = veh.Id,
            DepartamentoId = dep.Id, TipoCombustibleId = tipo.Id
        });
        await _db.SaveChangesAsync();

        // solicitudes rechazadas no bloquean la desactivación
        var result = await _controller.Deactivate(emp.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);
    }
}
