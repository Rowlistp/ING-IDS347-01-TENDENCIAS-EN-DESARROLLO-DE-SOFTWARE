# Fase 6 — Inventario Completo: Design Spec

## Objetivo

Implementar el control de inventario de combustible (RF-14 a RF-17): recepciones de combustible desde proveedor, ajustes manuales, transferencias entre tanques e historial de movimientos.

## Alcance

Esta fase cubre exclusivamente el inventario (Fase 6). Fase 7 (Cierre diario) se planifica por separado debido a su dependencia en `Despacho` (Builder 2).

---

## Arquitectura

Tres controladores nuevos que siguen el patrón directo `AppDbContext` del repo (sin capa de servicio):

| Controlador | Ruta base | Responsabilidad |
|---|---|---|
| `RecepcionesController` | `/api/v1/recepciones` | CRUD recepciones + cascade inventario |
| `InventarioController` | `/api/v1/inventario` | Estado actual, ajustes, transferencias |
| `MovimientosController` | `/api/v1/inventario/movimientos` | Historial de movimientos |

### Regla transversal

Todo cambio de inventario crea un `MovimientoInventario` asociado y actualiza `Inventario.ExistenciaActual`, `Inventario.Disponibilidad` e `Inventario.UltimaActualizacion` — todo en una sola transacción (`SaveChangesAsync`).

---

## Endpoints

### RecepcionesController

```
GET  /api/v1/recepciones       → lista todas las recepciones
GET  /api/v1/recepciones/{id}  → detalle por ID
POST /api/v1/recepciones       → [Administrador, Supervisor]
```

**POST /api/v1/recepciones — cascade:**
1. Validar `ProveedorId` existe
2. Validar `TanqueId` existe y `Tanque.Activo == true`
3. Crear `RecepcionCombustible`
4. Crear `MovimientoInventario { Tipo=Entrada, Volumen=VolumenRecibido, ReferenciaOperacion=NumeroFactura, UsuarioId=usuarioActual }`
5. Actualizar `Inventario`: `ExistenciaActual += VolumenRecibido`, `Disponibilidad += VolumenRecibido`, `UltimaActualizacion = UtcNow`
6. `SaveChangesAsync` (atómico)

### InventarioController

```
GET  /api/v1/inventario              → lista todos los inventarios (con datos del tanque)
GET  /api/v1/inventario/{tanqueId}   → inventario de un tanque
POST /api/v1/inventario/ajustes      → [Administrador]
POST /api/v1/inventario/transferencias → [Administrador, Supervisor]
```

**POST /ajustes — cascade:**
1. Validar `TanqueId` existe y `Tanque.Activo == true`
2. Validar que `Inventario.ExistenciaActual + Volumen >= 0` (no puede quedar negativo)
3. Crear `MovimientoInventario { Tipo=Ajuste, Volumen=Volumen, ReferenciaOperacion=null, UsuarioId=usuarioActual }`
   - `Observaciones` es obligatorio (trazabilidad de la justificación)
   - `Volumen` se almacena con signo: positivo = incremento, negativo = reducción
4. Actualizar `Inventario`: `ExistenciaActual += Volumen`, `Disponibilidad += Volumen`, `UltimaActualizacion = UtcNow`
5. `SaveChangesAsync`

> `Volumen` es signed en todo el sistema: positivo suma, negativo resta. Se almacena como fue recibido — nunca con `Math.Abs()`. El signo en el historial de movimientos indica la dirección del cambio.

**POST /transferencias — cascade:**
1. Validar `TanqueOrigenId != TanqueDestinoId`
2. Validar ambos tanques existen y están activos
3. Validar `Inventario.ExistenciaActual` del origen `>= Volumen`
4. Crear `MovimientoInventario` en origen: `{ Tipo=Transferencia, Volumen=-Volumen, ReferenciaOperacion="HACIA-TANQUE-{destinoId}", UsuarioId }`
5. Crear `MovimientoInventario` en destino: `{ Tipo=Transferencia, Volumen=Volumen, ReferenciaOperacion="DESDE-TANQUE-{origenId}", UsuarioId }`
6. Actualizar `Inventario` origen: `ExistenciaActual -= Volumen`, `Disponibilidad -= Volumen`
7. Actualizar `Inventario` destino: `ExistenciaActual += Volumen`, `Disponibilidad += Volumen`
8. `SaveChangesAsync` (ambas actualizaciones en la misma transacción)

### MovimientosController

```
GET /api/v1/inventario/movimientos              → historial completo
GET /api/v1/inventario/movimientos?tanqueId={n} → filtrado por tanque
```

---

## DTOs

### Recepciones

```csharp
// Request
record CreateRecepcionRequest(
    [Required] int ProveedorId,
    [Required] int TanqueId,
    [Required, MaxLength(100)] string NumeroFactura,
    [Required, Range(0.0001, 999999.9999)] decimal VolumenRecibido,
    [Required] DateTime Fecha
);

// Response
record RecepcionDto(
    int Id,
    string NumeroFactura,
    decimal VolumenRecibido,
    DateTime Fecha,
    int ProveedorId, string ProveedorNombre,
    int TanqueId, string TanqueIdentificacion
);
```

### Inventario

```csharp
// Response
record InventarioDto(
    int Id,
    decimal ExistenciaActual,
    decimal Disponibilidad,
    DateTime UltimaActualizacion,
    int TanqueId, string TanqueIdentificacion, decimal TanqueCapacidad
);

// Ajuste request
record AjustarInventarioRequest(
    [Required] int TanqueId,
    decimal Volumen,                      // signed: positivo suma, negativo resta; 0 es no-op válido
    [Required, MaxLength(500)] string Observaciones
);

// Transferencia request
record TransferirRequest(
    [Required] int TanqueOrigenId,
    [Required] int TanqueDestinoId,
    [Required, Range(0.0001, 999999.9999)] decimal Volumen,
    [MaxLength(500)] string? Observaciones
);

// Transferencia response
record TransferenciaResultDto(
    InventarioDto Origen,
    InventarioDto Destino
);
```

### Movimientos

```csharp
// Response
record MovimientoDto(
    int Id,
    TipoMovimiento Tipo,   // serializa como int: 0=Entrada,1=Salida,2=Ajuste,3=Transferencia,4=Merma
    decimal Volumen,       // signed: negativo en origen de transferencia y ajuste de reducción
    DateTime FechaHora,
    string? ReferenciaOperacion,
    string? Observaciones,
    int TanqueId, string TanqueIdentificacion,
    int UsuarioId, string UsuarioNombreUsuario  // mapea a Usuario.NombreUsuario
);
```

---

## Errores de negocio

| Código | HTTP | Cuándo | Endpoint |
|---|---|---|---|
| `PROVEEDOR_NOT_FOUND` | 400 | ProveedorId no existe | POST /recepciones |
| `TANQUE_NOT_FOUND` | 400 | TanqueId no existe | POST /recepciones, POST /ajustes |
| `TANQUE_INACTIVO` | 400 | Tanque.Activo == false | POST /recepciones, POST /ajustes |
| `TANQUE_ORIGEN_NOT_FOUND` | 400 | TanqueOrigenId no existe | POST /transferencias |
| `TANQUE_ORIGEN_INACTIVO` | 400 | TanqueOrigen.Activo == false | POST /transferencias |
| `TANQUE_DESTINO_NOT_FOUND` | 400 | TanqueDestinoId no existe | POST /transferencias |
| `TANQUE_DESTINO_INACTIVO` | 400 | TanqueDestino.Activo == false | POST /transferencias |
| `TANQUE_ORIGEN_IGUAL_DESTINO` | 400 | TanqueOrigenId == TanqueDestinoId | POST /transferencias |
| `INVENTARIO_INSUFICIENTE` | 409 | Ajuste: `ExistenciaActual + Volumen < 0`; Transferencia: `ExistenciaActual < Volumen` | POST /ajustes, POST /transferencias |

Formato: `{ "code": "...", "message": "..." }` (igual que otros controladores).

---

## Autorización

| Operación | Roles |
|---|---|
| `POST /recepciones` | Administrador, Supervisor |
| `POST /inventario/ajustes` | Administrador |
| `POST /inventario/transferencias` | Administrador, Supervisor |
| `GET *` | Todos los autenticados (`[Authorize]` sin rol) |

Extracción del usuario actual: `int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId)` — patrón de `AuthController`/`UsersController`.

---

## Datos técnicos

- `TipoMovimiento` se almacena como `integer` en BD (schema inicial, `InitialSchema`). No requiere migración nueva para este enum.
- `MovimientoInventario.Volumen` es `decimal` con `HasPrecision(18,4)` — soporta valores negativos en PostgreSQL `numeric(18,4)`. Ya configurado en `AppDbContext`.
- `RecepcionCombustible.VolumenRecibido` es `decimal` con `HasPrecision(18,4)` — ya configurado.
- No se requieren migraciones de schema: todas las tablas existen desde `InitialSchema`.
- `AppDbContext` ya tiene `DbSet` para `RecepcionesCombustible`, `Inventarios`, `MovimientosInventario`.
- **Invariante garantizada:** Todo `Tanque` tiene exactamente un `Inventario` asociado, creado automáticamente en el POST de `TanquesController` (Bloque A). Si el `Tanque` existe, su `Inventario` existe. No se requieren null-checks adicionales para el `Inventario` después de confirmar que el `Tanque` existe.

---

## Tests

Patrón MSTest + SQLite in-memory (igual que `SolicitudesControllerTests`):
- `[TestInitialize]`: conexión SQLite abierta + `EnsureCreatedAsync`
- `[TestCleanup]`: dispose
- Helper `CrearDependenciasAsync()` que inserta en este orden:
  1. `TipoCombustible` (FK requerida por Tanque)
  2. `Tanque` (Activo=true)
  3. `Inventario` con TanqueId=tanque.Id, ExistenciaActual=0, Disponibilidad=0 — **creado explícitamente** porque la auto-creación ocurre en TanquesController, no en EF; en tests se inserta directo al DbContext
  4. `Proveedor`
  5. `Usuario` (FK requerida por MovimientoInventario)
  6. `SaveChangesAsync`

Cobertura mínima por controlador:
- `RecepcionesController`: GetAll, GetById, Create happy (verifica RecepcionCombustible + MovimientoInventario + Inventario actualizado), Create proveedor no existe, Create tanque no existe, Create tanque inactivo
- `InventarioController`: GetAll, GetById, Ajustar happy (verifica Inventario actualizado + MovimientoInventario), Ajustar tanque no existe, Ajustar inventario insuficiente (Volumen negativo que deja negativo), Transferir happy (verifica ambos Inventarios + 2 MovimientosInventario), Transferir mismo tanque, Transferir origen insuficiente
- `MovimientosController`: GetAll (sin filtro), GetAll con `?tanqueId=` que filtra correctamente

---

## Trazabilidad

| Endpoint | RF cubierto |
|---|---|
| POST /recepciones | RF-14, RF-16 |
| POST /inventario/ajustes | RF-14, RF-15 |
| POST /inventario/transferencias | RF-14, RF-15 |
| GET /inventario | RF-15 |
| GET /inventario/movimientos | RF-17 |
