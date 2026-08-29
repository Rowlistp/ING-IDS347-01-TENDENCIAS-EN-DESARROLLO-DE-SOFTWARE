# FuelTrack Fase 0 — Backend Setup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Crear la infraestructura base del backend — proyecto .NET 10 Web API, todas las entidades del modelo de datos, AppDbContext con Fluent API, migración `InitialSchema`, y `docker-compose.yml` para PostgreSQL portátil.

**Architecture:** Monoproyecto ASP.NET Core Web API (`FuelTrack.Api`) con EF Core 9 + Npgsql para PostgreSQL. Entidades en `Models/`, DbContext en `Data/`, `docker-compose.yml` en raíz de `backend/`. Sin controllers de negocio en Fase 0.

**Tech Stack:** .NET 10, EF Core 9.x, Npgsql.EntityFrameworkCore.PostgreSQL 9.x, PostgreSQL 16 (Docker), Swashbuckle 8.x

---

## Mapa de archivos

| Archivo | Acción | Responsabilidad |
|---|---|---|
| `backend/FuelTrack.sln` | Crear | Solución .NET |
| `backend/.gitignore` | Crear | Excluir `appsettings.Development.json`, `bin/`, `obj/` |
| `backend/docker-compose.yml` | Crear | Levantar PostgreSQL con un comando |
| `backend/FuelTrack.Api/FuelTrack.Api.csproj` | Crear | Proyecto Web API con paquetes NuGet |
| `backend/FuelTrack.Api/Program.cs` | Modificar | Registrar DbContext, Swagger, CORS |
| `backend/FuelTrack.Api/appsettings.json` | Modificar | Config sin secretos |
| `backend/FuelTrack.Api/appsettings.Development.json` | Crear (gitignored) | Connection string local |
| `backend/FuelTrack.Api/Models/Enums/EstadoTicket.cs` | Crear | Enum estados de ticket |
| `backend/FuelTrack.Api/Models/Enums/TipoMovimiento.cs` | Crear | Enum tipos de movimiento de inventario |
| `backend/FuelTrack.Api/Models/Departamento.cs` | Crear | Entidad catálogo |
| `backend/FuelTrack.Api/Models/TipoCombustible.cs` | Crear | Entidad catálogo |
| `backend/FuelTrack.Api/Models/Estacion.cs` | Crear | Entidad catálogo |
| `backend/FuelTrack.Api/Models/Proveedor.cs` | Crear | Entidad catálogo |
| `backend/FuelTrack.Api/Models/Rol.cs` | Crear | Entidad rol de usuario |
| `backend/FuelTrack.Api/Models/Usuario.cs` | Crear | Entidad usuario del sistema |
| `backend/FuelTrack.Api/Models/UsuarioRol.cs` | Crear | Tabla intermedia N:N |
| `backend/FuelTrack.Api/Models/Empleado.cs` | Crear | Entidad empleado |
| `backend/FuelTrack.Api/Models/Vehiculo.cs` | Crear | Entidad vehículo |
| `backend/FuelTrack.Api/Models/SolicitudCombustible.cs` | Crear | Entidad solicitud |
| `backend/FuelTrack.Api/Models/Ticket.cs` | Crear | Entidad ticket digital (PK Guid) |
| `backend/FuelTrack.Api/Models/Tanque.cs` | Crear | Entidad tanque de combustible |
| `backend/FuelTrack.Api/Models/Inventario.cs` | Crear | Entidad inventario por tanque |
| `backend/FuelTrack.Api/Models/MovimientoInventario.cs` | Crear | Entidad historial de movimientos |
| `backend/FuelTrack.Api/Models/RecepcionCombustible.cs` | Crear | Entidad recepción de proveedor |
| `backend/FuelTrack.Api/Models/Despacho.cs` | Crear | Entidad despacho (1:1 con Ticket) |
| `backend/FuelTrack.Api/Models/CierreDiario.cs` | Crear | Entidad cierre diario |
| `backend/FuelTrack.Api/Models/Auditoria.cs` | Crear | Entidad auditoría (PK long) |
| `backend/FuelTrack.Api/Models/Notificacion.cs` | Crear | Entidad notificaciones |
| `backend/FuelTrack.Api/Data/AppDbContext.cs` | Crear | DbContext con todos los DbSets y Fluent API |
| `backend/FuelTrack.Api/Migrations/` | Crear (EF) | Generado por `dotnet ef` — no editar |

---

### Task 1: Crear rama, scaffolding y dependencias

**Files:**
- Create: `backend/FuelTrack.sln`
- Create: `backend/FuelTrack.Api/FuelTrack.Api.csproj`
- Create: `backend/.gitignore`

- [ ] **Step 1: Posicionarse en la raíz del repo y crear la rama**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE
git checkout main
git pull origin main
git checkout -b feature/backend-datos
```

Expected: `Switched to a new branch 'feature/backend-datos'`

- [ ] **Step 2: Crear directorio `backend/` y scaffoldear la solución**

```bash
mkdir backend
cd backend
dotnet new sln -n FuelTrack
```

Expected: `The template "Solution File" was created successfully.`

- [ ] **Step 3: Crear el proyecto Web API con controllers**

```bash
dotnet new webapi -n FuelTrack.Api -o FuelTrack.Api --use-controllers
```

Expected: `The template "ASP.NET Core Web API" was created successfully.`

- [ ] **Step 4: Agregar el proyecto a la solución**

```bash
dotnet sln FuelTrack.sln add FuelTrack.Api/FuelTrack.Api.csproj
```

Expected: `Project 'FuelTrack.Api/FuelTrack.Api.csproj' added to the solution.`

- [ ] **Step 5: Agregar paquetes NuGet**

```bash
cd FuelTrack.Api
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.*
dotnet add package Swashbuckle.AspNetCore --version 8.*
```

Cada comando debe terminar con `Successfully installed ...`

- [ ] **Step 6: Instalar EF Core tools globales**

```bash
dotnet tool install --global dotnet-ef
```

Si ya está instalado: `dotnet tool update --global dotnet-ef`

Expected: `You can invoke the tool using the following command: dotnet-ef`

- [ ] **Step 7: Crear `backend/.gitignore`**

Crear el archivo `C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend/.gitignore` con este contenido exacto:

```gitignore
# Outputs de compilación
**/bin/
**/obj/
*.user
.vs/

# Secretos — nunca al repo
**/appsettings.Development.json
```

- [ ] **Step 8: Eliminar archivos de ejemplo generados por el scaffolding**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend
rm FuelTrack.Api/Controllers/WeatherForecastController.cs
rm FuelTrack.Api/WeatherForecast.cs
```

- [ ] **Step 9: Verificar que el proyecto base compila**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend
dotnet build
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 10: Commit de la estructura base**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE
git add backend/
git commit -m "chore: scaffold solución .NET backend (Fase 0)"
```

---

### Task 2: Crear enums

**Files:**
- Create: `backend/FuelTrack.Api/Models/Enums/EstadoTicket.cs`
- Create: `backend/FuelTrack.Api/Models/Enums/TipoMovimiento.cs`

- [ ] **Step 1: Crear directorio de enums**

```bash
mkdir -p "C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend/FuelTrack.Api/Models/Enums"
```

- [ ] **Step 2: Crear `EstadoTicket.cs`**

```csharp
// backend/FuelTrack.Api/Models/Enums/EstadoTicket.cs
namespace FuelTrack.Api.Models.Enums;

public enum EstadoTicket
{
    Creado,
    Enviado,
    Pendiente,
    ProximoAVencer,
    Vencido,
    Consumido,
    Anulado
}
```

- [ ] **Step 3: Crear `TipoMovimiento.cs`**

```csharp
// backend/FuelTrack.Api/Models/Enums/TipoMovimiento.cs
namespace FuelTrack.Api.Models.Enums;

public enum TipoMovimiento
{
    Entrada,
    Salida,
    Ajuste,
    Transferencia,
    Merma
}
```

---

### Task 3: Crear entidades de catálogo

**Files:**
- Create: `backend/FuelTrack.Api/Models/Departamento.cs`
- Create: `backend/FuelTrack.Api/Models/TipoCombustible.cs`
- Create: `backend/FuelTrack.Api/Models/Estacion.cs`
- Create: `backend/FuelTrack.Api/Models/Proveedor.cs`

> Nota: estas entidades referencian tipos que se crearán en tasks posteriores. No compilar hasta el Task 9.

- [ ] **Step 1: Crear `Departamento.cs`**

```csharp
// backend/FuelTrack.Api/Models/Departamento.cs
namespace FuelTrack.Api.Models;

public class Departamento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
    public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    public ICollection<SolicitudCombustible> Solicitudes { get; set; } = new List<SolicitudCombustible>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
```

- [ ] **Step 2: Crear `TipoCombustible.cs`**

```csharp
// backend/FuelTrack.Api/Models/TipoCombustible.cs
namespace FuelTrack.Api.Models;

public class TipoCombustible
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<Tanque> Tanques { get; set; } = new List<Tanque>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<SolicitudCombustible> Solicitudes { get; set; } = new List<SolicitudCombustible>();
}
```

- [ ] **Step 3: Crear `Estacion.cs`**

```csharp
// backend/FuelTrack.Api/Models/Estacion.cs
namespace FuelTrack.Api.Models;

public class Estacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<Despacho> Despachos { get; set; } = new List<Despacho>();
}
```

- [ ] **Step 4: Crear `Proveedor.cs`**

```csharp
// backend/FuelTrack.Api/Models/Proveedor.cs
namespace FuelTrack.Api.Models;

public class Proveedor
{
    public int Id { get; set; }
    public string Rnc { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    public ICollection<RecepcionCombustible> Recepciones { get; set; } = new List<RecepcionCombustible>();
}
```

---

### Task 4: Crear entidades de usuarios y roles

**Files:**
- Create: `backend/FuelTrack.Api/Models/Rol.cs`
- Create: `backend/FuelTrack.Api/Models/Usuario.cs`
- Create: `backend/FuelTrack.Api/Models/UsuarioRol.cs`

- [ ] **Step 1: Crear `Rol.cs`**

```csharp
// backend/FuelTrack.Api/Models/Rol.cs
namespace FuelTrack.Api.Models;

public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}
```

- [ ] **Step 2: Crear `Usuario.cs`**

```csharp
// backend/FuelTrack.Api/Models/Usuario.cs
namespace FuelTrack.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public Empleado? Empleado { get; set; }
    public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
    public ICollection<Despacho> DespachosOperados { get; set; } = new List<Despacho>();
    public ICollection<Auditoria> Auditorias { get; set; } = new List<Auditoria>();
}
```

- [ ] **Step 3: Crear `UsuarioRol.cs`**

```csharp
// backend/FuelTrack.Api/Models/UsuarioRol.cs
namespace FuelTrack.Api.Models;

public class UsuarioRol
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
}
```

---

### Task 5: Crear entidades de personas y vehículos

**Files:**
- Create: `backend/FuelTrack.Api/Models/Empleado.cs`
- Create: `backend/FuelTrack.Api/Models/Vehiculo.cs`

- [ ] **Step 1: Crear `Empleado.cs`**

```csharp
// backend/FuelTrack.Api/Models/Empleado.cs
namespace FuelTrack.Api.Models;

public class Empleado
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public ICollection<SolicitudCombustible> Solicitudes { get; set; } = new List<SolicitudCombustible>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
```

- [ ] **Step 2: Crear `Vehiculo.cs`**

```csharp
// backend/FuelTrack.Api/Models/Vehiculo.cs
namespace FuelTrack.Api.Models;

public class Vehiculo
{
    public int Id { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Ficha { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Año { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal CapacidadTanque { get; set; }
    public decimal Odometro { get; set; }
    public bool Activo { get; set; } = true;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    public ICollection<SolicitudCombustible> Solicitudes { get; set; } = new List<SolicitudCombustible>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
```

---

### Task 6: Crear entidades de solicitudes y tickets

**Files:**
- Create: `backend/FuelTrack.Api/Models/SolicitudCombustible.cs`
- Create: `backend/FuelTrack.Api/Models/Ticket.cs`

- [ ] **Step 1: Crear `SolicitudCombustible.cs`**

```csharp
// backend/FuelTrack.Api/Models/SolicitudCombustible.cs
namespace FuelTrack.Api.Models;

public class SolicitudCombustible
{
    public int Id { get; set; }
    public decimal CantidadSolicitada { get; set; }
    public decimal? CantidadAutorizada { get; set; }
    public string TipoSolicitud { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
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

- [ ] **Step 2: Crear `Ticket.cs`**

```csharp
// backend/FuelTrack.Api/Models/Ticket.cs
using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.Models;

public class Ticket
{
    public Guid Id { get; set; }
    public int NumeroSecuencial { get; set; }
    public string Prefijo { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public EstadoTicket Estado { get; set; }
    public decimal CantidadAutorizada { get; set; }
    public string HashSeguridad { get; set; } = string.Empty;
    public string TokenValidacion { get; set; } = string.Empty;

    public int TipoCombustibleId { get; set; }
    public TipoCombustible TipoCombustible { get; set; } = null!;

    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    public int? SolicitudId { get; set; }
    public SolicitudCombustible? Solicitud { get; set; }

    public Despacho? Despacho { get; set; }
}
```

---

### Task 7: Crear entidades de inventario

**Files:**
- Create: `backend/FuelTrack.Api/Models/Tanque.cs`
- Create: `backend/FuelTrack.Api/Models/Inventario.cs`
- Create: `backend/FuelTrack.Api/Models/MovimientoInventario.cs`
- Create: `backend/FuelTrack.Api/Models/RecepcionCombustible.cs`

- [ ] **Step 1: Crear `Tanque.cs`**

```csharp
// backend/FuelTrack.Api/Models/Tanque.cs
namespace FuelTrack.Api.Models;

public class Tanque
{
    public int Id { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public decimal Capacidad { get; set; }
    public decimal NivelActual { get; set; }
    public decimal NivelCritico { get; set; }

    public int TipoCombustibleId { get; set; }
    public TipoCombustible TipoCombustible { get; set; } = null!;

    public Inventario? Inventario { get; set; }
    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
    public ICollection<RecepcionCombustible> Recepciones { get; set; } = new List<RecepcionCombustible>();
}
```

- [ ] **Step 2: Crear `Inventario.cs`**

```csharp
// backend/FuelTrack.Api/Models/Inventario.cs
namespace FuelTrack.Api.Models;

public class Inventario
{
    public int Id { get; set; }
    public decimal ExistenciaActual { get; set; }
    public decimal Disponibilidad { get; set; }
    public DateTime UltimaActualizacion { get; set; }

    public int TanqueId { get; set; }
    public Tanque Tanque { get; set; } = null!;
}
```

- [ ] **Step 3: Crear `MovimientoInventario.cs`**

```csharp
// backend/FuelTrack.Api/Models/MovimientoInventario.cs
using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.Models;

public class MovimientoInventario
{
    public int Id { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public decimal Volumen { get; set; }
    public DateTime FechaHora { get; set; }
    public string? ReferenciaOperacion { get; set; }
    public string? Observaciones { get; set; }

    public int TanqueId { get; set; }
    public Tanque Tanque { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
```

- [ ] **Step 4: Crear `RecepcionCombustible.cs`**

```csharp
// backend/FuelTrack.Api/Models/RecepcionCombustible.cs
namespace FuelTrack.Api.Models;

public class RecepcionCombustible
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public decimal VolumenRecibido { get; set; }
    public DateTime Fecha { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public int TanqueId { get; set; }
    public Tanque Tanque { get; set; } = null!;
}
```

---

### Task 8: Crear entidades de operaciones

**Files:**
- Create: `backend/FuelTrack.Api/Models/Despacho.cs`
- Create: `backend/FuelTrack.Api/Models/CierreDiario.cs`

- [ ] **Step 1: Crear `Despacho.cs`**

```csharp
// backend/FuelTrack.Api/Models/Despacho.cs
namespace FuelTrack.Api.Models;

public class Despacho
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public decimal GalonesServidos { get; set; }
    public string? Observaciones { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int OperadorId { get; set; }
    public Usuario Operador { get; set; } = null!;

    public int EstacionId { get; set; }
    public Estacion Estacion { get; set; } = null!;
}
```

- [ ] **Step 2: Crear `CierreDiario.cs`**

```csharp
// backend/FuelTrack.Api/Models/CierreDiario.cs
namespace FuelTrack.Api.Models;

public class CierreDiario
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal VolumenDespachado { get; set; }
    public decimal InventarioFinal { get; set; }
    public decimal Diferencias { get; set; }
    public string? ActaDigital { get; set; }
    public string? ReporteUrl { get; set; }
}
```

---

### Task 9: Crear entidades de trazabilidad y verificar build completo

**Files:**
- Create: `backend/FuelTrack.Api/Models/Auditoria.cs`
- Create: `backend/FuelTrack.Api/Models/Notificacion.cs`

- [ ] **Step 1: Crear `Auditoria.cs`**

```csharp
// backend/FuelTrack.Api/Models/Auditoria.cs
namespace FuelTrack.Api.Models;

public class Auditoria
{
    public long Id { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string EntidadAfectada { get; set; } = string.Empty;
    public string IdentificadorRegistro { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string? DireccionIp { get; set; }
    public string? DatosRelevantes { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
```

- [ ] **Step 2: Crear `Notificacion.cs`**

```csharp
// backend/FuelTrack.Api/Models/Notificacion.cs
namespace FuelTrack.Api.Models;

public class Notificacion
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Destinatario { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string? ReferenciaEvento { get; set; }
}
```

- [ ] **Step 3: Verificar que todas las entidades compilan**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

Si hay errores de tipo no encontrado, verificar que el archivo de ese tipo fue creado con el namespace correcto (`FuelTrack.Api.Models` o `FuelTrack.Api.Models.Enums`).

---

### Task 10: Crear AppDbContext

**Files:**
- Create: `backend/FuelTrack.Api/Data/AppDbContext.cs`

- [ ] **Step 1: Crear directorio `Data/`**

```bash
mkdir "C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend/FuelTrack.Api/Data"
```

- [ ] **Step 2: Crear `AppDbContext.cs`**

```csharp
// backend/FuelTrack.Api/Data/AppDbContext.cs
using FuelTrack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<TipoCombustible> TiposCombustible => Set<TipoCombustible>();
    public DbSet<Estacion> Estaciones => Set<Estacion>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<SolicitudCombustible> SolicitudesCombustible => Set<SolicitudCombustible>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Tanque> Tanques => Set<Tanque>();
    public DbSet<Inventario> Inventarios => Set<Inventario>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<RecepcionCombustible> RecepcionesCombustible => Set<RecepcionCombustible>();
    public DbSet<Despacho> Despachos => Set<Despacho>();
    public DbSet<CierreDiario> CierresDiarios => Set<CierreDiario>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // UsuarioRol — PK compuesta
        modelBuilder.Entity<UsuarioRol>()
            .HasKey(ur => new { ur.UsuarioId, ur.RolId });

        // Ticket — Guid generado automáticamente por PostgreSQL
        modelBuilder.Entity<Ticket>()
            .Property(t => t.Id)
            .ValueGeneratedOnAdd();

        // Auditoria — DatosRelevantes almacenado como jsonb en PostgreSQL
        modelBuilder.Entity<Auditoria>()
            .Property(a => a.DatosRelevantes)
            .HasColumnType("jsonb");

        // Índices únicos
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

        // Precisión decimal (18,4) para todos los campos de volumen y cantidad
        modelBuilder.Entity<Vehiculo>().Property(v => v.CapacidadTanque).HasPrecision(18, 4);
        modelBuilder.Entity<Vehiculo>().Property(v => v.Odometro).HasPrecision(18, 4);
        modelBuilder.Entity<SolicitudCombustible>().Property(s => s.CantidadSolicitada).HasPrecision(18, 4);
        modelBuilder.Entity<SolicitudCombustible>().Property(s => s.CantidadAutorizada).HasPrecision(18, 4);
        modelBuilder.Entity<Ticket>().Property(t => t.CantidadAutorizada).HasPrecision(18, 4);
        modelBuilder.Entity<Tanque>().Property(t => t.Capacidad).HasPrecision(18, 4);
        modelBuilder.Entity<Tanque>().Property(t => t.NivelActual).HasPrecision(18, 4);
        modelBuilder.Entity<Tanque>().Property(t => t.NivelCritico).HasPrecision(18, 4);
        modelBuilder.Entity<Inventario>().Property(i => i.ExistenciaActual).HasPrecision(18, 4);
        modelBuilder.Entity<Inventario>().Property(i => i.Disponibilidad).HasPrecision(18, 4);
        modelBuilder.Entity<MovimientoInventario>().Property(m => m.Volumen).HasPrecision(18, 4);
        modelBuilder.Entity<RecepcionCombustible>().Property(r => r.VolumenRecibido).HasPrecision(18, 4);
        modelBuilder.Entity<Despacho>().Property(d => d.GalonesServidos).HasPrecision(18, 4);
        modelBuilder.Entity<CierreDiario>().Property(c => c.VolumenDespachado).HasPrecision(18, 4);
        modelBuilder.Entity<CierreDiario>().Property(c => c.InventarioFinal).HasPrecision(18, 4);
        modelBuilder.Entity<CierreDiario>().Property(c => c.Diferencias).HasPrecision(18, 4);

        // Despacho -> Ticket: Restrict (ticket consumido no se puede borrar en cascada)
        modelBuilder.Entity<Despacho>()
            .HasOne(d => d.Ticket)
            .WithOne(t => t.Despacho)
            .HasForeignKey<Despacho>(d => d.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        // Despacho -> Operador (Usuario): Restrict
        modelBuilder.Entity<Despacho>()
            .HasOne(d => d.Operador)
            .WithMany(u => u.DespachosOperados)
            .HasForeignKey(d => d.OperadorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Auditoria -> Usuario: SetNull (auditoría persiste aunque se desactive el usuario)
        modelBuilder.Entity<Auditoria>()
            .HasOne(a => a.Usuario)
            .WithMany(u => u.Auditorias)
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 3: Compilar con DbContext incluido**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

---

### Task 11: Configurar Program.cs, appsettings y docker-compose

**Files:**
- Modify: `backend/FuelTrack.Api/Program.cs`
- Modify: `backend/FuelTrack.Api/appsettings.json`
- Create: `backend/FuelTrack.Api/appsettings.Development.json` (gitignored)
- Create: `backend/docker-compose.yml`

- [ ] **Step 1: Reemplazar `Program.cs` completo**

```csharp
// backend/FuelTrack.Api/Program.cs
using FuelTrack.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();

app.Run();
```

- [ ] **Step 2: Reemplazar `appsettings.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 3: Crear `appsettings.Development.json`** (NO va al repo — está en .gitignore)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fueltrack_db;Username=postgres;Password=fueltrack2026"
  }
}
```

- [ ] **Step 4: Crear `backend/docker-compose.yml`**

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
    restart: unless-stopped

volumes:
  pgdata:
```

- [ ] **Step 5: Compilar todo**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

---

### Task 12: Levantar PostgreSQL, generar y aplicar migración

- [ ] **Step 1: Instalar Docker Desktop** (si no está instalado aún)

Descargar desde https://www.docker.com/products/docker-desktop/ e instalar.
Reiniciar el equipo si el instalador lo pide.
Abrir Docker Desktop y esperar a que el ícono en la barra de tareas muestre "Docker Desktop is running".

- [ ] **Step 2: Levantar PostgreSQL con docker-compose**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend
docker-compose up -d
```

Expected:
```
✔ Network backend_default  Created
✔ Volume "backend_pgdata"  Created
✔ Container backend-db-1   Started
```

- [ ] **Step 3: Verificar que el contenedor está corriendo**

```bash
docker-compose ps
```

Expected: el servicio `db` aparece con estado `running`.

- [ ] **Step 4: Generar la migración inicial**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend
dotnet ef migrations add InitialSchema --project FuelTrack.Api
```

Expected:
```
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

Esto crea tres archivos en `FuelTrack.Api/Migrations/`:
- `<timestamp>_InitialSchema.cs` — SQL generado
- `<timestamp>_InitialSchema.Designer.cs` — metadata de EF
- `AppDbContextModelSnapshot.cs` — snapshot del modelo

- [ ] **Step 5: Aplicar la migración a la base de datos**

```bash
dotnet ef database update --project FuelTrack.Api
```

Expected: `Done.` (sin errores)

Si aparece `Unable to connect to database`, verificar que Docker Desktop está corriendo y el contenedor `backend-db-1` está activo (`docker-compose ps`).

- [ ] **Step 6: Verificar tablas creadas en PostgreSQL**

```bash
docker exec -it backend-db-1 psql -U postgres -d fueltrack_db -c "\dt"
```

Expected: lista de tablas que incluye:
```
 Auditorias | Departamentos | Despachos | Empleados | Estaciones |
 Inventarios | MovimientosInventario | Notificaciones | Proveedores |
 RecepcionesCombustible | Roles | SolicitudesCombustible | Tanques |
 Tickets | TiposCombustible | UsuarioRoles | Usuarios | Vehiculos |
 CierresDiarios | __EFMigrationsHistory
```

---

### Task 13: Verificación final y commit

- [ ] **Step 1: Arrancar la aplicación**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE/backend
dotnet run --project FuelTrack.Api
```

Expected en consola:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

- [ ] **Step 2: Verificar Swagger en el navegador**

Abrir: `http://localhost:5000/swagger`

Expected: Swagger UI carga con el título "FuelTrack.Api" y sin endpoints de negocio.

- [ ] **Step 3: Detener la aplicación**

Presionar `Ctrl+C` en la terminal.

- [ ] **Step 4: Confirmar que `appsettings.Development.json` NO está siendo trackeado por git**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE
git status backend/
```

El archivo `appsettings.Development.json` NO debe aparecer en la lista. Si aparece, verificar que el `.gitignore` está correcto.

- [ ] **Step 5: Commit final en `feature/backend-datos`**

```bash
cd C:/Users/anpro/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE
git add backend/
git commit -m "chore: setup proyecto backend y schema inicial (Fase 0)"
```

Expected: commit creado en rama `feature/backend-datos`. Verificar con:

```bash
git log --oneline -3
```

La rama `feature/backend-datos` debe tener 2 commits:
1. `chore: scaffold solución .NET backend (Fase 0)`
2. `chore: setup proyecto backend y schema inicial (Fase 0)`
