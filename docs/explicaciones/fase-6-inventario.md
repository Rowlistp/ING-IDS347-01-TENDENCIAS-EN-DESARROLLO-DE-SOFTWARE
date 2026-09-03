# Fase 6 – Inventario Completo

## 1. Qué se construyó

Se implementó el sistema de control de combustible almacenado en los tanques de la estación. Esto incluye recibir combustible de proveedores, ajustar cantidades por errores de medición, transferir combustible entre tanques, y consultar el historial de todos esos movimientos. Todo queda registrado automáticamente para trazabilidad.

## 2. Por qué se construyó así

**Una sola transacción para todo:** Cuando llega combustible de un proveedor, el sistema necesita hacer tres cosas al mismo tiempo: registrar la recepción, crear un registro de movimiento y actualizar el saldo del tanque. Si alguna de esas fallas a mitad, los datos quedarían inconsistentes (por ejemplo: registrada la recepción pero el tanque sin actualizar). Para evitar eso, todo se guarda en una sola operación atómica (`SaveChangesAsync`), que o funciona completa o no guarda nada.

**Volumen con signo:** En lugar de guardar por separado "tipo: entrada" y "cantidad: 200", el sistema guarda la cantidad con signo: `+200` para incrementos, `-200` para reducciones. Esto permite calcular el saldo histórico simplemente sumando todos los movimientos, y hace que el historial sea legible de un vistazo.

**Sin capa intermedia:** Los controladores acceden directamente a la base de datos (via `AppDbContext`) sin pasar por servicios adicionales. Esto sigue el patrón ya establecido en Fases 2 y 3, manteniendo el código simple y consistente para un equipo pequeño.

**Tres controladores en lugar de uno:** Se separaron en tres responsabilidades distintas porque sus ciclos de vida son diferentes: las recepciones se registran al recibir un camión cisterna, los ajustes los hace solo el administrador para correcciones, y el historial lo consultan todos. Separarlos hace más claro quién puede hacer qué.

## 3. Recorrido archivo por archivo

### Controladores (`backend/FuelTrack.Api/Controllers/`)

**`RecepcionesController.cs`**
- **Qué hace:** Registra la llegada de combustible desde un proveedor. Al crear una recepción, automáticamente actualiza el saldo del tanque y crea un registro de movimiento de tipo "Entrada".
- **Por qué existe:** Sin esto no habría forma oficial de incrementar el inventario. Cualquier aumento de combustible en el sistema pasa por aquí.
- **Ejemplo:** El supervisor recibe un camión cisterna con la factura `FAC-2026-001`. Hace POST a `/api/v1/recepciones` con el ID del proveedor, el tanque de destino, el volumen y la factura. El sistema guarda la recepción, suma los litros al tanque y deja constancia del movimiento.

**`InventarioController.cs`**
- **Qué hace:** Expone el estado actual de cada tanque (saldo, disponibilidad, última actualización) y permite dos operaciones: ajustes y transferencias.
  - *Ajuste:* El administrador puede corregir el saldo de un tanque (positivo o negativo), con una observación obligatoria que justifica el cambio.
  - *Transferencia:* Mueve litros de un tanque a otro. Descuenta del origen, suma al destino, y crea dos registros de movimiento (uno en cada tanque).
- **Por qué existe:** Los tanques pierden o ganan pequeñas cantidades por evaporación, medición imprecisa, o errores de registro. El ajuste permite corregirlos con trazabilidad. La transferencia permite redistribuir combustible entre puntos de despacho.
- **Ejemplo de transferencia:** Se necesita pasar 500 litros del Tanque A al Tanque B. POST a `/api/v1/inventario/transferencias`. El sistema valida que A tenga suficiente, resta 500 de A, suma 500 a B, y crea dos MovimientosInventario: uno en A con `-500` y referencia `"HACIA-TANQUE-2"`, y otro en B con `+500` y referencia `"DESDE-TANQUE-1"`.

**`MovimientosController.cs`**
- **Qué hace:** Permite consultar el historial de todos los movimientos de inventario. Acepta un filtro opcional `?tanqueId=` para ver solo los movimientos de un tanque específico.
- **Por qué existe:** Sin historial, sería imposible auditar discrepancias, responder preguntas como "¿cuándo y por qué bajó el saldo del Tanque 3?" o cumplir requerimientos de trazabilidad (RF-17).
- **Ejemplo:** El auditor quiere ver todo lo que pasó en el Tanque 2 durante el mes. GET a `/api/v1/inventario/movimientos?tanqueId=2`. Recibe la lista de todos los movimientos filtrados.

### DTOs (`backend/FuelTrack.Api/DTOs/`)

**`CreateRecepcionRequest.cs` / `RecepcionDto.cs`**
- Definen qué datos se envían para crear una recepción (proveedor, tanque, factura, volumen, fecha) y qué datos se devuelven (incluyendo el nombre del proveedor y la identificación del tanque).

**`AjustarInventarioRequest.cs`**
- Define los datos para un ajuste: el tanque, el volumen (positivo o negativo), y una observación obligatoria. La observación es obligatoria porque un ajuste sin explicación no tiene trazabilidad válida.

**`TransferirRequest.cs` / `TransferenciaResultDto.cs`**
- El request pide tanque origen, destino, volumen y observación opcional. El resultado devuelve el estado actualizado de ambos inventarios (origen y destino) para que el frontend muestre el cambio inmediatamente.

**`MovimientoDto.cs`**
- Representa un movimiento del historial: tipo (Entrada/Salida/Ajuste/Transferencia/Merma), volumen con signo, fecha, referencia a la operación origen, observaciones, tanque y usuario que lo realizó.

**`InventarioDto.cs`**
- El estado actual de un tanque en inventario: existencia actual, disponibilidad, última actualización, y datos del tanque (identificación, capacidad).

### Tests (`backend/FuelTrack.Api.Tests/Controllers/`)

**`RecepcionesControllerTests.cs`** (7 tests)
- Prueba el happy path de creación (verifica que el saldo del tanque aumentó y que se creó el MovimientoInventario), más los casos de error: proveedor inexistente, tanque inexistente, tanque inactivo.

**`InventarioControllerTests.cs`** (16 tests)
- Cubre consultas de inventario, ajustes (happy path, tanque no existe, tanque inactivo, saldo insuficiente), y transferencias (happy path verificando ambos inventarios y ambos movimientos, origen igual a destino, origen no existe, destino no existe, destino inactivo, saldo insuficiente).

**`MovimientosControllerTests.cs`** (2 tests)
- Verifica que sin filtro devuelve todos los movimientos, y que con `?tanqueId=` filtra correctamente y mapea bien los campos del DTO.

## 4. Preguntas que podrían hacerme y cómo responderlas

**¿Qué es un MovimientoInventario y para qué sirve?**
Es el registro de toda operación que cambia el saldo de un tanque. Funciona como el estado de cuenta de un banco: cada depósito, retiro o ajuste queda registrado con fecha, quién lo hizo, y cuánto. Eso permite auditar y reconstruir el historial completo.

**¿Por qué el volumen puede ser negativo?**
Porque en lugar de tener dos campos ("tipo de operación" + "cantidad"), el sistema usa un solo campo con signo. Positivo significa que el tanque recibió combustible, negativo significa que salió o se redujo. En una transferencia, el tanque origen tiene `-200` y el destino tiene `+200` en sus respectivos registros. Esto simplifica los cálculos del historial.

**¿Qué pasa si se intenta ajustar un tanque y el resultado sería negativo?**
El sistema rechaza la operación con HTTP 409 (Conflict) y el código de error `INVENTARIO_INSUFICIENTE`. No se guarda nada. El saldo nunca puede quedar en negativo porque físicamente es imposible tener menos de cero litros.

**¿Por qué las transferencias crean dos movimientos en lugar de uno?**
Porque cada tanque tiene su propio historial. El Tanque A necesita un registro que diga "transferí 200 litros al Tanque B", y el Tanque B necesita uno que diga "recibí 200 litros del Tanque A". Si fuera un solo registro, no quedaría claro en el historial de cada tanque de dónde vino o a dónde fue el combustible.

**¿Cómo sabe el sistema qué usuario hizo cada operación?**
El token JWT del usuario autenticado contiene su ID. El controlador lo extrae con `User.FindFirstValue(ClaimTypes.NameIdentifier)` y lo guarda en cada MovimientoInventario. Sin estar autenticado, la operación es rechazada automáticamente.

**¿Qué pasa si se cae la conexión a mitad de una transferencia?**
Nada se guarda. Toda la transferencia (descuento del origen, incremento del destino, dos movimientos) se ejecuta en una sola transacción de base de datos. Si algo falla, Entity Framework revierte todo automáticamente. O se completa todo, o no se completa nada.

**¿Por qué el ajuste requiere observación obligatoria pero la transferencia no?**
El ajuste es una corrección de discrepancia (por evaporación, medición incorrecta, etc.) y es irreversible. Una observación obligatoria asegura que siempre haya una justificación documentada. La transferencia tiene origen y destino claros, por lo que la referencia cruzada entre tanques ya es suficiente trazabilidad.

**¿Qué diferencia hay entre ExistenciaActual y Disponibilidad en el inventario?**
Ambas se actualizan juntas en esta fase, pero conceptualmente representan cosas distintas: `ExistenciaActual` es el volumen físico total en el tanque, y `Disponibilidad` es cuánto se puede despachar (podría ser menor si hay combustible reservado para solicitudes aprobadas). En Fase 7 (cierre diario y despachos) estas dos cifras podrían divergir.

## 5. Términos clave

| Término | Definición |
|---|---|
| **MovimientoInventario** | Registro histórico de cualquier cambio en el saldo de un tanque (entrada, ajuste, transferencia, etc.) |
| **Transacción atómica** | Operación que se ejecuta completa o no se ejecuta nada — no hay estados intermedios en la base de datos |
| **Volumen con signo** | Número que puede ser positivo (incremento) o negativo (reducción), guardado en un solo campo |
| **TipoMovimiento** | Enumeración que clasifica un movimiento: Entrada, Salida, Ajuste, Transferencia, Merma |
| **SaveChangesAsync** | Método de Entity Framework que envía todos los cambios pendientes a la base de datos en una sola transacción |
| **AsNoTracking** | Modo de consulta donde EF no monitorea los objetos para detectar cambios — más eficiente para solo leer datos |
| **Cascade** | Efecto en cadena donde una sola operación de negocio desencadena múltiples cambios en la base de datos |
| **DTO (Data Transfer Object)** | Clase que define exactamente qué datos entran y salen de un endpoint, sin exponer el modelo interno |
| **HTTP 409 Conflict** | Código de respuesta para cuando la solicitud es válida pero entra en conflicto con el estado actual de los datos (ej: saldo insuficiente) |
| **ClaimTypes.NameIdentifier** | Campo del token JWT que contiene el ID del usuario autenticado |

## 6. Cómo se conecta con el resto del sistema

**Depende de:**
- **Fase 0 (Base de datos):** Las tablas `Inventario`, `MovimientoInventario`, `RecepcionCombustible`, `Tanque` y `Proveedor` fueron creadas en la migración inicial. Esta fase no requirió nuevas migraciones.
- **Bloque A (Catálogos — Fase 2):** Los `Tanques` y `Proveedores` que se usan en las recepciones y ajustes fueron creados en esa fase. Cada tanque creado en Bloque A ya tiene su `Inventario` asociado automáticamente.
- **Fase 1 (Seguridad):** El JWT y el sistema de roles controlan quién puede hacer qué. `POST /recepciones` requiere Administrador o Supervisor; `POST /ajustes` solo Administrador; las consultas las puede hacer cualquier usuario autenticado.

**Es base para:**
- **Fase 7 (Cierre diario):** El cierre diario necesita leer el inventario actual y crear movimientos de tipo Salida cuando se despacha combustible. Usa exactamente las mismas tablas y el mismo patrón de cascade que se estableció en esta fase.
- **Fase 8 (Reportes):** Los reportes de consumo y existencia se construyen sobre la tabla `MovimientosInventario`. Toda la trazabilidad que genera esta fase es la materia prima de los reportes.
- **Frontend (Builder 3):** Los endpoints de esta fase exponen el inventario en tiempo real para el dashboard y los formularios de recepción y transferencia.
