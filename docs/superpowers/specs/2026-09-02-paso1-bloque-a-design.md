# Spec: Paso 1 + Bloque A — Contrato API y Catálogos de Inventario

**Fecha:** 2026-09-02
**Builder:** Builder 1 (Backend + Datos)
**Rama:** `feature/backend-datos`

---

## Objetivo

Dos entregables independientes en un mismo PR:

1. **Paso 1:** Actualizar `docs/06-API.md` para que refleje las rutas en español ya implementadas y las nuevas que se van a agregar. El doc pasa de ser una propuesta inicial a ser el contrato real del equipo.
2. **Bloque A:** CRUD de los tres catálogos que el módulo de inventario necesita como base: `TiposCombustible`, `Tanques` y `Proveedores`.

---

## Paso 1 — Actualización de `06-API.md`

### Qué cambia

Reemplazar todas las rutas en inglés por rutas en español, alineándolas con:
- Lo ya implementado en Fases 1 y 2 (auth, usuarios, departamentos, empleados, vehículos)
- Los nuevos endpoints del Bloque A y los que vendrán en Fases 3, 6, 7 y 8

### Tabla de rutas actualizadas

| Inglés (anterior) | Español (nuevo) |
|---|---|
| `/api/v1/employees` | `/api/v1/empleados` |
| `/api/v1/vehicles` | `/api/v1/vehiculos` |
| `/api/v1/departments` | `/api/v1/departamentos` |
| `/api/v1/fuel-requests` | `/api/v1/solicitudes` |
| `/api/v1/tickets` | `/api/v1/tickets` (sin cambio) |
| `/api/v1/dispatches` | `/api/v1/despachos` |
| `/api/v1/inventory` | `/api/v1/inventario` |
| `/api/v1/inventory/movements` | `/api/v1/inventario/movimientos` |
| `/api/v1/inventory/adjustments` | `/api/v1/inventario/ajustes` |
| `/api/v1/inventory/transfers` | `/api/v1/inventario/transferencias` |
| `/api/v1/receipts` | `/api/v1/recepciones` |
| `/api/v1/daily-closures` | `/api/v1/cierres-diarios` |
| `/api/v1/reports` | `/api/v1/reportes` |
| `/api/v1/audit` | `/api/v1/auditoria` |
| `/api/v1/dashboard/summary` | `/api/v1/dashboard/resumen` |

### Nuevos endpoints agregados (Bloque A)

```
GET    /api/v1/tipos-combustible
GET    /api/v1/tipos-combustible/{id}
POST   /api/v1/tipos-combustible
PUT    /api/v1/tipos-combustible/{id}
DELETE /api/v1/tipos-combustible/{id}

GET    /api/v1/tanques
GET    /api/v1/tanques/{id}
POST   /api/v1/tanques
PUT    /api/v1/tanques/{id}
DELETE /api/v1/tanques/{id}

GET    /api/v1/proveedores
GET    /api/v1/proveedores/{id}
POST   /api/v1/proveedores
PUT    /api/v1/proveedores/{id}
DELETE /api/v1/proveedores/{id}
```

---

## Bloque A — Catálogos de Inventario

### Patrón de implementación

Idéntico al de Fase 2: controladores acceden directamente a `AppDbContext`, sin capa de servicio. DTOs `record` separados por entidad (request/response). Soft delete (`Activo = false`). Autorización: GET → cualquier autenticado; POST/PUT → Admin o Supervisor; DELETE → solo Admin.

### Migración de BD

Una sola migración `AddActivoTanqueProveedorIndices` que agrega:
1. `Tanques.Activo` (bool, default `true`) — `Tanque.Identificacion` ya tiene índice único en `AppDbContext`, no se recrea
2. `Proveedores.Activo` (bool, default `true`)
3. Índice único en `Proveedores.Rnc` — no existe en la BD actual
4. Índice único en `TiposCombustible.Nombre` — no existe en la BD actual

`TipoCombustible.Activo` ya existe. `Tanque.Identificacion` ya tiene índice único.

> **Nota:** `Tanques.TipoCombustibleId` tiene `Cascade` en la migración inicial. Como el DELETE de TipoCombustible es soft (`Activo = false`), esta cascada nunca se activa. No requiere cambio.

### TiposCombustible

**Modelo existente:** `Id`, `Nombre`, `Activo`

**DTOs:**
- `TipoCombustibleDto(int Id, string Nombre, bool Activo)`
- `SaveTipoCombustibleRequest([Required, MaxLength(50)] string Nombre, bool Activo = true)`

**Validación:** `Nombre` único — 409 Conflict si existe otro con el mismo nombre.

**Ruta:** `api/v1/tipos-combustible`

### Tanques

**Modelo existente (+ migración):** `Id`, `Identificacion`, `Capacidad`, `NivelActual`, `NivelCritico`, `TipoCombustibleId`, `Activo`

**DTOs:**
- `TanqueDto(int Id, string Identificacion, decimal Capacidad, decimal NivelActual, decimal NivelCritico, int TipoCombustibleId, string TipoCombustibleNombre, bool Activo)`
- `SaveTanqueRequest([Required, MaxLength(50)] string Identificacion, [Range(0.0001, 999999)] decimal Capacidad, [Range(0, 999999)] decimal NivelCritico, [Required] int TipoCombustibleId)`

**Regla especial en POST:** crear `Tanque` + `Inventario` (`ExistenciaActual = 0`, `Disponibilidad = 0`, `UltimaActualizacion = UtcNow`) en una sola transacción `SaveChangesAsync`.

**Validación:** `Identificacion` única — 409 Conflict si duplicada.

**Ruta:** `api/v1/tanques`

### Proveedores

**Modelo existente (+ migración):** `Id`, `Rnc`, `Nombre`, `Activo`

> El campo en el modelo C# es `Rnc` (no `RNC`) — así está definido en `Proveedor.cs` y en la migración inicial.

**DTOs:**
- `ProveedorDto(int Id, string Rnc, string Nombre, bool Activo)`
- `SaveProveedorRequest([Required, MaxLength(20)] string Rnc, [Required, MaxLength(150)] string Nombre, bool Activo = true)`

**Validación:** `Rnc` único — 409 Conflict si duplicado.

**Ruta:** `api/v1/proveedores`

---

## Archivos a crear

```
backend/FuelTrack.Api/DTOs/TiposCombustible/TipoCombustibleDto.cs
backend/FuelTrack.Api/DTOs/TiposCombustible/SaveTipoCombustibleRequest.cs
backend/FuelTrack.Api/DTOs/Tanques/TanqueDto.cs
backend/FuelTrack.Api/DTOs/Tanques/SaveTanqueRequest.cs
backend/FuelTrack.Api/DTOs/Proveedores/ProveedorDto.cs
backend/FuelTrack.Api/DTOs/Proveedores/SaveProveedorRequest.cs
backend/FuelTrack.Api/Controllers/TiposCombustibleController.cs
backend/FuelTrack.Api/Controllers/TanquesController.cs
backend/FuelTrack.Api/Controllers/ProveedoresController.cs
```

## Archivos a modificar

```
docs/06-API.md                    ← rutas en español
backend/FuelTrack.Api/Models/Tanque.cs       ← agregar Activo
backend/FuelTrack.Api/Models/Proveedor.cs    ← agregar Activo
backend/FuelTrack.Api/Migrations/...         ← nueva migración
```

---

## Criterios de aceptación para testers

- `POST /api/v1/tanques` crea simultáneamente Tanque e Inventario (verificar en BD)
- `Inventario.ExistenciaActual = 0` al crear tanque nuevo
- 409 en Identificacion duplicada de tanque
- 409 en Rnc duplicado de proveedor
- 409 en Nombre duplicado de tipo de combustible
- DELETE hace soft delete — el registro sigue en BD con `Activo = false`
- GET list devuelve todos (activos e inactivos), el frontend filtra
- Roles: POST/PUT responden 403 sin rol Admin o Supervisor; DELETE responde 403 sin rol Admin
