using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.TiposCombustible;
using FuelTrack.Api.Models;
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
        _controller = new TiposCombustibleController(_db);
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
}
