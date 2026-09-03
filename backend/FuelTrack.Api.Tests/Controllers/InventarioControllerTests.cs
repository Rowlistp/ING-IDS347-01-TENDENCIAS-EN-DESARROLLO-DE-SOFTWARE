using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Inventario;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class InventarioControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;

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
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private InventarioController CrearController(int usuarioId)
    {
        var controller = new InventarioController(_db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "Test"))
            }
        };
        return controller;
    }

    private async Task<(int tanqueId, int inventarioId, int usuarioId)> CrearDependenciasAsync(
        decimal existenciaActual = 500m)
    {
        var tipo = new TipoCombustible { Nombre = "Diesel", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var tanque = new Tanque
        {
            Identificacion = "T-002", Capacidad = 10000m, NivelActual = existenciaActual,
            NivelCritico = 1000m, Activo = true, TipoCombustibleId = tipo.Id
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        var inventario = new Inventario
        {
            TanqueId = tanque.Id, ExistenciaActual = existenciaActual,
            Disponibilidad = existenciaActual, UltimaActualizacion = DateTime.UtcNow
        };
        _db.Inventarios.Add(inventario);

        var usuario = new Usuario { NombreUsuario = "supervisor", PasswordHash = "hash", Activo = true };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return (tanque.Id, inventario.Id, usuario.Id);
    }

    // ── GETs ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_WhenNoData()
    {
        var ctrl = CrearController(1);
        var result = await ctrl.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<InventarioDto>;
        Assert.AreEqual(0, list!.Count);
    }

    [TestMethod]
    public async Task GetAll_ReturnsList_WithTankData()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);

        var result = await ctrl.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<InventarioDto>;

        Assert.AreEqual(1, list!.Count);
        Assert.AreEqual(tanqueId, list[0].TanqueId);
        Assert.AreEqual("T-002", list[0].TanqueIdentificacion);
        Assert.AreEqual(500m, list[0].ExistenciaActual);
    }

    [TestMethod]
    public async Task GetByTanque_ReturnsDto_WhenExists()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync(existenciaActual: 300m);
        var ctrl = CrearController(usuarioId);

        var result = await ctrl.GetByTanque(tanqueId, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as InventarioDto;

        Assert.IsNotNull(dto);
        Assert.AreEqual(tanqueId, dto.TanqueId);
        Assert.AreEqual("T-002", dto.TanqueIdentificacion);
        Assert.AreEqual(300m, dto.ExistenciaActual);
    }

    [TestMethod]
    public async Task GetByTanque_ReturnsNotFound_WhenMissing()
    {
        var (_, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);

        var result = await ctrl.GetByTanque(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    // ── Ajuste ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Ajustar_Returns200_AndUpdatesInventoryAndCreatesMovement()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync(existenciaActual: 500m);
        var ctrl = CrearController(usuarioId);
        var req = new AjustarInventarioRequest(tanqueId, -200m, "Corrección por medición física");

        var result = await ctrl.Ajustar(req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as InventarioDto;

        Assert.IsNotNull(ok);
        Assert.AreEqual(300m, dto!.ExistenciaActual);
        Assert.AreEqual(300m, dto.Disponibilidad);

        var movimiento = await _db.MovimientosInventario.FirstAsync();
        Assert.AreEqual(TipoMovimiento.Ajuste, movimiento.Tipo);
        Assert.AreEqual(-200m, movimiento.Volumen);
        Assert.AreEqual("Corrección por medición física", movimiento.Observaciones);
        Assert.AreEqual(usuarioId, movimiento.UsuarioId);
    }

    [TestMethod]
    public async Task Ajustar_Returns400_WhenTankNotFound()
    {
        var (_, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new AjustarInventarioRequest(999, 50m, "Test");

        var result = await ctrl.Ajustar(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Ajustar_Returns400_WhenTankInactive()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync();
        var tanque = await _db.Tanques.FindAsync(tanqueId);
        tanque!.Activo = false;
        await _db.SaveChangesAsync();
        var ctrl = CrearController(usuarioId);
        var req = new AjustarInventarioRequest(tanqueId, 50m, "Test inactivo");

        var result = await ctrl.Ajustar(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_INACTIVO"));
    }

    [TestMethod]
    public async Task Ajustar_Returns409_WhenInventoryInsufficient()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync(existenciaActual: 500m);
        var ctrl = CrearController(usuarioId);
        var req = new AjustarInventarioRequest(tanqueId, -600m, "Intento inválido");

        var result = await ctrl.Ajustar(req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("INVENTARIO_INSUFICIENTE"));
    }

    // ── Helpers para transferencias ───────────────────────────────────────────

    private async Task<int> AgregarSegundoTanqueAsync(int tipoCombustibleId, decimal existenciaActual = 0m)
    {
        var tanque2 = new Tanque
        {
            Identificacion = "T-003", Capacidad = 8000m, NivelActual = existenciaActual,
            NivelCritico = 800m, Activo = true, TipoCombustibleId = tipoCombustibleId
        };
        _db.Tanques.Add(tanque2);
        await _db.SaveChangesAsync();

        _db.Inventarios.Add(new Inventario
        {
            TanqueId = tanque2.Id, ExistenciaActual = existenciaActual,
            Disponibilidad = existenciaActual, UltimaActualizacion = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return tanque2.Id;
    }

    // ── Transferencias ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Transferir_Returns200_AndUpdatesBothInventories()
    {
        var (tanqueOrigenId, _, usuarioId) = await CrearDependenciasAsync(existenciaActual: 500m);
        var tipoCombustibleId = (await _db.TiposCombustible.FirstAsync()).Id;
        var tanqueDestinoId = await AgregarSegundoTanqueAsync(tipoCombustibleId, existenciaActual: 100m);
        var ctrl = CrearController(usuarioId);

        var req = new TransferirRequest(tanqueOrigenId, tanqueDestinoId, 200m, "Redistribución");
        var result = await ctrl.Transferir(req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as TransferenciaResultDto;

        Assert.IsNotNull(ok);
        Assert.AreEqual(300m, dto!.Origen.ExistenciaActual);
        Assert.AreEqual(300m, dto.Origen.Disponibilidad);
        Assert.AreEqual(300m, dto.Destino.ExistenciaActual);
        Assert.AreEqual(300m, dto.Destino.Disponibilidad);

        var movimientos = await _db.MovimientosInventario.ToListAsync();
        Assert.AreEqual(2, movimientos.Count);

        var movOrigen = movimientos.First(m => m.TanqueId == tanqueOrigenId);
        Assert.AreEqual(TipoMovimiento.Transferencia, movOrigen.Tipo);
        Assert.AreEqual(-200m, movOrigen.Volumen);
        Assert.IsTrue(movOrigen.ReferenciaOperacion!.Contains(tanqueDestinoId.ToString()));

        var movDestino = movimientos.First(m => m.TanqueId == tanqueDestinoId);
        Assert.AreEqual(TipoMovimiento.Transferencia, movDestino.Tipo);
        Assert.AreEqual(200m, movDestino.Volumen);
        Assert.IsTrue(movDestino.ReferenciaOperacion!.Contains(tanqueOrigenId.ToString()));
    }

    [TestMethod]
    public async Task Transferir_Returns400_WhenOriginEqualsDest()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new TransferirRequest(tanqueId, tanqueId, 100m, null);

        var result = await ctrl.Transferir(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_ORIGEN_IGUAL_DESTINO"));
    }

    [TestMethod]
    public async Task Transferir_Returns400_WhenOriginNotFound()
    {
        var (tanqueDestinoId, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new TransferirRequest(999, tanqueDestinoId, 100m, null);

        var result = await ctrl.Transferir(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_ORIGEN_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Transferir_Returns409_WhenOriginInsufficient()
    {
        var (tanqueOrigenId, _, usuarioId) = await CrearDependenciasAsync(existenciaActual: 100m);
        var tipoCombustibleId = (await _db.TiposCombustible.FirstAsync()).Id;
        var tanqueDestinoId = await AgregarSegundoTanqueAsync(tipoCombustibleId);
        var ctrl = CrearController(usuarioId);
        var req = new TransferirRequest(tanqueOrigenId, tanqueDestinoId, 500m, null);

        var result = await ctrl.Transferir(req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("INVENTARIO_INSUFICIENTE"));
    }
}
