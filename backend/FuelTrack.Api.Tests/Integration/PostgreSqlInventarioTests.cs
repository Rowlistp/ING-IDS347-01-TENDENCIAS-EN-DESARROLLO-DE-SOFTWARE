using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Inventario;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;

namespace FuelTrack.Api.Tests.Integration;

// Require FUELTRACK_TEST_CONNECTION pointing at a *test* PostgreSQL database.
// Skipped automatically (Inconclusive) when the variable is absent, just like the
// other PostgreSQL integration tests in this folder.
[TestClass]
[TestCategory("PostgreSQL")]
public sealed class PostgreSqlInventarioTests
{
    private string _connectionString = null!;
    private bool _canDestroyDatabase;

    [TestInitialize]
    public async Task Setup()
    {
        _connectionString = Environment.GetEnvironmentVariable("FUELTRACK_TEST_CONNECTION") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_connectionString))
            Assert.Inconclusive(
                "Defina FUELTRACK_TEST_CONNECTION apuntando exclusivamente a una base PostgreSQL de pruebas.");

        var databaseName = new NpgsqlConnectionStringBuilder(_connectionString).Database;
        if (string.IsNullOrWhiteSpace(databaseName) ||
            !databaseName.Contains("test", StringComparison.OrdinalIgnoreCase))
            Assert.Fail("La prueba destructiva solo acepta una base cuyo nombre contenga 'test'.");

        _canDestroyDatabase = true;
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (!_canDestroyDatabase) return;
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }

    // Regression test: new DateTime(year, month, 1) produces Kind=Unspecified which
    // Npgsql rejects on timestamptz columns. This test fails against PostgreSQL before
    // the fix and passes after, while the SQLite suite never catches it.
    [TestMethod]
    public async Task GetAll_ConsumoDiario_ConDateTimeKindUtc_NoLanza500EnPostgreSQL()
    {
        int tanqueId;
        await using (var db = CreateContext())
        {
            var (tid, _) = await SeedTanqueConSalidaAsync(db, 40m);
            tanqueId = tid;
        }

        await using var dbRead = CreateContext();
        var ctrl = new InventarioController(dbRead, new AuditService(dbRead));
        var result = await ctrl.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<InventarioDto>;

        Assert.IsNotNull(list, "GET /inventario devolvió null — probable 500");
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(40m, list[0].ConsumoDiario, "consumoDiario debe reflejar la salida del día");
        Assert.AreEqual(40m, list[0].ConsumoMensual, "consumoMensual debe reflejar la salida del mes");
    }

    [TestMethod]
    public async Task GetByTanque_ConsumoDiario_ConDateTimeKindUtc_NoLanza500EnPostgreSQL()
    {
        int tanqueId;
        await using (var db = CreateContext())
        {
            var (tid, _) = await SeedTanqueConSalidaAsync(db, 25m);
            tanqueId = tid;
        }

        await using var dbRead = CreateContext();
        var ctrl = new InventarioController(dbRead, new AuditService(dbRead));
        var result = await ctrl.GetByTanque(tanqueId, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as InventarioDto;

        Assert.IsNotNull(dto, "GET /inventario/{id} devolvió null — probable 500");
        Assert.AreEqual(25m, dto.ConsumoDiario);
        Assert.AreEqual(25m, dto.ConsumoMensual);
    }

    // Verifica que POST /inventario/ajustes no deja datos huérfanos si el cálculo
    // de consumo fallaba: ahora el consumo se calcula ANTES del SaveChangesAsync.
    [TestMethod]
    public async Task Ajustar_GuardaUnaVezYRetornaConsumoCorrectamente()
    {
        int tanqueId, usuarioId;
        await using (var db = CreateContext())
        {
            var (tid, uid) = await SeedTanqueConSalidaAsync(db, 30m);
            tanqueId = tid;
            usuarioId = uid;
        }

        await using var dbOp = CreateContext();
        var ctrl = CrearControllerConUsuario(dbOp, usuarioId);

        var req = new AjustarInventarioRequest(tanqueId, -50m, "Corrección física");
        var result = await ctrl.Ajustar(req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as InventarioDto;

        Assert.IsNotNull(dto, "POST /inventario/ajustes devolvió null — probable 500");
        Assert.AreEqual(450m, dto.ExistenciaActual, "existencia debe reflejar el ajuste");
        Assert.AreEqual(30m, dto.ConsumoDiario, "consumo es de salidas anteriores, no del ajuste");

        // Verificar que el ajuste se guardó EXACTAMENTE UNA VEZ
        await using var verify = CreateContext();
        var ajustes = await verify.MovimientosInventario
            .Where(m => m.TanqueId == tanqueId && m.Tipo == TipoMovimiento.Ajuste)
            .ToListAsync();
        Assert.AreEqual(1, ajustes.Count, "El ajuste debe guardarse una sola vez (no duplicado por retry)");
        Assert.AreEqual(-50m, ajustes[0].Volumen);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<(int TanqueId, int UsuarioId)> SeedTanqueConSalidaAsync(AppDbContext db, decimal galonesSalida)
    {
        var tipo = new TipoCombustible { Nombre = "Diesel PG", Activo = true };
        db.TiposCombustible.Add(tipo);
        var tanque = new Tanque
        {
            Identificacion = "T-PG", Capacidad = 5000m, NivelActual = 0m,
            NivelCritico = 200m, TipoCombustible = tipo, Activo = true
        };
        db.Tanques.Add(tanque);
        var usuario = new Usuario { NombreUsuario = "pg-operador", PasswordHash = "x", Activo = true };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        db.Inventarios.Add(new Inventario
        {
            TanqueId = tanque.Id, ExistenciaActual = 500m,
            Disponibilidad = 500m, UltimaActualizacion = DateTime.UtcNow
        });
        db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Salida, Volumen = -galonesSalida,
            TanqueId = tanque.Id, UsuarioId = usuario.Id,
            FechaHora = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return (tanque.Id, usuario.Id);
    }

    private static InventarioController CrearControllerConUsuario(AppDbContext db, int usuarioId)
    {
        var ctrl = new InventarioController(db, new AuditService(db));
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())], "Test"))
            }
        };
        return ctrl;
    }

    private AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);
}
