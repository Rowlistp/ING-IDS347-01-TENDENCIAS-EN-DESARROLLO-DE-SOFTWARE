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
    private int _tipoId;
    private int _tanqueId;
    private int _estacionId;
    private int _operadorId;
    private int _seq;

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

        var tipo = new TipoCombustible { Nombre = "Diesel", Activo = true };
        _db.TiposCombustible.Add(tipo);
        var tanque = new Tanque
        {
            Identificacion = "TQ-001", Capacidad = 5000m, NivelActual = 0m,
            NivelCritico = 100m, Activo = true, TipoCombustible = tipo
        };
        _db.Tanques.Add(tanque);
        var estacion = new Estacion { Nombre = "Est-1", Activo = true };
        _db.Estaciones.Add(estacion);
        var operador = new Usuario { NombreUsuario = "operador", PasswordHash = "x", Activo = true, SecurityVersion = 1 };
        _db.Usuarios.Add(operador);
        await _db.SaveChangesAsync();

        _tipoId = tipo.Id;
        _tanqueId = tanque.Id;
        _estacionId = estacion.Id;
        _operadorId = operador.Id;
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<(int DeptoId, int VehiculoId, int EmpleadoId)> CreateContextAsync(string deptoNombre, string placa)
    {
        var depto = new Departamento { Nombre = deptoNombre, Activo = true };
        _db.Departamentos.Add(depto);
        await _db.SaveChangesAsync();

        var n = ++_seq;
        var empleado = new Empleado
        {
            Codigo = $"E{n}", NombreCompleto = $"Empleado {n}", Cedula = $"C{n}",
            Cargo = "Analista", Correo = $"e{n}@test.com", Telefono = "8090000000",
            DepartamentoId = depto.Id, Activo = true
        };
        var vehiculo = new Vehiculo
        {
            Placa = placa, Ficha = $"F{n}", Marca = "Toyota", Modelo = "Hilux",
            Año = 2022, Tipo = "Pickup", CapacidadTanque = 70m, Odometro = 0m,
            DepartamentoId = depto.Id, Activo = true
        };
        _db.Empleados.Add(empleado);
        _db.Vehiculos.Add(vehiculo);
        await _db.SaveChangesAsync();

        return (depto.Id, vehiculo.Id, empleado.Id);
    }

    private async Task<SolicitudCombustible> SeedSolicitudAsync(int deptoId, int vehiculoId, int empleadoId)
    {
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 10m, TipoSolicitud = "Normal", Estado = EstadoSolicitud.Pendiente,
            FechaSolicitud = DateTime.UtcNow, EmpleadoId = empleadoId, VehiculoId = vehiculoId,
            DepartamentoId = deptoId, TipoCombustibleId = _tipoId
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();
        return solicitud;
    }

    private async Task<Ticket> SeedTicketAsync(int deptoId, int vehiculoId, int empleadoId, decimal cantidad = 10m)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(), NumeroSecuencial = ++_seq, Prefijo = "COM",
            FechaCreacion = DateTime.UtcNow, FechaVencimiento = DateTime.UtcNow.AddHours(8),
            Estado = EstadoTicket.Creado, CantidadAutorizada = cantidad,
            HashSeguridad = "x", TokenValidacion = "x", FirmaDigital = "x", QrCodePng = [],
            TipoCombustibleId = _tipoId, EmpleadoId = empleadoId,
            VehiculoId = vehiculoId, DepartamentoId = deptoId
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();
        return ticket;
    }

    private async Task<Despacho> SeedDespachoAsync(int deptoId, int vehiculoId, int empleadoId, decimal galones = 10m)
    {
        var ticket = await SeedTicketAsync(deptoId, vehiculoId, empleadoId, galones);
        var despacho = new Despacho
        {
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow), Hora = TimeOnly.FromDateTime(DateTime.UtcNow),
            GalonesServidos = galones, TanqueId = _tanqueId, TicketId = ticket.Id,
            OperadorId = _operadorId, EstacionId = _estacionId,
            InventarioRestante = 0m, DisponibilidadRestante = 0m
        };
        _db.Despachos.Add(despacho);
        await _db.SaveChangesAsync();
        return despacho;
    }

    [TestMethod]
    public async Task Get_TipoInvalido_LanzaExcepcion()
    {
        var q = new ReporteQuery("invalido", null, null, null, null, null, null, 1, 20);
        var ex = await Assert.ThrowsExactlyAsync<TicketDomainException>(
            () => _service.GetAsync(q, CancellationToken.None));
        Assert.AreEqual(400, ex.StatusCode);
        Assert.AreEqual("TIPO_REPORTE_INVALIDO", ex.Code);
    }

    [TestMethod]
    public async Task Get_Solicitudes_DevuelveLista()
    {
        var q = new ReporteQuery("solicitudes", null, null, null, null, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);
        Assert.AreEqual("solicitudes", result.Tipo);
        Assert.AreEqual(0, result.Total);
    }

    [TestMethod]
    public async Task Get_Despachos_DevuelveLista()
    {
        var q = new ReporteQuery("despachos", null, null, null, null, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);
        Assert.AreEqual("despachos", result.Tipo);
    }

    [TestMethod]
    public async Task Get_Inventario_DevuelveLista()
    {
        var q = new ReporteQuery("inventario", null, null, null, null, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);
        Assert.AreEqual("inventario", result.Tipo);
    }

    [TestMethod]
    public async Task Get_Cierres_DevuelveLista()
    {
        var q = new ReporteQuery("cierres", null, null, null, null, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);
        Assert.AreEqual("cierres", result.Tipo);
    }

    [TestMethod]
    public async Task ExportarCsv_FormatoInvalido_LanzaExcepcion()
    {
        var q = new ReporteQuery("solicitudes", null, null, null, null, null, null, 1, 20);
        var ex = await Assert.ThrowsExactlyAsync<TicketDomainException>(
            () => _service.ExportarAsync(q, "xml", CancellationToken.None));
        Assert.AreEqual(400, ex.StatusCode);
        Assert.AreEqual("FORMATO_INVALIDO", ex.Code);
    }

    [TestMethod]
    public async Task ExportarCsv_Solicitudes_ContieneEncabezado()
    {
        var q = new ReporteQuery("solicitudes", null, null, null, null, null, null, 1, 20);
        var bytes = await _service.ExportarAsync(q, "csv", CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);
        Assert.IsTrue(csv.StartsWith("Id,FechaSolicitud"), $"CSV no empieza con encabezado. Actual: {csv[..Math.Min(50, csv.Length)]}");
    }

    [TestMethod]
    public async Task ExportarExcel_Solicitudes_ContieneHoja()
    {
        var q = new ReporteQuery("solicitudes", null, null, null, null, null, null, 1, 20);
        var bytes = await _service.ExportarAsync(q, "excel", CancellationToken.None);
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.IsTrue(wb.Worksheets.Any(ws => ws.Name == "Solicitudes"));
    }

    [TestMethod]
    public async Task ExportarPdf_Solicitudes_RetornaBytes()
    {
        var q = new ReporteQuery("solicitudes", null, null, null, null, null, null, 1, 20);
        var bytes = await _service.ExportarAsync(q, "pdf", CancellationToken.None);
        Assert.IsTrue(bytes.Length > 0);
        // PDF magic bytes: %PDF
        Assert.AreEqual(0x25, bytes[0]); // %
        Assert.AreEqual(0x50, bytes[1]); // P
    }

    // ── RF-19: filtros Empleado/Vehiculo/Departamento ──────────────────────

    [TestMethod]
    public async Task Get_Solicitudes_FiltraPorEmpleado()
    {
        var (depto1, veh1, emp1) = await CreateContextAsync("Logística", "LOG-01");
        var (depto2, veh2, emp2) = await CreateContextAsync("IT", "IT-001");
        await SeedSolicitudAsync(depto1, veh1, emp1);
        await SeedSolicitudAsync(depto2, veh2, emp2);

        var q = new ReporteQuery("solicitudes", null, null, null, emp1, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);

        Assert.AreEqual(1, result.Total);
        Assert.AreEqual("Empleado 1", ((SolicitudReporteDto)result.Items[0]).Empleado);
    }

    [TestMethod]
    public async Task Get_Solicitudes_FiltraPorVehiculo()
    {
        var (depto1, veh1, emp1) = await CreateContextAsync("Logística", "LOG-01");
        var (depto2, veh2, emp2) = await CreateContextAsync("IT", "IT-001");
        await SeedSolicitudAsync(depto1, veh1, emp1);
        await SeedSolicitudAsync(depto2, veh2, emp2);

        var q = new ReporteQuery("solicitudes", null, null, null, null, veh2, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);

        Assert.AreEqual(1, result.Total);
        Assert.AreEqual("IT-001", ((SolicitudReporteDto)result.Items[0]).Vehiculo);
    }

    [TestMethod]
    public async Task Get_Solicitudes_FiltraPorDepartamento()
    {
        var (depto1, veh1, emp1) = await CreateContextAsync("Logística", "LOG-01");
        var (depto2, veh2, emp2) = await CreateContextAsync("IT", "IT-001");
        await SeedSolicitudAsync(depto1, veh1, emp1);
        await SeedSolicitudAsync(depto2, veh2, emp2);

        var q = new ReporteQuery("solicitudes", null, null, null, null, null, depto1, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);

        Assert.AreEqual(1, result.Total);
        Assert.AreEqual("Logística", ((SolicitudReporteDto)result.Items[0]).Departamento);
    }

    [TestMethod]
    public async Task Get_Despachos_FiltraPorEmpleado()
    {
        var (depto1, veh1, emp1) = await CreateContextAsync("Logística", "LOG-01");
        var (depto2, veh2, emp2) = await CreateContextAsync("IT", "IT-001");
        await SeedDespachoAsync(depto1, veh1, emp1);
        await SeedDespachoAsync(depto2, veh2, emp2);

        var q = new ReporteQuery("despachos", null, null, null, emp2, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);

        Assert.AreEqual(1, result.Total);
        Assert.AreEqual("Empleado 2", ((DespachoReporteDto)result.Items[0]).Empleado);
    }

    [TestMethod]
    public async Task Get_Despachos_FiltraPorVehiculo()
    {
        var (depto1, veh1, emp1) = await CreateContextAsync("Logística", "LOG-01");
        var (depto2, veh2, emp2) = await CreateContextAsync("IT", "IT-001");
        await SeedDespachoAsync(depto1, veh1, emp1);
        await SeedDespachoAsync(depto2, veh2, emp2);

        var q = new ReporteQuery("despachos", null, null, null, null, veh1, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);

        Assert.AreEqual(1, result.Total);
        Assert.AreEqual("LOG-01", ((DespachoReporteDto)result.Items[0]).Vehiculo);
    }

    [TestMethod]
    public async Task Get_Despachos_FiltraPorDepartamento()
    {
        var (depto1, veh1, emp1) = await CreateContextAsync("Logística", "LOG-01");
        var (depto2, veh2, emp2) = await CreateContextAsync("IT", "IT-001");
        await SeedDespachoAsync(depto1, veh1, emp1);
        await SeedDespachoAsync(depto2, veh2, emp2);

        var q = new ReporteQuery("despachos", null, null, null, null, null, depto2, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);

        Assert.AreEqual(1, result.Total);
        Assert.AreEqual("IT-001", ((DespachoReporteDto)result.Items[0]).Vehiculo);
    }

    [TestMethod]
    public async Task Get_Inventario_IgnoraFiltrosEmpleadoVehiculoDepartamento()
    {
        var q = new ReporteQuery("inventario", null, null, null, 999, 999, 999, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);

        Assert.AreEqual("inventario", result.Tipo);
        Assert.AreEqual(0, result.Total);
    }

    [TestMethod]
    public async Task Get_Cierres_IgnoraFiltrosEmpleadoVehiculoDepartamento()
    {
        var q = new ReporteQuery("cierres", null, null, null, 999, 999, 999, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);

        Assert.AreEqual("cierres", result.Tipo);
        Assert.AreEqual(0, result.Total);
    }

    // ── RF-24: tipo de reporte "tickets" ────────────────────────────────────

    [TestMethod]
    public async Task Get_Tickets_TipoValido_NoLanzaExcepcion()
    {
        var q = new ReporteQuery("tickets", null, null, null, null, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);
        Assert.AreEqual("tickets", result.Tipo);
        Assert.AreEqual(0, result.Total);
    }

    [TestMethod]
    public async Task Get_Tickets_DevuelveListaEsperada()
    {
        var (depto1, veh1, emp1) = await CreateContextAsync("Logística", "LOG-01");
        var (depto2, veh2, emp2) = await CreateContextAsync("IT", "IT-001");
        var ticket1 = await SeedTicketAsync(depto1, veh1, emp1, 25m);
        await SeedTicketAsync(depto2, veh2, emp2, 15m);

        var q = new ReporteQuery("tickets", null, null, null, emp1, null, null, 1, 20);
        var result = await _service.GetAsync(q, CancellationToken.None);

        Assert.AreEqual("tickets", result.Tipo);
        Assert.AreEqual(1, result.Total);
        var dto = (TicketReporteDto)result.Items[0];
        Assert.AreEqual(ticket1.Id, dto.Id);
        Assert.AreEqual($"COM-{ticket1.FechaCreacion.Year}-{ticket1.NumeroSecuencial:000000}", dto.Codigo);
        Assert.AreEqual("Empleado 1", dto.Empleado);
        Assert.AreEqual("LOG-01", dto.Vehiculo);
        Assert.AreEqual("Logística", dto.Departamento);
        Assert.AreEqual(25m, dto.CantidadAutorizada);
    }

    [TestMethod]
    public async Task ExportarCsv_Tickets_ContieneEncabezado()
    {
        var q = new ReporteQuery("tickets", null, null, null, null, null, null, 1, 20);
        var bytes = await _service.ExportarAsync(q, "csv", CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);
        Assert.IsTrue(csv.StartsWith("Id,Codigo"), $"CSV no empieza con encabezado. Actual: {csv[..Math.Min(50, csv.Length)]}");
    }

    [TestMethod]
    public async Task ExportarExcel_Tickets_ContieneHoja()
    {
        var q = new ReporteQuery("tickets", null, null, null, null, null, null, 1, 20);
        var bytes = await _service.ExportarAsync(q, "excel", CancellationToken.None);
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.IsTrue(wb.Worksheets.Any(ws => ws.Name == "Tickets"));
    }

    [TestMethod]
    public async Task ExportarPdf_Tickets_RetornaBytes()
    {
        var q = new ReporteQuery("tickets", null, null, null, null, null, null, 1, 20);
        var bytes = await _service.ExportarAsync(q, "pdf", CancellationToken.None);
        Assert.IsTrue(bytes.Length > 0);
        Assert.AreEqual(0x25, bytes[0]);
        Assert.AreEqual(0x50, bytes[1]);
    }
}
