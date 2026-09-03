# Paso 1 + Bloque A — Contrato API y Catálogos de Inventario

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Actualizar `06-API.md` con rutas en español y agregar CRUD completo de `TiposCombustible`, `Tanques` y `Proveedores` — los catálogos base que el módulo de inventario necesita.

**Architecture:** Tres controladores que acceden directamente a `AppDbContext` sin capa de servicio, siguiendo el patrón de Fase 2 (Departamentos/Empleados/Vehículos). Soft delete en todos. Una migración agrega `Activo` a `Tanques` y `Proveedores`, más índices únicos en `Proveedores.Rnc` y `TiposCombustible.Nombre`. El POST de Tanque crea su `Inventario` en cero en la misma transacción.

**Tech Stack:** .NET 10 Web API, EF Core 9 + Npgsql en producción, EF Core 9 + SQLite en memoria para tests, MSTest 4.

---

## Mapa de archivos

**Modificar:**
- `docs/06-API.md` — rutas en inglés → español + nuevos endpoints
- `backend/FuelTrack.Api/Models/Tanque.cs` — agregar `bool Activo`
- `backend/FuelTrack.Api/Models/Proveedor.cs` — agregar `bool Activo`
- `backend/FuelTrack.Api/Data/AppDbContext.cs` — agregar índices únicos

**Crear:**
- `backend/FuelTrack.Api/Migrations/<timestamp>_AddActivoTanqueProveedorIndices.cs` — generado por EF
- `backend/FuelTrack.Api/DTOs/TiposCombustible/TipoCombustibleDto.cs`
- `backend/FuelTrack.Api/DTOs/TiposCombustible/SaveTipoCombustibleRequest.cs`
- `backend/FuelTrack.Api/Controllers/TiposCombustibleController.cs`
- `backend/FuelTrack.Api.Tests/Controllers/TiposCombustibleControllerTests.cs`
- `backend/FuelTrack.Api/DTOs/Tanques/TanqueDto.cs`
- `backend/FuelTrack.Api/DTOs/Tanques/SaveTanqueRequest.cs`
- `backend/FuelTrack.Api/Controllers/TanquesController.cs`
- `backend/FuelTrack.Api.Tests/Controllers/TanquesControllerTests.cs`
- `backend/FuelTrack.Api/DTOs/Proveedores/ProveedorDto.cs`
- `backend/FuelTrack.Api/DTOs/Proveedores/SaveProveedorRequest.cs`
- `backend/FuelTrack.Api/Controllers/ProveedoresController.cs`
- `backend/FuelTrack.Api.Tests/Controllers/ProveedoresControllerTests.cs`

---

## Task 1: Actualizar contrato de API

**Files:**
- Modify: `docs/06-API.md`

- [ ] **Step 1: Reemplazar el contenido de `docs/06-API.md`**

Reemplazar la sección `## 3. Recursos propuestos` con las rutas en español, incluyendo los nuevos endpoints del Bloque A:

```markdown
## 3. Recursos

### Autenticación

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
POST /api/v1/auth/password/reset
```

### Usuarios

```text
GET    /api/v1/usuarios
GET    /api/v1/usuarios/{id}
POST   /api/v1/usuarios
PUT    /api/v1/usuarios/{id}
PATCH  /api/v1/usuarios/{id}/estado
```

### Empleados

```text
GET    /api/v1/empleados
GET    /api/v1/empleados/{id}
POST   /api/v1/empleados
PUT    /api/v1/empleados/{id}
DELETE /api/v1/empleados/{id}
```

### Vehículos

```text
GET    /api/v1/vehiculos
GET    /api/v1/vehiculos/{id}
POST   /api/v1/vehiculos
PUT    /api/v1/vehiculos/{id}
DELETE /api/v1/vehiculos/{id}
```

### Departamentos

```text
GET    /api/v1/departamentos
GET    /api/v1/departamentos/{id}
POST   /api/v1/departamentos
PUT    /api/v1/departamentos/{id}
DELETE /api/v1/departamentos/{id}
```

### Tipos de Combustible

```text
GET    /api/v1/tipos-combustible
GET    /api/v1/tipos-combustible/{id}
POST   /api/v1/tipos-combustible
PUT    /api/v1/tipos-combustible/{id}
DELETE /api/v1/tipos-combustible/{id}
```

### Tanques

```text
GET    /api/v1/tanques
GET    /api/v1/tanques/{id}
POST   /api/v1/tanques
PUT    /api/v1/tanques/{id}
DELETE /api/v1/tanques/{id}
```

> POST crea el Tanque y su registro de Inventario (existencia = 0) en una sola transacción.

### Proveedores

```text
GET    /api/v1/proveedores
GET    /api/v1/proveedores/{id}
POST   /api/v1/proveedores
PUT    /api/v1/proveedores/{id}
DELETE /api/v1/proveedores/{id}
```

### Solicitudes de Combustible

```text
GET    /api/v1/solicitudes
GET    /api/v1/solicitudes/{id}
POST   /api/v1/solicitudes
POST   /api/v1/solicitudes/{id}/aprobar
POST   /api/v1/solicitudes/{id}/rechazar
```

### Tickets

```text
GET    /api/v1/tickets
GET    /api/v1/tickets/{id}
POST   /api/v1/tickets
POST   /api/v1/tickets/{id}/enviar
POST   /api/v1/tickets/{id}/anular
POST   /api/v1/tickets/validar
```

### Despachos

```text
GET    /api/v1/despachos
GET    /api/v1/despachos/{id}
POST   /api/v1/despachos
```

### Inventario

```text
GET    /api/v1/inventario
GET    /api/v1/inventario/movimientos
POST   /api/v1/inventario/ajustes
POST   /api/v1/inventario/transferencias
```

### Recepciones de Combustible

```text
GET    /api/v1/recepciones
POST   /api/v1/recepciones
```

### Cierres Diarios

```text
GET    /api/v1/cierres-diarios
GET    /api/v1/cierres-diarios/{id}
POST   /api/v1/cierres-diarios
```

### Reportes

```text
GET /api/v1/reportes
GET /api/v1/reportes/exportar
```

### Auditoría

```text
GET /api/v1/auditoria
```

### Dashboard

```text
GET /api/v1/dashboard/resumen
```
```

- [ ] **Step 2: Commit**

```bash
git add docs/06-API.md
git commit -m "$(cat <<'EOF'
docs: actualizar contrato de API con rutas en español

Convierte 06-API.md de propuesta inicial a contrato real del equipo.
Todas las rutas ahora están en español, alineadas con lo implementado
en Fases 1 y 2.

Cambios de ruta:
  /api/v1/employees       → /api/v1/empleados
  /api/v1/vehicles        → /api/v1/vehiculos
  /api/v1/departments     → /api/v1/departamentos
  /api/v1/fuel-requests   → /api/v1/solicitudes
  /api/v1/dispatches      → /api/v1/despachos
  /api/v1/inventory       → /api/v1/inventario
  /api/v1/receipts        → /api/v1/recepciones
  /api/v1/daily-closures  → /api/v1/cierres-diarios
  /api/v1/reports         → /api/v1/reportes
  /api/v1/audit           → /api/v1/auditoria
  /api/v1/dashboard/summary → /api/v1/dashboard/resumen

Nuevos endpoints agregados (Bloque A - pendientes de implementación):
  /api/v1/tipos-combustible  (CRUD)
  /api/v1/tanques            (CRUD + crea Inventario en POST)
  /api/v1/proveedores        (CRUD)

Impacto para el equipo:
  Builder 3 (Frontend): actualizar llamadas a la API si usaba el doc anterior
  Builder 2 (Móvil): verificar rutas de tickets y validación
  Testers: usar estas rutas como referencia oficial
EOF
)"
```

---

## Task 2: Migración — Activo en Tanques y Proveedores + índices únicos

**Files:**
- Modify: `backend/FuelTrack.Api/Models/Tanque.cs`
- Modify: `backend/FuelTrack.Api/Models/Proveedor.cs`
- Modify: `backend/FuelTrack.Api/Data/AppDbContext.cs`
- Create: migración generada por EF Core

- [ ] **Step 1: Agregar `Activo` al modelo `Tanque`**

Editar `backend/FuelTrack.Api/Models/Tanque.cs`:

```csharp
namespace FuelTrack.Api.Models;

public class Tanque
{
    public int Id { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public decimal Capacidad { get; set; }
    public decimal NivelActual { get; set; }
    public decimal NivelCritico { get; set; }
    public bool Activo { get; set; } = true;

    public int TipoCombustibleId { get; set; }
    public TipoCombustible TipoCombustible { get; set; } = null!;

    public Inventario? Inventario { get; set; }
    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
    public ICollection<RecepcionCombustible> Recepciones { get; set; } = new List<RecepcionCombustible>();
}
```

- [ ] **Step 2: Agregar `Activo` al modelo `Proveedor`**

Editar `backend/FuelTrack.Api/Models/Proveedor.cs`:

```csharp
namespace FuelTrack.Api.Models;

public class Proveedor
{
    public int Id { get; set; }
    public string Rnc { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<RecepcionCombustible> Recepciones { get; set; } = new List<RecepcionCombustible>();
}
```

- [ ] **Step 3: Agregar índices únicos en `AppDbContext`**

En `backend/FuelTrack.Api/Data/AppDbContext.cs`, dentro del método `OnModelCreating`, agregar estas dos líneas junto a los índices existentes:

```csharp
modelBuilder.Entity<TipoCombustible>()
    .HasIndex(t => t.Nombre).IsUnique();
modelBuilder.Entity<Proveedor>()
    .HasIndex(p => p.Rnc).IsUnique();
```

El bloque de índices en `OnModelCreating` quedará así (completo):

```csharp
modelBuilder.Entity<Usuario>()
    .HasIndex(u => u.NombreUsuario).IsUnique();
modelBuilder.Entity<Empleado>()
    .HasIndex(e => e.Codigo).IsUnique();
modelBuilder.Entity<Empleado>()
    .HasIndex(e => e.Cedula).IsUnique();
modelBuilder.Entity<Vehiculo>()
    .HasIndex(v => v.Placa).IsUnique();
modelBuilder.Entity<Vehiculo>()
    .HasIndex(v => v.Ficha).IsUnique();
modelBuilder.Entity<Ticket>()
    .HasIndex(t => t.NumeroSecuencial).IsUnique();
modelBuilder.Entity<Tanque>()
    .HasIndex(t => t.Identificacion).IsUnique();
modelBuilder.Entity<Despacho>()
    .HasIndex(d => d.TicketId).IsUnique();
modelBuilder.Entity<CierreDiario>()
    .HasIndex(c => c.Fecha).IsUnique();
modelBuilder.Entity<RefreshToken>()
    .HasIndex(t => t.TokenHash).IsUnique();
modelBuilder.Entity<TipoCombustible>()   // NUEVO
    .HasIndex(t => t.Nombre).IsUnique();
modelBuilder.Entity<Proveedor>()          // NUEVO
    .HasIndex(p => p.Rnc).IsUnique();
```

- [ ] **Step 4: Generar la migración**

```bash
cd backend/FuelTrack.Api && dotnet ef migrations add AddActivoTanqueProveedorIndices
```

Resultado esperado: mensaje indicando que la migración fue creada exitosamente en la carpeta `Migrations/`.

- [ ] **Step 5: Verificar la migración generada**

Abrir el archivo `Migrations/<timestamp>_AddActivoTanqueProveedorIndices.cs` y confirmar que el método `Up` contiene:
- `AddColumn` en tabla `Tanques` para columna `Activo` (bool, defaultValue: true)
- `AddColumn` en tabla `Proveedores` para columna `Activo` (bool, defaultValue: true)
- `CreateIndex` para `IX_TiposCombustible_Nombre` (unique: true)
- `CreateIndex` para `IX_Proveedores_Rnc` (unique: true)

- [ ] **Step 6: Compilar**

```bash
cd backend/FuelTrack.Api && dotnet build
```

Resultado esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add backend/FuelTrack.Api/Models/Tanque.cs \
        backend/FuelTrack.Api/Models/Proveedor.cs \
        backend/FuelTrack.Api/Data/AppDbContext.cs \
        backend/FuelTrack.Api/Migrations/
git commit -m "$(cat <<'EOF'
feat(backend): migración AddActivoTanqueProveedorIndices — preparación Bloque A

Cambios en modelos:
  Tanque.cs   → agrega campo Activo (bool, default true) para soft delete
  Proveedor.cs → agrega campo Activo (bool, default true) para soft delete

Cambios en AppDbContext.OnModelCreating:
  + HasIndex(TipoCombustible.Nombre).IsUnique()
  + HasIndex(Proveedor.Rnc).IsUnique()

Migración AddActivoTanqueProveedorIndices:
  + Tanques.Activo       (boolean, default true)
  + Proveedores.Activo   (boolean, default true)
  + IX_TiposCombustible_Nombre (unique)
  + IX_Proveedores_Rnc         (unique)

Nota: Tanques.Identificacion ya tenía índice único desde InitialSchema.
Nota: TipoCombustible.Activo ya existía en el modelo inicial.

Impacto para el equipo:
  Testers: aplicar migración antes de probar Bloque A (dotnet ef database update)
  Builder 2/3: sin impacto en contratos existentes
EOF
)"
```

---

## Task 3: TiposCombustible — DTOs, Controller y Tests

**Files:**
- Create: `backend/FuelTrack.Api/DTOs/TiposCombustible/TipoCombustibleDto.cs`
- Create: `backend/FuelTrack.Api/DTOs/TiposCombustible/SaveTipoCombustibleRequest.cs`
- Create: `backend/FuelTrack.Api/Controllers/TiposCombustibleController.cs`
- Create: `backend/FuelTrack.Api.Tests/Controllers/TiposCombustibleControllerTests.cs`

- [ ] **Step 1: Crear `TipoCombustibleDto`**

```csharp
// backend/FuelTrack.Api/DTOs/TiposCombustible/TipoCombustibleDto.cs
namespace FuelTrack.Api.DTOs.TiposCombustible;

public record TipoCombustibleDto(int Id, string Nombre, bool Activo);
```

- [ ] **Step 2: Crear `SaveTipoCombustibleRequest`**

```csharp
// backend/FuelTrack.Api/DTOs/TiposCombustible/SaveTipoCombustibleRequest.cs
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.TiposCombustible;

public record SaveTipoCombustibleRequest(
    [Required, MaxLength(50)] string Nombre,
    bool Activo = true
);
```

- [ ] **Step 3: Crear el controller con stubs (para que los tests compilen)**

```csharp
// backend/FuelTrack.Api/Controllers/TiposCombustibleController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.TiposCombustible;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/tipos-combustible")]
[Authorize]
public sealed class TiposCombustibleController : ControllerBase
{
    private readonly AppDbContext _db;

    public TiposCombustibleController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<TipoCombustibleDto>>> GetAll(CancellationToken ct)
        => throw new NotImplementedException();

    [HttpGet("{id:int}")]
    public Task<ActionResult<TipoCombustibleDto>> GetById(int id, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<TipoCombustibleDto>> Create(SaveTipoCombustibleRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<TipoCombustibleDto>> Update(int id, SaveTipoCombustibleRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public Task<IActionResult> Deactivate(int id, CancellationToken ct)
        => throw new NotImplementedException();
}
```

- [ ] **Step 4: Crear los tests**

```csharp
// backend/FuelTrack.Api.Tests/Controllers/TiposCombustibleControllerTests.cs
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
```

- [ ] **Step 5: Correr tests — deben fallar (NotImplementedException)**

```bash
cd backend && dotnet test FuelTrack.Api.Tests --filter "FullyQualifiedName~TiposCombustibleControllerTests" -v minimal
```

Resultado esperado: todos los tests fallan con `NotImplementedException`.

- [ ] **Step 6: Implementar el controller completo**

```csharp
// backend/FuelTrack.Api/Controllers/TiposCombustibleController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.TiposCombustible;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/tipos-combustible")]
[Authorize]
public sealed class TiposCombustibleController : ControllerBase
{
    private readonly AppDbContext _db;

    public TiposCombustibleController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TipoCombustibleDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.TiposCombustible
            .AsNoTracking()
            .Select(t => new TipoCombustibleDto(t.Id, t.Nombre, t.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TipoCombustibleDto>> GetById(int id, CancellationToken ct)
    {
        var t = await _db.TiposCombustible
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? NotFound() : Ok(new TipoCombustibleDto(t.Id, t.Nombre, t.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TipoCombustibleDto>> Create(
        SaveTipoCombustibleRequest req, CancellationToken ct)
    {
        if (await _db.TiposCombustible.AnyAsync(t => t.Nombre == req.Nombre, ct))
            return Conflict(new { code = "NOMBRE_DUPLICADO",
                message = "Ya existe un tipo de combustible con ese nombre." });

        var entity = new TipoCombustible { Nombre = req.Nombre, Activo = req.Activo };
        _db.TiposCombustible.Add(entity);
        await _db.SaveChangesAsync(ct);
        var dto = new TipoCombustibleDto(entity.Id, entity.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TipoCombustibleDto>> Update(
        int id, SaveTipoCombustibleRequest req, CancellationToken ct)
    {
        var entity = await _db.TiposCombustible.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.TiposCombustible.AnyAsync(t => t.Nombre == req.Nombre && t.Id != id, ct))
            return Conflict(new { code = "NOMBRE_DUPLICADO",
                message = "Ya existe un tipo de combustible con ese nombre." });

        entity.Nombre = req.Nombre;
        entity.Activo = req.Activo;
        await _db.SaveChangesAsync(ct);
        return Ok(new TipoCombustibleDto(entity.Id, entity.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.TiposCombustible.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
```

- [ ] **Step 7: Correr tests — deben pasar**

```bash
cd backend && dotnet test FuelTrack.Api.Tests --filter "FullyQualifiedName~TiposCombustibleControllerTests" -v minimal
```

Resultado esperado: `Passed! - Failed: 0, Passed: 9, Skipped: 0`

- [ ] **Step 8: Commit**

```bash
git add backend/FuelTrack.Api/DTOs/TiposCombustible/ \
        backend/FuelTrack.Api/Controllers/TiposCombustibleController.cs \
        backend/FuelTrack.Api.Tests/Controllers/TiposCombustibleControllerTests.cs
git commit -m "$(cat <<'EOF'
feat(backend): CRUD TiposCombustible — catálogo base para inventario

Nuevos endpoints (ruta: /api/v1/tipos-combustible):
  GET    /api/v1/tipos-combustible        → cualquier usuario autenticado
  GET    /api/v1/tipos-combustible/{id}   → cualquier usuario autenticado
  POST   /api/v1/tipos-combustible        → Administrador, Supervisor
  PUT    /api/v1/tipos-combustible/{id}   → Administrador, Supervisor
  DELETE /api/v1/tipos-combustible/{id}   → Administrador (soft delete)

Archivos creados:
  DTOs/TiposCombustible/TipoCombustibleDto.cs
  DTOs/TiposCombustible/SaveTipoCombustibleRequest.cs
  Controllers/TiposCombustibleController.cs
  Tests/Controllers/TiposCombustibleControllerTests.cs (9 tests, todos pasan)

Validaciones:
  409 NOMBRE_DUPLICADO si ya existe un tipo con el mismo nombre

Impacto para el equipo:
  Builder 2: TipoCombustibleId ya puede usarse en Tickets y Solicitudes
  Builder 3: endpoint disponible para dropdown de tipos en formularios
  Testers: 9 casos cubiertos en tests unitarios; probar también roles con JWT real
EOF
)"
```

---

## Task 4: Tanques — DTOs, Controller y Tests

**Files:**
- Create: `backend/FuelTrack.Api/DTOs/Tanques/TanqueDto.cs`
- Create: `backend/FuelTrack.Api/DTOs/Tanques/SaveTanqueRequest.cs`
- Create: `backend/FuelTrack.Api/Controllers/TanquesController.cs`
- Create: `backend/FuelTrack.Api.Tests/Controllers/TanquesControllerTests.cs`

- [ ] **Step 1: Crear `TanqueDto`**

```csharp
// backend/FuelTrack.Api/DTOs/Tanques/TanqueDto.cs
namespace FuelTrack.Api.DTOs.Tanques;

public record TanqueDto(
    int Id,
    string Identificacion,
    decimal Capacidad,
    decimal NivelActual,
    decimal NivelCritico,
    int TipoCombustibleId,
    string TipoCombustibleNombre,
    bool Activo
);
```

- [ ] **Step 2: Crear `SaveTanqueRequest`**

```csharp
// backend/FuelTrack.Api/DTOs/Tanques/SaveTanqueRequest.cs
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Tanques;

public record SaveTanqueRequest(
    [Required, MaxLength(50)]        string Identificacion,
    [Range(0.0001, 999999.9999)]     decimal Capacidad,
    [Range(0, 999999.9999)]          decimal NivelCritico,
    [Required]                        int TipoCombustibleId
);
```

- [ ] **Step 3: Crear el controller con stubs**

```csharp
// backend/FuelTrack.Api/Controllers/TanquesController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Tanques;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/tanques")]
[Authorize]
public sealed class TanquesController : ControllerBase
{
    private readonly AppDbContext _db;

    public TanquesController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<TanqueDto>>> GetAll(CancellationToken ct)
        => throw new NotImplementedException();

    [HttpGet("{id:int}")]
    public Task<ActionResult<TanqueDto>> GetById(int id, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<TanqueDto>> Create(SaveTanqueRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<TanqueDto>> Update(int id, SaveTanqueRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public Task<IActionResult> Deactivate(int id, CancellationToken ct)
        => throw new NotImplementedException();
}
```

- [ ] **Step 4: Crear los tests**

```csharp
// backend/FuelTrack.Api.Tests/Controllers/TanquesControllerTests.cs
using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Tanques;
using FuelTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class TanquesControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private TanquesController _controller = null!;

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
        _controller = new TanquesController(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<TipoCombustible> CrearTipoCombustibleAsync(string nombre = "Gasolina")
    {
        var tipo = new TipoCombustible { Nombre = nombre, Activo = true };
        _db.TiposCombustible.Add(tipo);
        await _db.SaveChangesAsync();
        return tipo;
    }

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_WhenNoData()
    {
        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<TanqueDto>;
        Assert.AreEqual(0, list!.Count);
    }

    [TestMethod]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _controller.GetById(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Create_Returns201_YCreaInventarioEnCero()
    {
        var tipo = await CrearTipoCombustibleAsync();
        var req = new SaveTanqueRequest("T-01", 5000m, 500m, tipo.Id);

        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);

        var dto = created.Value as TanqueDto;
        Assert.AreEqual("T-01", dto!.Identificacion);
        Assert.AreEqual(5000m, dto.Capacidad);
        Assert.AreEqual(0m, dto.NivelActual);
        Assert.IsTrue(dto.Activo);

        // Verificar que se creó el Inventario en cero
        var inventario = await _db.Inventarios.FirstOrDefaultAsync(i => i.TanqueId == dto.Id);
        Assert.IsNotNull(inventario);
        Assert.AreEqual(0m, inventario.ExistenciaActual);
        Assert.AreEqual(0m, inventario.Disponibilidad);
    }

    [TestMethod]
    public async Task Create_Returns409_CuandoIdentificacionDuplicada()
    {
        var tipo = await CrearTipoCombustibleAsync();
        _db.Tanques.Add(new Tanque
        {
            Identificacion = "T-01", Capacidad = 5000m,
            NivelActual = 0, NivelCritico = 500m,
            TipoCombustibleId = tipo.Id, Activo = true
        });
        await _db.SaveChangesAsync();

        var req = new SaveTanqueRequest("T-01", 3000m, 300m, tipo.Id);
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoTipoCombustibleNoExiste()
    {
        var req = new SaveTanqueRequest("T-01", 5000m, 500m, 999);
        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
    }

    [TestMethod]
    public async Task Update_Returns200_ConDatosActualizados()
    {
        var tipo = await CrearTipoCombustibleAsync();
        var tanque = new Tanque
        {
            Identificacion = "T-01", Capacidad = 5000m,
            NivelActual = 0, NivelCritico = 500m,
            TipoCombustibleId = tipo.Id, Activo = true
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        var req = new SaveTanqueRequest("T-01-MOD", 6000m, 600m, tipo.Id);
        var result = await _controller.Update(tanque.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as TanqueDto;
        Assert.AreEqual("T-01-MOD", dto!.Identificacion);
        Assert.AreEqual(6000m, dto.Capacidad);
    }

    [TestMethod]
    public async Task Update_Returns404_CuandoNoExiste()
    {
        var tipo = await CrearTipoCombustibleAsync();
        var req = new SaveTanqueRequest("T-99", 1000m, 100m, tipo.Id);
        var result = await _controller.Update(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Deactivate_PoneActivoEnFalse()
    {
        var tipo = await CrearTipoCombustibleAsync();
        var tanque = new Tanque
        {
            Identificacion = "T-01", Capacidad = 5000m,
            NivelActual = 0, NivelCritico = 500m,
            TipoCombustibleId = tipo.Id, Activo = true
        };
        _db.Tanques.Add(tanque);
        await _db.SaveChangesAsync();

        var result = await _controller.Deactivate(tanque.Id, CancellationToken.None);
        Assert.IsInstanceOfType<NoContentResult>(result);

        await _db.Entry(tanque).ReloadAsync();
        Assert.IsFalse(tanque.Activo);
    }

    [TestMethod]
    public async Task Deactivate_Returns404_CuandoNoExiste()
    {
        var result = await _controller.Deactivate(999, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }
}
```

- [ ] **Step 5: Correr tests — deben fallar**

```bash
cd backend && dotnet test FuelTrack.Api.Tests --filter "FullyQualifiedName~TanquesControllerTests" -v minimal
```

Resultado esperado: todos fallan con `NotImplementedException`.

- [ ] **Step 6: Implementar el controller completo**

```csharp
// backend/FuelTrack.Api/Controllers/TanquesController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Tanques;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/tanques")]
[Authorize]
public sealed class TanquesController : ControllerBase
{
    private readonly AppDbContext _db;

    public TanquesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TanqueDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Tanques
            .AsNoTracking()
            .Include(t => t.TipoCombustible)
            .Select(t => new TanqueDto(
                t.Id, t.Identificacion, t.Capacidad, t.NivelActual, t.NivelCritico,
                t.TipoCombustibleId, t.TipoCombustible.Nombre, t.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TanqueDto>> GetById(int id, CancellationToken ct)
    {
        var t = await _db.Tanques
            .AsNoTracking()
            .Include(x => x.TipoCombustible)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        return Ok(new TanqueDto(
            t.Id, t.Identificacion, t.Capacidad, t.NivelActual, t.NivelCritico,
            t.TipoCombustibleId, t.TipoCombustible.Nombre, t.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TanqueDto>> Create(SaveTanqueRequest req, CancellationToken ct)
    {
        if (!await _db.TiposCombustible.AnyAsync(t => t.Id == req.TipoCombustibleId, ct))
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_NOT_FOUND",
                message = "El tipo de combustible no existe." });

        if (await _db.Tanques.AnyAsync(t => t.Identificacion == req.Identificacion, ct))
            return Conflict(new { code = "IDENTIFICACION_DUPLICADA",
                message = "Ya existe un tanque con esa identificación." });

        var tanque = new Tanque
        {
            Identificacion    = req.Identificacion,
            Capacidad         = req.Capacidad,
            NivelActual       = 0,
            NivelCritico      = req.NivelCritico,
            TipoCombustibleId = req.TipoCombustibleId,
            Activo            = true
        };
        _db.Tanques.Add(tanque);

        var inventario = new Inventario
        {
            Tanque              = tanque,
            ExistenciaActual    = 0,
            Disponibilidad      = 0,
            UltimaActualizacion = DateTime.UtcNow
        };
        _db.Inventarios.Add(inventario);

        await _db.SaveChangesAsync(ct);
        await _db.Entry(tanque).Reference(t => t.TipoCombustible).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = tanque.Id },
            new TanqueDto(tanque.Id, tanque.Identificacion, tanque.Capacidad, tanque.NivelActual,
                tanque.NivelCritico, tanque.TipoCombustibleId, tanque.TipoCombustible.Nombre, tanque.Activo));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<TanqueDto>> Update(int id, SaveTanqueRequest req, CancellationToken ct)
    {
        var tanque = await _db.Tanques
            .Include(t => t.TipoCombustible)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tanque is null) return NotFound();

        if (!await _db.TiposCombustible.AnyAsync(t => t.Id == req.TipoCombustibleId, ct))
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_NOT_FOUND",
                message = "El tipo de combustible no existe." });

        if (await _db.Tanques.AnyAsync(t => t.Identificacion == req.Identificacion && t.Id != id, ct))
            return Conflict(new { code = "IDENTIFICACION_DUPLICADA",
                message = "Ya existe un tanque con esa identificación." });

        tanque.Identificacion    = req.Identificacion;
        tanque.Capacidad         = req.Capacidad;
        tanque.NivelCritico      = req.NivelCritico;
        tanque.TipoCombustibleId = req.TipoCombustibleId;
        await _db.SaveChangesAsync(ct);

        if (tanque.TipoCombustible.Id != req.TipoCombustibleId)
            await _db.Entry(tanque).Reference(t => t.TipoCombustible).LoadAsync(ct);

        return Ok(new TanqueDto(tanque.Id, tanque.Identificacion, tanque.Capacidad, tanque.NivelActual,
            tanque.NivelCritico, tanque.TipoCombustibleId, tanque.TipoCombustible.Nombre, tanque.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.Tanques.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
```

- [ ] **Step 7: Correr tests — deben pasar**

```bash
cd backend && dotnet test FuelTrack.Api.Tests --filter "FullyQualifiedName~TanquesControllerTests" -v minimal
```

Resultado esperado: `Passed! - Failed: 0, Passed: 9, Skipped: 0`

- [ ] **Step 8: Commit**

```bash
git add backend/FuelTrack.Api/DTOs/Tanques/ \
        backend/FuelTrack.Api/Controllers/TanquesController.cs \
        backend/FuelTrack.Api.Tests/Controllers/TanquesControllerTests.cs
git commit -m "$(cat <<'EOF'
feat(backend): CRUD Tanques con creación automática de Inventario

Nuevos endpoints (ruta: /api/v1/tanques):
  GET    /api/v1/tanques        → cualquier usuario autenticado
  GET    /api/v1/tanques/{id}   → cualquier usuario autenticado
  POST   /api/v1/tanques        → Administrador, Supervisor
  PUT    /api/v1/tanques/{id}   → Administrador, Supervisor
  DELETE /api/v1/tanques/{id}   → Administrador (soft delete, Activo = false)

Regla clave en POST:
  Crea Tanque + Inventario (ExistenciaActual=0, Disponibilidad=0) en una
  sola transacción. El tanque nace con nivel en cero.

Archivos creados:
  DTOs/Tanques/TanqueDto.cs
  DTOs/Tanques/SaveTanqueRequest.cs
  Controllers/TanquesController.cs
  Tests/Controllers/TanquesControllerTests.cs (9 tests, todos pasan)

Validaciones:
  400 TIPO_COMBUSTIBLE_NOT_FOUND si TipoCombustibleId no existe
  409 IDENTIFICACION_DUPLICADA si ya hay un tanque con esa identificación

Impacto para el equipo:
  Builder 3: usar GET /api/v1/tanques para mostrar tanques disponibles
  Bloque B (Inventario): los tanques creados aquí son la base de recepciones
  Testers: verificar en BD que Inventario existe tras crear tanque
EOF
)"
```

---

## Task 5: Proveedores — DTOs, Controller y Tests

**Files:**
- Create: `backend/FuelTrack.Api/DTOs/Proveedores/ProveedorDto.cs`
- Create: `backend/FuelTrack.Api/DTOs/Proveedores/SaveProveedorRequest.cs`
- Create: `backend/FuelTrack.Api/Controllers/ProveedoresController.cs`
- Create: `backend/FuelTrack.Api.Tests/Controllers/ProveedoresControllerTests.cs`

- [ ] **Step 1: Crear `ProveedorDto`**

```csharp
// backend/FuelTrack.Api/DTOs/Proveedores/ProveedorDto.cs
namespace FuelTrack.Api.DTOs.Proveedores;

public record ProveedorDto(int Id, string Rnc, string Nombre, bool Activo);
```

- [ ] **Step 2: Crear `SaveProveedorRequest`**

```csharp
// backend/FuelTrack.Api/DTOs/Proveedores/SaveProveedorRequest.cs
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Proveedores;

public record SaveProveedorRequest(
    [Required, MaxLength(20)]  string Rnc,
    [Required, MaxLength(150)] string Nombre,
    bool Activo = true
);
```

- [ ] **Step 3: Crear el controller con stubs**

```csharp
// backend/FuelTrack.Api/Controllers/ProveedoresController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Proveedores;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/proveedores")]
[Authorize]
public sealed class ProveedoresController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProveedoresController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<ProveedorDto>>> GetAll(CancellationToken ct)
        => throw new NotImplementedException();

    [HttpGet("{id:int}")]
    public Task<ActionResult<ProveedorDto>> GetById(int id, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<ProveedorDto>> Create(SaveProveedorRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<ProveedorDto>> Update(int id, SaveProveedorRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public Task<IActionResult> Deactivate(int id, CancellationToken ct)
        => throw new NotImplementedException();
}
```

- [ ] **Step 4: Crear los tests**

```csharp
// backend/FuelTrack.Api.Tests/Controllers/ProveedoresControllerTests.cs
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
    public async Task Update_Returns200_ConDatosActualizados()
    {
        var proveedor = new Proveedor { Rnc = "101-12345-6", Nombre = "Petrobras RD", Activo = true };
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();

        var req = new SaveProveedorRequest("101-12345-6", "Petrobras RD S.A.", false);
        var result = await _controller.Update(proveedor.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as ProveedorDto;
        Assert.AreEqual("Petrobras RD S.A.", dto!.Nombre);
        Assert.IsFalse(dto.Activo);
    }

    [TestMethod]
    public async Task Update_Returns404_CuandoNoExiste()
    {
        var req = new SaveProveedorRequest("101-99999-9", "Fantasma");
        var result = await _controller.Update(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Update_Returns409_CuandoRncDuplicadoEnOtroRegistro()
    {
        _db.Proveedores.AddRange(
            new Proveedor { Rnc = "101-11111-1", Nombre = "Proveedor A", Activo = true },
            new Proveedor { Rnc = "101-22222-2", Nombre = "Proveedor B", Activo = true });
        await _db.SaveChangesAsync();
        var proveedorA = await _db.Proveedores.FirstAsync(p => p.Rnc == "101-11111-1");

        var req = new SaveProveedorRequest("101-22222-2", "Proveedor A Renombrado");
        var result = await _controller.Update(proveedorA.Id, req, CancellationToken.None);
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
```

- [ ] **Step 5: Correr tests — deben fallar**

```bash
cd backend && dotnet test FuelTrack.Api.Tests --filter "FullyQualifiedName~ProveedoresControllerTests" -v minimal
```

Resultado esperado: todos fallan con `NotImplementedException`.

- [ ] **Step 6: Implementar el controller completo**

```csharp
// backend/FuelTrack.Api/Controllers/ProveedoresController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Proveedores;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/proveedores")]
[Authorize]
public sealed class ProveedoresController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProveedoresController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<ProveedorDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Proveedores
            .AsNoTracking()
            .Select(p => new ProveedorDto(p.Id, p.Rnc, p.Nombre, p.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProveedorDto>> GetById(int id, CancellationToken ct)
    {
        var p = await _db.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? NotFound() : Ok(new ProveedorDto(p.Id, p.Rnc, p.Nombre, p.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<ProveedorDto>> Create(SaveProveedorRequest req, CancellationToken ct)
    {
        if (await _db.Proveedores.AnyAsync(p => p.Rnc == req.Rnc, ct))
            return Conflict(new { code = "RNC_DUPLICADO",
                message = "El RNC ya está registrado." });

        var entity = new Proveedor { Rnc = req.Rnc, Nombre = req.Nombre, Activo = req.Activo };
        _db.Proveedores.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new ProveedorDto(entity.Id, entity.Rnc, entity.Nombre, entity.Activo));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<ProveedorDto>> Update(int id, SaveProveedorRequest req, CancellationToken ct)
    {
        var entity = await _db.Proveedores.FindAsync([id], ct);
        if (entity is null) return NotFound();

        if (await _db.Proveedores.AnyAsync(p => p.Rnc == req.Rnc && p.Id != id, ct))
            return Conflict(new { code = "RNC_DUPLICADO",
                message = "El RNC ya está registrado." });

        entity.Rnc    = req.Rnc;
        entity.Nombre = req.Nombre;
        entity.Activo = req.Activo;
        await _db.SaveChangesAsync(ct);
        return Ok(new ProveedorDto(entity.Id, entity.Rnc, entity.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.Proveedores.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
```

- [ ] **Step 7: Correr tests — deben pasar**

```bash
cd backend && dotnet test FuelTrack.Api.Tests --filter "FullyQualifiedName~ProveedoresControllerTests" -v minimal
```

Resultado esperado: `Passed! - Failed: 0, Passed: 9, Skipped: 0`

- [ ] **Step 8: Commit**

```bash
git add backend/FuelTrack.Api/DTOs/Proveedores/ \
        backend/FuelTrack.Api/Controllers/ProveedoresController.cs \
        backend/FuelTrack.Api.Tests/Controllers/ProveedoresControllerTests.cs
git commit -m "$(cat <<'EOF'
feat(backend): CRUD Proveedores — catálogo para recepciones de combustible

Nuevos endpoints (ruta: /api/v1/proveedores):
  GET    /api/v1/proveedores        → cualquier usuario autenticado
  GET    /api/v1/proveedores/{id}   → cualquier usuario autenticado
  POST   /api/v1/proveedores        → Administrador, Supervisor
  PUT    /api/v1/proveedores/{id}   → Administrador, Supervisor
  DELETE /api/v1/proveedores/{id}   → Administrador (soft delete, Activo = false)

Archivos creados:
  DTOs/Proveedores/ProveedorDto.cs       (campos: Id, Rnc, Nombre, Activo)
  DTOs/Proveedores/SaveProveedorRequest.cs
  Controllers/ProveedoresController.cs
  Tests/Controllers/ProveedoresControllerTests.cs (9 tests, todos pasan)

Validaciones:
  409 RNC_DUPLICADO si ya existe un proveedor con ese RNC

Nota: campo es Rnc (no RNC) — así definido en el modelo desde Fase 0

Impacto para el equipo:
  Bloque B (Recepciones): ProveedorId ya puede usarse en RecepcionCombustible
  Builder 3: endpoint disponible para selector de proveedores en formulario de recepción
  Testers: verificar que el mismo RNC no puede registrarse dos veces
EOF
)"
```

---

## Task 6: Verificación final y PR

**Files:** ninguno nuevo

- [ ] **Step 1: Build limpio completo**

```bash
cd backend/FuelTrack.Api && dotnet build
```

Resultado esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 2: Suite completa de tests**

```bash
cd backend && dotnet test FuelTrack.Api.Tests -v minimal
```

Resultado esperado: todos los tests pasan (incluyendo los de Security y AuthService de Fases anteriores).

- [ ] **Step 3: Push de la rama**

```bash
git push origin feature/backend-datos
```

- [ ] **Step 4: Crear el PR**

```bash
gh pr create \
  --title "feat(backend): Paso 1 + Bloque A — contrato API y catálogos de inventario" \
  --base main \
  --head feature/backend-datos \
  --body "$(cat <<'EOF'
## Resumen

- **Paso 1:** `docs/06-API.md` actualizado con rutas en español (contrato real del equipo)
- **Migración:** `AddActivoTanqueProveedorIndices` — agrega `Activo` a Tanques y Proveedores, índices únicos en `TipoCombustible.Nombre` y `Proveedor.Rnc`
- **Bloque A:** CRUD completo de TiposCombustible, Tanques y Proveedores

## Endpoints nuevos

| Recurso | Ruta | Roles POST/PUT | Rol DELETE |
|---|---|---|---|
| Tipos combustible | `/api/v1/tipos-combustible` | Admin, Supervisor | Admin |
| Tanques | `/api/v1/tanques` | Admin, Supervisor | Admin |
| Proveedores | `/api/v1/proveedores` | Admin, Supervisor | Admin |

## Regla clave

`POST /api/v1/tanques` crea el Tanque y su Inventario (existencia = 0) en una sola transacción. El tanque siempre nace con nivel en cero.

## Tests

27 tests unitarios nuevos (9 TiposCombustible + 9 Tanques + 9 Proveedores). Todos pasan.

## Para aplicar la migración

```bash
cd backend/FuelTrack.Api && dotnet ef database update
```

## Impacto por rol

- **Builder 2:** TipoCombustibleId disponible para Tickets y Solicitudes
- **Builder 3:** Los tres catálogos disponibles para dropdowns
- **Testers:** Aplicar migración, luego probar endpoints con JWT válido y roles correctos
EOF
)"
```
