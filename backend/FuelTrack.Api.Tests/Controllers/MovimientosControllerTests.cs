using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Movimientos;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class MovimientosControllerTests
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

    private MovimientosController CrearController()
    {
        var controller = new MovimientosController(_db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "1") }, "Test"))
            }
        };
        return controller;
    }

    private async Task<(int tanque1Id, int tanque2Id, int usuarioId)> CrearDependenciasConMovimientosAsync()
    {
        var tipo = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var tanque1 = new Tanque
        {
            Identificacion = "T-A", Capacidad = 5000m, NivelActual = 0m,
            NivelCritico = 500m, Activo = true, TipoCombustibleId = tipo.Id
        };
        var tanque2 = new Tanque
        {
            Identificacion = "T-B", Capacidad = 5000m, NivelActual = 0m,
            NivelCritico = 500m, Activo = true, TipoCombustibleId = tipo.Id
        };
        _db.Tanques.AddRange(tanque1, tanque2);
        await _db.SaveChangesAsync();

        var usuario = new Usuario { NombreUsuario = "operador", PasswordHash = "hash", Activo = true };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Entrada, Volumen = 300m,
            FechaHora = DateTime.UtcNow, ReferenciaOperacion = "FAC-X",
            TanqueId = tanque1.Id, UsuarioId = usuario.Id
        });
        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Ajuste, Volumen = -50m,
            FechaHora = DateTime.UtcNow, Observaciones = "Merma",
            TanqueId = tanque2.Id, UsuarioId = usuario.Id
        });
        await _db.SaveChangesAsync();

        return (tanque1.Id, tanque2.Id, usuario.Id);
    }

    [TestMethod]
    public async Task GetAll_WithoutFilter_ReturnsAllMovements()
    {
        await CrearDependenciasConMovimientosAsync();
        var ctrl = CrearController();

        var result = await ctrl.GetAll(null, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<MovimientoDto>;

        Assert.AreEqual(2, list!.Count);
    }

    [TestMethod]
    public async Task GetAll_WithTanqueIdFilter_ReturnsOnlyMatchingMovements()
    {
        var (tanque1Id, _, _) = await CrearDependenciasConMovimientosAsync();
        var ctrl = CrearController();

        var result = await ctrl.GetAll(tanque1Id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<MovimientoDto>;

        Assert.AreEqual(1, list!.Count);
        Assert.AreEqual(tanque1Id, list[0].TanqueId);
        Assert.AreEqual(TipoMovimiento.Entrada, list[0].Tipo);
        Assert.AreEqual(300m, list[0].Volumen);
        Assert.AreEqual("T-A", list[0].TanqueIdentificacion);
        Assert.AreEqual("operador", list[0].UsuarioNombreUsuario);
    }
}
