// backend/FuelTrack.Api.Tests/Controllers/SolicitudesControllerTests.cs
using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Solicitudes;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class SolicitudesControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private SolicitudesController _controller = null!;

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
        _controller = new SolicitudesController(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<(Empleado empleado, Vehiculo vehiculo, Departamento departamento, TipoCombustible tipo)>
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
        return (empleado, vehiculo, depto, tipo);
    }

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_WhenNoData()
    {
        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<SolicitudDto>;
        Assert.AreEqual(0, list!.Count);
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
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m,
            TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = empleado.Id,
            VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id,
            TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(solicitud.Id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as SolicitudDto;

        Assert.AreEqual(solicitud.Id, dto!.Id);
        Assert.AreEqual(50m, dto.CantidadSolicitada);
        Assert.AreEqual("Manual", dto.TipoSolicitud);
        Assert.AreEqual(EstadoSolicitud.Pendiente, dto.Estado);
        Assert.AreEqual("Juan Pérez", dto.EmpleadoNombre);
        Assert.AreEqual("A123456", dto.VehiculoPlaca);
        Assert.AreEqual("TI", dto.DepartamentoNombre);
        Assert.AreEqual("Gasolina", dto.TipoCombustibleNombre);
    }

    [TestMethod]
    public async Task Create_Returns201_ConDto()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var req = new CreateSolicitudRequest(75m, empleado.Id, vehiculo.Id, depto.Id, tipo.Id, null);

        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;

        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as SolicitudDto;
        Assert.AreEqual(75m, dto!.CantidadSolicitada);
        Assert.AreEqual("Manual", dto.TipoSolicitud);
        Assert.AreEqual(EstadoSolicitud.Pendiente, dto.Estado);
        Assert.AreNotEqual(default(DateTime), dto.FechaSolicitud);
        Assert.AreEqual("Juan Pérez", dto.EmpleadoNombre);
        Assert.AreEqual("A123456", dto.VehiculoPlaca);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoEmpleadoNoExiste()
    {
        var (_, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var req = new CreateSolicitudRequest(50m, 999, vehiculo.Id, depto.Id, tipo.Id, null);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("EMPLEADO_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoVehiculoNoExiste()
    {
        var (empleado, _, depto, tipo) = await CrearDependenciasAsync();
        var req = new CreateSolicitudRequest(50m, empleado.Id, 999, depto.Id, tipo.Id, null);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("VEHICULO_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoDepartamentoNoExiste()
    {
        var (empleado, vehiculo, _, tipo) = await CrearDependenciasAsync();
        var req = new CreateSolicitudRequest(50m, empleado.Id, vehiculo.Id, 999, tipo.Id, null);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("DEPARTAMENTO_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoTipoCombustibleNoExiste()
    {
        var (empleado, vehiculo, depto, _) = await CrearDependenciasAsync();
        var req = new CreateSolicitudRequest(50m, empleado.Id, vehiculo.Id, depto.Id, 999, null);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TIPO_COMBUSTIBLE_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Aprobar_Returns200_ConCantidadAutorizada()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente, FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var req = new AprobarSolicitudRequest(45m);
        var result = await _controller.Aprobar(solicitud.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as SolicitudDto;

        Assert.AreEqual(EstadoSolicitud.Aprobada, dto!.Estado);
        Assert.AreEqual(45m, dto.CantidadAutorizada);
    }

    [TestMethod]
    public async Task Aprobar_Returns404_CuandoNoExiste()
    {
        var req = new AprobarSolicitudRequest(45m);
        var result = await _controller.Aprobar(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Aprobar_Returns409_CuandoYaFueProcesada()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Aprobada, FechaSolicitud = DateTime.UtcNow,
            CantidadAutorizada = 50m,
            EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var req = new AprobarSolicitudRequest(45m);
        var result = await _controller.Aprobar(solicitud.Id, req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("SOLICITUD_YA_PROCESADA"));
    }

    [TestMethod]
    public async Task Rechazar_Returns200_ConMotivoRechazo()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente, FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var req = new RechazarSolicitudRequest("Presupuesto agotado");
        var result = await _controller.Rechazar(solicitud.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as SolicitudDto;

        Assert.AreEqual(EstadoSolicitud.Rechazada, dto!.Estado);
        Assert.AreEqual("Presupuesto agotado", dto.MotivoRechazo);
    }

    [TestMethod]
    public async Task Rechazar_Returns404_CuandoNoExiste()
    {
        var req = new RechazarSolicitudRequest("Motivo");
        var result = await _controller.Rechazar(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Rechazar_Returns409_CuandoYaFueProcesada()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Rechazada, FechaSolicitud = DateTime.UtcNow,
            MotivoRechazo = "Ya rechazada",
            EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var req = new RechazarSolicitudRequest("Otro motivo");
        var result = await _controller.Rechazar(solicitud.Id, req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("SOLICITUD_YA_PROCESADA"));
    }
}
