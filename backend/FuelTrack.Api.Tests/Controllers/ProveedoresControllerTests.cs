using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Proveedores;
using FuelTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class ProveedoresControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private ProveedoresController _controller = null!;

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
        _controller = new ProveedoresController(_db);
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
        var list = ok!.Value as List<ProveedorDto>;
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
        var proveedor = new Proveedor { Rnc = "101-12345-6", Nombre = "Petrobras RD", Activo = true };
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(proveedor.Id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as ProveedorDto;
        Assert.AreEqual("101-12345-6", dto!.Rnc);
        Assert.AreEqual("Petrobras RD", dto.Nombre);
    }

    [TestMethod]
    public async Task Create_Returns201_ConDto()
    {
        var req = new SaveProveedorRequest("101-12345-6", "Petrobras RD");
        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as ProveedorDto;
        Assert.AreEqual("101-12345-6", dto!.Rnc);
        Assert.IsTrue(dto.Activo);
    }

    [TestMethod]
    public async Task Create_Returns409_CuandoRncDuplicado()
    {
        _db.Proveedores.Add(new Proveedor { Rnc = "101-12345-6", Nombre = "Petrobras RD", Activo = true });
        await _db.SaveChangesAsync();

        var req = new SaveProveedorRequest("101-12345-6", "Otro Proveedor");
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Update_Returns200_ConDtoActualizado()
    {
        var proveedor = new Proveedor { Rnc = "101-12345-6", Nombre = "Petrobras RD", Activo = true };
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();

        var req = new SaveProveedorRequest("202-99999-1", "Shell RD", false);
        var result = await _controller.Update(proveedor.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as ProveedorDto;
        Assert.AreEqual("202-99999-1", dto!.Rnc);
        Assert.AreEqual("Shell RD", dto.Nombre);
        Assert.IsFalse(dto.Activo);
    }

    [TestMethod]
    public async Task Update_Returns404_CuandoNoExiste()
    {
        var req = new SaveProveedorRequest("101-12345-6", "Petrobras RD");
        var result = await _controller.Update(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Update_Returns409_CuandoRncDuplicadoEnOtroRegistro()
    {
        _db.Proveedores.AddRange(
            new Proveedor { Rnc = "101-12345-6", Nombre = "Petrobras RD", Activo = true },
            new Proveedor { Rnc = "202-99999-1", Nombre = "Shell RD", Activo = true });
        await _db.SaveChangesAsync();
        var petrobras = await _db.Proveedores.FirstAsync(p => p.Rnc == "101-12345-6");

        var req = new SaveProveedorRequest("202-99999-1", "Petrobras Renombrada");
        var result = await _controller.Update(petrobras.Id, req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Deactivate_PoneActivoEnFalse()
    {
        var proveedor = new Proveedor { Rnc = "101-12345-6", Nombre = "Petrobras RD", Activo = true };
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(proveedor.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);

        await _db.Entry(proveedor).ReloadAsync();
        Assert.IsFalse(proveedor.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns404_CuandoNoExiste()
    {
        var result = await _controller.Deactivate(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }
}
