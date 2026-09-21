using System.Security.Claims;
using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Empleados;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Http;
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
        _controller = new EmpleadosController(_db, new AuditService(_db))
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
    public async Task Create_RegistraAuditoria()
    {
        var dep = await CrearDepartamentoAsync();
        var req = new SaveEmpleadoRequest("EMP-001", "María González", "001-0000003-3",
            "Analista", "maria@test.com", "809-000-0003", dep.Id);
        await _controller.Create(req, CancellationToken.None);

        var auditoria = await _db.Auditorias.FirstOrDefaultAsync(a => a.Evento == "EMPLEADO_CREADO");
        Assert.IsNotNull(auditoria);
        Assert.AreEqual("Empleado", auditoria.EntidadAfectada);
        Assert.AreEqual(_usuarioActorId, auditoria.UsuarioId);
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

    // ── Vincular / Desvincular Usuario ──────────────────────────────────────

    [TestMethod]
    public async Task VincularUsuario_ReturnsOk_CuandoUsuarioValido()
    {
        var dep = await CrearDepartamentoAsync();
        var emp = new Empleado
        {
            Codigo = "EMP-V01", NombreCompleto = "Vinculado Test", Cedula = "001-0000020-0",
            Cargo = "Chofer", Correo = "v01@test.com", Telefono = "809-000-0020",
            Activo = true, DepartamentoId = dep.Id
        };
        var user = new Usuario { NombreUsuario = "solicitante.v01", Activo = true };
        _db.Empleados.Add(emp);
        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var req = new VincularUsuarioRequest(user.Id);
        var result = await _controller.VincularUsuario(emp.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var dto = ok.Value as EmpleadoDto;
        Assert.IsNotNull(dto);
        Assert.AreEqual(user.Id, dto.UsuarioId);
        Assert.AreEqual("solicitante.v01", dto.UsuarioNombre);

        await _db.Entry(emp).ReloadAsync();
        Assert.AreEqual(user.Id, emp.UsuarioId);
    }

    [TestMethod]
    public async Task VincularUsuario_Returns404_CuandoEmpleadoNoExiste()
    {
        var req = new VincularUsuarioRequest(1);
        var result = await _controller.VincularUsuario(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task VincularUsuario_Returns404_CuandoUsuarioNoExiste()
    {
        var dep = await CrearDepartamentoAsync();
        var emp = new Empleado
        {
            Codigo = "EMP-V02", NombreCompleto = "Test 404", Cedula = "001-0000021-0",
            Cargo = "Chofer", Correo = "v02@test.com", Telefono = "809-000-0021",
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Empleados.Add(emp);
        await _db.SaveChangesAsync();

        var req = new VincularUsuarioRequest(9999);
        var result = await _controller.VincularUsuario(emp.Id, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task VincularUsuario_Returns400_CuandoUsuarioInactivo()
    {
        var dep = await CrearDepartamentoAsync();
        var emp = new Empleado
        {
            Codigo = "EMP-V03", NombreCompleto = "Test Inactivo", Cedula = "001-0000022-0",
            Cargo = "Chofer", Correo = "v03@test.com", Telefono = "809-000-0022",
            Activo = true, DepartamentoId = dep.Id
        };
        var user = new Usuario { NombreUsuario = "inactivo.user", Activo = false };
        _db.Empleados.Add(emp);
        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var req = new VincularUsuarioRequest(user.Id);
        var result = await _controller.VincularUsuario(emp.Id, req, CancellationToken.None);
        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task VincularUsuario_Returns409_CuandoUsuarioYaVinculadoAOtroEmpleado()
    {
        var dep = await CrearDepartamentoAsync();
        var user = new Usuario { NombreUsuario = "ocupado.user", Activo = true };
        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var emp1 = new Empleado
        {
            Codigo = "EMP-V04A", NombreCompleto = "Empleado Uno", Cedula = "001-0000023-0",
            Cargo = "Chofer", Correo = "v04a@test.com", Telefono = "809-000-0023",
            Activo = true, DepartamentoId = dep.Id, UsuarioId = user.Id
        };
        var emp2 = new Empleado
        {
            Codigo = "EMP-V04B", NombreCompleto = "Empleado Dos", Cedula = "001-0000024-0",
            Cargo = "Chofer", Correo = "v04b@test.com", Telefono = "809-000-0024",
            Activo = true, DepartamentoId = dep.Id
        };
        _db.Empleados.AddRange(emp1, emp2);
        await _db.SaveChangesAsync();

        var req = new VincularUsuarioRequest(user.Id);
        var result = await _controller.VincularUsuario(emp2.Id, req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task VincularUsuario_ReturnsOk_DesvincularUsuario()
    {
        var dep = await CrearDepartamentoAsync();
        var user = new Usuario { NombreUsuario = "desvincular.user", Activo = true };
        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var emp = new Empleado
        {
            Codigo = "EMP-V05", NombreCompleto = "Empleado A Desvincular", Cedula = "001-0000025-0",
            Cargo = "Chofer", Correo = "v05@test.com", Telefono = "809-000-0025",
            Activo = true, DepartamentoId = dep.Id, UsuarioId = user.Id
        };
        _db.Empleados.Add(emp);
        await _db.SaveChangesAsync();

        var req = new VincularUsuarioRequest(null);
        var result = await _controller.VincularUsuario(emp.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var dto = ok.Value as EmpleadoDto;
        Assert.IsNull(dto!.UsuarioId);
        Assert.IsNull(dto.UsuarioNombre);

        await _db.Entry(emp).ReloadAsync();
        Assert.IsNull(emp.UsuarioId);
    }

    [TestMethod]
    public async Task Create_ConUsuarioIdValido_Retorna201()
    {
        var dep = await CrearDepartamentoAsync();
        var user = new Usuario { NombreUsuario = "crear.con.usuario", Activo = true };
        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var req = new SaveEmpleadoRequest("EMP-V06", "Empleado Nuevo Vinculado", "001-0000026-0",
            "Inspector", "v06@test.com", "809-000-0026", dep.Id, true, user.Id);

        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as EmpleadoDto;
        Assert.AreEqual(user.Id, dto!.UsuarioId);
        Assert.AreEqual("crear.con.usuario", dto.UsuarioNombre);
    }

    [TestMethod]
    public async Task Create_ConUsuarioYaVinculado_Retorna409()
    {
        var dep = await CrearDepartamentoAsync();
        var user = new Usuario { NombreUsuario = "duplicado.user", Activo = true };
        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var empExistente = new Empleado
        {
            Codigo = "EMP-V07A", NombreCompleto = "Emp Existente", Cedula = "001-0000027-0",
            Cargo = "Chofer", Correo = "v07a@test.com", Telefono = "809-000-0027",
            Activo = true, DepartamentoId = dep.Id, UsuarioId = user.Id
        };
        _db.Empleados.Add(empExistente);
        await _db.SaveChangesAsync();

        var req = new SaveEmpleadoRequest("EMP-V07B", "Emp Nuevo", "001-0000028-0",
            "Chofer", "v07b@test.com", "809-000-0028", dep.Id, true, user.Id);

        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Solicitante_SoloRecibeSuFicha_YNoPuedePedirOtraPorId()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, _, _) = await CrearDependenciasAsync(dep);
        var otro = new Empleado { Codigo = "OTRO", NombreCompleto = "Otro", Cedula = "999",
            Cargo = "Privado", Correo = "otro@test.com", Telefono = "8090000000", DepartamentoId = dep.Id, Activo = true };
        emp.UsuarioId = _usuarioActorId;
        _db.Empleados.Add(otro);
        await _db.SaveChangesAsync();
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, _usuarioActorId.ToString()), new Claim(ClaimTypes.Role, "Solicitante")], "Test"));
        var result = await _controller.GetAll(default);
        var list = (List<EmpleadoDto>)((OkObjectResult)result.Result!).Value!;
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(emp.Id, list[0].Id);
        Assert.IsInstanceOfType<NotFoundResult>((await _controller.GetById(otro.Id, default)).Result);
    }

    [TestMethod]
    public async Task Consulta_UsaOpcionesSinDatosPersonales_YNoLeeFichas()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, _, _) = await CrearDependenciasAsync(dep);
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, _usuarioActorId.ToString()), new Claim(ClaimTypes.Role, "Consulta")], "Test"));
        Assert.IsInstanceOfType<ForbidResult>((await _controller.GetAll(default)).Result);
        Assert.IsInstanceOfType<ForbidResult>((await _controller.GetById(emp.Id, default)).Result);
        var options = (OkObjectResult)await _controller.GetOptions(default);
        var json = System.Text.Json.JsonSerializer.Serialize(options.Value);
        StringAssert.Contains(json, "NombreCompleto");
        Assert.IsFalse(json.Contains("Cedula") || json.Contains("Telefono") || json.Contains("Correo"));
    }

    // ── Vehículo habitual ───────────────────────────────────────────────────

    private static SaveEmpleadoRequest Req(string codigo, string cedula, int depId, bool activo = true, int? vehiculoHabitualId = null)
        => new(codigo, "Empleado " + codigo, cedula, "Chofer", codigo.ToLower() + "@test.com",
            "809-000-0100", depId, activo, null, vehiculoHabitualId);

    [TestMethod]
    public async Task Create_ConVehiculoHabitual_Retorna201_ConIdYPlaca()
    {
        var dep = await CrearDepartamentoAsync();
        var (_, vehiculo, _) = await CrearDependenciasAsync(dep);

        var result = await _controller.Create(Req("EMP-H01", "001-0000101-1", dep.Id, vehiculoHabitualId: vehiculo.Id), default);

        var dto = (result.Result as CreatedAtActionResult)!.Value as EmpleadoDto;
        Assert.AreEqual(vehiculo.Id, dto!.VehiculoHabitualId);
        Assert.AreEqual("A000001", dto.VehiculoHabitualPlaca);
    }

    [TestMethod]
    public async Task Create_SinVehiculoHabitual_LoDejaNulo()
    {
        var dep = await CrearDepartamentoAsync();

        var result = await _controller.Create(Req("EMP-H02", "001-0000102-2", dep.Id), default);

        var dto = (result.Result as CreatedAtActionResult)!.Value as EmpleadoDto;
        Assert.IsNull(dto!.VehiculoHabitualId);
        Assert.IsNull(dto.VehiculoHabitualPlaca);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoVehiculoHabitualNoExiste()
    {
        var dep = await CrearDepartamentoAsync();

        var result = await _controller.Create(Req("EMP-H03", "001-0000103-3", dep.Id, vehiculoHabitualId: 999), default);

        var bad = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(bad);
        StringAssert.Contains(bad.Value!.ToString()!, "VEHICULO_NOT_FOUND");
        Assert.AreEqual(0, await _db.Empleados.CountAsync());
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoVehiculoHabitualInactivo()
    {
        var dep = await CrearDepartamentoAsync();
        var (_, vehiculo, _) = await CrearDependenciasAsync(dep);
        vehiculo.Activo = false;
        await _db.SaveChangesAsync();

        var result = await _controller.Create(Req("EMP-H04", "001-0000104-4", dep.Id, vehiculoHabitualId: vehiculo.Id), default);

        var bad = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(bad);
        StringAssert.Contains(bad.Value!.ToString()!, "VEHICULO_INACTIVO");
    }

    [TestMethod]
    public async Task Update_AsignaYLuegoQuitaVehiculoHabitual()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, vehiculo, _) = await CrearDependenciasAsync(dep);

        var asignado = ((await _controller.Update(emp.Id,
            Req(emp.Codigo, emp.Cedula, dep.Id, vehiculoHabitualId: vehiculo.Id), default)).Result as OkObjectResult)!.Value as EmpleadoDto;
        Assert.AreEqual(vehiculo.Id, asignado!.VehiculoHabitualId);
        Assert.AreEqual("A000001", asignado.VehiculoHabitualPlaca);

        var quitado = ((await _controller.Update(emp.Id,
            Req(emp.Codigo, emp.Cedula, dep.Id), default)).Result as OkObjectResult)!.Value as EmpleadoDto;
        Assert.IsNull(quitado!.VehiculoHabitualId);
        Assert.IsNull(quitado.VehiculoHabitualPlaca);
        Assert.IsNull((await _db.Empleados.AsNoTracking().FirstAsync(e => e.Id == emp.Id)).VehiculoHabitualId);
    }

    [TestMethod]
    public async Task Update_Returns400_CuandoNuevoVehiculoHabitualInactivo_YNoModifica()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, vehiculo, _) = await CrearDependenciasAsync(dep);
        vehiculo.Activo = false;
        await _db.SaveChangesAsync();

        var result = await _controller.Update(emp.Id,
            Req(emp.Codigo, emp.Cedula, dep.Id, vehiculoHabitualId: vehiculo.Id), default);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        Assert.IsNull((await _db.Empleados.AsNoTracking().FirstAsync(e => e.Id == emp.Id)).VehiculoHabitualId);
    }

    [TestMethod]
    public async Task Update_NoRechaza_CuandoElVehiculoHabitualActualSeDesactivoDespues()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, vehiculo, _) = await CrearDependenciasAsync(dep);
        emp.VehiculoHabitualId = vehiculo.Id;
        await _db.SaveChangesAsync();
        vehiculo.Activo = false;
        await _db.SaveChangesAsync();

        var result = await _controller.Update(emp.Id,
            Req(emp.Codigo, emp.Cedula, dep.Id, vehiculoHabitualId: vehiculo.Id), default);

        var dto = (result.Result as OkObjectResult)!.Value as EmpleadoDto;
        Assert.AreEqual(vehiculo.Id, dto!.VehiculoHabitualId);
    }

    [TestMethod]
    public async Task GetAll_IncluyeVehiculoHabitualPlaca()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, vehiculo, _) = await CrearDependenciasAsync(dep);
        emp.VehiculoHabitualId = vehiculo.Id;
        await _db.SaveChangesAsync();

        var list = ((await _controller.GetAll(default)).Result as OkObjectResult)!.Value as List<EmpleadoDto>;

        Assert.AreEqual(vehiculo.Id, list![0].VehiculoHabitualId);
        Assert.AreEqual("A000001", list[0].VehiculoHabitualPlaca);
    }

    [TestMethod]
    public async Task VincularUsuario_ConservaElVehiculoHabitual()
    {
        var dep = await CrearDepartamentoAsync();
        var (emp, vehiculo, _) = await CrearDependenciasAsync(dep);
        emp.VehiculoHabitualId = vehiculo.Id;
        var user = new Usuario { NombreUsuario = "habitual.user", Activo = true };
        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        var result = await _controller.VincularUsuario(emp.Id, new VincularUsuarioRequest(user.Id), default);

        var dto = (result.Result as OkObjectResult)!.Value as EmpleadoDto;
        Assert.AreEqual(vehiculo.Id, dto!.VehiculoHabitualId);
        Assert.AreEqual(user.Id, dto.UsuarioId);
    }
}
