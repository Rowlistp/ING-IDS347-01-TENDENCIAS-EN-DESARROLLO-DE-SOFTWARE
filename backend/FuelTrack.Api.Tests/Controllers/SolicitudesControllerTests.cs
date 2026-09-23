using System.Security.Claims;
using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Solicitudes;
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
        _db.Usuarios.Add(new Usuario { Id = 1, NombreUsuario = "admin-regression", Activo = true });
        await _db.SaveChangesAsync();
        _controller = CrearController(1, Roles.Administrador);
    }

    private SolicitudesController CrearController(int usuarioId, string rol) => new(_db, new AuditService(_db))
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                        new Claim(ClaimTypes.Role, rol)
                    ], "Test"))
            }
        }
    };

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

    private async Task<Usuario> CrearUsuarioAsync(string nombreUsuario)
    {
        var usuario = new Usuario { NombreUsuario = nombreUsuario, PasswordHash = "hash", Activo = true };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        return usuario;
    }

    [TestMethod]
    public async Task GetAll_Solicitante_SoloVeSusPropiasSolicitudes()
    {
        var (empleadoPropio, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var usuarioPropio = await CrearUsuarioAsync("solicitante.propio");
        var usuarioAjeno = await CrearUsuarioAsync("solicitante.ajeno");
        empleadoPropio.UsuarioId = usuarioPropio.Id;

        var empleadoAjeno = new Empleado
        {
            Codigo = "E-002", NombreCompleto = "María Gómez", Cedula = "001-0000002-2",
            Cargo = "Analista", Correo = "maria@test.com", Telefono = "8091234568",
            DepartamentoId = depto.Id, Activo = true, UsuarioId = usuarioAjeno.Id
        };
        _db.Empleados.Add(empleadoAjeno);
        await _db.SaveChangesAsync();

        _db.SolicitudesCombustible.AddRange(
            new SolicitudCombustible
            {
                CantidadSolicitada = 50m, TipoSolicitud = "Manual", Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow, EmpleadoId = empleadoPropio.Id, VehiculoId = vehiculo.Id,
                DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
            },
            new SolicitudCombustible
            {
                CantidadSolicitada = 30m, TipoSolicitud = "Manual", Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow, EmpleadoId = empleadoAjeno.Id, VehiculoId = vehiculo.Id,
                DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
            });
        await _db.SaveChangesAsync();

        var solicitanteCtrl = CrearController(usuarioPropio.Id, Roles.Solicitante);
        var result = await solicitanteCtrl.GetAll(CancellationToken.None);
        var list = (result.Result as OkObjectResult)!.Value as List<SolicitudDto>;

        Assert.AreEqual(1, list!.Count);
        Assert.AreEqual("Juan Pérez", list[0].EmpleadoNombre);
    }

    [TestMethod]
    public async Task GetById_Solicitante_Returns404_ParaSolicitudAjena()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var usuarioAjeno = await CrearUsuarioAsync("solicitante.ajeno");
        var usuarioPropio = await CrearUsuarioAsync("solicitante.propio");
        empleado.UsuarioId = usuarioAjeno.Id;
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m, TipoSolicitud = "Manual", Estado = EstadoSolicitud.Pendiente,
            FechaSolicitud = DateTime.UtcNow, EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var solicitanteCtrl = CrearController(usuarioPropio.Id, Roles.Solicitante);
        var result = await solicitanteCtrl.GetById(solicitud.Id, CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
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

    [TestMethod]
    public async Task Aprobar_Returns409_CuandoYaFueRechazada()
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

        var req = new AprobarSolicitudRequest(45m);
        var result = await _controller.Aprobar(solicitud.Id, req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("SOLICITUD_YA_PROCESADA"));
    }

    [TestMethod]
    public async Task Rechazar_Returns409_CuandoYaFueAprobada()
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

        var req = new RechazarSolicitudRequest("Intento inválido");
        var result = await _controller.Rechazar(solicitud.Id, req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("SOLICITUD_YA_PROCESADA"));
    }

    // ── OwnerFilter / Solicitante ───────────────────────────────────────────

    private void SetUserContext(int userId, string role)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], "TestAuth");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [TestMethod]
    public async Task GetAll_Solicitante_FiltraSoloSusPropiasSolicitudes()
    {
        var (empleado1, vehiculo, depto, tipo) = await CrearDependenciasAsync();

        var user1 = new Usuario { NombreUsuario = "solicitante1", Activo = true };
        var user2 = new Usuario { NombreUsuario = "solicitante2", Activo = true };
        _db.Usuarios.AddRange(user1, user2);
        await _db.SaveChangesAsync();

        empleado1.UsuarioId = user1.Id;

        var empleado2 = new Empleado
        {
            Codigo = "E-002", NombreCompleto = "Otro Empleado", Cedula = "001-0000002-2",
            Cargo = "Chofer", Correo = "otro@test.com", Telefono = "8091234568",
            DepartamentoId = depto.Id, Activo = true, UsuarioId = user2.Id
        };
        _db.Empleados.Add(empleado2);
        await _db.SaveChangesAsync();

        _db.SolicitudesCombustible.AddRange(
            new SolicitudCombustible
            {
                CantidadSolicitada = 10m, TipoSolicitud = "Manual", Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow, EmpleadoId = empleado1.Id, VehiculoId = vehiculo.Id,
                DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
            },
            new SolicitudCombustible
            {
                CantidadSolicitada = 20m, TipoSolicitud = "Manual", Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow, EmpleadoId = empleado2.Id, VehiculoId = vehiculo.Id,
                DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
            }
        );
        await _db.SaveChangesAsync();

        SetUserContext(user1.Id, Roles.Solicitante);

        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var list = ok.Value as List<SolicitudDto>;
        Assert.IsNotNull(list);
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(empleado1.Id, list[0].EmpleadoId);
    }

    [TestMethod]
    public async Task GetAll_Administrador_RetornaTodasLasSolicitudes()
    {
        var (empleado1, vehiculo, depto, tipo) = await CrearDependenciasAsync();

        var user1 = new Usuario { NombreUsuario = "solicitante.admin.test", Activo = true };
        var userAdmin = new Usuario { NombreUsuario = "admin.test", Activo = true };
        _db.Usuarios.AddRange(user1, userAdmin);
        await _db.SaveChangesAsync();

        empleado1.UsuarioId = user1.Id;

        var empleado2 = new Empleado
        {
            Codigo = "E-003", NombreCompleto = "Tercer Empleado", Cedula = "001-0000003-3",
            Cargo = "Chofer", Correo = "tercero@test.com", Telefono = "8091234569",
            DepartamentoId = depto.Id, Activo = true
        };
        _db.Empleados.Add(empleado2);
        await _db.SaveChangesAsync();

        _db.SolicitudesCombustible.AddRange(
            new SolicitudCombustible
            {
                CantidadSolicitada = 10m, TipoSolicitud = "Manual", Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow, EmpleadoId = empleado1.Id, VehiculoId = vehiculo.Id,
                DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
            },
            new SolicitudCombustible
            {
                CantidadSolicitada = 20m, TipoSolicitud = "Manual", Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow, EmpleadoId = empleado2.Id, VehiculoId = vehiculo.Id,
                DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
            }
        );
        await _db.SaveChangesAsync();

        SetUserContext(userAdmin.Id, Roles.Administrador);

        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var list = ok.Value as List<SolicitudDto>;
        Assert.IsNotNull(list);
        Assert.AreEqual(2, list.Count);
    }

    [TestMethod]
    public async Task Create_Solicitante_NoPuedeCrearParaOtroEmpleado_Retorna403()
    {
        var (empleado1, vehiculo, depto, tipo) = await CrearDependenciasAsync();

        var user1 = new Usuario { NombreUsuario = "solicitante.propio", Activo = true };
        _db.Usuarios.Add(user1);
        await _db.SaveChangesAsync();

        empleado1.UsuarioId = user1.Id;

        var empleadoOtro = new Empleado
        {
            Codigo = "E-004", NombreCompleto = "Empleado Ajeno", Cedula = "001-0000004-4",
            Cargo = "Chofer", Correo = "ajeno@test.com", Telefono = "8091234570",
            DepartamentoId = depto.Id, Activo = true
        };
        _db.Empleados.Add(empleadoOtro);
        await _db.SaveChangesAsync();

        SetUserContext(user1.Id, Roles.Solicitante);

        var req = new CreateSolicitudRequest(50m, empleadoOtro.Id, vehiculo.Id, depto.Id, tipo.Id, null);
        var result = await _controller.Create(req, CancellationToken.None);
        var objResult = result.Result as ObjectResult;
        Assert.IsNotNull(objResult);
        Assert.AreEqual(StatusCodes.Status403Forbidden, objResult.StatusCode);
    }

    [TestMethod]
    public async Task Create_RechazaDepartamentoIncompatibleSinGuardarNiAuditar()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var otro = new Departamento { Nombre = "Otro", Activo = true };
        _db.Departamentos.Add(otro);
        await _db.SaveChangesAsync();
        var result = await _controller.Create(new CreateSolicitudRequest(10m, empleado.Id, vehiculo.Id, otro.Id, tipo.Id, null), default);
        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        Assert.AreEqual(0, await _db.SolicitudesCombustible.CountAsync());
        Assert.AreEqual(0, await _db.Auditorias.CountAsync());
    }

    [TestMethod]
    public async Task Aprobar_RevalidaRelacionesYCantidadYAuditaUnicamenteExito()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        await _controller.Create(new CreateSolicitudRequest(10m, empleado.Id, vehiculo.Id, depto.Id, tipo.Id, null), default);
        var solicitud = await _db.SolicitudesCombustible.SingleAsync();
        var otro = new Departamento { Nombre = "Reasignado", Activo = true };
        _db.Departamentos.Add(otro);
        vehiculo.Departamento = otro;
        await _db.SaveChangesAsync();
        Assert.IsInstanceOfType<ConflictObjectResult>((await _controller.Aprobar(solicitud.Id, new AprobarSolicitudRequest(10m), default)).Result);
        vehiculo.DepartamentoId = depto.Id;
        await _db.SaveChangesAsync();
        Assert.IsInstanceOfType<BadRequestObjectResult>((await _controller.Aprobar(solicitud.Id, new AprobarSolicitudRequest(11m), default)).Result);
        Assert.AreEqual(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.AreEqual(1, await _db.Auditorias.CountAsync());
        Assert.IsInstanceOfType<OkObjectResult>((await _controller.Aprobar(solicitud.Id, new AprobarSolicitudRequest(10m), default)).Result);
        Assert.IsTrue(await _db.Auditorias.AnyAsync(a => a.Evento == "SOLICITUD_APROBADA" && a.UsuarioId == 1 && a.IdentificadorRegistro == solicitud.Id.ToString()));
    }

    [TestMethod]
    public async Task Rechazar_AuditaActorYNoPermiteDobleProceso()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        await _controller.Create(new CreateSolicitudRequest(10m, empleado.Id, vehiculo.Id, depto.Id, tipo.Id, null), default);
        var solicitud = await _db.SolicitudesCombustible.SingleAsync();
        Assert.IsInstanceOfType<OkObjectResult>((await _controller.Rechazar(solicitud.Id, new RechazarSolicitudRequest("Sin disponibilidad"), default)).Result);
        Assert.IsInstanceOfType<ConflictObjectResult>((await _controller.Aprobar(solicitud.Id, new AprobarSolicitudRequest(10m), default)).Result);
        Assert.AreEqual(2, await _db.Auditorias.CountAsync());
        Assert.IsTrue(await _db.Auditorias.AnyAsync(a => a.Evento == "SOLICITUD_RECHAZADA" && a.UsuarioId == 1));
    }

    [TestMethod]
    public async Task Create_FalloAuditoriaRevierteSolicitud()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var controller = CrearController(99999, Roles.Administrador);
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => controller.Create(new CreateSolicitudRequest(10m, empleado.Id, vehiculo.Id, depto.Id, tipo.Id, null), default));
        _db.ChangeTracker.Clear();
        Assert.AreEqual(0, await _db.SolicitudesCombustible.CountAsync());
        Assert.AreEqual(0, await _db.Auditorias.CountAsync());
    }


    [TestMethod]
    public async Task DecisionConLecturaObsoleta_NoSobrescribeLaDecisionDeOtroOperador()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        await _controller.Create(new CreateSolicitudRequest(10m, empleado.Id, vehiculo.Id, depto.Id, tipo.Id, null), default);
        var solicitud = await _db.SolicitudesCombustible.SingleAsync();
        // Otra conexión decide después de la lectura; el objeto local sigue Pendiente.
        await _db.SolicitudesCombustible.Where(s => s.Id == solicitud.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Estado, EstadoSolicitud.Rechazada));
        var result = await _controller.Aprobar(solicitud.Id, new AprobarSolicitudRequest(10m), default);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
        await _db.Entry(solicitud).ReloadAsync();
        Assert.AreEqual(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.IsNull(solicitud.CantidadAutorizada);
        Assert.AreEqual(1, await _db.Auditorias.CountAsync());
    }
}
