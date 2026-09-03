using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Tanques;
using FuelTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class TanquesControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private TanquesController _controller = null!;

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
        _controller = new TanquesController(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<TipoCombustible> CrearTipoCombustibleAsync(string nombre = "Gasolina")
    {
        var tipo = new TipoCombustible { Nombre = nombre, Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();
        return tipo;
    }

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_WhenNoData()
    {
        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<TanqueDto>;
        Assert.AreEqual(0, list!.Count);
    }

    [TestMethod]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _controller.GetById(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Create_Returns201_YCreaInventarioEnCero()
    {
        var tipo = await CrearTipoCombustibleAsync();
        var req = new SaveTanqueRequest("T-01", 5000m, 500m, tipo.Id);

        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);

        var dto = created.Value as TanqueDto;
        Assert.AreEqual("T-01", dto!.Identificacion);
        Assert.AreEqual(5000m, dto.Capacidad);
        Assert.AreEqual(0m, dto.NivelActual);
        Assert.IsTrue(dto.Activo);

        // Verificar que se creó el Inventario en cero
        var inventario = await _db.Inventarios.FirstOrDefaultAsync(i => i.TanqueId == dto.Id);
        Assert.IsNotNull(inventario);
        Assert.AreEqual(0m, inventario.ExistenciaActual);
        Assert.AreEqual(0m, inventario.Disponibilidad);
    }

    [TestMethod]
    public async Task Create_Returns409_CuandoIdentificacionDuplicada()
    {
        var tipo = await CrearTipoCombustibleAsync();
        _db.Tanques.Add(new Tanque
        {
            Identificacion = "T-01", Capacidad = 5000m,
            NivelActual = 0, NivelCritico = 500m,
            TipoCombustibleId = tipo.Id, Activo = true
        });
        await _db.SaveChangesAsync();

        var req = new SaveTanqueRequest("T-01", 3000m, 300m, tipo.Id);
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoTipoCombustibleNoExiste()
    {
        var req = new SaveTanqueRequest("T-01", 5000m, 500m, 999);
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Update_Returns200_ConDatosActualizados()
    {
        var tipo = await CrearTipoCombustibleAsync();
        var tanque = new Tanque
        {
            Identificacion = "T-01", Capacidad = 5000m,
            NivelActual = 0, NivelCritico = 500m,
            TipoCombustibleId = tipo.Id, Activo = true
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        var req = new SaveTanqueRequest("T-01-MOD", 6000m, 600m, tipo.Id);
        var result = await _controller.Update(tanque.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as TanqueDto;
        Assert.AreEqual("T-01-MOD", dto!.Identificacion);
        Assert.AreEqual(6000m, dto.Capacidad);
    }

    [TestMethod]
    public async Task Update_Returns404_CuandoNoExiste()
    {
        var tipo = await CrearTipoCombustibleAsync();
        var req = new SaveTanqueRequest("T-99", 1000m, 100m, tipo.Id);
        var result = await _controller.Update(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Deactivate_PoneActivoEnFalse()
    {
        var tipo = await CrearTipoCombustibleAsync();
        var tanque = new Tanque
        {
            Identificacion = "T-01", Capacidad = 5000m,
            NivelActual = 0, NivelCritico = 500m,
            TipoCombustibleId = tipo.Id, Activo = true
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(tanque.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);

        await _db.Entry(tanque).ReloadAsync();
        Assert.IsFalse(tanque.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns404_CuandoNoExiste()
    {
        var result = await _controller.Deactivate(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Update_Returns200_CuandoTipoCombustibleCambia()
    {
        var gasolina = await CrearTipoCombustibleAsync("Gasolina");
        var diesel = await CrearTipoCombustibleAsync("Diesel");
        var tanque = new Tanque
        {
            Identificacion = "T-01", Capacidad = 5000m,
            NivelActual = 0, NivelCritico = 500m,
            TipoCombustibleId = gasolina.Id, Activo = true
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        var req = new SaveTanqueRequest("T-01", 5000m, 500m, diesel.Id);
        var result = await _controller.Update(tanque.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as TanqueDto;
        Assert.AreEqual(diesel.Id, dto!.TipoCombustibleId);
        Assert.AreEqual("Diesel", dto.TipoCombustibleNombre);
    }

    [TestMethod]
    public async Task Update_Returns400_CuandoTipoCombustibleNoExiste()
    {
        var tipo = await CrearTipoCombustibleAsync();
        var tanque = new Tanque
        {
            Identificacion = "T-01", Capacidad = 5000m,
            NivelActual = 0, NivelCritico = 500m,
            TipoCombustibleId = tipo.Id, Activo = true
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        var req = new SaveTanqueRequest("T-01", 5000m, 500m, 999);
        var result = await _controller.Update(tanque.Id, req, CancellationToken.None);
        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
    }
}
