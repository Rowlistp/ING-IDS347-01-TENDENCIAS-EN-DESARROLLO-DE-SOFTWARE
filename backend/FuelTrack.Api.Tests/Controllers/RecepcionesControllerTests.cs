using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Recepciones;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class RecepcionesControllerTests
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

    private RecepcionesController CrearController(int usuarioId)
    {
        var controller = new RecepcionesController(_db);
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

    private async Task<(int proveedorId, int tanqueId, int usuarioId)> CrearDependenciasAsync()
    {
        var tipo = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var tanque = new Tanque
        {
            Identificacion = "T-001", Capacidad = 5000m, NivelActual = 0m,
            NivelCritico = 500m, Activo = true, TipoCombustibleId = tipo.Id
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        _db.Inventarios.Add(new Inventario
        {
            TanqueId = tanque.Id, ExistenciaActual = 0m,
            Disponibilidad = 0m, UltimaActualizacion = DateTime.UtcNow
        });
        _db.Proveedores.Add(new Proveedor { Nombre = "Petro SA", Rnc = "101234567", Activo = true });
        _db.Usuarios.Add(new Usuario { NombreUsuario = "admin", PasswordHash = "hash", Activo = true });
        await _db.SaveChangesAsync();

        var proveedor = await _db.Proveedores.FirstAsync();
        var usuario = await _db.Usuarios.FirstAsync();
        return (proveedor.Id, tanque.Id, usuario.Id);
    }

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_WhenNoData()
    {
        var ctrl = CrearController(1);
        var result = await ctrl.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<RecepcionDto>;
        Assert.AreEqual(0, list!.Count);
    }

    [TestMethod]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var ctrl = CrearController(1);
        var result = await ctrl.GetById(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Create_Returns201_YActualizaInventarioYCreaMovimiento()
    {
        var (proveedorId, tanqueId, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new CreateRecepcionRequest(proveedorId, tanqueId, "FAC-001", 200m, DateTime.UtcNow);

        var result = await ctrl.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;

        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as RecepcionDto;
        Assert.AreEqual("FAC-001", dto!.NumeroFactura);
        Assert.AreEqual(200m, dto.VolumenRecibido);
        Assert.AreEqual("Petro SA", dto.ProveedorNombre);
        Assert.AreEqual("T-001", dto.TanqueIdentificacion);

        var inventario = await _db.Inventarios.FirstAsync(i => i.TanqueId == tanqueId);
        Assert.AreEqual(200m, inventario.ExistenciaActual);
        Assert.AreEqual(200m, inventario.Disponibilidad);

        var movimiento = await _db.MovimientosInventario.FirstAsync();
        Assert.AreEqual(TipoMovimiento.Entrada, movimiento.Tipo);
        Assert.AreEqual(200m, movimiento.Volumen);
        Assert.AreEqual("FAC-001", movimiento.ReferenciaOperacion);
        Assert.AreEqual(tanqueId, movimiento.TanqueId);
        Assert.AreEqual(usuarioId, movimiento.UsuarioId);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoProveedorNoExiste()
    {
        var (_, tanqueId, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new CreateRecepcionRequest(999, tanqueId, "FAC-002", 100m, DateTime.UtcNow);

        var result = await ctrl.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("PROVEEDOR_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoTanqueNoExiste()
    {
        var (proveedorId, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new CreateRecepcionRequest(proveedorId, 999, "FAC-003", 100m, DateTime.UtcNow);

        var result = await ctrl.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoTanqueInactivo()
    {
        var (proveedorId, tanqueId, usuarioId) = await CrearDependenciasAsync();
        var tanque = await _db.Tanques.FindAsync(tanqueId);
        tanque!.Activo = false;
        await _db.SaveChangesAsync();

        var ctrl = CrearController(usuarioId);
        var req = new CreateRecepcionRequest(proveedorId, tanqueId, "FAC-004", 100m, DateTime.UtcNow);

        var result = await ctrl.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_INACTIVO"));
    }
}
