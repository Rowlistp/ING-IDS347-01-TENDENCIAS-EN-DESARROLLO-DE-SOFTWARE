using System.Text;
using ClosedXML.Excel;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Reportes;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace FuelTrack.Api.Tests.Services;

[TestClass]
public sealed class ReporteServiceTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private ReporteService _service = null!;

    [TestInitialize]
    public async Task Setup()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        _service = new ReporteService(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [TestMethod]
    public async Task Get_TipoInvalido_LanzaExcepcion()
    {
        var q = new ReporteQuery("invalido", null, null, null, 1, 20);
        var ex = await Assert.ThrowsExactlyAsync<TicketDomainException>(
            () => _service.GetAsync(q, CancellationToken.None));
        Assert.AreEqual(400, ex.StatusCode);
        Assert.AreEqual("TIPO_REPORTE_INVALIDO", ex.Code);
    }

    [TestMethod]
    public async Task Get_Solicitudes_DevuelveLista()
    {
        var q = new ReporteQuery("solicitudes", null, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);
        Assert.AreEqual("solicitudes", result.Tipo);
        Assert.AreEqual(0, result.Total);
    }

    [TestMethod]
    public async Task Get_Despachos_DevuelveLista()
    {
        var q = new ReporteQuery("despachos", null, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);
        Assert.AreEqual("despachos", result.Tipo);
    }

    [TestMethod]
    public async Task Get_Inventario_DevuelveLista()
    {
        var q = new ReporteQuery("inventario", null, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);
        Assert.AreEqual("inventario", result.Tipo);
    }

    [TestMethod]
    public async Task Get_Cierres_DevuelveLista()
    {
        var q = new ReporteQuery("cierres", null, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);
        Assert.AreEqual("cierres", result.Tipo);
    }

    [TestMethod]
    public async Task ExportarCsv_FormatoInvalido_LanzaExcepcion()
    {
        var q = new ReporteQuery("solicitudes", null, null, null, 1, 20);
        var ex = await Assert.ThrowsExactlyAsync<TicketDomainException>(
            () => _service.ExportarAsync(q, "xml", CancellationToken.None));
        Assert.AreEqual(400, ex.StatusCode);
        Assert.AreEqual("FORMATO_INVALIDO", ex.Code);
    }

    [TestMethod]
    public async Task ExportarCsv_Solicitudes_ContieneEncabezado()
    {
        var q = new ReporteQuery("solicitudes", null, null, null, 1, 20);
        var bytes = await _service.ExportarAsync(q, "csv", CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);
        Assert.IsTrue(csv.StartsWith("Id,FechaSolicitud"), $"CSV no empieza con encabezado. Actual: {csv[..Math.Min(50, csv.Length)]}");
    }

    [TestMethod]
    public async Task ExportarExcel_Solicitudes_ContieneHoja()
    {
        var q = new ReporteQuery("solicitudes", null, null, null, 1, 20);
        var bytes = await _service.ExportarAsync(q, "excel", CancellationToken.None);
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.IsTrue(wb.Worksheets.Any(ws => ws.Name == "Solicitudes"));
    }

    [TestMethod]
    public async Task ExportarPdf_Solicitudes_RetornaBytes()
    {
        var q = new ReporteQuery("solicitudes", null, null, null, 1, 20);
        var bytes = await _service.ExportarAsync(q, "pdf", CancellationToken.None);
        Assert.IsTrue(bytes.Length > 0);
        // PDF magic bytes: %PDF
        Assert.AreEqual(0x25, bytes[0]); // %
        Assert.AreEqual(0x50, bytes[1]); // P
    }
}
