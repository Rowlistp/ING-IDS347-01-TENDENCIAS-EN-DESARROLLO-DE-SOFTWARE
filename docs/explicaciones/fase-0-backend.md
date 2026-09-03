# Fase 0 – Base de datos: DbContext, entidades y migraciones

> Documento pedagógico para el equipo de desarrollo. Tono: didáctico, orientado a quien sabe programar pero no conoce el proyecto.

---

## 1. Qué se construyó

Se definió la estructura completa de la base de datos del sistema FuelTrack mediante clases C# (entidades) y un contexto de datos central (`AppDbContext`). Con eso, Entity Framework Core genera automáticamente las tablas en PostgreSQL sin escribir SQL a mano. En esta fase también se produjeron las migraciones, que son el historial versionado de todos los cambios que ha sufrido el esquema de la base de datos desde el inicio del proyecto.

---

## 2. Por qué se construyó así

| Decisión técnica | Problema que resuelve |
|---|---|
| **ORM (Entity Framework Core)** | Elimina el SQL manual y mantiene el modelo de datos sincronizado con el código C#. Si cambia una clase, una migración actualiza la BD sin tocar scripts sueltos. |
| **Code-First** (clases primero, BD después) | El equipo trabaja en C# y no necesita diseñar el esquema en SQL por separado. La clase ES el diseño. |
| **Npgsql** como provider | Permite que EF Core hable con PostgreSQL, incluyendo tipos nativos de Postgres como `uuid`, `jsonb`, `date` y `time without time zone` que no existen igual en otros motores. |
| **`OnModelCreating` en lugar de Data Annotations** | Centraliza toda la configuración de relaciones, índices únicos y precisiones decimales en un solo lugar. Mantiene las clases de modelo limpias y legibles. |
| **`decimal` con precisión `(18,4)`** | Los volúmenes de combustible requieren cuatro decimales de exactitud (galones, litros). Usar `float` o `double` introduce errores de redondeo inaceptables en cálculos financieros/operativos. |
| **Migraciones incrementales** | Cada cambio al esquema se registra como un archivo versionado. El equipo puede aplicar o revertir cambios en cualquier entorno sin perder datos. |
| **`Guid` como PK del Ticket** | El ticket es el documento que viaja en el QR. Un `Guid` es globalmente único y no predecible, lo que impide adivinar IDs secuenciales y forjar tickets. |

---

## 3. Recorrido archivo por archivo

### `FuelTrack.Api.csproj` — Las dependencias del proyecto

Este archivo le dice a .NET qué paquetes externos necesita el proyecto para funcionar:

- **`Npgsql.EntityFrameworkCore.PostgreSQL`**: el "traductor" entre EF Core y PostgreSQL. Sin él, EF Core no sabe hablar con Postgres.
- **`Microsoft.EntityFrameworkCore.Design`**: herramientas de diseño que solo se usan al desarrollar (para generar migraciones con `dotnet ef migrations add`). Está marcado con `PrivateAssets=all` para que no se incluya en el binario de producción.
- **`Microsoft.AspNetCore.Authentication.JwtBearer`**: soporte para tokens JWT, que es el mecanismo de autenticación de la API.
- **`Swashbuckle.AspNetCore`**: genera la interfaz Swagger para documentar y probar los endpoints.

---

### `Models/` — Las entidades (tablas de la BD)

Cada archivo `.cs` en esta carpeta representa una tabla en PostgreSQL. Se pueden agrupar por propósito:

#### Catálogos (tablas de referencia simples)

- **`Departamento.cs`**: representa un área organizacional de la empresa (ej.: "Operaciones", "Mantenimiento"). Tiene colecciones de empleados, vehículos y solicitudes porque es el punto de agrupación central.
- **`TipoCombustible.cs`**: catálogo de tipos (ej.: "Gasolina Regular", "Diesel"). Único por nombre, vincula tanques, tickets y solicitudes.
- **`Estacion.cs`**: punto físico de despacho donde un operador surte el vehículo.
- **`Proveedor.cs`**: empresa que suministra combustible. Su RNC (registro fiscal) es único en BD.
- **`Rol.cs`** y **`UsuarioRol.cs`**: sistema de roles para control de acceso. `UsuarioRol` es una tabla intermedia con clave primaria compuesta `(UsuarioId, RolId)` porque un usuario puede tener varios roles.

#### Actores del sistema

- **`Usuario.cs`**: credenciales de acceso al sistema. Guarda solo el hash de la contraseña, nunca el texto plano. Tiene relación con `Empleado` (un empleado puede o no tener cuenta de usuario).
- **`Empleado.cs`**: persona física de la empresa. Tiene código interno y cédula únicos. El campo `UsuarioId` es opcional (`int?`) porque no todos los empleados acceden al sistema directamente.
- **`Vehiculo.cs`**: flota de vehículos. Placa y ficha son únicos. Guarda la capacidad del tanque y el odómetro con cuatro decimales de precisión.

#### Flujo operativo principal

- **`SolicitudCombustible.cs`**: petición de un empleado para obtener combustible. Pasa por tres estados (`Pendiente`, `Aprobada`, `Rechazada`). Si es rechazada, se guarda el motivo. La `CantidadAutorizada` es nullable porque solo existe después de que alguien aprueba.
- **`Ticket.cs`**: el documento digital que autoriza el despacho. Es la entidad central del sistema. Su PK es un `Guid` (no un entero) precisamente porque este valor viaja codificado en el código QR. Tiene un `HashSeguridad` y un `TokenValidacion` para evitar falsificaciones. Su estado recorre el ciclo: `Creado → Enviado → Pendiente → Consumido` (o `Vencido`/`Anulado`). La relación con `SolicitudCombustible` es opcional porque un ticket puede crearse directamente sin pasar por solicitud previa.
- **`Despacho.cs`**: registro del momento exacto en que el operador surtió el vehículo. Tiene relación uno-a-uno con `Ticket` (un ticket se consume exactamente una vez). Usa `DateOnly` y `TimeOnly` en lugar de `DateTime` para separar fecha de hora en columnas nativas de Postgres.

#### Inventario y stock

- **`Tanque.cs`**: recipiente físico de almacenamiento. Lleva nivel actual y nivel crítico (umbral de alerta). Asociado a un tipo de combustible.
- **`Inventario.cs`**: instantánea del stock disponible por tanque. Diferencia entre `ExistenciaActual` (lo que hay físicamente) y `Disponibilidad` (lo que se puede asignar, descontando compromisos pendientes).
- **`MovimientoInventario.cs`**: bitácora de cada cambio de volumen en un tanque. El `TipoMovimiento` puede ser `Entrada`, `Salida`, `Ajuste`, `Transferencia` o `Merma`. Toda operación sobre el inventario queda aquí registrada.
- **`RecepcionCombustible.cs`**: registro de cuando llega combustible de un proveedor externo con su número de factura.

#### Control y trazabilidad

- **`CierreDiario.cs`**: resumen diario operativo: cuánto se despachó, cuánto quedó en inventario y si hay diferencias. La fecha es única (no puede haber dos cierres para el mismo día).
- **`Auditoria.cs`**: log inmutable de eventos del sistema. El campo `DatosRelevantes` es de tipo `jsonb` de Postgres, lo que permite guardar cualquier estructura de datos variable sin columnas adicionales. El `UsuarioId` es nullable con `OnDelete: SetNull` para que el log no se borre si el usuario es eliminado.
- **`Notificacion.cs`**: registro de mensajes enviados a usuarios o sistemas externos (ej.: alerta de nivel crítico de tanque). Independiente de cualquier entidad específica.
- **`RefreshToken.cs`**: tokens de renovación de sesión. Almacena el hash del token (nunca el token en texto), su IP de origen y si fue revocado. La propiedad calculada `IsActive` verifica en memoria si el token sigue válido.

#### Enumeraciones (`Models/Enums/`)

- **`EstadoSolicitud`**: `Pendiente`, `Aprobada`, `Rechazada`. Se guarda como `string` en BD (configurado en `OnModelCreating`) para que sea legible directamente en la tabla.
- **`EstadoTicket`**: siete estados que representan el ciclo de vida completo del ticket QR.
- **`TipoMovimiento`**: cinco tipos de operación sobre el inventario.

---

### `Data/AppDbContext.cs` — El contexto de datos

Este archivo es el corazón de la capa de datos. Cumple tres funciones:

**1. Expone las tablas** mediante propiedades `DbSet<T>`. Cada propiedad es la puerta de entrada para hacer consultas a esa tabla. Por ejemplo, `context.Tickets` da acceso a todos los tickets.

**2. Recibe la configuración de conexión** en el constructor mediante `DbContextOptions`. Esto permite que el mismo contexto apunte a distintas bases de datos según el entorno (desarrollo, pruebas, producción) sin cambiar el código.

**3. Configura el esquema en `OnModelCreating`**. Aquí se definen:

- La clave compuesta de `UsuarioRol`: `HasKey(ur => new { ur.UsuarioId, ur.RolId })`
- Todos los índices únicos (placa, cédula, número de ticket, etc.)
- La precisión decimal de columnas de volumen: `HasPrecision(18, 4)`
- Conversión de `EstadoSolicitud` a `string` para que sea legible en BD
- Comportamientos de eliminación personalizados: el despacho no se borra si se borra el ticket (`Restrict`); la auditoría deja el UsuarioId en null si el usuario es eliminado (`SetNull`)
- La columna `DatosRelevantes` de Auditoría como tipo `jsonb` nativo de PostgreSQL

---

### `Migrations/` — El historial del esquema

Las migraciones son archivos de código generados automáticamente por EF Core. Cada uno tiene dos métodos: `Up()` (aplica el cambio) y `Down()` (lo revierte). El proyecto tiene cuatro migraciones:

| Archivo | Qué hace |
|---|---|
| `20260829191436_InitialSchema` | Crea las 17 tablas iniciales del sistema con todos sus índices y relaciones. |
| `20260829235631_AddSecurityRefreshTokens` | Agrega la tabla `RefreshTokens` para la renovación segura de sesiones JWT. |
| `20260903030725_AddActivoTanqueProveedorIndices` | Agrega el campo `Activo` a Tanques y Proveedores, más índices únicos para `TipoCombustible.Nombre` y `Proveedor.Rnc`. |
| `20260903132351_AddMotivoRechazoSolicitud` | Agrega la columna `MotivoRechazo` (nullable) a `SolicitudesCombustible`. |

El archivo `AppDbContextModelSnapshot.cs` no es una migración: es una foto del estado actual del modelo que EF Core usa para calcular qué cambió cuando se crea una nueva migración.

---

## 4. Preguntas que podrían hacerme y cómo responderlas

**¿Por qué el `Id` del `Ticket` es `Guid` y no `int` como los demás?**
Porque el Id del ticket viaja dentro del código QR. Si fuera un entero secuencial (1, 2, 3...), cualquiera podría intentar forjar un ticket cambiando ese número. Un `Guid` como `3f7a1c9e-...` es prácticamente imposible de adivinar. El `HashSeguridad` y el `TokenValidacion` son capas adicionales de verificación.

**¿Por qué `EstadoSolicitud` se guarda como `string` en la BD pero `EstadoTicket` se guarda como número entero?**
Es una decisión de legibilidad vs. rendimiento. `EstadoSolicitud` usa `.HasConversion<string>()` en `OnModelCreating`, por lo que en la tabla aparece "Aprobada" en vez de "1". Esto hace la BD más auditable directamente. `EstadoTicket` se guardó como entero (el valor por defecto de los enums en EF Core), lo que es más eficiente pero menos legible. Ambos enfoques son válidos; lo importante es ser consistente.

**¿Qué diferencia hay entre `Inventario` y `MovimientoInventario`?**
`Inventario` es el saldo actual de un tanque (como el extracto bancario resumido). `MovimientoInventario` es cada operación individual que modifica ese saldo (como los movimientos del estado de cuenta). Uno dice "cuánto hay ahora"; el otro dice "por qué llegamos a ese número".

**¿Por qué `OnDelete: Restrict` en la relación Despacho → Ticket?**
Significa que si alguien intenta eliminar un ticket que ya tiene un despacho asociado, la BD rechazará la operación. Esto protege la integridad: no se puede borrar la autorización (ticket) si ya existe evidencia de que el combustible fue entregado (despacho).

**¿Cómo se aplican las migraciones en un entorno nuevo?**
Con el comando `dotnet ef database update` desde el directorio del proyecto. EF Core revisa qué migraciones ya fueron aplicadas (las guarda en la tabla `__EFMigrationsHistory`) y ejecuta solo las pendientes en orden cronológico.

**¿Para qué sirve `PrivateAssets=all` en `Microsoft.EntityFrameworkCore.Design`?**
Indica que ese paquete solo existe en tiempo de desarrollo, no se empaqueta en el binario final que va a producción. Las herramientas de diseño de EF Core (como el generador de migraciones) no son necesarias cuando la aplicación ya está desplegada.

**¿Por qué `Auditoria.Id` es `long` (bigint) en vez de `int`?**
Porque la tabla de auditoría puede crecer muy rápido: cada acción del sistema genera un registro. Un `int` permite hasta ~2.1 mil millones de filas; un `long` permite hasta ~9.2 trillones. Para una tabla de logs es la elección correcta.

**¿Por qué `DatosRelevantes` en `Auditoria` es de tipo `jsonb` y no columnas separadas?**
Porque los datos que se auditan varían según la entidad: auditar un ticket tiene campos distintos a auditar una recepción de combustible. Con `jsonb` se puede guardar cualquier estructura sin alterar el esquema de la tabla. Además, PostgreSQL indexa y permite consultar campos dentro de un `jsonb`.

---

## 5. Términos clave

| Término | Significado en este contexto |
|---|---|
| **ORM** (Object-Relational Mapper) | Herramienta que traduce clases C# a tablas SQL y viceversa. EF Core es el ORM utilizado. |
| **DbContext** | Clase central de EF Core. Representa la "sesión" con la base de datos y expone las tablas como colecciones C#. |
| **DbSet\<T\>** | Propiedad del DbContext que representa una tabla. Permite hacer consultas LINQ sobre ella. |
| **Migración** | Archivo de código generado que describe un cambio al esquema de la BD (crear tabla, agregar columna, etc.) y sabe cómo revertirlo. |
| **Code-First** | Enfoque de EF Core donde el código C# define el modelo y la BD se genera a partir de él, no al revés. |
| **Navigation property** | Propiedad en una entidad que apunta a otra entidad relacionada (ej.: `Empleado.Departamento`). EF Core la usa para construir los JOINs automáticamente. |
| **`jsonb`** | Tipo de dato nativo de PostgreSQL para guardar JSON binario. Permite indexar y consultar campos internos del JSON de forma eficiente. |
| **Índice único** | Restricción en la BD que garantiza que no existan dos filas con el mismo valor en una columna (ej.: dos empleados con la misma cédula). |
| **`OnDelete: Restrict`** | Comportamiento de eliminación que impide borrar un registro si existen registros relacionados en otra tabla. Protege la integridad referencial. |
| **`Guid`** | Identificador único global de 128 bits. Prácticamente imposible de predecir o colisionar, ideal para identificadores que viajan fuera del sistema (como en un QR). |

---

## 6. Cómo se conecta con el resto del sistema

La Fase 0 es el cimiento de todo lo demás. Sin estas entidades y el `AppDbContext`, ninguna otra capa puede existir:

- **Fase 1 (Seguridad / JWT)**: usa `Usuario`, `Rol`, `UsuarioRol` y `RefreshToken` para autenticar y autorizar cada petición a la API.
- **Fase 2 (Catálogos / CRUD)**: los endpoints de departamentos, tipos de combustible, empleados, vehículos y proveedores operan directamente sobre estas entidades a través del `AppDbContext`.
- **Fase 3 (Solicitudes)**: el flujo de `SolicitudCombustible` (crear, aprobar, rechazar) manipula la entidad del mismo nombre.
- **Fase 4 (Tickets QR)**: la entidad `Ticket` con su `Guid`, `HashSeguridad` y `TokenValidacion` es la base sobre la que se genera y valida el código QR.
- **Fase 5 (Despacho)**: al escanear el QR en la estación, el sistema busca el `Ticket` en BD, verifica su estado y crea un `Despacho` vinculado a él.
- **Fase 6 (Inventario)**: `Tanque`, `Inventario`, `MovimientoInventario` y `RecepcionCombustible` registran el stock y cada variación de volumen.
- **Transversal (Auditoría y Notificaciones)**: `Auditoria` y `Notificacion` son escritas por los servicios de cualquier fase para dejar trazabilidad y alertar eventos operativos.

En resumen: cambiar una entidad en `Models/` o agregar una propiedad en `AppDbContext` impacta a toda la cadena. Por eso las migraciones son el mecanismo de control de versiones del esquema y deben coordinarse entre todos los builders del equipo.
