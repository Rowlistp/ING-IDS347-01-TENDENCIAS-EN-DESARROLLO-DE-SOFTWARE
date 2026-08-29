# Diseño Fase 0 — Backend + Datos (Builder 1)

**Fecha:** 2026-08-29  
**Autor:** Builder 1  
**Fase:** 0 — Preparación del backend  
**Rama de trabajo:** `feature/backend-datos`

---

## 1. Objetivo

Scaffoldear el proyecto .NET 10 Web API con Entity Framework Core y PostgreSQL, crear todas las entidades del modelo de datos, el DbContext y la migración inicial `InitialSchema` que crea todas las tablas en la base de datos.

No se implementa lógica de negocio ni controllers en esta fase.

---

## 2. Estructura de carpetas

```
backend/
├── FuelTrack.Api/
│   ├── FuelTrack.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json    ← connection string local, en .gitignore
│   ├── Controllers/                    ← vacío en Fase 0
│   ├── Models/
│   │   ├── Enums/
│   │   │   ├── EstadoTicket.cs
│   │   │   └── TipoMovimiento.cs
│   │   ├── Auditoria.cs
│   │   ├── CierreDiario.cs
│   │   ├── Departamento.cs
│   │   ├── Despacho.cs
│   │   ├── Empleado.cs
│   │   ├── Estacion.cs
│   │   ├── Inventario.cs
│   │   ├── MovimientoInventario.cs
│   │   ├── Notificacion.cs
│   │   ├── Proveedor.cs
│   │   ├── RecepcionCombustible.cs
│   │   ├── Rol.cs
│   │   ├── SolicitudCombustible.cs
│   │   ├── Tanque.cs
│   │   ├── Ticket.cs
│   │   ├── TipoCombustible.cs
│   │   ├── Usuario.cs
│   │   └── UsuarioRol.cs
│   ├── Data/
│   │   └── AppDbContext.cs
│   └── Migrations/                     ← generado por EF Core, no editar a mano
├── docker-compose.yml                  ← levanta PostgreSQL con un solo comando
├── FuelTrack.sln
└── .gitignore
```

---

## 3. Stack y paquetes NuGet

- **Runtime:** .NET 10
- **Framework:** ASP.NET Core Web API
- **ORM:** Entity Framework Core 9.x
- **Driver PostgreSQL:** `Npgsql.EntityFrameworkCore.PostgreSQL`
- **Tooling EF:** `Microsoft.EntityFrameworkCore.Design`
- **Swagger:** `Swashbuckle.AspNetCore`

---

## 4. Entidades C#

### Enums

**`EstadoTicket`**
```
Creado, Enviado, Pendiente, ProximoAVencer, Vencido, Consumido, Anulado
```

**`TipoMovimiento`**
```
Entrada, Salida, Ajuste, Transferencia, Merma
```

### Catálogos base

| Entidad | Campos clave | Restricciones |
|---|---|---|
| `Departamento` | Id (int), Nombre, Activo (bool) | — |
| `TipoCombustible` | Id (int), Nombre, Activo | — |
| `Estacion` | Id (int), Nombre, Activo | — |
| `Proveedor` | Id (int), Rnc, Nombre | — |

### Usuarios y roles

| Entidad | Campos clave | Restricciones |
|---|---|---|
| `Rol` | Id (int), Nombre | — |
| `Usuario` | Id (int), NombreUsuario, PasswordHash, Activo, RolId FK | NombreUsuario unique |
| `UsuarioRol` | UsuarioId FK, RolId FK | PK compuesta |

### Personas y vehículos

| Entidad | Campos clave | Restricciones |
|---|---|---|
| `Empleado` | Id (int), Codigo, NombreCompleto, Cedula, DepartamentoId FK, Cargo, Correo, Telefono, Activo, UsuarioId FK (nullable) | Codigo unique, Cedula unique |
| `Vehiculo` | Id (int), Placa, Ficha, Marca, Modelo, Año (int), Tipo, CapacidadTanque (decimal), Odometro (decimal), Activo, DepartamentoId FK | Placa unique, Ficha unique |

### Solicitudes y tickets

| Entidad | Campos clave | Restricciones |
|---|---|---|
| `SolicitudCombustible` | Id (int), EmpleadoId FK, VehiculoId FK, DepartamentoId FK, TipoCombustibleId FK, CantidadSolicitada (decimal), CantidadAutorizada (decimal) nullable, TipoSolicitud, Estado, FechaSolicitud, FechaVencimiento nullable | — |
| `Ticket` | Id (Guid PK), NumeroSecuencial (int), Prefijo, FechaCreacion, FechaVencimiento, Estado (EstadoTicket), CantidadAutorizada (decimal), TipoCombustibleId FK, EmpleadoId FK, VehiculoId FK, DepartamentoId FK, SolicitudId FK (nullable), HashSeguridad, TokenValidacion | NumeroSecuencial unique |

### Inventario

| Entidad | Campos clave | Restricciones |
|---|---|---|
| `Tanque` | Id (int), Identificacion, TipoCombustibleId FK, Capacidad (decimal), NivelActual (decimal), NivelCritico (decimal) | Identificacion unique |
| `Inventario` | Id (int), TanqueId FK, ExistenciaActual (decimal), Disponibilidad (decimal), UltimaActualizacion | — |
| `MovimientoInventario` | Id (int), Tipo (TipoMovimiento), Volumen (decimal), FechaHora, UsuarioId FK, ReferenciaOperacion, Observaciones, TanqueId FK | Sin borrado físico |
| `RecepcionCombustible` | Id (int), ProveedorId FK, NumeroFactura, VolumenRecibido (decimal), Fecha, TanqueId FK | — |

### Operaciones

| Entidad | Campos clave | Restricciones |
|---|---|---|
| `Despacho` | Id (int), TicketId FK, Fecha, Hora, GalonesServidos (decimal), OperadorId FK (→ Usuario), EstacionId FK, Observaciones | TicketId unique (1 despacho por ticket) |
| `CierreDiario` | Id (int), Fecha, VolumenDespachado (decimal), InventarioFinal (decimal), Diferencias (decimal), ActaDigital, ReporteUrl | Fecha unique |

### Trazabilidad

| Entidad | Campos clave | Restricciones |
|---|---|---|
| `Auditoria` | Id (long), Evento, EntidadAfectada, IdentificadorRegistro, UsuarioId FK (nullable), FechaHora, DireccionIp, DatosRelevantes (jsonb) | Sin borrado físico |
| `Notificacion` | Id (int), Tipo, Destinatario, Estado, FechaHora, Canal, ReferenciaEvento | — |

---

## 5. AppDbContext

- Un `DbSet<T>` por cada entidad.
- Configuración Fluent API en `OnModelCreating`:
  - Índices únicos declarados explícitamente.
  - `Ticket.Id` → `ValueGeneratedOnAdd()` en PostgreSQL.
  - `Auditoria` y `MovimientoInventario` → sin `DeleteBehavior` que permita borrado en cascada.
  - `UsuarioRol` → PK compuesta.
- Nombre de la base de datos: `fueltrack_db`.

---

## 6. Configuración

**`appsettings.json`** (valores sin secretos):
```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*"
}
```

**`appsettings.Development.json`** (en `.gitignore`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fueltrack_db;Username=postgres;Password=TU_PASSWORD"
  }
}
```

---

## 7. Program.cs (Fase 0)

Registra únicamente:
1. `AppDbContext` con Npgsql.
2. Swagger/OpenAPI.
3. CORS permisivo para desarrollo (`AllowAnyOrigin`).

Sin controllers de negocio. Sin autenticación JWT (eso es Fase 1, Builder 2).

---

## 8. Migración inicial

- Nombre: `InitialSchema`
- Comando: `dotnet ef migrations add InitialSchema`
- Aplicar: `dotnet ef database update`
- Crea todas las tablas en PostgreSQL local.

---

## 9. PostgreSQL vía Docker (portabilidad)

El proyecto debe poder correr en cualquier máquina (incluyendo la del maestro) sin instalación manual de PostgreSQL. La solución es un `docker-compose.yml` en la raíz de `backend/`:

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: fueltrack_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: fueltrack2026
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

**Para levantar la base de datos:**
```bash
docker-compose up -d
```

**Requisito:** Docker Desktop instalado en la máquina. Un solo comando, sin configuración extra.

**Connection string** en `appsettings.Development.json` (en `.gitignore`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fueltrack_db;Username=postgres;Password=fueltrack2026"
  }
}
```

La contraseña `fueltrack2026` es fija en el `docker-compose.yml` (que sí va al repo), por lo que `appsettings.Development.json` puede omitir la contraseña si se usa la del compose. Alternativamente se usa un `.env` para separar secretos del compose.

---

## 10. Entregable y criterio de terminado

- [ ] `docker-compose up -d` levanta PostgreSQL sin errores.
- [ ] Proyecto compila sin errores (`dotnet build`).
- [ ] `dotnet ef database update` crea todas las tablas sin errores.
- [ ] Swagger abre en `https://localhost:5001/swagger` (sin endpoints de negocio).
- [ ] Commit en rama `feature/backend-datos` con mensaje: `chore: setup proyecto backend y schema inicial (Fase 0)`.
- [ ] `appsettings.Development.json` está en `.gitignore` (no se sube al repo).
- [ ] `docker-compose.yml` sí se sube al repo (no contiene secretos reales).

---

## 11. Lo que NO entra en Fase 0

- Controllers con lógica de negocio.
- Autenticación JWT / OAuth2 (Fase 1 — Builder 2).
- Endpoints de catálogos CRUD (Fase 2).
- Datos semilla (seeds).
- Lógica de inventario, tickets, QR.
