# Fase 2 — CRUD Catálogos Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar CRUD completo (Departamentos, Empleados, Vehículos) con endpoints REST, DTOs y autorización por roles en FuelTrack.Api.

**Architecture:** Tres controladores (`DepartamentosController`, `EmpleadosController`, `VehiculosController`) acceden directamente a `AppDbContext` sin capa de servicio. DTOs separados para request y response. Soft delete (campo `Activo = false`). Autorización: GET → cualquier usuario autenticado; POST/PUT → Administrador o Supervisor; DELETE → solo Administrador.

**Tech Stack:** .NET 10 Web API, EF Core 9 + Npgsql, `[Authorize]` con roles de `Security/Roles.cs`, records C# para DTOs.

---

## Mapa de archivos

**Crear:**
- `backend/FuelTrack.Api/DTOs/Departamentos/DepartamentoDto.cs`
- `backend/FuelTrack.Api/DTOs/Departamentos/SaveDepartamentoRequest.cs`
- `backend/FuelTrack.Api/DTOs/Empleados/EmpleadoDto.cs`
- `backend/FuelTrack.Api/DTOs/Empleados/SaveEmpleadoRequest.cs`
- `backend/FuelTrack.Api/DTOs/Vehiculos/VehiculoDto.cs`
- `backend/FuelTrack.Api/DTOs/Vehiculos/SaveVehiculoRequest.cs`
- `backend/FuelTrack.Api/Controllers/DepartamentosController.cs`
- `backend/FuelTrack.Api/Controllers/EmpleadosController.cs`
- `backend/FuelTrack.Api/Controllers/VehiculosController.cs`

**Referencia (no modificar):**
- `backend/FuelTrack.Api/Models/Departamento.cs` — Id, Nombre, Activo
- `backend/FuelTrack.Api/Models/Empleado.cs` — Id, Codigo, NombreCompleto, Cedula, Cargo, Correo, Telefono, DepartamentoId, Activo
- `backend/FuelTrack.Api/Models/Vehiculo.cs` — Id, Placa, Ficha, Marca, Modelo, Año, Tipo, CapacidadTanque, Odometro, DepartamentoId, Activo
- `backend/FuelTrack.Api/Security/Roles.cs` — constantes de rol (Administrador, Supervisor, etc.)
- `backend/FuelTrack.Api/Data/AppDbContext.cs` — DbSets ya existentes

---

## Task 1: DTOs de Departamentos

**Files:**
- Create: `backend/FuelTrack.Api/DTOs/Departamentos/DepartamentoDto.cs`
- Create: `backend/FuelTrack.Api/DTOs/Departamentos/SaveDepartamentoRequest.cs`

- [ ] **Step 1: Crear DepartamentoDto**

```csharp
// backend/FuelTrack.Api/DTOs/Departamentos/DepartamentoDto.cs
namespace FuelTrack.Api.DTOs.Departamentos;

public record DepartamentoDto(int Id, string Nombre, bool Activo);
```

- [ ] **Step 2: Crear SaveDepartamentoRequest**

```csharp
// backend/FuelTrack.Api/DTOs/Departamentos/SaveDepartamentoRequest.cs
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Departamentos;

public record SaveDepartamentoRequest(
    [Required, MaxLength(100)] string Nombre,
    bool Activo = true
);
```

- [ ] **Step 3: Verificar que compila**

```bash
cd backend/FuelTrack.Api && dotnet build
```

Resultado esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Task 2: DepartamentosController

**Files:**
- Create: `backend/FuelTrack.Api/Controllers/DepartamentosController.cs`

- [ ] **Step 1: Crear el controlador completo**

```csharp
// backend/FuelTrack.Api/Controllers/DepartamentosController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Departamentos;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/departamentos")]
[Authorize]
public sealed class DepartamentosController : ControllerBase
{
    private readonly AppDbContext _db;

    public DepartamentosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<DepartamentoDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Departamentos
            .AsNoTracking()
            .Select(d => new DepartamentoDto(d.Id, d.Nombre, d.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartamentoDto>> GetById(int id, CancellationToken ct)
    {
        var d = await _db.Departamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return d is null ? NotFound() : Ok(new DepartamentoDto(d.Id, d.Nombre, d.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<DepartamentoDto>> Create(
        SaveDepartamentoRequest req, CancellationToken ct)
    {
        var entity = new Departamento { Nombre = req.Nombre, Activo = req.Activo };
        _db.Departamentos.Add(entity);
        await _db.SaveChangesAsync(ct);
        var dto = new DepartamentoDto(entity.Id, entity.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<DepartamentoDto>> Update(
        int id, SaveDepartamentoRequest req, CancellationToken ct)
    {
        var entity = await _db.Departamentos.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Nombre = req.Nombre;
        entity.Activo = req.Activo;
        await _db.SaveChangesAsync(ct);
        return Ok(new DepartamentoDto(entity.Id, entity.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.Departamentos.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
```

- [ ] **Step 2: Compilar**

```bash
cd backend/FuelTrack.Api && dotnet build
```

Resultado esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add backend/FuelTrack.Api/DTOs/Departamentos/ backend/FuelTrack.Api/Controllers/DepartamentosController.cs
git commit -m "feat: CRUD Departamentos con DTOs y autorización por roles"
```

---

## Task 3: DTOs de Empleados

**Files:**
- Create: `backend/FuelTrack.Api/DTOs/Empleados/EmpleadoDto.cs`
- Create: `backend/FuelTrack.Api/DTOs/Empleados/SaveEmpleadoRequest.cs`

- [ ] **Step 1: Crear EmpleadoDto**

Incluye `DepartamentoNombre` para evitar que el frontend haga una segunda llamada.

```csharp
// backend/FuelTrack.Api/DTOs/Empleados/EmpleadoDto.cs
namespace FuelTrack.Api.DTOs.Empleados;

public record EmpleadoDto(
    int Id,
    string Codigo,
    string NombreCompleto,
    string Cedula,
    string Cargo,
    string Correo,
    string Telefono,
    int DepartamentoId,
    string DepartamentoNombre,
    bool Activo
);
```

- [ ] **Step 2: Crear SaveEmpleadoRequest**

```csharp
// backend/FuelTrack.Api/DTOs/Empleados/SaveEmpleadoRequest.cs
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Empleados;

public record SaveEmpleadoRequest(
    [Required, MaxLength(20)]              string Codigo,
    [Required, MaxLength(150)]             string NombreCompleto,
    [Required, MaxLength(20)]              string Cedula,
    [Required, MaxLength(100)]             string Cargo,
    [Required, MaxLength(150), EmailAddress] string Correo,
    [Required, MaxLength(20)]              string Telefono,
    [Required]                             int DepartamentoId,
    bool Activo = true
);
```

- [ ] **Step 3: Compilar**

```bash
cd backend/FuelTrack.Api && dotnet build
```

Resultado esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Task 4: EmpleadosController

**Files:**
- Create: `backend/FuelTrack.Api/Controllers/EmpleadosController.cs`

- [ ] **Step 1: Crear el controlador completo**

```csharp
// backend/FuelTrack.Api/Controllers/EmpleadosController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Empleados;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/empleados")]
[Authorize]
public sealed class EmpleadosController : ControllerBase
{
    private readonly AppDbContext _db;

    public EmpleadosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<EmpleadoDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Empleados
            .AsNoTracking()
            .Include(e => e.Departamento)
            .Select(e => new EmpleadoDto(
                e.Id, e.Codigo, e.NombreCompleto, e.Cedula, e.Cargo,
                e.Correo, e.Telefono, e.DepartamentoId, e.Departamento.Nombre, e.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpleadoDto>> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Empleados
            .AsNoTracking()
            .Include(x => x.Departamento)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();
        return Ok(new EmpleadoDto(
            e.Id, e.Codigo, e.NombreCompleto, e.Cedula, e.Cargo,
            e.Correo, e.Telefono, e.DepartamentoId, e.Departamento.Nombre, e.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<EmpleadoDto>> Create(
        SaveEmpleadoRequest req, CancellationToken ct)
    {
        if (!await _db.Departamentos.AnyAsync(d => d.Id == req.DepartamentoId, ct))
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND",
                message = "El departamento no existe." });

        if (await _db.Empleados.AnyAsync(e => e.Codigo == req.Codigo, ct))
            return Conflict(new { code = "CODIGO_DUPLICADO",
                message = "El código de empleado ya existe." });

        if (await _db.Empleados.AnyAsync(e => e.Cedula == req.Cedula, ct))
            return Conflict(new { code = "CEDULA_DUPLICADA",
                message = "La cédula ya está registrada." });

        var entity = new Empleado
        {
            Codigo        = req.Codigo,
            NombreCompleto = req.NombreCompleto,
            Cedula        = req.Cedula,
            Cargo         = req.Cargo,
            Correo        = req.Correo,
            Telefono      = req.Telefono,
            DepartamentoId = req.DepartamentoId,
            Activo        = req.Activo
        };
        _db.Empleados.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(entity).Reference(e => e.Departamento).LoadAsync(ct);

        var dto = new EmpleadoDto(
            entity.Id, entity.Codigo, entity.NombreCompleto, entity.Cedula, entity.Cargo,
            entity.Correo, entity.Telefono, entity.DepartamentoId,
            entity.Departamento.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<EmpleadoDto>> Update(
        int id, SaveEmpleadoRequest req, CancellationToken ct)
    {
        var entity = await _db.Empleados
            .Include(e => e.Departamento)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null) return NotFound();

        if (!await _db.Departamentos.AnyAsync(d => d.Id == req.DepartamentoId, ct))
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND",
                message = "El departamento no existe." });

        if (await _db.Empleados.AnyAsync(e => e.Codigo == req.Codigo && e.Id != id, ct))
            return Conflict(new { code = "CODIGO_DUPLICADO",
                message = "El código de empleado ya existe." });

        if (await _db.Empleados.AnyAsync(e => e.Cedula == req.Cedula && e.Id != id, ct))
            return Conflict(new { code = "CEDULA_DUPLICADA",
                message = "La cédula ya está registrada." });

        entity.Codigo         = req.Codigo;
        entity.NombreCompleto = req.NombreCompleto;
        entity.Cedula         = req.Cedula;
        entity.Cargo          = req.Cargo;
        entity.Correo         = req.Correo;
        entity.Telefono       = req.Telefono;
        entity.DepartamentoId = req.DepartamentoId;
        entity.Activo         = req.Activo;
        await _db.SaveChangesAsync(ct);

        if (entity.Departamento.Id != req.DepartamentoId)
            await _db.Entry(entity).Reference(e => e.Departamento).LoadAsync(ct);

        return Ok(new EmpleadoDto(
            entity.Id, entity.Codigo, entity.NombreCompleto, entity.Cedula, entity.Cargo,
            entity.Correo, entity.Telefono, entity.DepartamentoId,
            entity.Departamento.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.Empleados.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
```

- [ ] **Step 2: Compilar**

```bash
cd backend/FuelTrack.Api && dotnet build
```

Resultado esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add backend/FuelTrack.Api/DTOs/Empleados/ backend/FuelTrack.Api/Controllers/EmpleadosController.cs
git commit -m "feat: CRUD Empleados con DTOs, validaciones de duplicados y autorización"
```

---

## Task 5: DTOs de Vehículos

**Files:**
- Create: `backend/FuelTrack.Api/DTOs/Vehiculos/VehiculoDto.cs`
- Create: `backend/FuelTrack.Api/DTOs/Vehiculos/SaveVehiculoRequest.cs`

- [ ] **Step 1: Crear VehiculoDto**

```csharp
// backend/FuelTrack.Api/DTOs/Vehiculos/VehiculoDto.cs
namespace FuelTrack.Api.DTOs.Vehiculos;

public record VehiculoDto(
    int Id,
    string Placa,
    string Ficha,
    string Marca,
    string Modelo,
    int Año,
    string Tipo,
    decimal CapacidadTanque,
    decimal Odometro,
    int DepartamentoId,
    string DepartamentoNombre,
    bool Activo
);
```

- [ ] **Step 2: Crear SaveVehiculoRequest**

```csharp
// backend/FuelTrack.Api/DTOs/Vehiculos/SaveVehiculoRequest.cs
using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Vehiculos;

public record SaveVehiculoRequest(
    [Required, MaxLength(10)]             string Placa,
    [Required, MaxLength(20)]             string Ficha,
    [Required, MaxLength(50)]             string Marca,
    [Required, MaxLength(50)]             string Modelo,
    [Range(1990, 2100)]                   int Año,
    [Required, MaxLength(50)]             string Tipo,
    [Required]                            int DepartamentoId,
    [Range(0.0001, 9999.9999)]            decimal CapacidadTanque,
    decimal Odometro = 0
);
```

- [ ] **Step 3: Compilar**

```bash
cd backend/FuelTrack.Api && dotnet build
```

Resultado esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Task 6: VehiculosController

**Files:**
- Create: `backend/FuelTrack.Api/Controllers/VehiculosController.cs`

- [ ] **Step 1: Crear el controlador completo**

```csharp
// backend/FuelTrack.Api/Controllers/VehiculosController.cs
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Vehiculos;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Controllers;

[ApiController]
[Route("api/v1/vehiculos")]
[Authorize]
public sealed class VehiculosController : ControllerBase
{
    private readonly AppDbContext _db;

    public VehiculosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<VehiculoDto>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Vehiculos
            .AsNoTracking()
            .Include(v => v.Departamento)
            .Select(v => new VehiculoDto(
                v.Id, v.Placa, v.Ficha, v.Marca, v.Modelo, v.Año,
                v.Tipo, v.CapacidadTanque, v.Odometro,
                v.DepartamentoId, v.Departamento.Nombre, v.Activo))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehiculoDto>> GetById(int id, CancellationToken ct)
    {
        var v = await _db.Vehiculos
            .AsNoTracking()
            .Include(x => x.Departamento)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return NotFound();
        return Ok(new VehiculoDto(
            v.Id, v.Placa, v.Ficha, v.Marca, v.Modelo, v.Año,
            v.Tipo, v.CapacidadTanque, v.Odometro,
            v.DepartamentoId, v.Departamento.Nombre, v.Activo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<VehiculoDto>> Create(
        SaveVehiculoRequest req, CancellationToken ct)
    {
        if (!await _db.Departamentos.AnyAsync(d => d.Id == req.DepartamentoId, ct))
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND",
                message = "El departamento no existe." });

        if (await _db.Vehiculos.AnyAsync(v => v.Placa == req.Placa, ct))
            return Conflict(new { code = "PLACA_DUPLICADA",
                message = "La placa ya está registrada." });

        if (await _db.Vehiculos.AnyAsync(v => v.Ficha == req.Ficha, ct))
            return Conflict(new { code = "FICHA_DUPLICADA",
                message = "La ficha ya está registrada." });

        var entity = new Vehiculo
        {
            Placa          = req.Placa,
            Ficha          = req.Ficha,
            Marca          = req.Marca,
            Modelo         = req.Modelo,
            Año            = req.Año,
            Tipo           = req.Tipo,
            CapacidadTanque = req.CapacidadTanque,
            Odometro       = req.Odometro,
            DepartamentoId = req.DepartamentoId,
            Activo         = true
        };
        _db.Vehiculos.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(entity).Reference(v => v.Departamento).LoadAsync(ct);

        var dto = new VehiculoDto(
            entity.Id, entity.Placa, entity.Ficha, entity.Marca, entity.Modelo, entity.Año,
            entity.Tipo, entity.CapacidadTanque, entity.Odometro,
            entity.DepartamentoId, entity.Departamento.Nombre, entity.Activo);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Supervisor}")]
    public async Task<ActionResult<VehiculoDto>> Update(
        int id, SaveVehiculoRequest req, CancellationToken ct)
    {
        var entity = await _db.Vehiculos
            .Include(v => v.Departamento)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null) return NotFound();

        if (!await _db.Departamentos.AnyAsync(d => d.Id == req.DepartamentoId, ct))
            return BadRequest(new { code = "DEPARTAMENTO_NOT_FOUND",
                message = "El departamento no existe." });

        if (await _db.Vehiculos.AnyAsync(v => v.Placa == req.Placa && v.Id != id, ct))
            return Conflict(new { code = "PLACA_DUPLICADA",
                message = "La placa ya está registrada." });

        if (await _db.Vehiculos.AnyAsync(v => v.Ficha == req.Ficha && v.Id != id, ct))
            return Conflict(new { code = "FICHA_DUPLICADA",
                message = "La ficha ya está registrada." });

        entity.Placa           = req.Placa;
        entity.Ficha           = req.Ficha;
        entity.Marca           = req.Marca;
        entity.Modelo          = req.Modelo;
        entity.Año             = req.Año;
        entity.Tipo            = req.Tipo;
        entity.CapacidadTanque = req.CapacidadTanque;
        entity.Odometro        = req.Odometro;
        entity.DepartamentoId  = req.DepartamentoId;
        await _db.SaveChangesAsync(ct);

        if (entity.Departamento.Id != req.DepartamentoId)
            await _db.Entry(entity).Reference(v => v.Departamento).LoadAsync(ct);

        return Ok(new VehiculoDto(
            entity.Id, entity.Placa, entity.Ficha, entity.Marca, entity.Modelo, entity.Año,
            entity.Tipo, entity.CapacidadTanque, entity.Odometro,
            entity.DepartamentoId, entity.Departamento.Nombre, entity.Activo));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var entity = await _db.Vehiculos.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.Activo = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
```

- [ ] **Step 2: Compilar**

```bash
cd backend/FuelTrack.Api && dotnet build
```

Resultado esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add backend/FuelTrack.Api/DTOs/Vehiculos/ backend/FuelTrack.Api/Controllers/VehiculosController.cs
git commit -m "feat: CRUD Vehículos con DTOs, validaciones de duplicados y autorización"
```

---

## Task 7: Verificación final y PR

**Files:** ninguno nuevo

- [ ] **Step 1: Build final limpio**

```bash
cd backend/FuelTrack.Api && dotnet build
```

Resultado esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 2: Push de la rama**

```bash
git push origin feature/backend-datos
```

- [ ] **Step 3: Crear PR a main**

Título: `feat(backend): CRUD catálogos Fase 2 — Departamentos, Empleados, Vehículos`

Body:
```
## Resumen
- 3 controladores REST con CRUD completo (GET list, GET by id, POST, PUT, DELETE soft)
- DTOs separados por entidad (response + request)
- Autorización por roles: GET → cualquier autenticado; POST/PUT → Administrador/Supervisor; DELETE → Administrador
- Validaciones de duplicados (409 Conflict) para campos únicos: Codigo, Cedula, Placa, Ficha
- Soft delete (Activo = false), sin borrado físico

## Endpoints
- GET/POST /api/v1/departamentos
- GET/PUT/DELETE /api/v1/departamentos/{id}
- GET/POST /api/v1/empleados
- GET/PUT/DELETE /api/v1/empleados/{id}
- GET/POST /api/v1/vehiculos
- GET/PUT/DELETE /api/v1/vehiculos/{id}
```

```bash
gh pr create \
  --title "feat(backend): CRUD catálogos Fase 2 — Departamentos, Empleados, Vehículos" \
  --body "$(cat docs/superpowers/plans/pr-fase2-body.md)" \
  --base main \
  --head feature/backend-datos
```
