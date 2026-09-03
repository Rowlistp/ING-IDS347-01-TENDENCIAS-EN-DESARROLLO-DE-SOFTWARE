# Fase 6 — Inventario Completo: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar RF-14 a RF-17: recepciones de combustible, ajustes, transferencias entre tanques e historial de movimientos, con cascade automático a `MovimientoInventario` e `Inventario` en cada operación.

**Architecture:** Tres controladores nuevos (`RecepcionesController`, `InventarioController`, `MovimientosController`) que acceden `AppDbContext` directamente — sin capa de servicio, igual que el resto del repo. Cada operación de escritura crea un `MovimientoInventario` y actualiza `Inventario` en la misma transacción (`SaveChangesAsync`). No se requieren migraciones — todas las tablas existen desde `InitialSchema`.

**Tech Stack:** .NET 10 Web API, EF Core 9 + Npgsql (PostgreSQL), SQLite in-memory (tests), MSTest 4.3, JWT auth (`ClaimTypes.NameIdentifier` para UsuarioId).

---

## Contexto crítico para el implementador

- **No GlobalUsings**: cada archivo necesita sus propios `using`.
- **Patrón de error**: `new { code = "CODIGO", message = "Texto." }` — ambos campos siempre.
- **Obtener usuario del JWT**: `int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)` — igual que `AuthController.cs`.
- **Invariante clave**: todo `Tanque` tiene exactamente un `Inventario` asociado (creado en `TanquesController`). Si el `Tanque` existe, su `Inventario` existe. Cargar con `Include(t => t.Inventario)`.
- **Volumen es signed**: en `MovimientoInventario.Volumen` nunca se usa `Math.Abs()`. Negativo = reducción (ajuste negativo o origen de transferencia). Positivo = incremento.
- **TipoMovimiento enum**: almacenado como `integer` en BD — `Entrada=0, Salida=1, Ajuste=2, Transferencia=3, Merma=4`.
- **Tests con ControllerContext**: los 3 controladores nuevos usan `User.FindFirstValue()`, por lo que los tests necesitan `controller.ControllerContext` con un `ClaimsPrincipal`.
- **Tests — Inventario explícito**: en los helpers de test, `Inventario` se inserta directo al `DbContext` (no pasa por el `TanquesController`).

---

## Estructura de archivos

```
Crear:
  backend/FuelTrack.Api/DTOs/Recepciones/CreateRecepcionRequest.cs
  backend/FuelTrack.Api/DTOs/Recepciones/RecepcionDto.cs
  backend/FuelTrack.Api/DTOs/Inventario/InventarioDto.cs
  backend/FuelTrack.Api/DTOs/Inventario/AjustarInventarioRequest.cs
  backend/FuelTrack.Api/DTOs/Inventario/TransferirRequest.cs
  backend/FuelTrack.Api/DTOs/Inventario/TransferenciaResultDto.cs
  backend/FuelTrack.Api/DTOs/Movimientos/MovimientoDto.cs
  backend/FuelTrack.Api/Controllers/RecepcionesController.cs
  backend/FuelTrack.Api/Controllers/InventarioController.cs
  backend/FuelTrack.Api/Controllers/MovimientosController.cs
  backend/FuelTrack.Api.Tests/Controllers/RecepcionesControllerTests.cs
  backend/FuelTrack.Api.Tests/Controllers/InventarioControllerTests.cs
  backend/FuelTrack.Api.Tests/Controllers/MovimientosControllerTests.cs
```

---

## Task 1: DTOs

**Files:**
- Create: `backend/FuelTrack.Api/DTOs/Recepciones/CreateRecepcionRequest.cs`
- Create: `backend/FuelTrack.Api/DTOs/Recepciones/RecepcionDto.cs`
- Create: `backend/FuelTrack.Api/DTOs/Inventario/InventarioDto.cs`
- Create: `backend/FuelTrack.Api/DTOs/Inventario/AjustarInventarioRequest.cs`
- Create: `backend/FuelTrack.Api/DTOs/Inventario/TransferirRequest.cs`
- Create: `backend/FuelTrack.Api/DTOs/Inventario/TransferenciaResultDto.cs`
- Create: `backend/FuelTrack.Api/DTOs/Movimientos/MovimientoDto.cs`

- [ ] **Step 1: Crear DTOs de Recepciones**

`backend/FuelTrack.Api/DTOs/Recepciones/CreateRecepcionRequest.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Recepciones;

public record CreateRecepcionRequest(
    [Required] int ProveedorId,
    [Required] int TanqueId,
    [Required, MaxLength(100)] string NumeroFactura,
    [Required, Range(0.0001, 999999.9999)] decimal VolumenRecibido,
    [Required] DateTime Fecha
);
```

`backend/FuelTrack.Api/DTOs/Recepciones/RecepcionDto.cs`:
```csharp
namespace FuelTrack.Api.DTOs.Recepciones;

public record RecepcionDto(
    int Id,
    string NumeroFactura,
    decimal VolumenRecibido,
    DateTime Fecha,
    int ProveedorId, string ProveedorNombre,
    int TanqueId, string TanqueIdentificacion
);
```

- [ ] **Step 2: Crear DTOs de Inventario**

`backend/FuelTrack.Api/DTOs/Inventario/InventarioDto.cs`:
```csharp
namespace FuelTrack.Api.DTOs.Inventario;

public record InventarioDto(
    int Id,
    decimal ExistenciaActual,
    decimal Disponibilidad,
    DateTime UltimaActualizacion,
    int TanqueId, string TanqueIdentificacion, decimal TanqueCapacidad
);
```

`backend/FuelTrack.Api/DTOs/Inventario/AjustarInventarioRequest.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Inventario;

public record AjustarInventarioRequest(
    [Required] int TanqueId,
    decimal Volumen,
    [Required, MaxLength(500)] string Observaciones
);
```

`backend/FuelTrack.Api/DTOs/Inventario/TransferirRequest.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Inventario;

public record TransferirRequest(
    [Required] int TanqueOrigenId,
    [Required] int TanqueDestinoId,
    [Required, Range(0.0001, 999999.9999)] decimal Volumen,
    [MaxLength(500)] string? Observaciones
);
```

`backend/FuelTrack.Api/DTOs/Inventario/TransferenciaResultDto.cs`:
```csharp
namespace FuelTrack.Api.DTOs.Inventario;

public record TransferenciaResultDto(
    InventarioDto Origen,
    InventarioDto Destino
);
```

- [ ] **Step 3: Crear DTO de Movimientos**

`backend/FuelTrack.Api/DTOs/Movimientos/MovimientoDto.cs`:
```csharp
using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Movimientos;

public record MovimientoDto(
    int Id,
    TipoMovimiento Tipo,
    decimal Volumen,
    DateTime FechaHora,
    string? ReferenciaOperacion,
    string? Observaciones,
    int TanqueId, string TanqueIdentificacion,
    int UsuarioId, string UsuarioNombreUsuario
);
```

- [ ] **Step 4: Verificar build**

```
dotnet build backend/FuelTrack.Api/FuelTrack.Api.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```
git add backend/FuelTrack.Api/DTOs/
git commit -m "feat: DTOs Fase 6 (Recepciones, Inventario, Movimientos)"
```

---

## Task 2: Controller stubs

**Files:**
- Create: `backend/FuelTrack.Api/Controllers/RecepcionesController.cs`
- Create: `backend/FuelTrack.Api/Controllers/InventarioController.cs`
- Create: `backend/FuelTrack.Api/Controllers/MovimientosController.cs`

- [ ] **Step 1: Crear stub RecepcionesController**

`backend/FuelTrack.Api/Controllers/RecepcionesController.cs`:
```csharp
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Recepciones;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/recepciones")]
[Authorize]
public sealed class RecepcionesController : ControllerBase
{
    private readonly AppDbContext _db;
    public RecepcionesController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<RecepcionDto>>> GetAll(CancellationToken ct)
        => throw new NotImplementedException();

    [HttpGet("{id:int}")]
    public Task<ActionResult<RecepcionDto>> GetById(int id, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<RecepcionDto>> Create(CreateRecepcionRequest req, CancellationToken ct)
        => throw new NotImplementedException();
}
```

- [ ] **Step 2: Crear stub InventarioController**

`backend/FuelTrack.Api/Controllers/InventarioController.cs`:
```csharp
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Inventario;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/inventario")]
[Authorize]
public sealed class InventarioController : ControllerBase
{
    private readonly AppDbContext _db;
    public InventarioController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<InventarioDto>>> GetAll(CancellationToken ct)
        => throw new NotImplementedException();

    [HttpGet("{tanqueId:int}")]
    public Task<ActionResult<InventarioDto>> GetByTanque(int tanqueId, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost("ajustes")]
    [Authorize(Roles = Roles.Administrador)]
    public Task<ActionResult<InventarioDto>> Ajustar(AjustarInventarioRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost("transferencias")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<TransferenciaResultDto>> Transferir(TransferirRequest req, CancellationToken ct)
        => throw new NotImplementedException();
}
```

- [ ] **Step 3: Crear stub MovimientosController**

`backend/FuelTrack.Api/Controllers/MovimientosController.cs`:
```csharp
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Movimientos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/inventario/movimientos")]
[Authorize]
public sealed class MovimientosController : ControllerBase
{
    private readonly AppDbContext _db;
    public MovimientosController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<MovimientoDto>>> GetAll([FromQuery] int? tanqueId, CancellationToken ct)
        => throw new NotImplementedException();
}
```

- [ ] **Step 4: Verificar build**

```
dotnet build backend/FuelTrack.Api/FuelTrack.Api.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```
git add backend/FuelTrack.Api/Controllers/RecepcionesController.cs
git add backend/FuelTrack.Api/Controllers/InventarioController.cs
git add backend/FuelTrack.Api/Controllers/MovimientosController.cs
git commit -m "feat: stubs RecepcionesController, InventarioController, MovimientosController"
```

---

## Task 3: RecepcionesController — TDD completo

**Files:**
- Create: `backend/FuelTrack.Api.Tests/Controllers/RecepcionesControllerTests.cs`
- Modify: `backend/FuelTrack.Api/Controllers/RecepcionesController.cs`

- [ ] **Step 1: Crear archivo de tests con los 6 tests (todos fallarán)**

`backend/FuelTrack.Api.Tests/Controllers/RecepcionesControllerTests.cs`:
```csharp
using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Recepciones;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class RecepcionesControllerTests
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

    private RecepcionesController CrearController(int usuarioId)
    {
        var controller = new RecepcionesController(_db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "Test"))
            }
        };
        return controller;
    }

    private async Task<(int proveedorId, int tanqueId, int usuarioId)> CrearDependenciasAsync()
    {
        var tipo = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var tanque = new Tanque
        {
            Identificacion = "T-001", Capacidad = 5000m, NivelActual = 0m,
            NivelCritico = 500m, Activo = true, TipoCombustibleId = tipo.Id
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        _db.Inventarios.Add(new Inventario
        {
            TanqueId = tanque.Id, ExistenciaActual = 0m,
            Disponibilidad = 0m, UltimaActualizacion = DateTime.UtcNow
        });
        _db.Proveedores.Add(new Proveedor { Nombre = "Petro SA", Rnc = "101234567", Activo = true });
        _db.Usuarios.Add(new Usuario { NombreUsuario = "admin", PasswordHash = "hash", Activo = true });
        await _db.SaveChangesAsync();

        var proveedor = await _db.Proveedores.FirstAsync();
        var usuario = await _db.Usuarios.FirstAsync();
        return (proveedor.Id, tanque.Id, usuario.Id);
    }

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_WhenNoData()
    {
        var ctrl = CrearController(1);
        var result = await ctrl.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<RecepcionDto>;
        Assert.AreEqual(0, list!.Count);
    }

    [TestMethod]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var ctrl = CrearController(1);
        var result = await ctrl.GetById(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Create_Returns201_YActualizaInventarioYCreaMovimiento()
    {
        var (proveedorId, tanqueId, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new CreateRecepcionRequest(proveedorId, tanqueId, "FAC-001", 200m, DateTime.UtcNow);

        var result = await ctrl.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;

        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as RecepcionDto;
        Assert.AreEqual("FAC-001", dto!.NumeroFactura);
        Assert.AreEqual(200m, dto.VolumenRecibido);
        Assert.AreEqual("Petro SA", dto.ProveedorNombre);
        Assert.AreEqual("T-001", dto.TanqueIdentificacion);

        var inventario = await _db.Inventarios.FirstAsync(i => i.TanqueId == tanqueId);
        Assert.AreEqual(200m, inventario.ExistenciaActual);
        Assert.AreEqual(200m, inventario.Disponibilidad);

        var movimiento = await _db.MovimientosInventario.FirstAsync();
        Assert.AreEqual(TipoMovimiento.Entrada, movimiento.Tipo);
        Assert.AreEqual(200m, movimiento.Volumen);
        Assert.AreEqual("FAC-001", movimiento.ReferenciaOperacion);
        Assert.AreEqual(tanqueId, movimiento.TanqueId);
        Assert.AreEqual(usuarioId, movimiento.UsuarioId);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoProveedorNoExiste()
    {
        var (_, tanqueId, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new CreateRecepcionRequest(999, tanqueId, "FAC-002", 100m, DateTime.UtcNow);

        var result = await ctrl.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("PROVEEDOR_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoTanqueNoExiste()
    {
        var (proveedorId, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new CreateRecepcionRequest(proveedorId, 999, "FAC-003", 100m, DateTime.UtcNow);

        var result = await ctrl.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoTanqueInactivo()
    {
        var (proveedorId, tanqueId, usuarioId) = await CrearDependenciasAsync();
        var tanque = await _db.Tanques.FindAsync(tanqueId);
        tanque!.Activo = false;
        await _db.SaveChangesAsync();

        var ctrl = CrearController(usuarioId);
        var req = new CreateRecepcionRequest(proveedorId, tanqueId, "FAC-004", 100m, DateTime.UtcNow);

        var result = await ctrl.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_INACTIVO"));
    }
}
```

- [ ] **Step 2: Correr tests y verificar que TODOS fallan**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj --filter "FullyQualifiedName~RecepcionesControllerTests"
```

Expected: 6 tests FAIL (NotImplementedException o similar). Si alguno pasa, hay un problema.

- [ ] **Step 3: Implementar RecepcionesController completo**

Reemplazar completamente `backend/FuelTrack.Api/Controllers/RecepcionesController.cs`:
```csharp
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Recepciones;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/recepciones")]
[Authorize]
public sealed class RecepcionesController : ControllerBase
{
    private readonly AppDbContext _db;
    public RecepcionesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<RecepcionDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.RecepcionesCombustible
            .AsNoTracking()
            .Include(r => r.Proveedor)
            .Include(r => r.Tanque)
            .ToListAsync(ct);
        return Ok(list.ConvertAll(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecepcionDto>> GetById(int id, CancellationToken ct)
    {
        var r = await _db.RecepcionesCombustible
            .AsNoTracking()
            .Include(r => r.Proveedor)
            .Include(r => r.Tanque)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        return r is null ? NotFound() : Ok(ToDto(r));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<RecepcionDto>> Create(CreateRecepcionRequest req, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        if (!await _db.Proveedores.AnyAsync(p => p.Id == req.ProveedorId, ct))
            return BadRequest(new { code = "PROVEEDOR_NOT_FOUND", message = "El proveedor no existe." });

        var tanque = await _db.Tanques
            .Include(t => t.Inventario)
            .FirstOrDefaultAsync(t => t.Id == req.TanqueId, ct);

        if (tanque is null)
            return BadRequest(new { code = "TANQUE_NOT_FOUND", message = "El tanque no existe." });
        if (!tanque.Activo)
            return BadRequest(new { code = "TANQUE_INACTIVO", message = "El tanque no está activo." });

        var recepcion = new RecepcionCombustible
        {
            NumeroFactura = req.NumeroFactura,
            VolumenRecibido = req.VolumenRecibido,
            Fecha = req.Fecha,
            ProveedorId = req.ProveedorId,
            TanqueId = req.TanqueId
        };
        _db.RecepcionesCombustible.Add(recepcion);

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Entrada,
            Volumen = req.VolumenRecibido,
            FechaHora = DateTime.UtcNow,
            ReferenciaOperacion = req.NumeroFactura,
            TanqueId = req.TanqueId,
            UsuarioId = usuarioId
        });

        tanque.Inventario!.ExistenciaActual += req.VolumenRecibido;
        tanque.Inventario.Disponibilidad += req.VolumenRecibido;
        tanque.Inventario.UltimaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _db.Entry(recepcion).Reference(r => r.Proveedor).LoadAsync(ct);
        await _db.Entry(recepcion).Reference(r => r.Tanque).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = recepcion.Id }, ToDto(recepcion));
    }

    private static RecepcionDto ToDto(RecepcionCombustible r) => new(
        r.Id,
        r.NumeroFactura,
        r.VolumenRecibido,
        r.Fecha,
        r.ProveedorId, r.Proveedor.Nombre,
        r.TanqueId, r.Tanque.Identificacion);
}
```

- [ ] **Step 4: Correr tests y verificar que TODOS pasan**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj --filter "FullyQualifiedName~RecepcionesControllerTests"
```

Expected: 6/6 PASS.

- [ ] **Step 5: Correr suite completa para verificar no hay regresiones**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj
```

Expected: todos los tests existentes siguen pasando.

- [ ] **Step 6: Commit**

```
git add backend/FuelTrack.Api.Tests/Controllers/RecepcionesControllerTests.cs
git add backend/FuelTrack.Api/Controllers/RecepcionesController.cs
git commit -m "feat: RecepcionesController con cascade inventario (6/6 tests)"
```

---

## Task 4: InventarioController — GETs y Ajuste TDD

**Files:**
- Create: `backend/FuelTrack.Api.Tests/Controllers/InventarioControllerTests.cs`
- Modify: `backend/FuelTrack.Api/Controllers/InventarioController.cs`

- [ ] **Step 1: Crear tests para GET y Ajuste (5 tests, todos deben fallar)**

`backend/FuelTrack.Api.Tests/Controllers/InventarioControllerTests.cs`:
```csharp
using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Inventario;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class InventarioControllerTests
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

    private InventarioController CrearController(int usuarioId)
    {
        var controller = new InventarioController(_db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "Test"))
            }
        };
        return controller;
    }

    private async Task<(int tanqueId, int inventarioId, int usuarioId)> CrearDependenciasAsync(
        decimal existenciaActual = 500m)
    {
        var tipo = new TipoCombustible { Nombre = "Diesel", Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();

        var tanque = new Tanque
        {
            Identificacion = "T-002", Capacidad = 10000m, NivelActual = existenciaActual,
            NivelCritico = 1000m, Activo = true, TipoCombustibleId = tipo.Id
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        var inventario = new Inventario
        {
            TanqueId = tanque.Id, ExistenciaActual = existenciaActual,
            Disponibilidad = existenciaActual, UltimaActualizacion = DateTime.UtcNow
        };
        _db.Inventarios.Add(inventario);

        var usuario = new Usuario { NombreUsuario = "supervisor", PasswordHash = "hash", Activo = true };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return (tanque.Id, inventario.Id, usuario.Id);
    }

    // ── GETs ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAll_ReturnsList_ConDatosTanque()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);

        var result = await ctrl.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<InventarioDto>;

        Assert.AreEqual(1, list!.Count);
        Assert.AreEqual(tanqueId, list[0].TanqueId);
        Assert.AreEqual("T-002", list[0].TanqueIdentificacion);
        Assert.AreEqual(500m, list[0].ExistenciaActual);
    }

    [TestMethod]
    public async Task GetByTanque_ReturnsNotFound_WhenMissing()
    {
        var (_, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);

        var result = await ctrl.GetByTanque(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    // ── Ajuste ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Ajustar_Returns200_YActualizaInventarioYCreaMovimiento()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync(existenciaActual: 500m);
        var ctrl = CrearController(usuarioId);
        var req = new AjustarInventarioRequest(tanqueId, -200m, "Corrección por medición física");

        var result = await ctrl.Ajustar(req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as InventarioDto;

        Assert.IsNotNull(ok);
        Assert.AreEqual(300m, dto!.ExistenciaActual);
        Assert.AreEqual(300m, dto.Disponibilidad);

        var movimiento = await _db.MovimientosInventario.FirstAsync();
        Assert.AreEqual(TipoMovimiento.Ajuste, movimiento.Tipo);
        Assert.AreEqual(-200m, movimiento.Volumen);
        Assert.AreEqual("Corrección por medición física", movimiento.Observaciones);
        Assert.AreEqual(usuarioId, movimiento.UsuarioId);
    }

    [TestMethod]
    public async Task Ajustar_Returns400_CuandoTanqueNoExiste()
    {
        var (_, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new AjustarInventarioRequest(999, 50m, "Test");

        var result = await ctrl.Ajustar(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Ajustar_Returns409_CuandoInventarioInsuficiente()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync(existenciaActual: 500m);
        var ctrl = CrearController(usuarioId);
        var req = new AjustarInventarioRequest(tanqueId, -600m, "Intento inválido");

        var result = await ctrl.Ajustar(req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("INVENTARIO_INSUFICIENTE"));
    }
}
```

- [ ] **Step 2: Correr tests y verificar que TODOS fallan**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj --filter "FullyQualifiedName~InventarioControllerTests"
```

Expected: 5 tests FAIL (NotImplementedException).

- [ ] **Step 3: Implementar GETs y Ajuste en InventarioController**

Reemplazar `backend/FuelTrack.Api/Controllers/InventarioController.cs` con el siguiente contenido (los métodos `Transferir` todavía lanzan `NotImplementedException`):
```csharp
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Inventario;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/inventario")]
[Authorize]
public sealed class InventarioController : ControllerBase
{
    private readonly AppDbContext _db;
    public InventarioController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<InventarioDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Inventarios
            .AsNoTracking()
            .Include(i => i.Tanque)
            .ToListAsync(ct);
        return Ok(list.ConvertAll(ToDto));
    }

    [HttpGet("{tanqueId:int}")]
    public async Task<ActionResult<InventarioDto>> GetByTanque(int tanqueId, CancellationToken ct)
    {
        var inv = await _db.Inventarios
            .AsNoTracking()
            .Include(i => i.Tanque)
            .FirstOrDefaultAsync(i => i.TanqueId == tanqueId, ct);
        return inv is null ? NotFound() : Ok(ToDto(inv));
    }

    [HttpPost("ajustes")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<InventarioDto>> Ajustar(AjustarInventarioRequest req, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        var tanque = await _db.Tanques
            .Include(t => t.Inventario)
            .FirstOrDefaultAsync(t => t.Id == req.TanqueId, ct);

        if (tanque is null)
            return BadRequest(new { code = "TANQUE_NOT_FOUND", message = "El tanque no existe." });
        if (!tanque.Activo)
            return BadRequest(new { code = "TANQUE_INACTIVO", message = "El tanque no está activo." });

        var inventario = tanque.Inventario!;
        if (inventario.ExistenciaActual + req.Volumen < 0)
            return Conflict(new { code = "INVENTARIO_INSUFICIENTE", message = "El ajuste dejaría el inventario en negativo." });

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Ajuste,
            Volumen = req.Volumen,
            FechaHora = DateTime.UtcNow,
            Observaciones = req.Observaciones,
            TanqueId = req.TanqueId,
            UsuarioId = usuarioId
        });

        inventario.ExistenciaActual += req.Volumen;
        inventario.Disponibilidad += req.Volumen;
        inventario.UltimaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _db.Entry(inventario).Reference(i => i.Tanque).LoadAsync(ct);

        return Ok(ToDto(inventario));
    }

    [HttpPost("transferencias")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<TransferenciaResultDto>> Transferir(TransferirRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    private static InventarioDto ToDto(Inventario i) => new(
        i.Id,
        i.ExistenciaActual,
        i.Disponibilidad,
        i.UltimaActualizacion,
        i.TanqueId, i.Tanque.Identificacion, i.Tanque.Capacidad);
}
```

- [ ] **Step 4: Correr solo los 5 tests de GET y Ajuste**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj --filter "FullyQualifiedName~InventarioControllerTests"
```

Expected: 5/5 PASS.

- [ ] **Step 5: Suite completa sin regresiones**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj
```

Expected: todos los tests existentes siguen pasando.

- [ ] **Step 6: Commit**

```
git add backend/FuelTrack.Api.Tests/Controllers/InventarioControllerTests.cs
git add backend/FuelTrack.Api/Controllers/InventarioController.cs
git commit -m "feat: InventarioController GETs + Ajuste con cascade (5/5 tests)"
```

---

## Task 5: InventarioController — Transferencias TDD

**Files:**
- Modify: `backend/FuelTrack.Api.Tests/Controllers/InventarioControllerTests.cs` (añadir 4 tests)
- Modify: `backend/FuelTrack.Api/Controllers/InventarioController.cs` (implementar Transferir)

- [ ] **Step 1: Añadir helper de segundo tanque y 4 tests de transferencia al final de InventarioControllerTests**

Al final de la clase `InventarioControllerTests`, después del último test existente, añadir:

```csharp
    // ── Helpers para transferencias ───────────────────────────────────────────

    private async Task<int> AgregarSegundoTanqueAsync(int tipoCombustibleId, decimal existenciaActual = 0m)
    {
        var tanque2 = new Tanque
        {
            Identificacion = "T-003", Capacidad = 8000m, NivelActual = existenciaActual,
            NivelCritico = 800m, Activo = true, TipoCombustibleId = tipoCombustibleId
        };
        _db.Tanques.Add(tanque2);
        await _db.SaveChangesAsync();

        _db.Inventarios.Add(new Inventario
        {
            TanqueId = tanque2.Id, ExistenciaActual = existenciaActual,
            Disponibilidad = existenciaActual, UltimaActualizacion = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return tanque2.Id;
    }

    // ── Transferencias ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Transferir_Returns200_YActualizaAmbosInventarios()
    {
        var (tanqueOrigenId, _, usuarioId) = await CrearDependenciasAsync(existenciaActual: 500m);
        var tipoCombustibleId = (await _db.TiposCombustible.FirstAsync()).Id;
        var tanqueDestinoId = await AgregarSegundoTanqueAsync(tipoCombustibleId, existenciaActual: 100m);
        var ctrl = CrearController(usuarioId);

        var req = new TransferirRequest(tanqueOrigenId, tanqueDestinoId, 200m, "Redistribución");
        var result = await ctrl.Transferir(req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as TransferenciaResultDto;

        Assert.IsNotNull(ok);
        Assert.AreEqual(300m, dto!.Origen.ExistenciaActual);
        Assert.AreEqual(300m, dto.Origen.Disponibilidad);
        Assert.AreEqual(300m, dto.Destino.ExistenciaActual);
        Assert.AreEqual(300m, dto.Destino.Disponibilidad);

        var movimientos = await _db.MovimientosInventario.ToListAsync();
        Assert.AreEqual(2, movimientos.Count);

        var movOrigen = movimientos.First(m => m.TanqueId == tanqueOrigenId);
        Assert.AreEqual(TipoMovimiento.Transferencia, movOrigen.Tipo);
        Assert.AreEqual(-200m, movOrigen.Volumen);
        Assert.IsTrue(movOrigen.ReferenciaOperacion!.Contains(tanqueDestinoId.ToString()));

        var movDestino = movimientos.First(m => m.TanqueId == tanqueDestinoId);
        Assert.AreEqual(TipoMovimiento.Transferencia, movDestino.Tipo);
        Assert.AreEqual(200m, movDestino.Volumen);
        Assert.IsTrue(movDestino.ReferenciaOperacion!.Contains(tanqueOrigenId.ToString()));
    }

    [TestMethod]
    public async Task Transferir_Returns400_CuandoOrigenIgualDestino()
    {
        var (tanqueId, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new TransferirRequest(tanqueId, tanqueId, 100m, null);

        var result = await ctrl.Transferir(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_ORIGEN_IGUAL_DESTINO"));
    }

    [TestMethod]
    public async Task Transferir_Returns400_CuandoTanqueOrigenNoExiste()
    {
        var (tanqueDestinoId, _, usuarioId) = await CrearDependenciasAsync();
        var ctrl = CrearController(usuarioId);
        var req = new TransferirRequest(999, tanqueDestinoId, 100m, null);

        var result = await ctrl.Transferir(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TANQUE_ORIGEN_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Transferir_Returns409_CuandoOrigenInsuficiente()
    {
        var (tanqueOrigenId, _, usuarioId) = await CrearDependenciasAsync(existenciaActual: 100m);
        var tipoCombustibleId = (await _db.TiposCombustible.FirstAsync()).Id;
        var tanqueDestinoId = await AgregarSegundoTanqueAsync(tipoCombustibleId);
        var ctrl = CrearController(usuarioId);
        var req = new TransferirRequest(tanqueOrigenId, tanqueDestinoId, 500m, null);

        var result = await ctrl.Transferir(req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("INVENTARIO_INSUFICIENTE"));
    }
```

- [ ] **Step 2: Correr los 4 tests nuevos y verificar que fallan**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj --filter "FullyQualifiedName~InventarioControllerTests.Transferir"
```

Expected: 4 tests FAIL (NotImplementedException).

- [ ] **Step 3: Implementar Transferir en InventarioController**

En `backend/FuelTrack.Api/Controllers/InventarioController.cs`, reemplazar el stub del método `Transferir` con:

```csharp
    [HttpPost("transferencias")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TransferenciaResultDto>> Transferir(TransferirRequest req, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        if (req.TanqueOrigenId == req.TanqueDestinoId)
            return BadRequest(new { code = "TANQUE_ORIGEN_IGUAL_DESTINO", message = "El tanque de origen y destino no pueden ser el mismo." });

        var tanqueOrigen = await _db.Tanques
            .Include(t => t.Inventario)
            .FirstOrDefaultAsync(t => t.Id == req.TanqueOrigenId, ct);
        if (tanqueOrigen is null)
            return BadRequest(new { code = "TANQUE_ORIGEN_NOT_FOUND", message = "El tanque de origen no existe." });
        if (!tanqueOrigen.Activo)
            return BadRequest(new { code = "TANQUE_ORIGEN_INACTIVO", message = "El tanque de origen no está activo." });

        var tanqueDestino = await _db.Tanques
            .Include(t => t.Inventario)
            .FirstOrDefaultAsync(t => t.Id == req.TanqueDestinoId, ct);
        if (tanqueDestino is null)
            return BadRequest(new { code = "TANQUE_DESTINO_NOT_FOUND", message = "El tanque de destino no existe." });
        if (!tanqueDestino.Activo)
            return BadRequest(new { code = "TANQUE_DESTINO_INACTIVO", message = "El tanque de destino no está activo." });

        var inventarioOrigen = tanqueOrigen.Inventario!;
        var inventarioDestino = tanqueDestino.Inventario!;

        if (inventarioOrigen.ExistenciaActual < req.Volumen)
            return Conflict(new { code = "INVENTARIO_INSUFICIENTE", message = "El tanque de origen no tiene suficiente combustible." });

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Transferencia,
            Volumen = -req.Volumen,
            FechaHora = DateTime.UtcNow,
            ReferenciaOperacion = $"HACIA-TANQUE-{req.TanqueDestinoId}",
            Observaciones = req.Observaciones,
            TanqueId = req.TanqueOrigenId,
            UsuarioId = usuarioId
        });

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            Tipo = TipoMovimiento.Transferencia,
            Volumen = req.Volumen,
            FechaHora = DateTime.UtcNow,
            ReferenciaOperacion = $"DESDE-TANQUE-{req.TanqueOrigenId}",
            Observaciones = req.Observaciones,
            TanqueId = req.TanqueDestinoId,
            UsuarioId = usuarioId
        });

        inventarioOrigen.ExistenciaActual -= req.Volumen;
        inventarioOrigen.Disponibilidad -= req.Volumen;
        inventarioOrigen.UltimaActualizacion = DateTime.UtcNow;

        inventarioDestino.ExistenciaActual += req.Volumen;
        inventarioDestino.Disponibilidad += req.Volumen;
        inventarioDestino.UltimaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _db.Entry(inventarioOrigen).Reference(i => i.Tanque).LoadAsync(ct);
        await _db.Entry(inventarioDestino).Reference(i => i.Tanque).LoadAsync(ct);

        return Ok(new TransferenciaResultDto(ToDto(inventarioOrigen), ToDto(inventarioDestino)));
    }
```

- [ ] **Step 4: Correr todos los tests de InventarioController**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj --filter "FullyQualifiedName~InventarioControllerTests"
```

Expected: 9/9 PASS.

- [ ] **Step 5: Suite completa sin regresiones**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj
```

Expected: todos los tests existentes siguen pasando.

- [ ] **Step 6: Commit**

```
git add backend/FuelTrack.Api.Tests/Controllers/InventarioControllerTests.cs
git add backend/FuelTrack.Api/Controllers/InventarioController.cs
git commit -m "feat: InventarioController Transferencias completas (9/9 tests)"
```

---

## Task 6: MovimientosController — TDD completo

**Files:**
- Create: `backend/FuelTrack.Api.Tests/Controllers/MovimientosControllerTests.cs`
- Modify: `backend/FuelTrack.Api/Controllers/MovimientosController.cs`

- [ ] **Step 1: Crear tests para MovimientosController (2 tests, deben fallar)**

`backend/FuelTrack.Api.Tests/Controllers/MovimientosControllerTests.cs`:
```csharp
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
    public async Task GetAll_SinFiltro_ReturnsTodosLosMovimientos()
    {
        var (_, _, _) = await CrearDependenciasConMovimientosAsync();
        var ctrl = CrearController();

        var result = await ctrl.GetAll(null, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<MovimientoDto>;

        Assert.AreEqual(2, list!.Count);
    }

    [TestMethod]
    public async Task GetAll_ConFiltroTanqueId_ReturnsSoloMovimientosDelTanque()
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
```

- [ ] **Step 2: Correr tests y verificar que fallan**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj --filter "FullyQualifiedName~MovimientosControllerTests"
```

Expected: 2 tests FAIL (NotImplementedException).

- [ ] **Step 3: Implementar MovimientosController completo**

Reemplazar `backend/FuelTrack.Api/Controllers/MovimientosController.cs`:
```csharp
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Movimientos;
using FuelTrack.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/inventario/movimientos")]
[Authorize]
public sealed class MovimientosController : ControllerBase
{
    private readonly AppDbContext _db;
    public MovimientosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MovimientoDto>>> GetAll(
        [FromQuery] int? tanqueId, CancellationToken ct)
    {
        var query = _db.MovimientosInventario
            .AsNoTracking()
            .Include(m => m.Tanque)
            .Include(m => m.Usuario)
            .AsQueryable();

        if (tanqueId.HasValue)
            query = query.Where(m => m.TanqueId == tanqueId.Value);

        var list = await query.ToListAsync(ct);
        return Ok(list.ConvertAll(ToDto));
    }

    private static MovimientoDto ToDto(MovimientoInventario m) => new(
        m.Id,
        m.Tipo,
        m.Volumen,
        m.FechaHora,
        m.ReferenciaOperacion,
        m.Observaciones,
        m.TanqueId, m.Tanque.Identificacion,
        m.UsuarioId, m.Usuario.NombreUsuario);
}
```

- [ ] **Step 4: Correr tests de MovimientosController**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj --filter "FullyQualifiedName~MovimientosControllerTests"
```

Expected: 2/2 PASS.

- [ ] **Step 5: Suite completa — verificar total de tests**

```
dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj
```

Expected: todos los tests pasan. El total debe incluir los 16 de SolicitudesController + 6 de Recepciones + 9 de Inventario + 2 de Movimientos = 33 tests.

- [ ] **Step 6: Commit**

```
git add backend/FuelTrack.Api.Tests/Controllers/MovimientosControllerTests.cs
git add backend/FuelTrack.Api/Controllers/MovimientosController.cs
git commit -m "feat: MovimientosController con filtro tanqueId (2/2 tests)"
```
