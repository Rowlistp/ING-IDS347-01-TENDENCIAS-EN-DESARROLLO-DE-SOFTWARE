# Fase 3 — Solicitudes de Combustible (Manuales) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar CRUD de solicitudes de combustible manuales con ciclo de vida completo (crear, aprobar, rechazar) para los CU-06 y CU-07 del SRS.

**Architecture:** Controller directo a AppDbContext, sin capa de servicio — mismo patrón del Bloque A. Enum tipado `EstadoSolicitud` almacenado como string en PostgreSQL via `HasConversion<string>()`. Ciclo de vida unidireccional: Pendiente → Aprobada | Rechazada. La creación de Ticket al aprobar es responsabilidad de Builder 2.

**Tech Stack:** .NET 10, EF Core 9, Npgsql (prod), SQLite in-memory (tests), MSTest 4.3, ASP.NET Core authorization declarativa.

---

## Mapa de archivos

| Acción | Archivo |
|---|---|
| Crear | `backend/FuelTrack.Api/Models/Enums/EstadoSolicitud.cs` |
| Modificar | `backend/FuelTrack.Api/Models/SolicitudCombustible.cs` |
| Modificar | `backend/FuelTrack.Api/Data/AppDbContext.cs` |
| Generado por EF | `backend/FuelTrack.Api/Migrations/<timestamp>_AddMotivoRechazoSolicitud.cs` |
| Crear | `backend/FuelTrack.Api/DTOs/Solicitudes/SolicitudDto.cs` |
| Crear | `backend/FuelTrack.Api/DTOs/Solicitudes/CreateSolicitudRequest.cs` |
| Crear | `backend/FuelTrack.Api/DTOs/Solicitudes/AprobarSolicitudRequest.cs` |
| Crear | `backend/FuelTrack.Api/DTOs/Solicitudes/RechazarSolicitudRequest.cs` |
| Crear | `backend/FuelTrack.Api/Controllers/SolicitudesController.cs` |
| Crear | `backend/FuelTrack.Api.Tests/Controllers/SolicitudesControllerTests.cs` |

---

## Task 1: Enum, Model, DbContext y Migración

**Files:**
- Create: `backend/FuelTrack.Api/Models/Enums/EstadoSolicitud.cs`
- Modify: `backend/FuelTrack.Api/Models/SolicitudCombustible.cs`
- Modify: `backend/FuelTrack.Api/Data/AppDbContext.cs`
- Generated: `backend/FuelTrack.Api/Migrations/<timestamp>_AddMotivoRechazoSolicitud.cs`

- [ ] **Step 1: Crear EstadoSolicitud.cs**

```csharp
// backend/FuelTrack.Api/Models/Enums/EstadoSolicitud.cs
namespace FuelTrack.Api.Models.Enums;

public enum EstadoSolicitud
{
    Pendiente,
    Aprobada,
    Rechazada
}
```

- [ ] **Step 2: Reemplazar SolicitudCombustible.cs**

El modelo actual tiene `Estado` como `string`. Reemplazar el archivo completo:

```csharp
// backend/FuelTrack.Api/Models/SolicitudCombustible.cs
using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.Models;

public class SolicitudCombustible
{
    public int Id { get; set; }
    public decimal CantidadSolicitada { get; set; }
    public decimal? CantidadAutorizada { get; set; }
    public string TipoSolicitud { get; set; } = string.Empty;
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
    public string? MotivoRechazo { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaVencimiento { get; set; }

    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    public int TipoCombustibleId { get; set; }
    public TipoCombustible TipoCombustible { get; set; } = null!;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
```

- [ ] **Step 3: Agregar HasConversion en AppDbContext**

En `OnModelCreating`, después de las líneas de `HasPrecision` para `SolicitudCombustible` (alrededor de la línea 72), agregar:

```csharp
modelBuilder.Entity<SolicitudCombustible>()
    .Property(s => s.Estado)
    .HasConversion<string>();
```

El bloque de precisiones existente para `SolicitudCombustible` queda así:

```csharp
modelBuilder.Entity<SolicitudCombustible>().Property(s => s.CantidadSolicitada).HasPrecision(18, 4);
modelBuilder.Entity<SolicitudCombustible>().Property(s => s.CantidadAutorizada).HasPrecision(18, 4);
modelBuilder.Entity<SolicitudCombustible>()
    .Property(s => s.Estado)
    .HasConversion<string>();
```

- [ ] **Step 4: Build para verificar que compila**

```
dotnet build backend/FuelTrack.Api
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Generar la migración**

```
dotnet ef migrations add AddMotivoRechazoSolicitud --project backend/FuelTrack.Api --startup-project backend/FuelTrack.Api
```

Expected: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 6: Verificar el archivo de migración generado**

Abrir el archivo `backend/FuelTrack.Api/Migrations/<timestamp>_AddMotivoRechazoSolicitud.cs` y confirmar:
- Existe `AddColumn` para `MotivoRechazo` (tipo `text`, nullable: `true`)
- Si existe `AlterColumn` para `Estado`, verificar que el tipo sigue siendo `text` — es un no-op en PostgreSQL y es aceptable dejarlo
- No existe ningún `DropColumn`

- [ ] **Step 7: Commit**

```bash
git add backend/FuelTrack.Api/Models/Enums/EstadoSolicitud.cs \
        backend/FuelTrack.Api/Models/SolicitudCombustible.cs \
        backend/FuelTrack.Api/Data/AppDbContext.cs \
        backend/FuelTrack.Api/Migrations/
git commit -m "feat(solicitudes): EstadoSolicitud enum, MotivoRechazo y migración AddMotivoRechazoSolicitud

- Nuevo enum EstadoSolicitud (Pendiente/Aprobada/Rechazada) en Models/Enums
- SolicitudCombustible.Estado cambiado de string a EstadoSolicitud con HasConversion<string>()
- SolicitudCombustible.MotivoRechazo agregado (string?, nullable)
- Migración AddMotivoRechazoSolicitud: agrega columna MotivoRechazo (text, nullable)"
```

---

## Task 2: DTOs

**Files:**
- Create: `backend/FuelTrack.Api/DTOs/Solicitudes/SolicitudDto.cs`
- Create: `backend/FuelTrack.Api/DTOs/Solicitudes/CreateSolicitudRequest.cs`
- Create: `backend/FuelTrack.Api/DTOs/Solicitudes/AprobarSolicitudRequest.cs`
- Create: `backend/FuelTrack.Api/DTOs/Solicitudes/RechazarSolicitudRequest.cs`

- [ ] **Step 1: Crear SolicitudDto.cs**

```csharp
// backend/FuelTrack.Api/DTOs/Solicitudes/SolicitudDto.cs
using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Solicitudes;

public record SolicitudDto(
    int Id,
    decimal CantidadSolicitada,
    decimal? CantidadAutorizada,
    string TipoSolicitud,
    EstadoSolicitud Estado,
    DateTime FechaSolicitud,
    DateTime? FechaVencimiento,
    string? MotivoRechazo,
    int EmpleadoId,
    string EmpleadoNombre,
    int VehiculoId,
    string VehiculoPlaca,
    int DepartamentoId,
    string DepartamentoNombre,
    int TipoCombustibleId,
    string TipoCombustibleNombre
);
```

- [ ] **Step 2: Crear CreateSolicitudRequest.cs**

```csharp
// backend/FuelTrack.Api/DTOs/Solicitudes/CreateSolicitudRequest.cs
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Solicitudes;

public record CreateSolicitudRequest(
    [Required, Range(0.0001, 999999.9999)] decimal CantidadSolicitada,
    [Required] int EmpleadoId,
    [Required] int VehiculoId,
    [Required] int DepartamentoId,
    [Required] int TipoCombustibleId,
    DateTime? FechaVencimiento
);
```

- [ ] **Step 3: Crear AprobarSolicitudRequest.cs**

```csharp
// backend/FuelTrack.Api/DTOs/Solicitudes/AprobarSolicitudRequest.cs
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Solicitudes;

public record AprobarSolicitudRequest(
    [Required, Range(0.0001, 999999.9999)] decimal CantidadAutorizada
);
```

- [ ] **Step 4: Crear RechazarSolicitudRequest.cs**

```csharp
// backend/FuelTrack.Api/DTOs/Solicitudes/RechazarSolicitudRequest.cs
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Solicitudes;

public record RechazarSolicitudRequest(
    [Required, MaxLength(500)] string MotivoRechazo
);
```

- [ ] **Step 5: Build**

```
dotnet build backend/FuelTrack.Api
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add backend/FuelTrack.Api/DTOs/Solicitudes/
git commit -m "feat(solicitudes): DTOs SolicitudDto, CreateSolicitudRequest, AprobarSolicitudRequest, RechazarSolicitudRequest

4 records en DTOs/Solicitudes/ para los endpoints GET (respuesta), POST crear,
POST /aprobar y POST /rechazar"
```

---

## Task 3: Controller stubs + tests (TDD — escribir tests primero, deben FALLAR)

**Files:**
- Create: `backend/FuelTrack.Api/Controllers/SolicitudesController.cs` (stubs)
- Create: `backend/FuelTrack.Api.Tests/Controllers/SolicitudesControllerTests.cs`

- [ ] **Step 1: Crear SolicitudesController.cs con stubs**

```csharp
// backend/FuelTrack.Api/Controllers/SolicitudesController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Solicitudes;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/solicitudes")]
[Authorize]
public sealed class SolicitudesController : ControllerBase
{
    private readonly AppDbContext _db;
    public SolicitudesController(AppDbContext db) => _db = db;

    [HttpGet]
    public Task<ActionResult<List<SolicitudDto>>> GetAll(CancellationToken ct)
        => throw new NotImplementedException();

    [HttpGet("{id:int}")]
    public Task<ActionResult<SolicitudDto>> GetById(int id, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Solicitante}")]
    public Task<ActionResult<SolicitudDto>> Create(CreateSolicitudRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost("{id:int}/aprobar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<SolicitudDto>> Aprobar(int id, AprobarSolicitudRequest req, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost("{id:int}/rechazar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public Task<ActionResult<SolicitudDto>> Rechazar(int id, RechazarSolicitudRequest req, CancellationToken ct)
        => throw new NotImplementedException();
}
```

- [ ] **Step 2: Build para confirmar que los stubs compilan**

```
dotnet build backend/FuelTrack.Api
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Crear SolicitudesControllerTests.cs**

```csharp
// backend/FuelTrack.Api.Tests/Controllers/SolicitudesControllerTests.cs
using FuelTrack.Api.Controllers;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Solicitudes;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Controllers;

[TestClass]
public sealed class SolicitudesControllerTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private SolicitudesController _controller = null!;

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
        _controller = new SolicitudesController(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<(Empleado empleado, Vehiculo vehiculo, Departamento departamento, TipoCombustible tipo)>
        CrearDependenciasAsync()
    {
        var depto = new Departamento { Nombre = "TI", Activo = true };
        _db.Departamentos.Add(depto);
        await _db.SaveChangesAsync();

        var tipo = new TipoCombustible { Nombre = "Gasolina", Activo = true };
        var empleado = new Empleado
        {
            Codigo = "E-001", NombreCompleto = "Juan Pérez", Cedula = "001-0000001-1",
            Cargo = "Analista", Correo = "juan@test.com", Telefono = "8091234567",
            DepartamentoId = depto.Id, Activo = true
        };
        var vehiculo = new Vehiculo
        {
            Placa = "A123456", Ficha = "F-001", Marca = "Toyota", Modelo = "Hilux",
            Año = 2022, Tipo = "Pickup", CapacidadTanque = 70m, Odometro = 0m,
            DepartamentoId = depto.Id, Activo = true
        };
        _db.TiposCombustible.Add(tipo);
        _db.Empleados.Add(empleado);
        _db.Vehiculos.Add(vehiculo);
        await _db.SaveChangesAsync();
        return (empleado, vehiculo, depto, tipo);
    }

    [TestMethod]
    public async Task GetAll_ReturnsEmptyList_WhenNoData()
    {
        var result = await _controller.GetAll(CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as List<SolicitudDto>;
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
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m,
            TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = empleado.Id,
            VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id,
            TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(solicitud.Id, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as SolicitudDto;

        Assert.AreEqual(solicitud.Id, dto!.Id);
        Assert.AreEqual(50m, dto.CantidadSolicitada);
        Assert.AreEqual("Manual", dto.TipoSolicitud);
        Assert.AreEqual(EstadoSolicitud.Pendiente, dto.Estado);
        Assert.AreEqual("Juan Pérez", dto.EmpleadoNombre);
        Assert.AreEqual("A123456", dto.VehiculoPlaca);
        Assert.AreEqual("TI", dto.DepartamentoNombre);
        Assert.AreEqual("Gasolina", dto.TipoCombustibleNombre);
    }

    [TestMethod]
    public async Task Create_Returns201_ConDto()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var req = new CreateSolicitudRequest(75m, empleado.Id, vehiculo.Id, depto.Id, tipo.Id, null);

        var result = await _controller.Create(req, CancellationToken.None);
        var created = result.Result as CreatedAtActionResult;

        Assert.IsNotNull(created);
        Assert.AreEqual(201, created.StatusCode);
        var dto = created.Value as SolicitudDto;
        Assert.AreEqual(75m, dto!.CantidadSolicitada);
        Assert.AreEqual("Manual", dto.TipoSolicitud);
        Assert.AreEqual(EstadoSolicitud.Pendiente, dto.Estado);
        Assert.AreNotEqual(default(DateTime), dto.FechaSolicitud);
        Assert.AreEqual("Juan Pérez", dto.EmpleadoNombre);
        Assert.AreEqual("A123456", dto.VehiculoPlaca);
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoEmpleadoNoExiste()
    {
        var (_, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var req = new CreateSolicitudRequest(50m, 999, vehiculo.Id, depto.Id, tipo.Id, null);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("EMPLEADO_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoVehiculoNoExiste()
    {
        var (empleado, _, depto, tipo) = await CrearDependenciasAsync();
        var req = new CreateSolicitudRequest(50m, empleado.Id, 999, depto.Id, tipo.Id, null);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("VEHICULO_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoDepartamentoNoExiste()
    {
        var (empleado, vehiculo, _, tipo) = await CrearDependenciasAsync();
        var req = new CreateSolicitudRequest(50m, empleado.Id, vehiculo.Id, 999, tipo.Id, null);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("DEPARTAMENTO_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Create_Returns400_CuandoTipoCombustibleNoExiste()
    {
        var (empleado, vehiculo, depto, _) = await CrearDependenciasAsync();
        var req = new CreateSolicitudRequest(50m, empleado.Id, vehiculo.Id, depto.Id, 999, null);

        var result = await _controller.Create(req, CancellationToken.None);
        var bad = result.Result as BadRequestObjectResult;

        Assert.IsNotNull(bad);
        Assert.IsTrue(bad.Value!.ToString()!.Contains("TIPO_COMBUSTIBLE_NOT_FOUND"));
    }

    [TestMethod]
    public async Task Aprobar_Returns200_ConCantidadAutorizada()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente, FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var req = new AprobarSolicitudRequest(45m);
        var result = await _controller.Aprobar(solicitud.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as SolicitudDto;

        Assert.AreEqual(EstadoSolicitud.Aprobada, dto!.Estado);
        Assert.AreEqual(45m, dto.CantidadAutorizada);
    }

    [TestMethod]
    public async Task Aprobar_Returns404_CuandoNoExiste()
    {
        var req = new AprobarSolicitudRequest(45m);
        var result = await _controller.Aprobar(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Aprobar_Returns409_CuandoYaFueProcesada()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Aprobada, FechaSolicitud = DateTime.UtcNow,
            CantidadAutorizada = 50m,
            EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var req = new AprobarSolicitudRequest(45m);
        var result = await _controller.Aprobar(solicitud.Id, req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("SOLICITUD_YA_PROCESADA"));
    }

    [TestMethod]
    public async Task Rechazar_Returns200_ConMotivoRechazo()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente, FechaSolicitud = DateTime.UtcNow,
            EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var req = new RechazarSolicitudRequest("Presupuesto agotado");
        var result = await _controller.Rechazar(solicitud.Id, req, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as SolicitudDto;

        Assert.AreEqual(EstadoSolicitud.Rechazada, dto!.Estado);
        Assert.AreEqual("Presupuesto agotado", dto.MotivoRechazo);
    }

    [TestMethod]
    public async Task Rechazar_Returns404_CuandoNoExiste()
    {
        var req = new RechazarSolicitudRequest("Motivo");
        var result = await _controller.Rechazar(999, req, CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result.Result);
    }

    [TestMethod]
    public async Task Rechazar_Returns409_CuandoYaFueProcesada()
    {
        var (empleado, vehiculo, depto, tipo) = await CrearDependenciasAsync();
        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = 50m, TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Rechazada, FechaSolicitud = DateTime.UtcNow,
            MotivoRechazo = "Ya rechazada",
            EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id,
            DepartamentoId = depto.Id, TipoCombustibleId = tipo.Id
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync();

        var req = new RechazarSolicitudRequest("Otro motivo");
        var result = await _controller.Rechazar(solicitud.Id, req, CancellationToken.None);
        var conflict = result.Result as ConflictObjectResult;

        Assert.IsNotNull(conflict);
        Assert.IsTrue(conflict.Value!.ToString()!.Contains("SOLICITUD_YA_PROCESADA"));
    }
}
```

- [ ] **Step 4: Build del proyecto de tests**

```
dotnet build backend/FuelTrack.Api.Tests
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Correr los tests — deben FALLAR**

```
dotnet test backend/FuelTrack.Api.Tests --filter "SolicitudesControllerTests"
```

Expected: 14 tests FAILED con `NotImplementedException`. Si alguno pasa, hay un error en los stubs — revisar.

- [ ] **Step 6: Commit (stubs + tests en rojo)**

```bash
git add backend/FuelTrack.Api/Controllers/SolicitudesController.cs \
        backend/FuelTrack.Api.Tests/Controllers/SolicitudesControllerTests.cs
git commit -m "test(solicitudes): 14 tests para SolicitudesController (TDD — en rojo)

Tests: GetAll lista vacía, GetById 404/DTO con navegaciones aplanadas, Create 201 + 4x400 FK,
Aprobar 200/404/409, Rechazar 200/404/409"
```

---

## Task 4: Implementación completa del controller

**Files:**
- Modify: `backend/FuelTrack.Api/Controllers/SolicitudesController.cs`

- [ ] **Step 1: Reemplazar SolicitudesController.cs con implementación completa**

```csharp
// backend/FuelTrack.Api/Controllers/SolicitudesController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Solicitudes;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/solicitudes")]
[Authorize]
public sealed class SolicitudesController : ControllerBase
{
    private readonly AppDbContext _db;
    public SolicitudesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<SolicitudDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.SolicitudesCombustible
            .AsNoTracking()
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .ToListAsync(ct);
        return Ok(list.ConvertAll(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitudDto>> GetById(int id, CancellationToken ct)
    {
        var s = await _db.SolicitudesCombustible
            .AsNoTracking()
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        return s is null ? NotFound() : Ok(ToDto(s));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor},{Roles.Solicitante}")]
    public async Task<ActionResult<SolicitudDto>> Create(CreateSolicitudRequest req, CancellationToken ct)
    {
        if (!await _db.Empleados.AnyAsync(e => e.Id == req.EmpleadoId, ct))
            return BadRequest(new { code = "EMPLEADO_NOT_FOUND" });
        if (!await _db.Vehiculos.AnyAsync(v => v.Id == req.VehiculoId, ct))
            return BadRequest(new { code = "VEHICULO_NOT_FOUND" });
        if (!await _db.Departamentos.AnyAsync(d => d.Id == req.DepartamentoId, ct))
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND" });
        if (!await _db.TiposCombustible.AnyAsync(t => t.Id == req.TipoCombustibleId, ct))
            return BadRequest(new { code = "TIPO_COMBUSTIBLE_NOT_FOUND" });

        var solicitud = new SolicitudCombustible
        {
            CantidadSolicitada = req.CantidadSolicitada,
            EmpleadoId = req.EmpleadoId,
            VehiculoId = req.VehiculoId,
            DepartamentoId = req.DepartamentoId,
            TipoCombustibleId = req.TipoCombustibleId,
            FechaVencimiento = req.FechaVencimiento,
            TipoSolicitud = "Manual",
            Estado = EstadoSolicitud.Pendiente,
            FechaSolicitud = DateTime.UtcNow
        };
        _db.SolicitudesCombustible.Add(solicitud);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(solicitud).Reference(s => s.Empleado).LoadAsync(ct);
        await _db.Entry(solicitud).Reference(s => s.Vehiculo).LoadAsync(ct);
        await _db.Entry(solicitud).Reference(s => s.Departamento).LoadAsync(ct);
        await _db.Entry(solicitud).Reference(s => s.TipoCombustible).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = solicitud.Id }, ToDto(solicitud));
    }

    [HttpPost("{id:int}/aprobar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<SolicitudDto>> Aprobar(int id, AprobarSolicitudRequest req, CancellationToken ct)
    {
        var solicitud = await _db.SolicitudesCombustible
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (solicitud is null) return NotFound();
        if (solicitud.Estado != EstadoSolicitud.Pendiente)
            return Conflict(new { code = "SOLICITUD_YA_PROCESADA" });

        solicitud.Estado = EstadoSolicitud.Aprobada;
        solicitud.CantidadAutorizada = req.CantidadAutorizada;
        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(solicitud));
    }

    [HttpPost("{id:int}/rechazar")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<SolicitudDto>> Rechazar(int id, RechazarSolicitudRequest req, CancellationToken ct)
    {
        var solicitud = await _db.SolicitudesCombustible
            .Include(s => s.Empleado)
            .Include(s => s.Vehiculo)
            .Include(s => s.Departamento)
            .Include(s => s.TipoCombustible)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (solicitud is null) return NotFound();
        if (solicitud.Estado != EstadoSolicitud.Pendiente)
            return Conflict(new { code = "SOLICITUD_YA_PROCESADA" });

        solicitud.Estado = EstadoSolicitud.Rechazada;
        solicitud.MotivoRechazo = req.MotivoRechazo;
        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(solicitud));
    }

    private static SolicitudDto ToDto(SolicitudCombustible s) => new(
        s.Id,
        s.CantidadSolicitada,
        s.CantidadAutorizada,
        s.TipoSolicitud,
        s.Estado,
        s.FechaSolicitud,
        s.FechaVencimiento,
        s.MotivoRechazo,
        s.EmpleadoId, s.Empleado.NombreCompleto,
        s.VehiculoId, s.Vehiculo.Placa,
        s.DepartamentoId, s.Departamento.Nombre,
        s.TipoCombustibleId, s.TipoCombustible.Nombre);
}
```

- [ ] **Step 2: Build completo de la solución**

```
dotnet build
```

Expected: `Build succeeded. 0 Error(s), 0 Warning(s)`

- [ ] **Step 3: Correr los 14 tests de SolicitudesController — deben pasar**

```
dotnet test backend/FuelTrack.Api.Tests --filter "SolicitudesControllerTests"
```

Expected: `14/14 tests passed`

- [ ] **Step 4: Correr todos los tests de la solución — sin regresiones**

```
dotnet test backend/FuelTrack.Api.Tests
```

Expected: Todos los tests pasan (44 previos + 14 nuevos = 58 total)

- [ ] **Step 5: Commit final**

```bash
git add backend/FuelTrack.Api/Controllers/SolicitudesController.cs
git commit -m "feat(solicitudes): implementar SolicitudesController — 5 endpoints, 14/14 tests en verde

Endpoints:
- GET    /api/v1/solicitudes               → lista (autenticado)
- GET    /api/v1/solicitudes/{id}          → detalle (autenticado)
- POST   /api/v1/solicitudes               → crear manual (Admin/Supervisor/Solicitante)
- POST   /api/v1/solicitudes/{id}/aprobar  → aprobar pendiente (Admin/Supervisor)
- POST   /api/v1/solicitudes/{id}/rechazar → rechazar pendiente (Admin/Supervisor)

Reglas: 409 SOLICITUD_YA_PROCESADA si no está Pendiente, 4x400 si FK inválida.
Nota: aprobación NO crea Ticket — responsabilidad de Builder 2 (lee spec Fase 3).
58/58 tests en verde."
```
