using System.Security.Claims;
using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Inventario;
using FuelTrack.Api.DTOs.Recepciones;
using FuelTrack.Api.DTOs.Solicitudes;
using FuelTrack.Api.DTOs.Tanques;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class BusinessIntegrityValidationTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private AuditService _audit = null!;
    private int _usuarioId;

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

        var usuario = new Usuario { NombreUsuario = "test.admin", PasswordHash = "hash", Activo = true };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        _usuarioId = usuario.Id;
        _audit = new AuditService(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private ControllerContext CreateControllerContext() => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, _usuarioId.ToString()),
                new Claim(ClaimTypes.Role, Roles.Administrador)
            ], "Test"))
        }
    };

    // 1. TanquesController: Crear con tipo de combustible inactivo debe retornar Conflict 409
    [TestMethod]
    public async Task Tanques_Create_TipoCombustibleInactivo_RetornaConflict()
    {
        var tipoInactivo = new TipoCombustible { Nombre = "Gasolina Inactiva", Activo = false };
        _db.TiposCombustible.Add(tipoInactivo);
        await _db.SaveChangesAsync();

        var controller = new TanquesController(_db, _audit) { ControllerContext = CreateControllerContext() };
        var req = new SaveTanqueRequest("TNQ-TEST-01", 1000m, 100m, tipoInactivo.Id, true);

        var result = await controller.Create(req, CancellationToken.None);

        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
        var conflict = (ConflictObjectResult)result.Result!;
        Assert.AreEqual(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    // 2. SolicitudesRecurrentesController: Crear con tipo de combustible inactivo debe retornar BadRequest 400
    [TestMethod]
    public async Task SolicitudesRecurrentes_Create_TipoCombustibleInactivo_RetornaBadRequest()
    {
        var depto = new Departamento { Nombre = "Operaciones", Activo = true };
        _db.Departamentos.Add(depto);
        await _db.SaveChangesAsync();

        var emp = new Empleado
        {
            Codigo = "EMP-01",
            NombreCompleto = "Juan Perez",
            Cedula = "40200000001",
            Cargo = "Chofer",
            DepartamentoId = depto.Id,
            Activo = true
        };
        var veh = new Vehiculo
        {
            Placa = "G123456",
            Ficha = "F-01",
            Marca = "Toyota",
            Modelo = "Hilux",
            Año = 2020,
            Tipo = "Camioneta",
            CapacidadTanque = 20m,
            DepartamentoId = depto.Id,
            Activo = true
        };
        var tipoInactivo = new TipoCombustible { Nombre = "Diesel Inactivo", Activo = false };

        _db.Empleados.Add(emp);
        _db.Vehiculos.Add(veh);
        _db.TiposCombustible.Add(tipoInactivo);
        await _db.SaveChangesAsync();

        var controller = new SolicitudesRecurrentesController(_db);
        var req = new CreateSolicitudRecurrenteRequest(
            10m,
            Periodicidad.Semanal,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            emp.Id,
            veh.Id,
            depto.Id,
            tipoInactivo.Id
        );

        var result = await controller.Create(req, CancellationToken.None);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
    }

    // 3. SolicitudesController: Crear con departamento diferente al del empleado debe retornar BadRequest 400
    [TestMethod]
    public async Task Solicitudes_Create_DepartamentoNoCoincideConEmpleado_RetornaBadRequest()
    {
        var deptoEmpleado = new Departamento { Nombre = "Finanzas", Activo = true };
        var deptoOtro = new Departamento { Nombre = "Logística", Activo = true };
        var tipo = new TipoCombustible { Nombre = "Gasolina Regular", Activo = true };
        _db.Departamentos.AddRange(deptoEmpleado, deptoOtro);
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var emp = new Empleado
        {
            Codigo = "EMP-02",
            NombreCompleto = "Ana Lopez",
            Cedula = "40200000002",
            Cargo = "Contadora",
            DepartamentoId = deptoEmpleado.Id,
            Activo = true
        };
        var veh = new Vehiculo
        {
            Placa = "A654321",
            Ficha = "F-02",
            Marca = "Nissan",
            Modelo = "Frontier",
            Año = 2021,
            Tipo = "Camioneta",
            CapacidadTanque = 25m,
            DepartamentoId = deptoEmpleado.Id,
            Activo = true
        };
        _db.Empleados.Add(emp);
        _db.Vehiculos.Add(veh);
        await _db.SaveChangesAsync();

        var controller = new SolicitudesController(_db, _audit) { ControllerContext = CreateControllerContext() };
        var req = new CreateSolicitudRequest(
            15m,
            emp.Id,
            veh.Id,
            deptoOtro.Id, // Departamento incorrecto (no coincide con el empleado)
            tipo.Id,
            DateTime.UtcNow.AddDays(7)
        );

        var result = await controller.Create(req, CancellationToken.None);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
    }

    // 4. RecepcionesController: Crear recepción en tanque cuyo tipo de combustible esté inactivo retorna Conflict 409
    [TestMethod]
    public async Task Recepciones_Create_TanqueConTipoCombustibleInactivo_RetornaConflict()
    {
        var prov = new Proveedor { Nombre = "Refidomsa", Rnc = "101000001", Activo = true };
        var tipoInactivo = new TipoCombustible { Nombre = "Fuel Oil Inactivo", Activo = false };
        _db.Proveedores.Add(prov);
        _db.TiposCombustible.Add(tipoInactivo);
        await _db.SaveChangesAsync();

        var tanque = new Tanque
        {
            Identificacion = "TNQ-INACT-01",
            Capacidad = 5000m,
            NivelCritico = 500m,
            TipoCombustibleId = tipoInactivo.Id,
            Activo = true
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        var inv = new Inventario { TanqueId = tanque.Id, ExistenciaActual = 1000m, UltimaActualizacion = DateTime.UtcNow };
        _db.Inventarios.Add(inv);
        await _db.SaveChangesAsync();

        var controller = new RecepcionesController(_db, _audit) { ControllerContext = CreateControllerContext() };
        var req = new CreateRecepcionRequest(
            prov.Id,
            tanque.Id,
            "FACT-009988",
            500m,
            DateTime.UtcNow
        );

        var result = await controller.Create(req, CancellationToken.None);

        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    // 5. InventarioController: Ajustar en tanque con tipo de combustible inactivo retorna Conflict 409
    [TestMethod]
    public async Task Inventario_Ajustar_TanqueConTipoCombustibleInactivo_RetornaConflict()
    {
        var tipoInactivo = new TipoCombustible { Nombre = "AvGas Inactivo", Activo = false };
        _db.TiposCombustible.Add(tipoInactivo);
        await _db.SaveChangesAsync();

        var tanque = new Tanque
        {
            Identificacion = "TNQ-AVGAS-01",
            Capacidad = 3000m,
            NivelCritico = 300m,
            TipoCombustibleId = tipoInactivo.Id,
            Activo = true
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        var inv = new Inventario { TanqueId = tanque.Id, ExistenciaActual = 1500m, UltimaActualizacion = DateTime.UtcNow };
        _db.Inventarios.Add(inv);
        await _db.SaveChangesAsync();

        var controller = new InventarioController(_db, _audit) { ControllerContext = CreateControllerContext() };
        var req = new AjustarInventarioRequest(tanque.Id, 100m, "Ajuste por calibración de sonda");

        var result = await controller.Ajustar(req, CancellationToken.None);

        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    private async Task<Tanque> AddTankAsync(string name, bool activeFuel)
    {
        var tipo = new TipoCombustible { Nombre = name, Activo = activeFuel };
        var tank = new Tanque { Identificacion = name, Capacidad = 100m, NivelCritico = 10m,
            TipoCombustible = tipo, Activo = true };
        _db.Tanques.Add(tank);
        _db.Inventarios.Add(new Inventario { Tanque = tank, ExistenciaActual = 50m,
            Disponibilidad = 50m, UltimaActualizacion = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        return tank;
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Transferir_CombustibleInactivo_NoModificaSaldosNiAuditoria(bool sourceInactive)
    {
        var source = await AddTankAsync("Origen", !sourceInactive);
        var target = await AddTankAsync("Destino", sourceInactive);
        var controller = new InventarioController(_db, _audit) { ControllerContext = CreateControllerContext() };
        var response = await controller.Transferir(new TransferirRequest(source.Id, target.Id, 5m, "Validación"), default);
        Assert.IsInstanceOfType<ConflictObjectResult>(response.Result);
        var expectedCode = sourceInactive ? "TIPO_COMBUSTIBLE_ORIGEN_INACTIVO" : "TIPO_COMBUSTIBLE_DESTINO_INACTIVO";
        StringAssert.Contains(((ConflictObjectResult)response.Result!).Value!.ToString(), expectedCode);
        _db.ChangeTracker.Clear();
        Assert.IsTrue(await _db.Inventarios.AllAsync(i => i.ExistenciaActual == 50m && i.Disponibilidad == 50m));
        Assert.AreEqual(0, await _db.MovimientosInventario.CountAsync());
        Assert.AreEqual(0, await _db.Auditorias.CountAsync());
    }

    [TestMethod]
    public async Task Tanques_ListaYDetalle_ExponenCombustibleInactivo()
    {
        var tank = await AddTankAsync("Histórico", false);
        var controller = new TanquesController(_db, _audit) { ControllerContext = CreateControllerContext() };
        var list = (OkObjectResult)(await controller.GetAll(default)).Result!;
        Assert.IsFalse(((List<TanqueDto>)list.Value!).Single().TipoCombustibleActivo);
        var detail = (OkObjectResult)(await controller.GetById(tank.Id, default)).Result!;
        Assert.IsFalse(((TanqueDto)detail.Value!).TipoCombustibleActivo);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public async Task Solicitudes_DepartamentoDerivado_ConservaCoherenciaDelVehiculo(bool recurrent, bool incompatibleVehicle)
    {
        var department = new Departamento { Nombre = "Empleado", Activo = true };
        var vehicleDepartment = incompatibleVehicle ? new Departamento { Nombre = "Otro", Activo = true } : department;
        var employee = new Empleado { Codigo = "DER-1", NombreCompleto = "Prueba", Cedula = "40200000009",
            Cargo = "QA", Departamento = department, Activo = true };
        var vehicle = new Vehiculo { Placa = "DER1234", Ficha = "DER1", Marca = "QA", Modelo = "QA",
            Año = 2026, Tipo = "QA", CapacidadTanque = 20m, Departamento = vehicleDepartment, Activo = true };
        var fuel = new TipoCombustible { Nombre = "Combustible", Activo = true };
        _db.Empleados.Add(employee); _db.Vehiculos.Add(vehicle); _db.TiposCombustible.Add(fuel);
        await _db.SaveChangesAsync();
        if (recurrent)
        {
            var controller = new SolicitudesRecurrentesController(_db) { ControllerContext = CreateControllerContext() };
            var result = await controller.Create(new CreateSolicitudRecurrenteRequest(5m, Periodicidad.Semanal,
                DateOnly.FromDateTime(DateTime.UtcNow), null, employee.Id, vehicle.Id, 0, fuel.Id), default);
            if (incompatibleVehicle)
            {
                Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
                Assert.AreEqual(0, await _db.SolicitudesRecurrentes.CountAsync());
            }
            else
            {
                Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
                Assert.AreEqual(department.Id, (await _db.SolicitudesRecurrentes.SingleAsync()).DepartamentoId);
            }
        }
        else
        {
            var controller = new SolicitudesController(_db, _audit) { ControllerContext = CreateControllerContext() };
            var result = await controller.Create(new CreateSolicitudRequest(5m, employee.Id, vehicle.Id, 0,
                fuel.Id, DateTime.UtcNow.AddDays(7)), default);
            if (incompatibleVehicle)
            {
                Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
                Assert.AreEqual(0, await _db.SolicitudesCombustible.CountAsync());
                Assert.AreEqual(0, await _db.Auditorias.CountAsync());
            }
            else
            {
                Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
                Assert.AreEqual(department.Id, (await _db.SolicitudesCombustible.SingleAsync()).DepartamentoId);
                Assert.AreEqual(1, await _db.Auditorias.CountAsync());
            }
        }
    }
}
