using System.Security.Claims;
using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.TiposCombustible;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class TiposCombustibleControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private TiposCombustibleController _controller = null!;
    private int _usuarioActorId;

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
        var usuarioActor = new Usuario { NombreUsuario = "test.actor", PasswordHash = "hash", Activo = true };
        _db.Usuarios.Add(usuarioActor);
        await _db.SaveChangesAsync();
        _usuarioActorId = usuarioActor.Id;
        _controller = new TiposCombustibleController(_db, new AuditService(_db))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, usuarioActor.Id.ToString()), new Claim(ClaimTypes.Role, "Administrador")], "Test"))
                }
            }
        };
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_WhenNoData()
    {
        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var list = ok.Value as List<TipoCombustibleDto>;
        Assert.AreEqual(0, list!.Count);
    }

    [TestMethod]
    public async Task GetAll_ReturnsTodosLosTipos()
    {
        _db.TiposCombustible.AddRange(
            new TipoCombustible { Nombre = "Gasolina", Activo = true },
            new TipoCombustible { Nombre = "Diesel", Activo = true });
        await _db.SaveChangesAsync();

        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<TipoCombustibleDto>;
        Assert.AreEqual(2, list!.Count);
    }

    [TestMethod]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _controller.GetById(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task GetById_ReturnsDto_WhenExists()
    {
        var tipo = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(tipo.Id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as TipoCombustibleDto;
        Assert.AreEqual("Gasolina", dto!.Nombre);
        Assert.IsTrue(dto.Activo);
    }

    [TestMethod]
    public async Task Create_Returns201_ConDto()
    {
        var req = new SaveTipoCombustibleRequest("Gasolina");
        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as TipoCombustibleDto;
        Assert.AreEqual("Gasolina", dto!.Nombre);
        Assert.IsTrue(dto.Activo);
        Assert.IsTrue(dto.Id > 0);
    }

    [TestMethod]
    public async Task Create_RegistraAuditoria()
    {
        var req = new SaveTipoCombustibleRequest("Gasolina");
        await _controller.Create(req, CancellationToken.None);

        var auditoria = await _db.Auditorias.FirstOrDefaultAsync(a => a.Evento == "TIPO_COMBUSTIBLE_CREADO");
        Assert.IsNotNull(auditoria);
        Assert.AreEqual("TipoCombustible", auditoria.EntidadAfectada);
        Assert.AreEqual(_usuarioActorId, auditoria.UsuarioId);
    }

    [TestMethod]
    public async Task Create_Returns409_CuandoNombreDuplicado()
    {
        _db.TiposCombustible.Add(new TipoCombustible { Nombre = "Gasolina", Activo = true });
        await _db.SaveChangesAsync();

        var req = new SaveTipoCombustibleRequest("Gasolina");
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Update_Returns200_ConDtoActualizado()
    {
        var tipo = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var req = new SaveTipoCombustibleRequest("Diesel", false);
        var result = await _controller.Update(tipo.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as TipoCombustibleDto;
        Assert.AreEqual("Diesel", dto!.Nombre);
        Assert.IsFalse(dto.Activo);
    }

    [TestMethod]
    public async Task Update_Returns404_CuandoNoExiste()
    {
        var req = new SaveTipoCombustibleRequest("Diesel");
        var result = await _controller.Update(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Update_Returns409_CuandoNombreDuplicadoEnOtroRegistro()
    {
        _db.TiposCombustible.AddRange(
            new TipoCombustible { Nombre = "Gasolina", Activo = true },
            new TipoCombustible { Nombre = "Diesel", Activo = true });
        await _db.SaveChangesAsync();
        var gasolina = await _db.TiposCombustible.FirstAsync(t => t.Nombre == "Gasolina");

        var req = new SaveTipoCombustibleRequest("Diesel");
        var result = await _controller.Update(gasolina.Id, req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Deactivate_PoneActivoEnFalse()
    {
        var tipo = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(tipo.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);

        await _db.Entry(tipo).ReloadAsync();
        Assert.IsFalse(tipo.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns404_CuandoNoExiste()
    {
        var result = await _controller.Deactivate(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoHayTanquesActivos()
    {
        var tipo = new TipoCombustible { Nombre = "Gasolina Premium", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        _db.Tanques.Add(new Tanque
        {
            Identificacion = "T-ACTIVO", Capacidad = 3000m,
            NivelActual = 0m, NivelCritico = 300m,
            TipoCombustibleId = tipo.Id, Activo = true
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(tipo.Id, CancellationToken.None);
        var conflict = result as ConflictObjectResult;
        Assert.IsNotNull(conflict, "Esperaba 409 Conflict");

        var code = conflict.Value!.GetType().GetProperty("code")?.GetValue(conflict.Value)?.ToString();
        Assert.AreEqual("TIPO_COMBUSTIBLE_CON_TANQUES_ACTIVOS", code);

        await _db.Entry(tipo).ReloadAsync();
        Assert.IsTrue(tipo.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns204_CuandoTanquesInactivos()
    {
        var tipo = new TipoCombustible { Nombre = "Diesel 50", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        _db.Tanques.Add(new Tanque
        {
            Identificacion = "T-INACTIVO", Capacidad = 2000m,
            NivelActual = 0m, NivelCritico = 200m,
            TipoCombustibleId = tipo.Id, Activo = false
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(tipo.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoHaySolicitudesActivas()
    {
        var tipo = new TipoCombustible { Nombre = "Gasolina Regular", Activo = true };
        _db.TiposCombustible.Add(tipo);
        var dep = new Departamento { Nombre = "Ops", Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();

        var emp = new Empleado
        {
            Codigo = "TC-EMP01", NombreCompleto = "Test Emp", Cedula = "003-0000001-1",
            Cargo = "Cargo", Correo = "tc@test.com", Telefono = "809-300-0001",
            Activo = true, DepartamentoId = dep.Id
        };
        var veh = new Vehiculo
        {
            Placa = "TC00001", Ficha = "TCF01", Marca = "Toyota", Modelo = "Prado",
            Año = 2021, Tipo = "SUV", CapacidadTanque = 65, Odometro = 0,
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Empleados.Add(emp);
        _db.Vehiculos.Add(veh);
        await _db.SaveChangesAsync();

        _db.SolicitudesCombustible.Add(new SolicitudCombustible
        {
            CantidadSolicitada = 30m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente, FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = emp.Id, VehiculoId = veh.Id,
            DepartamentoId = dep.Id, TipoCombustibleId = tipo.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(tipo.Id, CancellationToken.None);
        var conflict = result as ConflictObjectResult;
        Assert.IsNotNull(conflict, "Esperaba 409 Conflict");
        var code = conflict.Value!.GetType().GetProperty("code")?.GetValue(conflict.Value)?.ToString();
        Assert.AreEqual("TIPO_COMBUSTIBLE_CON_SOLICITUDES_ACTIVAS", code);
    }

    [TestMethod]
    public async Task Deactivate_Returns409_CuandoHayTicketsActivos()
    {
        var tipo = new TipoCombustible { Nombre = "Gasolina Premium", Activo = true };
        _db.TiposCombustible.Add(tipo);
        var dep = new Departamento { Nombre = "Flota", Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();

        var emp = new Empleado
        {
            Codigo = "TC-EMP02", NombreCompleto = "Test Emp2", Cedula = "003-0000002-2",
            Cargo = "Cargo", Correo = "tc2@test.com", Telefono = "809-300-0002",
            Activo = true, DepartamentoId = dep.Id
        };
        var veh = new Vehiculo
        {
            Placa = "TC00002", Ficha = "TCF02", Marca = "Ford", Modelo = "Explorer",
            Año = 2020, Tipo = "SUV", CapacidadTanque = 70, Odometro = 0,
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Empleados.Add(emp);
        _db.Vehiculos.Add(veh);
        await _db.SaveChangesAsync();

        _db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(), NumeroSecuencial = 1, Prefijo = "COM",
            FechaCreacion = DateTime.UtcNow,
            FechaVencimiento = DateTime.UtcNow.AddDays(7),
            Estado = EstadoTicket.Pendiente, CantidadAutorizada = 30m,
            TipoCombustibleId = tipo.Id,
            EmpleadoId = emp.Id, VehiculoId = veh.Id, DepartamentoId = dep.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(tipo.Id, CancellationToken.None);
        var conflict = result as ConflictObjectResult;
        Assert.IsNotNull(conflict, "Esperaba 409 Conflict");
        var code = conflict.Value!.GetType().GetProperty("code")?.GetValue(conflict.Value)?.ToString();
        Assert.AreEqual("TIPO_COMBUSTIBLE_CON_TICKETS_ACTIVOS", code);
    }

    [TestMethod]
    public async Task Deactivate_Returns204_CuandoSoloHayTicketsTerminales()
    {
        var tipo = new TipoCombustible { Nombre = "Diesel Premium", Activo = true };
        _db.TiposCombustible.Add(tipo);
        var dep = new Departamento { Nombre = "Archivo", Activo = true };
        _db.Departamentos.Add(dep);
        await _db.SaveChangesAsync();

        var emp = new Empleado
        {
            Codigo = "TC-EMP03", NombreCompleto = "Test Emp3", Cedula = "003-0000003-3",
            Cargo = "Cargo", Correo = "tc3@test.com", Telefono = "809-300-0003",
            Activo = true, DepartamentoId = dep.Id
        };
        var veh = new Vehiculo
        {
            Placa = "TC00003", Ficha = "TCF03", Marca = "Nissan", Modelo = "Pathfinder",
            Año = 2019, Tipo = "SUV", CapacidadTanque = 75, Odometro = 0,
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Empleados.Add(emp);
        _db.Vehiculos.Add(veh);
        await _db.SaveChangesAsync();

        // ticket consumido — no debe bloquear
        _db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(), NumeroSecuencial = 2, Prefijo = "COM",
            FechaCreacion = DateTime.UtcNow.AddDays(-10),
            FechaVencimiento = DateTime.UtcNow.AddDays(-3),
            Estado = EstadoTicket.Consumido, CantidadAutorizada = 25m,
            TipoCombustibleId = tipo.Id,
            EmpleadoId = emp.Id, VehiculoId = veh.Id, DepartamentoId = dep.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(tipo.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);
    }

    [TestMethod]
    public async Task Update_SupervisorNoPuedeDesactivarPorEdicion()
    {
        var entity = new TipoCombustible { Nombre = "Combustible", Activo = true };
        _db.TiposCombustible.Add(entity);
        await _db.SaveChangesAsync();
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, _usuarioActorId.ToString()), new Claim(ClaimTypes.Role, "Supervisor")], "Test"));
        var result = await _controller.Update(entity.Id, new SaveTipoCombustibleRequest("Combustible", false), default);
        Assert.AreEqual(403, ((ObjectResult)result.Result!).StatusCode);
        _db.ChangeTracker.Clear();
        Assert.IsTrue((await _db.TiposCombustible.SingleAsync()).Activo);
        Assert.AreEqual(0, await _db.Auditorias.CountAsync());
    }

}
