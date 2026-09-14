using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
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
    private int _actorId;
    private int _tanqueId;
    private int _tipoId;
    private int _estacionId;
    private int _seq = 0;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        _service = new DashboardService(_db);

        var actor = new Usuario { NombreUsuario = "operador", PasswordHash = "x", Activo = true, SecurityVersion = 1 };
        _db.Usuarios.Add(actor);
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
        await _db.SaveChangesAsync();

        _db.Inventarios.Add(new Inventario
        {
            TanqueId = tanque.Id, ExistenciaActual = 5000m,
            Disponibilidad = 5000m, UltimaActualizacion = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _actorId = actor.Id;
        _tanqueId = tanque.Id;
        _tipoId = tipo.Id;
        _estacionId = estacion.Id;
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_db is not null) await _db.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }

    private async Task<(int DeptoId, int VehiculoId, int EmpleadoId)> CreateContextAsync(string deptoNombre, string placa)
    {
        var depto = new Departamento { Nombre = deptoNombre, Activo = true };
        _db.Departamentos.Add(depto);
        await _db.SaveChangesAsync();

        var n = ++_seq;
        var empleado = new Empleado
        {
            Codigo = $"E{n}", NombreCompleto = "Test", Cedula = $"C{n}",
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

    private async Task SeedDespachoAsync(int deptoId, int vehiculoId, int empleadoId, decimal galones)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(), NumeroSecuencial = ++_seq, Prefijo = "COM",
            FechaCreacion = DateTime.UtcNow, FechaVencimiento = DateTime.UtcNow.AddHours(8),
            Estado = EstadoTicket.Consumido, CantidadAutorizada = galones,
            HashSeguridad = "x", TokenValidacion = "x", FirmaDigital = "x", QrCodePng = [],
            TipoCombustibleId = _tipoId, EmpleadoId = empleadoId,
            VehiculoId = vehiculoId, DepartamentoId = deptoId
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        _db.Despachos.Add(new Despacho
        {
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow), Hora = TimeOnly.FromDateTime(DateTime.UtcNow),
            GalonesServidos = galones, TanqueId = _tanqueId, TicketId = ticket.Id,
            OperadorId = _actorId, EstacionId = _estacionId,
            InventarioRestante = 0m, DisponibilidadRestante = 0m
        });
        await _db.SaveChangesAsync();
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
        Assert.AreEqual(0, result.ConsumoPorDepartamento.Count);
        Assert.AreEqual(0, result.ConsumoPorVehiculo.Count);
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
        Assert.IsNotNull(result.ConsumoPorDepartamento);
        Assert.IsNotNull(result.ConsumoPorVehiculo);
    }

    [TestMethod]
    public async Task ConsumoPorDepartamento_AgrupaYOrdenaCorrectamente()
    {
        var (deptoLogId, vehLogId, empLogId) = await CreateContextAsync("Logística", "LOG-01");
        var (deptoItId, vehItId, empItId) = await CreateContextAsync("IT", "IT-001");

        await SeedDespachoAsync(deptoLogId, vehLogId, empLogId, 30m);
        await SeedDespachoAsync(deptoLogId, vehLogId, empLogId, 20m);
        await SeedDespachoAsync(deptoItId, vehItId, empItId, 10m);

        var result = await _service.GetResumenAsync(CancellationToken.None);

        Assert.AreEqual(2, result.ConsumoPorDepartamento.Count);
        Assert.AreEqual("Logística", result.ConsumoPorDepartamento[0].Departamento);
        Assert.AreEqual(50m, result.ConsumoPorDepartamento[0].TotalGalones);
        Assert.AreEqual("IT", result.ConsumoPorDepartamento[1].Departamento);
        Assert.AreEqual(10m, result.ConsumoPorDepartamento[1].TotalGalones);
    }

    [TestMethod]
    public async Task ConsumoPorVehiculo_AgrupaYOrdenaCorrectamente()
    {
        var (deptoId, veh1Id, empId) = await CreateContextAsync("Trans", "TRK-01");
        var veh2 = new Vehiculo
        {
            Placa = "TRK-02", Ficha = "F99", Marca = "Ford", Modelo = "F150",
            Año = 2021, Tipo = "Pickup", CapacidadTanque = 90m, Odometro = 0m,
            DepartamentoId = deptoId, Activo = true
        };
        _db.Vehiculos.Add(veh2);
        await _db.SaveChangesAsync();

        await SeedDespachoAsync(deptoId, veh1Id, empId, 15m);
        await SeedDespachoAsync(deptoId, veh2.Id, empId, 40m);
        await SeedDespachoAsync(deptoId, veh2.Id, empId, 10m);

        var result = await _service.GetResumenAsync(CancellationToken.None);

        Assert.AreEqual(2, result.ConsumoPorVehiculo.Count);
        Assert.AreEqual("TRK-02", result.ConsumoPorVehiculo[0].Placa);
        Assert.AreEqual(50m, result.ConsumoPorVehiculo[0].TotalGalones);
        Assert.AreEqual("TRK-01", result.ConsumoPorVehiculo[1].Placa);
        Assert.AreEqual(15m, result.ConsumoPorVehiculo[1].TotalGalones);
    }
}
