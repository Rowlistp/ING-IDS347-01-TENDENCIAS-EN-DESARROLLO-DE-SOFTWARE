using System.Security.Claims;
using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Vehiculos;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Http;
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
        _controller = new VehiculosController(_db, new AuditService(_db))
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

    private async Task<TipoCombustible> CrearTipoCombustibleAsync(string nombre = "Gasolina", bool activo = true)
    {
        var tipo = new TipoCombustible { Nombre = nombre, Activo = activo };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();
        return tipo;
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
        var tipo = await CrearTipoCombustibleAsync("Diesel");
        var req = new SaveVehiculoRequest("D300001", "FV03", "Toyota", "Hilux",
            2022, "Camioneta", dep.Id, 60m, TipoCombustibleId: tipo.Id);

        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as VehiculoDto;
        Assert.AreEqual("D300001", dto!.Placa);
        Assert.AreEqual(tipo.Id, dto.TipoCombustibleId);
        Assert.AreEqual("Diesel", dto.TipoCombustibleNombre);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoFaltaTipoCombustible()
    {
        var dep = await CrearDepartamentoAsync();
        var req = new SaveVehiculoRequest("D300002", "FV03B", "Toyota", "Hilux",
            2022, "Camioneta", dep.Id, 60m);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(bad);
        StringAssert.Contains(bad.Value!.ToString()!, "TIPO_COMBUSTIBLE_REQUERIDO");
        Assert.AreEqual(0, await _db.Vehiculos.CountAsync());
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoTipoCombustibleNoExiste()
    {
        var dep = await CrearDepartamentoAsync();
        var req = new SaveVehiculoRequest("D300003", "FV03C", "Toyota", "Hilux",
            2022, "Camioneta", dep.Id, 60m, TipoCombustibleId: 999);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(bad);
        StringAssert.Contains(bad.Value!.ToString()!, "TIPO_COMBUSTIBLE_NOT_FOUND");
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoTipoCombustibleInactivo()
    {
        var dep = await CrearDepartamentoAsync();
        var tipo = await CrearTipoCombustibleAsync("GLP", activo: false);
        var req = new SaveVehiculoRequest("D300004", "FV03D", "Toyota", "Hilux",
            2022, "Camioneta", dep.Id, 60m, TipoCombustibleId: tipo.Id);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(bad);
        StringAssert.Contains(bad.Value!.ToString()!, "TIPO_COMBUSTIBLE_INACTIVO");
    }

    [TestMethod]
    public async Task Create_RegistraAuditoria()
    {
        var dep = await CrearDepartamentoAsync();
        var tipo = await CrearTipoCombustibleAsync();
        var req = new SaveVehiculoRequest("D300001", "FV03", "Toyota", "Hilux",
            2022, "Camioneta", dep.Id, 60m, TipoCombustibleId: tipo.Id);
        await _controller.Create(req, CancellationToken.None);

        var auditoria = await _db.Auditorias.FirstOrDefaultAsync(a => a.Evento == "VEHICULO_CREADO");
        Assert.IsNotNull(auditoria);
        Assert.AreEqual("Vehiculo", auditoria.EntidadAfectada);
        Assert.AreEqual(_usuarioActorId, auditoria.UsuarioId);
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

        var tipo = await CrearTipoCombustibleAsync();
        var req = new SaveVehiculoRequest("F500001", "FV06", "KIA", "Picanto",
            2022, "Sedan", dep.Id, 40m, TipoCombustibleId: tipo.Id);
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    // ── Combustible del vehículo en Update ──────────────────────────────────

    private async Task<Vehiculo> CrearVehiculoConCombustibleAsync(Departamento dep, TipoCombustible tipo)
    {
        var v = new Vehiculo
        {
            Placa = "K900001", Ficha = "FV09", Marca = "Toyota", Modelo = "Hilux",
            Año = 2022, Tipo = "Camioneta", CapacidadTanque = 60, Odometro = 0,
            Activo = true, DepartamentoId = dep.Id, TipoCombustibleId = tipo.Id
        };
        _db.Vehiculos.Add(v);
        await _db.SaveChangesAsync();
        return v;
    }

    [TestMethod]
    public async Task Update_ConservaCombustible_CuandoNoVieneEnElRequest()
    {
        var dep = await CrearDepartamentoAsync();
        var tipo = await CrearTipoCombustibleAsync("Diesel");
        var v = await CrearVehiculoConCombustibleAsync(dep, tipo);

        var req = new SaveVehiculoRequest("K900001", "FV09", "Toyota", "Hilux XL",
            2022, "Camioneta", dep.Id, 60m);
        var result = await _controller.Update(v.Id, req, CancellationToken.None);

        var dto = (result.Result as OkObjectResult)!.Value as VehiculoDto;
        Assert.AreEqual(tipo.Id, dto!.TipoCombustibleId);
        Assert.AreEqual("Diesel", dto.TipoCombustibleNombre);
    }

    [TestMethod]
    public async Task Update_CambiaCombustible_CuandoVieneEnElRequest()
    {
        var dep = await CrearDepartamentoAsync();
        var diesel = await CrearTipoCombustibleAsync("Diesel");
        var gasolina = await CrearTipoCombustibleAsync("Gasolina");
        var v = await CrearVehiculoConCombustibleAsync(dep, diesel);

        var req = new SaveVehiculoRequest("K900001", "FV09", "Toyota", "Hilux",
            2022, "Camioneta", dep.Id, 60m, TipoCombustibleId: gasolina.Id);
        var result = await _controller.Update(v.Id, req, CancellationToken.None);

        var dto = (result.Result as OkObjectResult)!.Value as VehiculoDto;
        Assert.AreEqual(gasolina.Id, dto!.TipoCombustibleId);
        Assert.AreEqual("Gasolina", dto.TipoCombustibleNombre);
        Assert.AreEqual(gasolina.Id, (await _db.Vehiculos.AsNoTracking().FirstAsync(x => x.Id == v.Id)).TipoCombustibleId);
    }

    [TestMethod]
    public async Task Update_Returns400_CuandoNuevoCombustibleInactivo_YNoModifica()
    {
        var dep = await CrearDepartamentoAsync();
        var diesel = await CrearTipoCombustibleAsync("Diesel");
        var inactivo = await CrearTipoCombustibleAsync("GLP", activo: false);
        var v = await CrearVehiculoConCombustibleAsync(dep, diesel);

        var req = new SaveVehiculoRequest("K900001", "FV09", "Toyota", "Hilux",
            2022, "Camioneta", dep.Id, 60m, TipoCombustibleId: inactivo.Id);
        var result = await _controller.Update(v.Id, req, CancellationToken.None);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        Assert.AreEqual(diesel.Id, (await _db.Vehiculos.AsNoTracking().FirstAsync(x => x.Id == v.Id)).TipoCombustibleId);
    }

    [TestMethod]
    public async Task Update_PermiteEditar_VehiculoLegadoSinCombustible()
    {
        var dep = await CrearDepartamentoAsync();
        var (_, vehiculo, _) = await CrearDependenciasAsync(dep);

        var req = new SaveVehiculoRequest(vehiculo.Placa, vehiculo.Ficha, "Ford", "Ranger XL",
            2021, "Camioneta", dep.Id, 80m);
        var result = await _controller.Update(vehiculo.Id, req, CancellationToken.None);

        var dto = (result.Result as OkObjectResult)!.Value as VehiculoDto;
        Assert.IsNull(dto!.TipoCombustibleId);
        Assert.IsNull(dto.TipoCombustibleNombre);
    }

    // ── Update ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Update_Returns404_CuandoNoExiste()
    {
        var dep = await CrearDepartamentoAsync();
        var req = new SaveVehiculoRequest("H700001", "FV08", "Mazda", "BT-50",
            2021, "Camioneta", dep.Id, 60m);
        var result = await _controller.Update(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Update_PermiteReactivar_VehiculoDesactivado()
    {
        var dep = await CrearDepartamentoAsync();
        var veh = new Vehiculo
        {
            Placa = "H700001", Ficha = "FV08", Marca = "Mazda", Modelo = "BT-50",
            Año = 2021, Tipo = "Camioneta", CapacidadTanque = 60, Odometro = 0,
            Activo = false, DepartamentoId = dep.Id
        };
        _db.Vehiculos.Add(veh);
        await _db.SaveChangesAsync();

        var req = new SaveVehiculoRequest("H700001", "FV08", "Mazda", "BT-50",
            2021, "Camioneta", dep.Id, 60m, Odometro: 0, Activo: true);
        var result = await _controller.Update(veh.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as VehiculoDto;
        Assert.IsTrue(dto!.Activo);

        await _db.Entry(veh).ReloadAsync();
        Assert.IsTrue(veh.Activo);
    }

    [TestMethod]
    public async Task Update_NoTocaActivo_CuandoElCampoNoVieneEnElRequest()
    {
        // Regresión: un caller (como el frontend actual, que no envía "activo" en el
        // payload de edición) no debe reactivar ni desactivar el vehículo sin querer.
        var dep = await CrearDepartamentoAsync();
        var veh = new Vehiculo
        {
            Placa = "J900001", Ficha = "FV09", Marca = "Toyota", Modelo = "Hilux",
            Año = 2021, Tipo = "Camioneta", CapacidadTanque = 60, Odometro = 0,
            Activo = false, DepartamentoId = dep.Id
        };
        _db.Vehiculos.Add(veh);
        await _db.SaveChangesAsync();

        var req = new SaveVehiculoRequest("J900001", "FV09", "Toyota", "Hilux MOD",
            2021, "Camioneta", dep.Id, 60m);
        var result = await _controller.Update(veh.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as VehiculoDto;
        Assert.IsFalse(dto!.Activo);

        await _db.Entry(veh).ReloadAsync();
        Assert.IsFalse(veh.Activo);
    }

    [TestMethod]
    public async Task Update_Returns409_CuandoDesactivaConSolicitudPendiente()
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

        var req = new SaveVehiculoRequest(veh.Placa, veh.Ficha, veh.Marca, veh.Modelo,
            veh.Año, veh.Tipo, dep.Id, veh.CapacidadTanque, Odometro: 0, Activo: false);
        var result = await _controller.Update(veh.Id, req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;
        Assert.IsNotNull(conflict, "Esperaba 409 Conflict");

        var code = conflict.Value!.GetType().GetProperty("code")?.GetValue(conflict.Value)?.ToString();
        Assert.AreEqual("VEHICULO_CON_SOLICITUDES_ACTIVAS", code);

        await _db.Entry(veh).ReloadAsync();
        Assert.IsTrue(veh.Activo);
    }

    [TestMethod]
    public async Task Update_PermiteEditarDatos_SinDesactivar_AunConSolicitudPendiente()
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

        var req = new SaveVehiculoRequest(veh.Placa, veh.Ficha, veh.Marca, "Ranger XL",
            veh.Año, veh.Tipo, dep.Id, veh.CapacidadTanque, Odometro: 0, Activo: true);
        var result = await _controller.Update(veh.Id, req, CancellationToken.None);
        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
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
