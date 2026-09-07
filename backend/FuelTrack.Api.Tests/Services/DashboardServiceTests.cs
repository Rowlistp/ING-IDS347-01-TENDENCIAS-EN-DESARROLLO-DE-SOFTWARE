using FuelTrack.Api.Data;
using FuelTrack.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Services;

[TestClass]
public sealed class DashboardServiceTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private DashboardService _service = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        _service = new DashboardService(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [TestMethod]
    public async Task GetResumen_SinDatos_DevuelveCeros()
    {
        var result = await _service.GetResumenAsync(CancellationToken.None);

        Assert.AreEqual(0, result.Hoy.TotalDespachos);
        Assert.AreEqual(0m, result.Hoy.VolumenDespachado);
        Assert.AreEqual(0, result.Hoy.SolicitudesPendientes);
        Assert.AreEqual(0, result.Hoy.TanquesConInventarioBajo);
        Assert.AreEqual(7, result.Ultimos7Dias.Count);
        Assert.AreEqual(0, result.Top3TanquesMasUsados.Count);
        Assert.AreEqual(0m, result.ComparativaMes.MesActual.VolumenDespachado);
        Assert.AreEqual(0m, result.ComparativaMes.MesAnterior.VolumenDespachado);
        Assert.AreEqual(0, result.DistribucionPorTipoCombustible.Count);
        Assert.AreEqual(0m, result.EficienciaAprobacion.TasaAprobacion);
    }

    [TestMethod]
    public async Task GetResumen_DevuelveEstructuraCompleta()
    {
        var result = await _service.GetResumenAsync(CancellationToken.None);

        Assert.IsNotNull(result.Hoy);
        Assert.IsNotNull(result.Ultimos7Dias);
        Assert.IsNotNull(result.Top3TanquesMasUsados);
        Assert.IsNotNull(result.ComparativaMes);
        Assert.IsNotNull(result.DistribucionPorTipoCombustible);
        Assert.IsNotNull(result.EficienciaAprobacion);
    }
}
