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
    public async Task GetAll_ReturnsList_ConDatosTanque()
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
    public async Task GetByTanque_ReturnsNotFound_WhenMissing()
    {
        var (_, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);

        var result = await ctrl.GetByTanque(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    // ── Ajuste ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Ajustar_Returns200_YActualizaInventarioYCreaMovimiento()
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
    public async Task Ajustar_Returns400_CuandoTanqueNoExiste()
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
    public async Task Ajustar_Returns409_CuandoInventarioInsuficiente()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync(existenciaActual: 500m);
        var ctrl = CrearController(usuarioId);
        var req = new AjustarInventarioRequest(tanqueId, -600m, "Intento inválido");

        var result = await ctrl.Ajustar(req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("INVENTARIO_INSUFICIENTE"));
    }
}
