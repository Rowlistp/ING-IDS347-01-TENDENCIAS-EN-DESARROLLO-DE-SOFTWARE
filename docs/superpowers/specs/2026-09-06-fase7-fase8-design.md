# Diseño — Fase 7 (Cierre Diario) y Fase 8 (Reportes + Dashboard)

**Fecha:** 2026-09-06  
**Autor:** Builder 1  
**Requisitos:** RF-18 (cierre diario), RF-19 (reportes), RF-20 (exportación), RF-22 (dashboard ejecutivo)  
**Rama:** `feature/backend-datos`

---

## 1. Contexto

Con las Fases 0–6 mergeadas en `main` y las Fases 1, 4 y 5 de Builder 2 integradas (PR #9), el backend tiene:
- Solicitudes, tickets, despachos y movimientos de inventario completos.
- `CierresDiarios` como tabla stub (sin FK, sin PDF, sin detalle por tanque).
- Patrón de servicios establecido por Builder 2: `DispatchService`, `TicketService`, `AuditService`.

Este documento especifica la implementación de dos módulos nuevos:
- **Fase 7:** generación de cierre diario con PDF acta y desglose por tanque.
- **Fase 8:** reportes filtrables + exportación CSV/Excel/PDF + dashboard ejecutivo.

---

## 2. Decisiones de diseño

| Decisión | Elección | Razón |
|---|---|---|
| Arquitectura | Capa de servicios (como `DispatchService`) | Los controllers permanecen delgados; la lógica es testeable |
| Granularidad del cierre | Global por día + detalle por tanque | Un tanque puede tener combustible distinto; las diferencias son por tanque |
| `InventarioInicial` | Reconstruido desde `MovimientosInventario` | Permite cierres de fechas pasadas sin campo histórico adicional |
| PDF acta | QuestPDF inline en columna `bytea` | Consistente con `QrCodePng` (Ticket) y `TicketPdfService` |
| Excel | ClosedXML | MIT, sin licencia comercial; API fluida |
| CSV | StringBuilder nativo | Sin dependencia externa |
| Endpoint de reportes | `tipo` param + filtros de fecha/tanque | Un solo endpoint cubre RF-19 y sus cuatro dimensiones |

---

## 3. Modelo de datos

### 3.1 Migración: `AddCierreDiarioDetalle`

**`CierresDiarios` (ALTER):**

```sql
-- DROP columnas stub sin uso real
ALTER TABLE "CierresDiarios" DROP COLUMN "ActaDigital";
ALTER TABLE "CierresDiarios" DROP COLUMN "ReporteUrl";

-- ADD columnas definitivas
ALTER TABLE "CierresDiarios" ADD "PdfActa"        bytea;
ALTER TABLE "CierresDiarios" ADD "TotalDespachos" integer NOT NULL DEFAULT 0;
ALTER TABLE "CierresDiarios" ADD "CreadoPorId"    integer NOT NULL;
ALTER TABLE "CierresDiarios" ADD "CreadoEn"       timestamptz NOT NULL;

ALTER TABLE "CierresDiarios"
  ADD CONSTRAINT "FK_CierresDiarios_Usuarios_CreadoPorId"
  FOREIGN KEY ("CreadoPorId") REFERENCES "Usuarios"("Id") ON DELETE RESTRICT;
```

Las columnas `VolumenDespachado`, `InventarioFinal`, `Diferencias` permanecen como totales del día.  
El índice único `IX_CierresDiarios_Fecha` ya existe en BD — no se recrea.

**`CierresDiariosDetalle` (CREATE):**

```sql
CREATE TABLE "CierresDiariosDetalle" (
  "Id"                SERIAL PRIMARY KEY,
  "CierreDiarioId"    integer NOT NULL
    REFERENCES "CierresDiarios"("Id") ON DELETE CASCADE,
  "TanqueId"          integer NOT NULL
    REFERENCES "Tanques"("Id") ON DELETE RESTRICT,
  "NumeroDespachos"   integer NOT NULL,
  "VolumenDespachado" numeric(18,4) NOT NULL,
  "VolumenRecibido"   numeric(18,4) NOT NULL,
  "InventarioInicial" numeric(18,4) NOT NULL,
  "InventarioFinal"   numeric(18,4) NOT NULL,
  "Diferencias"       numeric(18,4) NOT NULL,
  UNIQUE ("CierreDiarioId", "TanqueId")
);
```

### 3.2 Modelos C#

**`CierreDiario` (actualizar stub):**

```csharp
public class CierreDiario
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal VolumenDespachado { get; set; }   // total día
    public decimal InventarioFinal { get; set; }     // suma inventarios finales
    public decimal Diferencias { get; set; }         // suma diferencias
    public int TotalDespachos { get; set; }
    public byte[]? PdfActa { get; set; }
    public int CreadoPorId { get; set; }
    public Usuario CreadoPor { get; set; } = null!;
    public DateTime CreadoEn { get; set; }
    public ICollection<CierreDiarioDetalle> Detalles { get; set; } = [];
}
```

**`CierreDiarioDetalle` (nuevo):**

```csharp
public class CierreDiarioDetalle
{
    public int Id { get; set; }
    public int CierreDiarioId { get; set; }
    public CierreDiario CierreDiario { get; set; } = null!;
    public int TanqueId { get; set; }
    public Tanque Tanque { get; set; } = null!;
    public int NumeroDespachos { get; set; }
    public decimal VolumenDespachado { get; set; }
    public decimal VolumenRecibido { get; set; }
    public decimal InventarioInicial { get; set; }
    public decimal InventarioFinal { get; set; }
    public decimal Diferencias { get; set; }
}
```

### 3.3 Fórmulas de cálculo

```
dayStart = fecha 00:00:00 UTC
dayEnd   = fecha 23:59:59.9999999 UTC

// Por tanque:
movsDia       = SUM(MovimientosInventario.Volumen WHERE TanqueId=X AND dayStart ≤ FechaHora ≤ dayEnd)
movsDespues   = SUM(MovimientosInventario.Volumen WHERE TanqueId=X AND FechaHora > dayEnd)

InventarioFinalReal   = Inventarios.ExistenciaActual - movsDespues
InventarioInicial     = InventarioFinalReal - movsDia

VolumenDespachado     = SUM(Despachos.GalonesServidos WHERE TanqueId=X AND Fecha=fecha)
NumeroDespachos       = COUNT(Despachos WHERE TanqueId=X AND Fecha=fecha)
VolumenRecibido       = SUM(RecepcionesCombustible.VolumenRecibido WHERE TanqueId=X AND dayStart ≤ Fecha ≤ dayEnd)

InventarioFinalTeorico = InventarioInicial + VolumenRecibido - VolumenDespachado
Diferencias            = InventarioFinalReal - InventarioFinalTeorico

// Totales del CierreDiario:
CierreDiario.VolumenDespachado = SUM(detalle.VolumenDespachado)
CierreDiario.InventarioFinal   = SUM(detalle.InventarioFinal)
CierreDiario.Diferencias       = SUM(detalle.Diferencias)
CierreDiario.TotalDespachos    = SUM(detalle.NumeroDespachos)
```

**Nota:** Solo se incluyen tanques que tengan al menos un despacho en el día. Tanques sin actividad no generan `CierreDiarioDetalle`.

### 3.4 Cambios en `AppDbContext`

```csharp
// Nuevo DbSet
public DbSet<CierreDiarioDetalle> CierresDiariosDetalle => Set<CierreDiarioDetalle>();

// OnModelCreating
modelBuilder.Entity<CierreDiario>()
    .HasMany(c => c.Detalles)
    .WithOne(d => d.CierreDiario)
    .HasForeignKey(d => d.CierreDiarioId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<CierreDiarioDetalle>()
    .HasIndex(d => new { d.CierreDiarioId, d.TanqueId }).IsUnique();

// Precisiones decimales para CierreDiario (existentes)
// + nuevas para CierreDiarioDetalle
modelBuilder.Entity<CierreDiarioDetalle>().Property(d => d.VolumenDespachado).HasPrecision(18, 4);
modelBuilder.Entity<CierreDiarioDetalle>().Property(d => d.VolumenRecibido).HasPrecision(18, 4);
modelBuilder.Entity<CierreDiarioDetalle>().Property(d => d.InventarioInicial).HasPrecision(18, 4);
modelBuilder.Entity<CierreDiarioDetalle>().Property(d => d.InventarioFinal).HasPrecision(18, 4);
modelBuilder.Entity<CierreDiarioDetalle>().Property(d => d.Diferencias).HasPrecision(18, 4);
// FK con Usuarios
modelBuilder.Entity<CierreDiario>()
    .HasOne(c => c.CreadoPor)
    .WithMany()
    .HasForeignKey(c => c.CreadoPorId)
    .OnDelete(DeleteBehavior.Restrict);
```

---

## 4. Fase 7 — Cierre Diario

### 4.1 API

```
GET  /api/v1/cierres-diarios                      Administrador, Supervisor, Auditor
GET  /api/v1/cierres-diarios/{id}                 Administrador, Supervisor, Auditor
GET  /api/v1/cierres-diarios/{id}/pdf             Administrador, Supervisor, Auditor
POST /api/v1/cierres-diarios                      Administrador, Supervisor
```

### 4.2 Contratos

**Request POST:**
```json
{ "fecha": "2026-09-06" }
```

**Response 201:**
```json
{
  "id": 1,
  "fecha": "2026-09-06",
  "totalDespachos": 12,
  "totalVolumenDespachado": 480.0000,
  "totalInventarioFinal": 3200.0000,
  "totalDiferencias": -2.5000,
  "creadoPorId": 3,
  "creadoEn": "2026-09-06T23:00:00Z",
  "pdfDisponible": true,
  "detalles": [
    {
      "tanqueId": 1,
      "tanqueIdentificacion": "TQ-001",
      "tipoCombustible": "Gasolina 95",
      "numeroDespachos": 8,
      "volumenDespachado": 320.0000,
      "volumenRecibido": 0.0000,
      "inventarioInicial": 1500.0000,
      "inventarioFinal": 1180.0000,
      "diferencias": 0.0000
    }
  ]
}
```

**GET `/{id}/pdf`** — devuelve `application/pdf` del acta de cierre (bytes de `PdfActa`).

### 4.3 Errores de negocio

| Código | HTTP | Condición |
|---|---|---|
| `FECHA_FUTURA` | 400 | `fecha > today` |
| `SIN_DESPACHOS` | 400 | No hay despachos en la fecha |
| `CIERRE_YA_EXISTE` | 409 | Unique constraint en Fecha |

### 4.4 Servicio: `CierreDiarioService`

```csharp
public sealed class CierreDiarioService(AppDbContext db, AuditService audit)
{
    public async Task<CierreDiarioResponse> GenerarAsync(DateOnly fecha, int actorId, string? ip, CancellationToken ct);
    public async Task<IReadOnlyList<CierreDiarioResponse>> GetAllAsync(int pagina, int tamanoPagina, CancellationToken ct);
    public async Task<CierreDiarioResponse?> GetByIdAsync(int id, CancellationToken ct);
    public async Task<byte[]?> GetPdfAsync(int id, CancellationToken ct);
}
```

- `GenerarAsync` abre transacción, calcula por tanque, genera PDF con QuestPDF, persiste cierre + detalles, escribe auditoría `CIERRE_GENERADO`, hace commit.
- Lanza excepción con `{ code, message }` para errores de negocio (patrón `TicketDomainException`).

### 4.5 PDF del acta (QuestPDF)

Documento A4 con:
- Encabezado: "FuelTrack — Acta de Cierre Diario" + fecha
- Tabla de detalles por tanque: Tanque, Tipo Combustible, Despachos, Vol. Despachado, Vol. Recibido, Inv. Inicial, Inv. Final, Diferencias
- Totales al pie de tabla
- Generado por: `{CreadoPor.NombreUsuario}` — `{CreadoEn} UTC` (no es firma criptográfica, es trazabilidad del acta)
- Footer: número de página

---

## 5. Fase 8 — Reportes y Dashboard

### 5.1 API

```
GET /api/v1/reportes                  Administrador, Auditor
GET /api/v1/reportes/exportar         Administrador, Auditor
GET /api/v1/dashboard/resumen         Administrador
```

### 5.2 Parámetros de `GET /reportes`

| Param | Tipo | Requerido | Valores |
|---|---|---|---|
| `tipo` | string | sí | `solicitudes`, `despachos`, `inventario`, `cierres` |
| `fechaDesde` | DateOnly? | no | — |
| `fechaHasta` | DateOnly? | no | — |
| `tanqueId` | int? | no | solo aplica a `despachos` e `inventario` |
| `pagina` | int | no | default 1 |
| `tamanoPagina` | int | no | default 20, max 100 |

### 5.3 Parámetros de `GET /reportes/exportar`

Mismos filtros que `GET /reportes` (sin paginación) + `formato`: `csv`, `excel`, `pdf`.

Respuestas:
- `csv` → `text/csv; charset=utf-8`, nombre `reporte-{tipo}-{fecha}.csv`
- `excel` → `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- `pdf` → `application/pdf`

### 5.4 Datos por tipo de reporte

| `tipo` | Modelo fuente | Campos principales |
|---|---|---|
| `solicitudes` | `SolicitudCombustible` | FechaSolicitud, Empleado, Vehículo, Departamento, TipoCombustible, CantidadSolicitada, CantidadAutorizada, Estado |
| `despachos` | `Despacho` | Fecha, Hora, Ticket.Codigo, Empleado, Vehículo, GalonesServidos, Tanque, Estacion, Operador, InventarioRestante |
| `inventario` | `MovimientoInventario` | FechaHora, Tanque, TipoCombustible, Tipo (Entrada/Salida/Ajuste/…), Volumen, ReferenciaOperacion |
| `cierres` | `CierreDiario` + `CierreDiarioDetalle` | Fecha, TotalDespachos, VolumenDespachado, Diferencias, detalles por tanque |

**Error:** `tipo` no reconocido → `400 TIPO_REPORTE_INVALIDO`

### 5.5 Servicio: `ReporteService`

```csharp
public sealed class ReporteService(AppDbContext db)
{
    public async Task<ReportePageResponse> GetAsync(ReporteQuery q, CancellationToken ct);
    public async Task<byte[]> ExportarCsvAsync(ReporteQuery q, CancellationToken ct);
    public async Task<byte[]> ExportarExcelAsync(ReporteQuery q, CancellationToken ct);
    public async Task<byte[]> ExportarPdfAsync(ReporteQuery q, CancellationToken ct);
}
```

`ReporteQuery` es un record con los parámetros filtrados. `ReportePageResponse` contiene metadata (total, pagina, tamanoPagina) + una lista de DTOs específicos al tipo: `SolicitudReporteDto`, `DespachoReporteDto`, `MovimientoReporteDto` o `CierreReporteDto`. El controller serializa el tipo correcto según el parámetro `tipo`.

### 5.6 Dashboard: `GET /api/v1/dashboard/resumen`

**Response 200:**
```json
{
  "hoy": {
    "totalDespachos": 12,
    "volumenDespachado": 480.0,
    "solicitudesPendientes": 3,
    "tanquesConInventarioBajo": 1
  },
  "ultimos7Dias": [
    { "fecha": "2026-09-06", "volumenDespachado": 480.0, "totalDespachos": 12 }
  ],
  "top3TanquesMasUsados": [
    { "tanqueId": 1, "identificacion": "TQ-001", "totalGalones": 1200.0 }
  ],
  // top3 calculado sobre los últimos 30 días calendario
  "comparativaMes": {
    "mesActual": { "volumenDespachado": 2400.0, "solicitudes": 45 },
    "mesAnterior": { "volumenDespachado": 2100.0, "solicitudes": 38 }
  },
  "distribucionPorTipoCombustible": [
    { "tipoCombustible": "Gasolina 95", "porcentaje": 65.5 }
  ],
  "eficienciaAprobacion": {
    // calculado sobre el mes actual en curso
    "aprobadas": 42,
    "rechazadas": 3,
    "pendientes": 3,
    "tasaAprobacion": 93.3
  }
}
```

**Fuentes de datos:**
- `hoy.tanquesConInventarioBajo` → `Inventario.ExistenciaActual <= Tanque.NivelCritico` (campo existente en modelo `Tanque`)
- `comparativaMes` → `SolicitudCombustible.FechaSolicitud` + `Despacho.Fecha` filtrados por mes UTC
- Si no hay datos de mes anterior → devuelve ceros, no error

### 5.7 Servicio: `DashboardService`

```csharp
public sealed class DashboardService(AppDbContext db)
{
    public async Task<DashboardResumenResponse> GetResumenAsync(CancellationToken ct);
}
```

---

## 6. Manejo de errores

Patrón unificado del proyecto:
```json
{ "code": "CIERRE_YA_EXISTE", "message": "Ya existe un cierre para la fecha indicada." }
```

| Código | HTTP | Servicio |
|---|---|---|
| `FECHA_FUTURA` | 400 | CierreDiarioService |
| `SIN_DESPACHOS` | 400 | CierreDiarioService |
| `CIERRE_YA_EXISTE` | 409 | CierreDiarioService |
| `TIPO_REPORTE_INVALIDO` | 400 | ReporteService |
| `FORMATO_INVALIDO` | 400 | ReporteService |
| `CIERRE_NOT_FOUND` | 404 | CierresDiariosController |

---

## 7. Pruebas

| Test | Tipo | Cubre |
|---|---|---|
| `Generar_CierreDelDia_CreaEncabezadoYDetalles` | Unit | cálculo correcto de detalles por tanque |
| `Generar_FechaFutura_Retorna400` | Unit | validación de fecha |
| `Generar_FechaRepetida_Retorna409` | Unit | unique constraint |
| `Generar_SinDespachos_Retorna400` | Unit | sin actividad |
| `GetReporte_TipoInvalido_Retorna400` | Unit | validación de tipo |
| `ExportarCsv_Despachos_ContieneEncabezados` | Unit | formato CSV |
| `ExportarExcel_Solicitudes_ContieneHoja` | Unit | formato Excel |
| `Dashboard_SinDatos_DevuelveCeros` | Unit | mes sin datos |
| `Generar_InventarioInicialCorrecto_ConMovimientosPrevios` | Unit | fórmula InventarioInicial |

---

## 8. Dependencias nuevas

```xml
<!-- backend/FuelTrack.Api/FuelTrack.Api.csproj -->
<PackageReference Include="ClosedXML" Version="0.104.*" />
```

QuestPDF (`2026.8.0`) ya está instalado.

---

## 9. Archivos a crear/modificar

| Acción | Archivo |
|---|---|
| MODIFY | `backend/FuelTrack.Api/Models/CierreDiario.cs` |
| CREATE | `backend/FuelTrack.Api/Models/CierreDiarioDetalle.cs` |
| MODIFY | `backend/FuelTrack.Api/Data/AppDbContext.cs` |
| CREATE | `backend/FuelTrack.Api/Migrations/AddCierreDiarioDetalle.cs` (via `dotnet ef migrations add`) |
| CREATE | `backend/FuelTrack.Api/Services/CierreDiarioService.cs` |
| CREATE | `backend/FuelTrack.Api/Services/ReporteService.cs` |
| CREATE | `backend/FuelTrack.Api/Services/DashboardService.cs` |
| CREATE | `backend/FuelTrack.Api/Controllers/CierresDiariosController.cs` |
| CREATE | `backend/FuelTrack.Api/Controllers/ReportesController.cs` |
| CREATE | `backend/FuelTrack.Api/Controllers/DashboardController.cs` |
| CREATE | `backend/FuelTrack.Api/DTOs/Cierres/` (Request, Response, DetalleResponse) |
| CREATE | `backend/FuelTrack.Api/DTOs/Reportes/` (Query, PageResponse, item DTOs) |
| CREATE | `backend/FuelTrack.Api/DTOs/Dashboard/DashboardResumenResponse.cs` |
| MODIFY | `backend/FuelTrack.Api/Program.cs` (registrar 3 servicios) |
| MODIFY | `backend/FuelTrack.Api/FuelTrack.Api.csproj` (agregar ClosedXML) |
| MODIFY | `docs/06-API.md` (contratos definitivos) |
| CREATE | `backend/FuelTrack.Api.Tests/Services/CierreDiarioServiceTests.cs` |
| CREATE | `backend/FuelTrack.Api.Tests/Services/ReporteServiceTests.cs` |
| CREATE | `backend/FuelTrack.Api.Tests/Services/DashboardServiceTests.cs` |
