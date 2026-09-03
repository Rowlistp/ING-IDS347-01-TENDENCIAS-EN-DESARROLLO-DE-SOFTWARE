# Spec: Fase 3 — Solicitudes de Combustible (Manuales)

**Fecha:** 2026-09-02
**Builder:** Builder 1 (Backend + Datos)
**Rama:** `feature/backend-datos`
**Requisitos:** RF-05 (parcial), RF-11 (parcial — solo Manual)

---

## Objetivo

Implementar el CRUD de solicitudes de combustible manuales con ciclo de vida completo: creación, aprobación y rechazo. La fase cubre CU-06 (crear solicitud) y CU-07 (aprobar/rechazar solicitud) del SRS.

### Fuera de alcance (diferido)

- Solicitudes automáticas programadas y recurrentes (RF-05, RF-11) — el SRS las menciona como requisito pero están explícitamente marcadas como **"según refinamiento"** en `docs/12-PLANIFICACION.md`. No existen reglas de negocio, flujo ni campos de periodicidad definidos. Se implementarán en una Fase 3B separada una vez que el equipo las especifique.
- Generación de Ticket a partir de la solicitud aprobada (CU-08) — responsabilidad de **Builder 2**. Este PR solo cambia el estado de la solicitud a `Aprobada`; la creación del Ticket queda a cargo del módulo de Tickets/QR.

---

## Modelo de datos

### Cambios al modelo existente `SolicitudCombustible`

El modelo ya existe con todos los campos y FKs desde `InitialSchema`. Esta fase hace dos modificaciones:

1. **Cambiar `Estado` de `string` a `EstadoSolicitud`** (enum tipado, igual que `Ticket.Estado`)
2. **Agregar `MotivoRechazo`** (`string?`, nullable — solo se llena al rechazar)

```csharp
// backend/FuelTrack.Api/Models/SolicitudCombustible.cs
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

### Nuevo enum `EstadoSolicitud`

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

Ciclo de vida:
```
POST /solicitudes → Pendiente
POST /solicitudes/{id}/aprobar → Aprobada
POST /solicitudes/{id}/rechazar → Rechazada
```

Una solicitud `Aprobada` o `Rechazada` no puede volver a procesarse — 409 si se intenta.

### Cambio en `AppDbContext`

Agregar en `OnModelCreating` para que el enum se almacene como texto legible en PostgreSQL (mantiene la columna `text` existente, sin cambio de tipo de columna en BD):

```csharp
modelBuilder.Entity<SolicitudCombustible>()
    .Property(s => s.Estado)
    .HasConversion<string>();
```

### Migración `AddMotivoRechazoSolicitud`

Una sola migración que:
1. Agrega columna `MotivoRechazo` (`text`, nullable) a `SolicitudesCombustible`

> **Nota:** El campo `Estado` ya existe como `text` en BD. Al agregar `HasConversion<string>()` en `AppDbContext`, EF Core detectará el cambio en el snapshot pero no generará un `AlterColumn` porque el tipo de columna sigue siendo `text`. La migración solo tocará `MotivoRechazo`.

---

## Endpoints

**Ruta base:** `api/v1/solicitudes`

| Método | Ruta | Rol mínimo | Descripción |
|---|---|---|---|
| GET | `/api/v1/solicitudes` | Autenticado | Lista todas las solicitudes |
| GET | `/api/v1/solicitudes/{id}` | Autenticado | Detalle de una solicitud |
| POST | `/api/v1/solicitudes` | Administrador, Supervisor, Solicitante | Crear solicitud manual |
| POST | `/api/v1/solicitudes/{id}/aprobar` | Administrador, Supervisor | Aprobar solicitud pendiente |
| POST | `/api/v1/solicitudes/{id}/rechazar` | Administrador, Supervisor | Rechazar solicitud pendiente |

No hay `PUT` ni `DELETE` — el ciclo de vida se gestiona exclusivamente mediante `/aprobar` y `/rechazar`.

---

## DTOs

### `SolicitudDto` — respuesta

```csharp
// backend/FuelTrack.Api/DTOs/Solicitudes/SolicitudDto.cs
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

### `CreateSolicitudRequest` — crear

```csharp
// backend/FuelTrack.Api/DTOs/Solicitudes/CreateSolicitudRequest.cs
public record CreateSolicitudRequest(
    [Required, Range(0.0001, 999999.9999)] decimal CantidadSolicitada,
    [Required] int EmpleadoId,
    [Required] int VehiculoId,
    [Required] int DepartamentoId,
    [Required] int TipoCombustibleId,
    DateTime? FechaVencimiento
);
```

`TipoSolicitud` y `FechaSolicitud` los asigna el servidor (`"Manual"` y `DateTime.UtcNow`). El cliente no los envía.

### `AprobarSolicitudRequest` — aprobar

```csharp
// backend/FuelTrack.Api/DTOs/Solicitudes/AprobarSolicitudRequest.cs
public record AprobarSolicitudRequest(
    [Required, Range(0.0001, 999999.9999)] decimal CantidadAutorizada
);
```

La `CantidadAutorizada` puede ser diferente (menor o mayor) a la `CantidadSolicitada` — es decisión del Supervisor.

### `RechazarSolicitudRequest` — rechazar

```csharp
// backend/FuelTrack.Api/DTOs/Solicitudes/RechazarSolicitudRequest.cs
public record RechazarSolicitudRequest(
    [Required, MaxLength(500)] string MotivoRechazo
);
```

---

## Controller

### Patrón de implementación

Idéntico al de Fases 2 y Bloque A: controller accede directamente a `AppDbContext`, sin capa de servicio. DTOs `record` separados. Autorización declarativa con `[Authorize]` y `[Authorize(Roles = ...)]`.

### Validaciones de negocio

**`POST /api/v1/solicitudes` (Create):**
- 400 `EMPLEADO_NOT_FOUND` si `EmpleadoId` no existe
- 400 `VEHICULO_NOT_FOUND` si `VehiculoId` no existe
- 400 `DEPARTAMENTO_NOT_FOUND` si `DepartamentoId` no existe
- 400 `TIPO_COMBUSTIBLE_NOT_FOUND` si `TipoCombustibleId` no existe
- Asigna: `TipoSolicitud = "Manual"`, `Estado = Pendiente`, `FechaSolicitud = DateTime.UtcNow`
- Retorna 201 con `SolicitudDto` completo (Include de las 4 navegaciones)

**`POST /api/v1/solicitudes/{id}/aprobar` (Aprobar):**
- 404 si la solicitud no existe
- 409 `SOLICITUD_YA_PROCESADA` si `Estado != Pendiente`
- Asigna: `Estado = Aprobada`, `CantidadAutorizada = req.CantidadAutorizada`
- Retorna 200 con `SolicitudDto` actualizado

**`POST /api/v1/solicitudes/{id}/rechazar` (Rechazar):**
- 404 si la solicitud no existe
- 409 `SOLICITUD_YA_PROCESADA` si `Estado != Pendiente`
- Asigna: `Estado = Rechazada`, `MotivoRechazo = req.MotivoRechazo`
- Retorna 200 con `SolicitudDto` actualizado

### Skeleton del controller

```csharp
// backend/FuelTrack.Api/Controllers/SolicitudesController.cs
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

---

## Tests

**Archivo:** `backend/FuelTrack.Api.Tests/Controllers/SolicitudesControllerTests.cs`

**Setup:** SQLite in-memory idéntico al patrón establecido. Por las 4 FKs requeridas, el archivo incluye helpers privados para crear entidades de soporte:

```csharp
private async Task<(Empleado empleado, Vehiculo vehiculo, Departamento departamento, TipoCombustible tipo)>
    CrearDependenciasAsync()
```

**Casos de prueba (mínimo 13):**

| # | Test | Qué verifica |
|---|---|---|
| 1 | `GetAll_ReturnsEmptyList_WhenNoData` | Lista vacía |
| 2 | `GetById_ReturnsNotFound_WhenMissing` | 404 en GET por id |
| 3 | `GetById_ReturnsDto_WhenExists` | DTO con navegaciones aplanadas |
| 4 | `Create_Returns201_ConDto` | 201 + TipoSolicitud="Manual", Estado=Pendiente, FechaSolicitud != default |
| 5 | `Create_Returns400_CuandoEmpleadoNoExiste` | 400 EMPLEADO_NOT_FOUND |
| 6 | `Create_Returns400_CuandoVehiculoNoExiste` | 400 VEHICULO_NOT_FOUND |
| 7 | `Create_Returns400_CuandoDepartamentoNoExiste` | 400 DEPARTAMENTO_NOT_FOUND |
| 8 | `Create_Returns400_CuandoTipoCombustibleNoExiste` | 400 TIPO_COMBUSTIBLE_NOT_FOUND |
| 9 | `Aprobar_Returns200_ConCantidadAutorizada` | 200 + CantidadAutorizada en DTO, Estado=Aprobada |
| 10 | `Aprobar_Returns404_CuandoNoExiste` | 404 |
| 11 | `Aprobar_Returns409_CuandoYaFueProcesada` | 409 SOLICITUD_YA_PROCESADA (intentar aprobar una ya Aprobada) |
| 12 | `Rechazar_Returns200_ConMotivoRechazo` | 200 + MotivoRechazo en DTO, Estado=Rechazada |
| 13 | `Rechazar_Returns404_CuandoNoExiste` | 404 |
| 14 | `Rechazar_Returns409_CuandoYaFueProcesada` | 409 SOLICITUD_YA_PROCESADA (intentar rechazar una ya Rechazada) |

---

## Mapa de archivos

### Crear

```
backend/FuelTrack.Api/Models/Enums/EstadoSolicitud.cs
backend/FuelTrack.Api/DTOs/Solicitudes/SolicitudDto.cs
backend/FuelTrack.Api/DTOs/Solicitudes/CreateSolicitudRequest.cs
backend/FuelTrack.Api/DTOs/Solicitudes/AprobarSolicitudRequest.cs
backend/FuelTrack.Api/DTOs/Solicitudes/RechazarSolicitudRequest.cs
backend/FuelTrack.Api/Controllers/SolicitudesController.cs
backend/FuelTrack.Api.Tests/Controllers/SolicitudesControllerTests.cs
backend/FuelTrack.Api/Migrations/<timestamp>_AddMotivoRechazoSolicitud.cs  ← generado por EF
```

### Modificar

```
backend/FuelTrack.Api/Models/SolicitudCombustible.cs  ← Estado → EstadoSolicitud, + MotivoRechazo
backend/FuelTrack.Api/Data/AppDbContext.cs             ← HasConversion<string>() para Estado
```

---

## Criterios de aceptación para testers

- `POST /api/v1/solicitudes` crea con `Estado = "Pendiente"`, `TipoSolicitud = "Manual"`, `FechaSolicitud` en UTC
- `POST /api/v1/solicitudes/{id}/aprobar` actualiza `Estado = "Aprobada"` y `CantidadAutorizada` — la cantidad puede diferir de la solicitada
- `POST /api/v1/solicitudes/{id}/rechazar` actualiza `Estado = "Rechazada"` y registra `MotivoRechazo`
- Intentar aprobar o rechazar una solicitud ya procesada devuelve 409 `SOLICITUD_YA_PROCESADA`
- Roles: POST crear → 403 sin rol Administrador, Supervisor o Solicitante; aprobar/rechazar → 403 sin rol Administrador o Supervisor
- En BD: columna `Estado` almacena texto `"Pendiente"`, `"Aprobada"`, `"Rechazada"` (no entero)
- La aprobación NO crea un Ticket — eso es responsabilidad de Builder 2

---

## Nota para Builder 2

Cuando una `SolicitudCombustible` pasa a estado `"Aprobada"`, contiene:
- `CantidadAutorizada` (decimal) — la cantidad que el Supervisor autorizó
- `EmpleadoId`, `VehiculoId`, `DepartamentoId`, `TipoCombustibleId` — datos para el Ticket
- `Id` de la solicitud — disponible como `Ticket.SolicitudId` (FK nullable ya existe en el modelo)

El Ticket debe crearse tomando estos datos de la solicitud aprobada. El campo `Ticket.SolicitudId` ya está definido como FK nullable hacia `SolicitudesCombustible`.
