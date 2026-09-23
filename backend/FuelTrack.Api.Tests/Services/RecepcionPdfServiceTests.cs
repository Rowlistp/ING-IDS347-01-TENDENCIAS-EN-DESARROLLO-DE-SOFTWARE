using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Services;

[TestClass]
public sealed class RecepcionPdfServiceTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private RecepcionPdfService _service = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        _service = new RecepcionPdfService(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<RecepcionCombustible> CrearRecepcionAsync()
    {
        var dep = new Departamento { Nombre = "Logística", Activo = true };
        var tipo = new TipoCombustible { Nombre = "Diesel", Activo = true };
        _db.Departamentos.Add(dep);
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var tanque = new Tanque
        {
            Identificacion = "TNQ-01", Capacidad = 1000, NivelActual = 100, NivelCritico = 50,
            Activo = true, TipoCombustibleId = tipo.Id,
        };
        var proveedor = new Proveedor { Rnc = "131000001", Nombre = "Suplidora Test", Activo = true };
        _db.Tanques.Add(tanque);
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();

        var recepcion = new RecepcionCombustible
        {
            NumeroFactura = "FAC-0001", VolumenRecibido = 250.5m, Fecha = DateTime.UtcNow,
            ProveedorId = proveedor.Id, TanqueId = tanque.Id,
        };
        _db.RecepcionesCombustible.Add(recepcion);
        await _db.SaveChangesAsync();
        return recepcion;
    }

    [TestMethod]
    public async Task GetPdfAsync_RetornaNull_CuandoNoExiste()
    {
        var pdf = await _service.GetPdfAsync(999, CancellationToken.None);
        Assert.IsNull(pdf);
    }

    [TestMethod]
    public async Task GetPdfAsync_RetornaBytesPdf_CuandoExiste()
    {
        var recepcion = await CrearRecepcionAsync();

        var pdf = await _service.GetPdfAsync(recepcion.Id, CancellationToken.None);

        Assert.IsNotNull(pdf);
        Assert.IsTrue(pdf.Length > 0);
        // Firma estándar de un archivo PDF: "%PDF-"
        Assert.AreEqual("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [TestMethod]
    public async Task GetPdfAsync_CacheaElPdf_YReutilizaEnLaSegundaLlamada()
    {
        var recepcion = await CrearRecepcionAsync();

        var primero = await _service.GetPdfAsync(recepcion.Id, CancellationToken.None);
        var guardado = await _db.RecepcionesCombustible.AsNoTracking()
            .Where(r => r.Id == recepcion.Id).Select(r => r.PdfComprobante).SingleAsync();
        var segundo = await _service.GetPdfAsync(recepcion.Id, CancellationToken.None);

        Assert.IsNotNull(guardado);
        CollectionAssert.AreEqual(primero, guardado);
        CollectionAssert.AreEqual(primero, segundo);
    }
}
