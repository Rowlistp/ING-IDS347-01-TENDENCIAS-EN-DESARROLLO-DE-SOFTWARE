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

        var controller = new SolicitudesController(_db) { ControllerContext = CreateControllerContext() };
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
}
