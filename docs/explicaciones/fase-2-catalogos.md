# Fase 2 – Catálogos: empleados, vehículos, departamentos, tipos de combustible, tanques y proveedores

---

## 1. Qué se construyó

En esta fase se crearon seis endpoints REST que mantienen los datos maestros del sistema: quiénes son los empleados, qué vehículos existen, a qué departamento pertenecen, qué tipos de combustible se manejan, qué tanques físicos hay en la planta y qué proveedores abastecen esos tanques. Cada endpoint expone las operaciones estándar de listado, consulta individual, creación, edición y desactivación lógica. Estos catálogos son la base sobre la que se apoyan todas las fases siguientes: sin un empleado registrado no puede haber solicitud; sin un tanque, no puede haber inventario.

---

## 2. Por qué se construyó así

### Borrado lógico en lugar de físico
El verbo `DELETE` en todos los controladores no elimina el registro de la base de datos: pone `Activo = false`. Esto resuelve un problema real de auditoría: si un empleado solicitó combustible hace seis meses y hoy se "borra", el historial quedaría huérfano o inconsistente. Con borrado lógico los registros históricos siguen referenciando datos válidos, y el dato inactivo simplemente deja de aparecer en las pantallas operativas.

### Validaciones de unicidad antes de insertar
Campos como la cédula de un empleado, la placa de un vehículo o el RNC de un proveedor deben ser únicos en el mundo real. El código verifica la existencia del duplicado con `AnyAsync` antes de llamar a `SaveChangesAsync`, y devuelve HTTP 409 Conflict con un código de error semántico (por ejemplo `CEDULA_DUPLICADA`). Esto permite que el frontend distinga exactamente qué campo falló sin analizar texto libre.

### Validación de integridad referencial explícita en el controlador
Al crear un empleado o un vehículo se verifica con `AnyAsync` que el departamento indicado realmente exista, antes de intentar insertar. Lo mismo ocurre al crear un tanque: se comprueba que el tipo de combustible exista. Aunque la base de datos también rechazaría la inserción por clave foránea, esta verificación permite devolver un 400 Bad Request con un mensaje claro en lugar de una excepción genérica de base de datos.

### `AsNoTracking` en lecturas
Las consultas GET usan `AsNoTracking()`. Entity Framework Core, por defecto, rastrea en memoria todos los objetos que carga para detectar cambios. En lecturas de solo consulta ese seguimiento es trabajo innecesario. Al desactivarlo las consultas son más rápidas y consumen menos memoria, sobre todo en listados que pueden devolver muchos registros.

### `Include` + proyección directa a DTO en la misma consulta
En lugar de cargar la entidad completa y luego mapear en C#, los listados proyectan directamente a un record DTO dentro del `Select`. Esto genera SQL más eficiente porque solo se seleccionan las columnas necesarias. El `Include(e => e.Departamento)` hace que EF Core genere un JOIN en SQL para traer el nombre del departamento en la misma consulta, evitando una segunda ida a la base de datos.

### Creación de inventario junto con el tanque
Cuando se crea un tanque, el controlador también inserta un registro en la tabla `Inventarios` con existencia en cero, dentro de la misma transacción. Esto garantiza que nunca exista un tanque sin su inventario asociado, invariante que las fases posteriores (recepciones, despachos) asumen como garantizada. Ambas entidades se persisten en un solo `SaveChangesAsync`.

### Sin capa de servicio
Todos los controladores acceden directamente a `AppDbContext`. Esta decisión reduce la cantidad de archivos y capas de abstracción en un proyecto académico donde la lógica de negocio de los catálogos es simple (CRUD con validaciones). Si la lógica creciera, el refactor hacia servicios separados es directo porque el contexto ya está inyectado por constructor.

### Autorización por roles en escritura
Las operaciones de lectura (`GET`) exigen solo estar autenticado (`[Authorize]`). Crear y editar (`POST`, `PUT`) requieren el rol `Administrador` o `Supervisor`. Eliminar (desactivar) solo lo puede hacer un `Administrador`. Esta gradación refleja el principio de mínimo privilegio: un conductor puede consultar la lista de vehículos pero no puede borrar uno.

---

## 3. Recorrido archivo por archivo

### `Models/Departamento.cs`
Define la entidad más simple de la fase: id, nombre, activo. Lo relevante es que tiene colecciones de navegación hacia `Empleados`, `Vehiculos`, `SolicitudCombustible` y `Tickets`. Estas colecciones no se cargan por defecto (lazy loading está desactivado), pero documentan en código qué otras entidades dependen del departamento.

### `DTOs/Departamentos/DepartamentoDto.cs` y `SaveDepartamentoRequest.cs`
El DTO de salida es un record de tres campos: `(int Id, string Nombre, bool Activo)`. El request de entrada valida que el nombre no supere 100 caracteres y que exista (`[Required]`). La separación entre DTO de entrada y de salida evita que el cliente pueda enviar el `Id` (que lo asigna la base de datos) o campos internos que no debería controlar.

### `Controllers/DepartamentosController.cs`
Es el controlador más simple de la fase. No tiene relaciones que resolver al crear; solo inserta, actualiza y desactiva. Sirve como referencia base para entender el patrón que repiten todos los demás controladores. Ejemplo de uso típico: el frontend llama `GET /api/v1/departamentos` para poblar un combo de selección antes de registrar un empleado.

### `Models/Empleado.cs`
Tiene más campos que Departamento: código interno de empresa, nombre completo, cédula, cargo, correo y teléfono. La clave foránea `DepartamentoId` conecta al empleado con su área. También tiene una relación opcional hacia `Usuario` (la cuenta de acceso al sistema), lo que significa que no todo empleado tiene usuario, y no todo usuario tiene empleado asociado.

### `DTOs/Empleados/SaveEmpleadoRequest.cs`
Aplica anotaciones de validación directamente en el record: `[MaxLength(20)]` en el código, `[EmailAddress]` en el correo. ASP.NET Core valida estas restricciones automáticamente antes de que el controlador ejecute una sola línea, devolviendo 400 con detalle del campo fallido.

### `Controllers/EmpleadosController.cs`
El flujo de creación ilustra el patrón completo de validación: primero verifica que el departamento existe, luego que el código no esté duplicado, luego que la cédula no esté duplicada. Solo si pasa las tres comprobaciones inserta. Tras guardar, carga la relación con el departamento (`Reference(...).LoadAsync`) para poder incluir el nombre del departamento en la respuesta sin hacer otra consulta completa.

Ejemplo concreto: `POST /api/v1/empleados` con body `{ "codigo": "EMP-001", "cedula": "001-1234567-8", ... }`. Si la cédula ya existe, el servidor devuelve `409 { "code": "CEDULA_DUPLICADA", "message": "..." }`.

### `Models/Vehiculo.cs`
Similar en estructura a Empleado. El campo `Ficha` es el número interno de flota (diferente a la placa oficial). `CapacidadTanque` y `Odometro` son decimales porque se trata de medidas físicas. También conecta con Departamento (a qué área pertenece el vehículo).

### `Controllers/VehiculosController.cs`
Valida duplicidad tanto de placa como de ficha, porque ambos son identificadores únicos desde distintos contextos (el legal y el interno). En la creación, `Activo` siempre se fija en `true` desde el servidor, no se toma del request, lo que evita que alguien cree un vehículo directamente inactivo por error.

### `Models/TipoCombustible.cs`
La entidad más simple junto con Departamento: id, nombre, activo. Actúa como catálogo de referencia (Gasolina, Diesel, GLP, etc.) que los tanques referencian.

### `Controllers/TiposCombustibleController.cs`
Solo valida unicidad de nombre. Como no tiene relaciones externas, su flujo de creación es el más corto de todos los controladores de catálogos. El test `Create_Returns409_CuandoNombreDuplicado` demuestra que intentar registrar "Gasolina" dos veces devuelve 409.

### `Models/Tanque.cs`
Tiene `NivelActual` y `NivelCritico` además de `Capacidad`. El `NivelCritico` es el umbral por debajo del cual el sistema debería emitir alertas. La relación con `Inventario` es uno-a-uno (cada tanque tiene exactamente un inventario), y con `Movimientos` y `Recepciones` es uno-a-muchos.

### `Controllers/TanquesController.cs`
Es el controlador más interesante de la fase porque su método `Create` hace dos cosas en la misma transacción: inserta el tanque Y crea un registro en `Inventarios` con existencia y disponibilidad en cero. El test `Create_Returns201_YCreaInventarioEnCero` verifica explícitamente que ambas filas existen después de la llamada. Esto garantiza la consistencia del módulo de inventario desde el primer momento.

En la actualización hay un detalle fino: si el `TipoCombustibleId` cambia, se llama a `Reference(...).LoadAsync` para recargar la navegación; si no cambió, se omite esa carga porque el objeto en memoria ya tiene el dato correcto.

### `Models/Proveedor.cs`
El campo único del negocio es el `Rnc` (Registro Nacional de Contribuyente), que identifica a la empresa proveedora ante el fisco. La colección `Recepciones` enlaza con los registros de abastecimiento de tanques.

### `Controllers/ProveedoresController.cs`
Valida unicidad del RNC. El test `Create_Returns409_CuandoRncDuplicado` muestra el escenario donde dos intentos de registrar el mismo RNC resultan en 409 para el segundo.

---

### Patrón común a todos los tests

Los archivos en `FuelTrack.Api.Tests/Controllers/` usan SQLite en memoria (`Data Source=:memory:`) en lugar de PostgreSQL. Cada test levanta la base de datos, ejecuta y la descarta. Esto significa que los tests son completamente aislados, no necesitan servidor de base de datos externo y se ejecutan en milisegundos. El método `Setup` crea la conexión y el contexto; `Cleanup` los libera con `DisposeAsync`.

---

## 4. Preguntas que podrían hacerme y cómo responderlas

**P1: ¿Por qué el DELETE no borra el registro de la base de datos?**
Porque los registros históricos (solicitudes, tickets, recepciones) referencian a estas entidades. Si se borrara físicamente, el historial quedaría con claves foráneas apuntando a nada, lo que viola la integridad referencial. Con borrado lógico el dato sigue existiendo para mantener la coherencia histórica, pero queda marcado como inactivo para que los flujos operativos no lo consideren.

**P2: ¿Qué pasa si intento crear un empleado con una cédula que ya existe?**
El controlador ejecuta `AnyAsync` sobre la tabla `Empleados` filtrando por cédula antes de insertar. Si encuentra un registro existente, retorna inmediatamente con `409 Conflict` y un objeto JSON con el campo `code = "CEDULA_DUPLICADA"`. La base de datos nunca llega a recibir el `INSERT`.

**P3: ¿Por qué se usa `AsNoTracking()` en los GET pero no en los PUT?**
En un `GET` solo se leen datos para devolverlos al cliente; no hay intención de modificarlos. El tracking de EF Core consume memoria y CPU rastreando cambios innecesariamente. En un `PUT`, en cambio, se necesita que EF Core detecte las propiedades que cambiaron para generar el `UPDATE` correcto en SQL, por lo que el tracking es imprescindible.

**P4: ¿Por qué el controlador de tanques crea también un inventario?**
Porque el módulo de inventario asume como invariante que todo tanque activo tiene exactamente un registro de inventario. Si se permitiera crear un tanque sin su inventario, la primera operación que intentara leer el nivel disponible de ese tanque fallaría. Al hacerlo en la misma transacción, se garantiza atomicidad: o se crean ambos, o no se crea ninguno.

**P5: ¿Por qué los tests usan SQLite y no PostgreSQL?**
SQLite en modo memoria permite que los tests no dependan de ningún servidor externo, lo que los hace portables y rápidos. EF Core soporta múltiples proveedores de base de datos con la misma API; el esquema se crea con `EnsureCreatedAsync()` en segundos. El único costo es que algunos comportamientos específicos de PostgreSQL (como tipos de datos especiales o expresiones regulares avanzadas) no se testean así, pero para CRUD básico es suficiente.

**P6: ¿Qué significa el `[Authorize(Roles = "...")]` sobre algunos métodos pero no en la clase?**
La clase tiene `[Authorize]` a nivel de controlador, lo que exige que el usuario esté autenticado para cualquier acción. Los métodos de escritura refinan esa restricción agregando `[Authorize(Roles = "Administrador,Supervisor")]`, lo que requiere además que el token JWT contenga uno de esos roles. El DELETE solo acepta `Administrador`. Los GET no tienen atributo adicional porque heredan el `[Authorize]` de la clase, sin restricción de rol.

**P7: ¿Por qué los DTOs son `record` y no `class`?**
Los `record` en C# son inmutables por defecto: una vez construidos, sus propiedades no cambian. Esto es ideal para DTOs porque representan datos en tránsito que no deberían modificarse. Además, los `record` tienen igualdad por valor generada automáticamente, lo que facilita las comparaciones en tests.

**P8: ¿Qué es el `CancellationToken ct` que recibe cada método?**
Es un mecanismo de cancelación cooperativa. Si el cliente HTTP cancela la petición (por ejemplo, cierra el navegador), el token se activa y las operaciones asíncronas como `ToListAsync(ct)` o `SaveChangesAsync(ct)` abortan sin desperdiciar recursos del servidor. ASP.NET Core lo inyecta automáticamente cuando se declara como parámetro.

---

## 5. Términos clave

| Término | Significado en este proyecto |
|---|---|
| **Borrado lógico** | Marcar `Activo = false` en lugar de eliminar el registro de la tabla. El dato persiste para mantener integridad histórica. |
| **DTO (Data Transfer Object)** | Objeto que viaja entre el cliente y la API. Diferente del modelo de base de datos: solo expone los campos necesarios para cada operación. |
| **`AsNoTracking`** | Indicación a EF Core para no rastrear cambios en los objetos cargados, mejorando rendimiento en consultas de solo lectura. |
| **`Include`** | Instrucción a EF Core para hacer JOIN con una tabla relacionada en la misma consulta SQL, evitando consultas adicionales. |
| **`AnyAsync`** | Consulta que devuelve `true`/`false` según si existe al menos un registro que cumpla la condición. Se usa para validar unicidad sin cargar el registro completo. |
| **`record`** | Tipo de C# que define objetos inmutables con igualdad por valor. Ideal para DTOs. |
| **`CancellationToken`** | Token que permite cancelar operaciones asíncronas si el cliente abandona la petición. |
| **HTTP 409 Conflict** | Código de respuesta que indica que el recurso no pudo crearse/actualizarse porque viola una restricción de unicidad del negocio. |
| **`CreatedAtAction`** | Método de ASP.NET Core que devuelve HTTP 201 con la URL del recurso recién creado en el header `Location`. |
| **Invariante** | Condición que el sistema garantiza que siempre es verdadera, por ejemplo: todo tanque tiene exactamente un inventario asociado. |

---

## 6. Cómo se conecta con el resto del sistema

Los catálogos de Fase 2 son la capa de datos maestros sobre la que se apoya todo lo demás:

- **Fase 3 – Solicitudes**: cuando un empleado solicita combustible para un vehículo, la solicitud lleva `EmpleadoId`, `VehiculoId` y `DepartamentoId`. Los tres deben existir en los catálogos de esta fase.

- **Fase 4 – Recepciones**: cuando llega combustible de un proveedor, la recepción referencia un `TanqueId` y un `ProveedorId`. Ambos son catálogos de Fase 2.

- **Fase 5 – Tickets y despacho**: el ticket generado por el código QR referencia `EmpleadoId`, `VehiculoId` y `TanqueId`. Si cualquiera de esos catálogos no existiera, no podría generarse el ticket.

- **Fase 6 – Inventario**: el módulo de inventario existe desde que se crea el primer tanque; el registro `Inventarios` se crea atómicamente junto con el tanque en `TanquesController.Create`. La FK `TanqueId` de la tabla `Inventarios` apunta directamente al catálogo de esta fase.

- **Autorización (Fase 1)**: los roles `Administrador`, `Supervisor` y `Conductor` que protegen los endpoints de catálogos se definieron en Fase 1. La tabla `Empleados` tiene una relación opcional con `Usuarios`, lo que permite vincular una cuenta de acceso a un empleado físico.

En resumen: sin los catálogos de Fase 2 registrados y activos, ningún flujo de negocio del sistema puede ejecutarse.
