# Fase 7 – Cierre Diario

## 1. Qué se construyó

Se implementó el proceso de cierre operacional diario: al final de cada día, un supervisor o administrador registra un resumen oficial de todo lo que ocurrió con el combustible. El sistema calcula automáticamente cuánto se despachó por tanque, cuánto llegó de proveedores, cuál fue el inventario inicial y final, y si hay diferencias que no cuadran. El resultado queda guardado en la base de datos y se genera un PDF acta que sirve como evidencia oficial.

## 2. Por qué se construyó así

**Inventario inicial calculado, no almacenado manualmente:** En lugar de pedirle al usuario que ingrese el inventario de apertura, el sistema lo reconstruye automáticamente desde los `MovimientosInventario`. La lógica es: *inventario final real = existencia actual del tanque menos los movimientos que ocurrieron después del día*; *inventario inicial = inventario final real menos los movimientos del día*. Esto garantiza que el cierre siempre es consistente con el historial real y no depende de que alguien recuerde o anote una cifra manualmente.

**Volumen con signo para reconstruir el historial:** La tabla `MovimientosInventario` guarda el volumen con signo (`+` para entradas, `-` para salidas), establecido en Fase 6. Eso permite sumar todos los movimientos de un rango de tiempo y obtener el delta exacto, sin necesidad de filtrar por tipo.

**VolumenRecibido incluido en el detalle:** La fórmula de diferencias es `InventarioFinal - (InventarioInicial + VolumenRecibido - VolumenDespachado)`. Si se omite `VolumenRecibido`, cualquier recepción de combustible del día aparecería como una ganancia inexplicable. Incluirlo hace que las diferencias reflejen únicamente merma, evaporación o errores de medición reales.

**PDF almacenado en base de datos como `byte[]`:** El acta se genera una vez al crear el cierre y se guarda directamente en la columna `PdfActa` (PostgreSQL `bytea`). No se guarda en disco ni en almacenamiento externo. Esto simplifica el despliegue (sin configuración de storage), garantiza que el PDF queda vinculado al registro para siempre, y permite servirlo sin dependencias adicionales.

**Transacción completa o nada:** El cierre escribe la cabecera, todos los detalles por tanque, el PDF y la auditoría en una sola transacción de base de datos. Si algo falla a mitad (error de red, PDF inválido, etc.), Entity Framework revierte todo y la base de datos queda sin cierre parcial.

**Servicio separado del controlador:** Se creó `CierreDiarioService` siguiendo el mismo patrón de `DispatchService` de Builder 2. El controlador es delgado: extrae el actor del JWT, delega al servicio y maneja excepciones. El servicio contiene toda la lógica de negocio.

## 3. Recorrido archivo por archivo

### Modelos (`backend/FuelTrack.Api/Models/`)

**`CierreDiario.cs`**
- **Qué hace:** Cabecera del cierre: fecha, totales agregados (volumen total, inventario final, diferencias, número de despachos), PDF acta como bytes, y quién lo creó y cuándo.
- **Por qué existe:** Es el registro oficial del día. Su índice único en `Fecha` garantiza que solo puede existir un cierre por día.

**`CierreDiarioDetalle.cs`**
- **Qué hace:** Una fila por cada tanque que tuvo actividad ese día: sus despachos, recepción, inventario inicial y final, y diferencias individuales.
- **Por qué existe:** El cierre diario no es un número único — es la suma de lo que pasó en cada tanque. Guardar el detalle permite auditar discrepancias tanque por tanque, no solo en el global.

### Migración (`backend/FuelTrack.Api/Migrations/`)

**`20260907011209_AddCierreDiarioDetalle.cs`**
- Elimina columnas obsoletas `ActaDigital` y `ReporteUrl` del stub original.
- Agrega `PdfActa` (bytea), `TotalDespachos`, `CreadoPorId`, `CreadoEn` a `CierresDiarios`.
- Crea tabla `CierresDiariosDetalle` con FK cascade a `CierresDiarios` y FK restrict a `Tanques`, más índice único en `(CierreDiarioId, TanqueId)`.

### Servicio (`backend/FuelTrack.Api/Services/`)

**`CierreDiarioService.cs`**
- **`GenerarAsync`:** El método principal. Valida fecha futura y existencia de despachos. Para cada tanque con actividad ese día: suma movimientos del día y posteriores para reconstruir inventario inicial y final; suma recepciones del día; calcula diferencias. Crea la cabecera, los detalles, el PDF y la auditoría dentro de una transacción.
- **`GetAllAsync` / `GetByIdAsync`:** Consultas con EagerLoading de `CreadoPor` y de `Detalles → Tanque → TipoCombustible`.
- **`GetPdfAsync`:** Consulta selectiva que solo extrae el campo `PdfActa` — no carga todo el objeto.
- **`GenerarPdf`:** Genera el PDF con QuestPDF: tabla con una fila por tanque, totales, firma "Generado por: {usuario} — {timestamp} UTC".

### Controlador (`backend/FuelTrack.Api/Controllers/`)

**`CierresDiariosController.cs`**
- `GET /api/v1/cierres-diarios` — lista paginada, roles: Administrador, Supervisor, Auditor.
- `GET /api/v1/cierres-diarios/{id}` — detalle con desglose por tanque.
- `GET /api/v1/cierres-diarios/{id}/pdf` — descarga el PDF acta (`application/pdf`).
- `POST /api/v1/cierres-diarios` — genera el cierre, roles: Administrador, Supervisor.

### DTOs (`backend/FuelTrack.Api/DTOs/Cierres/`)

**`CierreDiarioRequest.cs`** — solo un campo: `DateOnly Fecha`. El actor y el timestamp los pone el servidor.

**`CierreDiarioDetalleResponse.cs`** — una fila del desglose por tanque: identificación, tipo de combustible, despachos, volúmenes, inventarios y diferencia.

**`CierreDiarioResponse.cs`** — cabecera del cierre: totales, quién lo creó, cuándo, y si el PDF está disponible, más la lista de detalles.

### Tests (`backend/FuelTrack.Api.Tests/Services/`)

**`CierreDiarioServiceTests.cs`** (8 tests)
- `Generar_FechaFutura_Lanza400` — no se puede cerrar un día que no ha terminado.
- `Generar_SinDespachos_Lanza400` — sin actividad no hay cierre.
- `Generar_ConDespachos_CreaEncabezadoYDetalle` — happy path: cabecera correcta, un detalle por tanque, PDF disponible.
- `Generar_InventarioInicialCorrecto` — dado inventario de 1000 y despacho de 100, el inicial es 1000 y el final 900.
- `Generar_FechaRepetida_Lanza409` — duplicado rechazado.
- `GetAll_DevuelveCierresOrdenados` — lista paginada funciona.
- `GetById_RetornaNull_CuandoNoExiste` — manejo de 404.
- `GetPdf_RetornaBytes_CuandoExiste` — el PDF se puede recuperar después de creado.

## 4. Preguntas que podrían hacerme y cómo responderlas

**¿Por qué el inventario inicial no lo ingresa el usuario?**
Porque cualquier número que ingrese manualmente puede diferir del historial real. El sistema lo reconstruye desde los `MovimientosInventario` que se generaron automáticamente en cada despacho, recepción y ajuste. Si el usuario ingresara el número, cualquier error tipográfico produciría diferencias ficticias.

**¿Qué son las "diferencias" en el cierre?**
Es la discrepancia entre lo que el inventario debería tener según los cálculos y lo que realmente hay. La fórmula es: `InventarioFinal - (InventarioInicial + VolumenRecibido - VolumenDespachado)`. Si el resultado es cero, todo cuadra. Si es negativo, hay más combustible "desaparecido" del que explican los despachos (merma, evaporación, fuga). Si es positivo, hay combustible de más (error de medición o recepción no registrada).

**¿Se puede cerrar el día de hoy mientras hay despachos en curso?**
Sí, técnicamente el sistema lo permite porque el cierre captura la fotografía del inventario en el momento en que se genera. En la práctica, el proceso operacional debería cerrar el día al terminar el turno, no a mitad. El sistema no impide hacerlo antes, pero las diferencias reflejarán cualquier despacho que no se hubiera registrado aún.

**¿Qué pasa si se intenta cerrar el mismo día dos veces?**
El sistema rechaza el segundo intento con HTTP 409 y código `CIERRE_YA_EXISTE`. La tabla tiene un índice único en `Fecha`, así que aunque la validación previa falle por alguna condición de carrera, la base de datos rechazaría el duplicado de todas formas.

**¿Por qué el PDF se guarda en la base de datos y no en disco?**
Para simplificar el despliegue. Si se guardara en disco, habría que configurar rutas, permisos, backups independientes y acceso compartido si hay múltiples instancias del servidor. Al guardarlo en PostgreSQL como `bytea`, el PDF viaja con el mismo backup de la base de datos y no requiere configuración adicional. La desventaja es que PDFs grandes ocupan espacio en la BD, pero para un acta diaria de pocos KB es totalmente aceptable.

**¿Qué rol se necesita para crear un cierre?**
Solo `Administrador` o `Supervisor`. El `Despachador` puede operar el surtidor pero no tiene autorización para generar el cierre oficial del día — esa es una responsabilidad gerencial.

**¿Qué pasa si la generación del PDF falla dentro de la transacción?**
La excepción propaga hacia el `catch` del bloque de transacción, que ejecuta `RollbackAsync`. Ningún dato se guarda: ni la cabecera del cierre, ni los detalles, ni la auditoría. El día queda sin cerrar y puede intentarse de nuevo.

## 5. Términos clave

| Término | Definición |
|---|---|
| **CierreDiario** | Registro oficial del día: totales de despacho, inventario final, diferencias y PDF acta |
| **CierreDiarioDetalle** | Desglose por tanque dentro de un cierre: volúmenes, inventarios y diferencias individuales |
| **InventarioInicial** | Cantidad de combustible en el tanque al inicio del día, reconstruida desde `MovimientosInventario` |
| **Diferencias** | Discrepancia entre el inventario teórico (calculado) y el real (según la base de datos) |
| **PdfActa** | Documento PDF generado al momento del cierre, almacenado como `byte[]` en la base de datos |
| **dayStart / dayEnd** | Límites UTC del día (`TimeOnly.MinValue` / `TimeOnly.MaxValue`) para filtrar movimientos con precisión |
| **QuestPDF** | Librería .NET para generar PDFs mediante código C# — sin plantillas HTML, sin dependencias externas |
| **bytea** | Tipo de columna PostgreSQL para almacenar datos binarios (equivalente a `byte[]` en C#) |
| **Transacción** | Operación de base de datos que se ejecuta completa o se revierte totalmente — no hay estados intermedios |

## 6. Cómo se conecta con el resto del sistema

**Depende de:**
- **Fase 6 (Inventario):** Lee `MovimientosInventario` para reconstruir el inventario inicial y final. Lee `RecepcionesCombustible` para conocer el volumen recibido en el día. Usa el mismo patrón de volumen con signo establecido en Fase 6.
- **Fase 5 (Despacho):** Lee la tabla `Despachos` para saber cuántos galones se sirvieron y desde qué tanque.
- **Fase 0 (Base de datos):** La tabla `CierresDiarios` fue creada en el schema inicial como stub. Esta fase la completa con la migración `AddCierreDiarioDetalle`.
- **Fase 1 (Seguridad):** El JWT provee el ID del actor para la auditoría y para el campo `CreadoPorId`.

**Es base para:**
- **Fase 8 (Reportes):** El reporte de tipo `cierres` consulta directamente la tabla `CierresDiarios`. Sin cierres generados, ese reporte estaría vacío.
- **Frontend (Builder 3):** El endpoint `GET /cierres-diarios` y el PDF descargable son la pantalla de cierre operacional del día en el panel de administración.
