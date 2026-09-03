# Fase 3 – Solicitudes de combustible: creación, aprobación y rechazo

## 1. Qué se construyó

Se construyó el flujo principal del sistema: la capacidad de registrar una solicitud de combustible, revisarla y tomar una decisión sobre ella. Un empleado (u otro usuario autorizado) indica cuántos litros necesita, para qué vehículo y de qué tipo de combustible. Un supervisor o administrador luego aprueba —indicando cuánto realmente se autoriza— o rechaza, dejando un motivo escrito. Todo queda registrado en la base de datos con su estado actual para que pueda consultarse en cualquier momento.

---

## 2. Por qué se construyó así

**Máquina de estados simple (Pendiente → Aprobada | Rechazada):**
El ciclo de vida de una solicitud tiene tres y solo tres estados posibles. Modelarlo como un `enum` en lugar de un campo de texto libre evita que lleguen valores inválidos a la base de datos y hace que el código de validación sea una sola comparación (`if estado != Pendiente`). Una solicitud ya procesada no puede volver atrás; eso simplifica la auditoría y evita doble gasto de combustible.

**Validación de existencia antes de guardar:**
Antes de crear la solicitud el controlador verifica que el empleado, vehículo, departamento y tipo de combustible referenciados existan en sus respectivas tablas. Hacerlo en el controlador en lugar de dejárselo a la base de datos tiene una ventaja de comunicación: devuelve un código de error legible (`EMPLEADO_NOT_FOUND`) en lugar de una excepción críptica de clave foránea. Esto facilita enormemente el diagnóstico desde el frontend o las pruebas.

**Cantidades distintas: solicitada vs. autorizada:**
Un supervisor puede aprobar menos litros de los que se pidieron (por ejemplo, por presupuesto limitado). Guardar ambas cifras —`CantidadSolicitada` y `CantidadAutorizada`— permite comparar lo pedido con lo concedido y construir reportes de eficiencia sin perder el dato original.

**Roles diferenciados:**
La creación está abierta a Solicitantes, Supervisores y Administradores porque cualquier usuario operativo puede tener una necesidad de combustible. La aprobación/rechazo es exclusiva de Supervisores y Administradores porque implica comprometer recursos. Esta separación se impone directamente con el atributo `[Authorize(Roles = ...)]` en cada endpoint, de modo que el servidor rechaza la petición antes de ejecutar cualquier lógica de negocio.

**Sin capa de servicio:**
El patrón elegido en el proyecto es: controlador → `AppDbContext` directamente. Esto reduce la fricción de entender el flujo de datos en un proyecto académico: hay menos archivos que seguir y la lógica está toda en un mismo lugar. La contrapartida es que el controlador concentra validación + persistencia, pero en este tamaño de proyecto es aceptable.

---

## 3. Recorrido archivo por archivo

### `Models/Enums/EstadoSolicitud.cs`
Define los tres estados posibles de una solicitud como un enumerado: `Pendiente`, `Aprobada`, `Rechazada`. Al ser un `enum` tipado, el compilador impide asignar cualquier valor que no exista en la lista. Entity Framework lo persiste como un entero en la columna de la base de datos.

Ejemplo de uso: cuando se crea una solicitud, el modelo se inicializa con `Estado = EstadoSolicitud.Pendiente` sin necesidad de escribir ninguna cadena de texto.

---

### `Models/SolicitudCombustible.cs`
Es la entidad principal de la fase. Tiene campos propios (cantidades, estado, fechas, motivo de rechazo) y cuatro claves foráneas con sus propiedades de navegación: `Empleado`, `Vehiculo`, `Departamento` y `TipoCombustible`. También incluye una colección `Tickets` que anticipa la Fase 4, donde una solicitud aprobada puede dar lugar a uno o más tickets QR.

Los campos opcionales (`CantidadAutorizada`, `MotivoRechazo`, `FechaVencimiento`) son `nullable` porque solo tienen valor en ciertas etapas: `CantidadAutorizada` aparece al aprobar, `MotivoRechazo` al rechazar.

---

### `DTOs/Solicitudes/CreateSolicitudRequest.cs`
Es el objeto que el cliente envía al crear una solicitud. Contiene los IDs de las cuatro entidades relacionadas, la cantidad pedida (con validación `[Range]` que exige un valor mayor que cero) y una fecha de vencimiento opcional. No incluye el estado ni la fecha de solicitud porque esos los asigna el servidor automáticamente.

---

### `DTOs/Solicitudes/AprobarSolicitudRequest.cs`
Tiene un único campo: `CantidadAutorizada`. Es todo lo que el aprobador necesita indicar. La cantidad también tiene `[Range]` para no permitir cero ni negativos.

---

### `DTOs/Solicitudes/RechazarSolicitudRequest.cs`
Tiene un único campo: `MotivoRechazo`, con `[Required]` y `[MaxLength(500)]`. No se puede rechazar sin dar una razón, y la razón tiene tope para proteger la columna en base de datos.

---

### `DTOs/Solicitudes/SolicitudDto.cs`
Es lo que el servidor devuelve en todas las respuestas. Combina los campos de la entidad con los nombres legibles de las entidades relacionadas (`EmpleadoNombre`, `VehiculoPlaca`, `DepartamentoNombre`, `TipoCombustibleNombre`). Así el cliente no necesita hacer llamadas adicionales para mostrar los datos en pantalla. Es un `record`, lo que lo hace inmutable y con igualdad por valor de forma automática.

---

### `Controllers/SolicitudesController.cs`
El controlador expone cinco endpoints bajo la ruta `api/v1/solicitudes`:

- `GET /` — lista todas las solicitudes con sus relaciones cargadas (`Include`).
- `GET /{id}` — devuelve una solicitud específica o 404 si no existe.
- `POST /` — crea una solicitud nueva. Valida las cuatro claves foráneas, construye el objeto con estado `Pendiente` y hora UTC, lo guarda, luego carga las relaciones y responde 201 con la URL del nuevo recurso en la cabecera `Location`.
- `POST /{id}/aprobar` — cambia estado a `Aprobada` y guarda `CantidadAutorizada`. Devuelve 409 si ya estaba procesada.
- `POST /{id}/rechazar` — cambia estado a `Rechazada` y guarda `MotivoRechazo`. Devuelve 409 si ya estaba procesada.

El método privado `ToDto` centraliza la conversión de entidad a DTO para no repetir ese mapeo en cada endpoint.

Un detalle importante en `Create`: después del `SaveChangesAsync`, el controlador hace `LoadAsync` explícito para cada relación. Esto es necesario porque el objeto recién guardado aún no tiene las propiedades de navegación pobladas en memoria (solo tiene los IDs), y sin ese paso el `ToDto` fallaría al acceder a `s.Empleado.NombreCompleto`.

---

### `FuelTrack.Api.Tests/Controllers/SolicitudesControllerTests.cs`
Contiene 14 pruebas unitarias que cubren todos los escenarios relevantes. Cada prueba levanta una base de datos SQLite en memoria, crea las dependencias necesarias (empleado, vehículo, departamento, tipo de combustible) a través del método auxiliar `CrearDependenciasAsync`, y llama al controlador directamente —sin HTTP real— para verificar el tipo y contenido del resultado.

Los escenarios cubiertos incluyen:
- Lista vacía al inicio.
- 404 en consultas con ID inexistente.
- 201 con datos completos al crear.
- 400 por cada clave foránea inválida (cuatro pruebas separadas).
- 200 con estado y cantidad correctos al aprobar.
- 200 con estado y motivo correctos al rechazar.
- 409 al intentar aprobar algo ya procesado (aprobado o rechazado).
- 409 al intentar rechazar algo ya procesado (aprobado o rechazado).

---

## 4. Preguntas que podrían hacerme y cómo responderlas

**¿Por qué se usa `AsNoTracking()` en los GETs pero no en Aprobar/Rechazar?**
`AsNoTracking()` le dice a Entity Framework que no registre el objeto en su caché de seguimiento de cambios. Es más eficiente cuando solo se van a leer datos sin modificarlos. En los endpoints de aprobación y rechazo se omite porque sí necesitamos modificar el objeto y luego llamar a `SaveChangesAsync`; si se usara `AsNoTracking`, EF no sabría que el objeto cambió y no persistiría nada.

**¿Por qué el endpoint de aprobar devuelve 409 y no 400?**
HTTP 400 (Bad Request) indica que la petición está malformada o tiene datos inválidos. HTTP 409 (Conflict) indica que la petición es válida, pero choca con el estado actual del recurso. Aprobar una solicitud ya aprobada es una petición bien formada —el ID existe, la cantidad es válida— pero conflicta con el estado actual. El código 409 comunica mejor ese matiz semántico.

**¿Por qué `CantidadAutorizada` puede ser diferente de `CantidadSolicitada`?**
Refleja un proceso real de negocio: el supervisor puede no tener presupuesto para el monto completo o puede considerar que el vehículo no necesita tanta cantidad. Guardar ambos valores permite auditar si sistemáticamente se aprueba menos de lo solicitado, lo cual podría indicar que el proceso de estimación necesita mejora.

**¿Qué pasa si se envía `POST /aprobar` sin el campo `CantidadAutorizada`?**
El atributo `[Required]` en `AprobarSolicitudRequest` hace que el middleware de validación de ASP.NET Core rechace la petición con un 400 antes de que el método del controlador se ejecute. Lo mismo aplica para `[Range]`: si se envía cero o negativo, también se rechaza automáticamente.

**¿Por qué las pruebas usan SQLite en memoria en lugar de PostgreSQL?**
SQLite en memoria es mucho más rápido de inicializar (no requiere un servidor externo) y cada prueba arranca con una base de datos limpia. La desventaja es que SQLite tiene algunas diferencias con PostgreSQL (por ejemplo, en tipos de datos o funciones), pero para validar la lógica de negocio que vive en el controlador es suficiente. Las pruebas de integración contra PostgreSQL real se harían en un entorno de CI/CD.

**¿Por qué el `TipoSolicitud` siempre se guarda como `"Manual"`?**
En Fase 3 todas las solicitudes se crean a mano. La arquitectura prevé que en fases futuras puedan existir solicitudes automáticas (por ejemplo, generadas por un sistema de telemetría de vehículos), de ahí que el campo exista desde ya. Fijar el valor a `"Manual"` en el servidor, en lugar de recibirlo del cliente, evita que alguien inyecte un valor arbitrario.

**¿Por qué se necesita `LoadAsync` después del `SaveChangesAsync` al crear una solicitud?**
Cuando se llama a `_db.SolicitudesCombustible.Add(solicitud)`, EF guarda el objeto con los IDs de las relaciones, pero las propiedades de navegación (`Empleado`, `Vehiculo`, etc.) quedan en `null` en memoria porque EF no hace un SELECT de vuelta automáticamente. `LoadAsync` ejecuta ese SELECT puntual para poblar esas propiedades antes de construir el DTO de respuesta.

---

## 5. Términos clave

| Término | Definición breve |
|---|---|
| **DTO (Data Transfer Object)** | Objeto plano que define exactamente qué datos entran o salen por la API; separa el contrato externo del modelo interno de base de datos. |
| **Enum** | Tipo de dato que solo puede tomar uno de un conjunto finito de valores nombrados; aquí define los tres estados posibles de una solicitud. |
| **AsNoTracking** | Instrucción a EF Core para leer datos sin registrarlos en el contexto de cambios, mejorando el rendimiento en consultas de solo lectura. |
| **Include** | Método de EF Core que indica que se deben cargar las entidades relacionadas (joins) junto con la consulta principal. |
| **LoadAsync** | Carga explícita de una propiedad de navegación de un objeto ya guardado o recuperado por EF, útil cuando no se usó `Include` inicialmente. |
| **409 Conflict** | Código HTTP que indica que la petición es válida pero entra en conflicto con el estado actual del recurso en el servidor. |
| **Record (C#)** | Tipo de referencia inmutable en C# donde dos instancias con los mismos valores son consideradas iguales; ideal para DTOs. |
| **CancellationToken** | Mecanismo de .NET para propagar la señal de cancelación de una operación (por ejemplo, cuando el cliente cierra la conexión antes de que el servidor termine). |
| **Máquina de estados** | Modelo de diseño donde un objeto tiene un conjunto finito de estados y transiciones explícitas entre ellos; aquí: Pendiente → Aprobada o Rechazada. |
| **SQLite in-memory** | Base de datos temporal que vive en RAM, destruida al cerrar la conexión; usada en pruebas para aislar cada caso sin dependencias externas. |

---

## 6. Cómo se conecta con el resto del sistema

**Con Fase 1 (seguridad y autenticación):** Todos los endpoints de esta fase requieren `[Authorize]`. Los roles `Administrador`, `Supervisor` y `Solicitante` que se definen en `Security/Roles.cs` (construido en Fase 1) son los que controlan quién puede crear, aprobar o rechazar.

**Con Fase 2 (catálogos base):** Una solicitud no puede existir sin un empleado, vehículo, departamento y tipo de combustible válidos. Esas entidades se construyeron en Fase 2 y son las que se validan en el endpoint `Create` antes de guardar.

**Con Fase 4 (tickets QR):** La entidad `SolicitudCombustible` ya incluye una colección `Tickets`. Una solicitud en estado `Aprobada` es el prerrequisito para que se genere un ticket con código QR; el sistema de tickets de Fase 4 leerá el campo `CantidadAutorizada` para saber cuánto combustible puede amparar el ticket.

**Con Fase 5 (despacho/consumo):** Cuando un ticket se usa en la bomba de combustible, el sistema necesita conocer los datos de la solicitud original (vehículo, tipo de combustible, departamento) para registrar el consumo correctamente. La solicitud actúa como el documento madre del flujo completo.

**Con reportes y auditoría:** Los campos `FechaSolicitud`, `Estado`, `CantidadSolicitada`, `CantidadAutorizada` y `MotivoRechazo` forman la traza histórica de cada solicitud. Cualquier módulo de reportes puede consultar `api/v1/solicitudes` para obtener el historial completo con todos sus datos desnormalizados en el DTO.
