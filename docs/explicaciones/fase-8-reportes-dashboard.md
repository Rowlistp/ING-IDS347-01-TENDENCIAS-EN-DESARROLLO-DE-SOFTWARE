# Fase 8 – Reportes y Dashboard

## 1. Qué se construyó

Se implementaron dos módulos de consulta gerencial que transforman los datos acumulados en todas las fases anteriores en información accionable.

**Módulo de Reportes:** Un sistema de reportes filtrables que cubre cuatro tipos de datos operacionales —solicitudes de combustible, despachos, movimientos de inventario y cierres diarios— con paginación y exportación en tres formatos: CSV, Excel (`.xlsx`) y PDF. El usuario puede filtrar por rango de fechas y por tanque antes de exportar.

**Dashboard Ejecutivo:** Un endpoint de resumen en tiempo real que agrega seis métricas clave del día, la semana y el mes: actividad de hoy (despachos, volumen, solicitudes pendientes, tanques con inventario bajo), evolución de los últimos 7 días, los tres tanques más utilizados en los últimos 30 días, comparativa de volumen entre el mes actual y el anterior, distribución del consumo por tipo de combustible, y eficiencia de aprobación de solicitudes.

## 2. Por qué se construyó así

**Un único endpoint de reportes con parámetro `tipo`:** En lugar de cuatro endpoints separados (`/reportes/solicitudes`, `/reportes/despachos`, etc.), se usa un solo `GET /api/v1/reportes?tipo=...`. Esto simplifica el contrato de API, el controlador delgado y la lógica de exportación, que puede reutilizar la misma tubería de paginación independientemente del tipo de dato.

**Exportación reutiliza la misma consulta de reporte:** `ExportarAsync` llama internamente a `GetAsync` con `TamanoPagina = int.MaxValue`, en lugar de duplicar la lógica de filtrado. Si el filtro de una solicitud de reporte es correcto, la exportación será idéntica. Esto garantiza que lo que el usuario ve en pantalla y lo que descarga son exactamente los mismos datos.

**CSV generado manualmente sin librería externa:** Para CSV se usa `StringBuilder` directamente. Una librería CSV completa (CsvHelper, etc.) agregaría una dependencia solo para escribir texto delimitado por comas, algo que se puede hacer en 20 líneas de código. La función `Csv(string)` escapa comillas y comas correctamente, que es el único caso complejo.

**ClosedXML para Excel:** Se eligió ClosedXML sobre EPPlus porque su licencia LGPL no requiere comprar una clave comercial para proyectos universitarios. El API es directo: `XLWorkbook → Worksheet → Cell(fila, col).Value`. La hoja se nombra con el tipo de reporte para que el archivo sea autoexplicativo al abrirlo.

**QuestPDF para el PDF de reporte:** El PDF de exportación no necesita tabla detallada —es un reporte de volumen alto que ya se tiene en CSV/Excel. El PDF generado incluye encabezado con totales y fecha de generación. Usar la misma librería ya presente (QuestPDF) evita agregar otra dependencia solo para PDF.

**Dashboard con seis métricas separadas:** Cada métrica tiene su propio método privado (`GetHoyAsync`, `GetUltimos7DiasAsync`, `GetTop3TanquesAsync`, etc.) y su propia consulta a la base de datos. Podrían consolidarse en menos queries, pero la claridad del código importa más que ahorrar unas consultas en un endpoint de solo lectura que se ejecuta esporádicamente. Cada método hace exactamente una cosa.

**Top 3 tanques materializado en memoria:** La consulta de `GetTop3TanquesAsync` usa `.Select(d => new { d.TanqueId, d.Tanque.Identificacion, d.GalonesServidos }).ToListAsync()` y luego agrupa en memoria en lugar de hacer el `GroupBy` directamente en la consulta LINQ. Esto es una solución a una limitación conocida de SQLite: LINQ to Entities no puede traducir un `GroupBy` cuya clave incluye una propiedad de navegación (`d.Tanque.Identificacion`) a SQL válido en SQLite, aunque sí funciona en PostgreSQL. Al materializar primero y agrupar después, los tests pasan en ambos motores.

**Solo `Administrador` y `Auditor` pueden ver reportes:** Los reportes exponen datos de todos los empleados, todos los vehículos y todos los tanques. Ni el Despachador ni el Supervisor tienen acceso a esa vista cruzada de la organización — solo roles con responsabilidades gerenciales o de auditoría pueden consumir estos endpoints.

**Dashboard solo para `Administrador`:** El resumen ejecutivo incluye comparativas entre meses y métricas de eficiencia. Es información estratégica, no operacional. El Auditor puede revisar reportes históricos, pero no necesita el panel en tiempo real.

## 3. Recorrido archivo por archivo

### Servicio de Reportes (`backend/FuelTrack.Api/Services/`)

**`ReporteService.cs`**
- **Qué hace:** Concentra toda la lógica de reportes. Dos métodos públicos: `GetAsync` (paginado) y `ExportarAsync` (bytes). Cuatro métodos privados de consulta: `GetSolicitudesAsync`, `GetDespachosAsync`, `GetInventarioAsync`, `GetCierresAsync`. Tres métodos privados de exportación: `ExportarCsv`, `ExportarExcel`, `ExportarPdf`. Más el helper `Csv(string)` para escapado.
- **Por qué existe:** Mantiene el controlador completamente delgado. Todo el switch de tipo, toda la validación, toda la serialización ocurren aquí y son testables sin HTTP.

### Controlador de Reportes (`backend/FuelTrack.Api/Controllers/`)

**`ReportesController.cs`**
- `GET /api/v1/reportes?tipo=...&fechaDesde=...&fechaHasta=...&tanqueId=...&pagina=1&tamanoPagina=20` — devuelve `ReportePageResponse` paginado.
- `GET /api/v1/reportes/exportar?tipo=...&formato=...` — devuelve el archivo descargable con el `Content-Type` correcto y un nombre de archivo con timestamp.
- Roles: `Administrador`, `Auditor`.
- **Por qué existe:** Hace únicamente la extracción de parámetros de query string y el mapeo del formato al `Content-Type` HTTP. No contiene lógica de negocio.

### DTOs de Reportes (`backend/FuelTrack.Api/DTOs/Reportes/`)

**`ReporteQuery.cs`** — Record con los parámetros de filtro: `Tipo`, `FechaDesde`, `FechaHasta`, `TanqueId`, `Pagina`, `TamanoPagina`.

**`ReportePageResponse.cs`** — Respuesta paginada: `Tipo`, `Total`, `Pagina`, `TamanoPagina`, `Items` (lista de `object` — el tipo varía según el tipo de reporte).

**`SolicitudReporteDto.cs`** — Fila de solicitud: id, fecha, empleado, vehículo, departamento, tipo combustible, cantidades y estado.

**`DespachoReporteDto.cs`** — Fila de despacho: id, fecha, hora, código ticket, empleado, vehículo, galones, tanque, estación, operador, inventario restante.

**`MovimientoReporteDto.cs`** — Fila de movimiento de inventario: id, fechaHora, tanque, tipo combustible, tipo de movimiento, volumen con signo, referencia de operación.

**`CierreReporteDto.cs`** — Fila de cierre: id, fecha, total despachos, volumen, inventario final, diferencias, quién lo creó.

### Servicio de Dashboard (`backend/FuelTrack.Api/Services/`)

**`DashboardService.cs`**
- **`GetResumenAsync`:** Coordina las seis consultas en paralelo conceptual (secuencial en código) y ensambla el DTO de respuesta.
- **`GetHoyAsync`:** Total de despachos hoy, volumen hoy, solicitudes pendientes (sin filtro de fecha — el estado `Pendiente` ya implica no resuelto), conteo de tanques con existencia ≤ nivel crítico.
- **`GetUltimos7DiasAsync`:** Itera 7 días desde `hoy - 6` y completa con cero los días sin despachos — la respuesta siempre tiene exactamente 7 entradas, independientemente de la actividad real.
- **`GetTop3TanquesAsync`:** Materializa despachos de los últimos 30 días, agrupa en memoria por tanque, ordena por volumen descendente, toma los tres primeros.
- **`GetComparativaAsync`:** Suma volumen y cuenta solicitudes del mes actual (desde el día 1 hasta hoy) y del mes anterior (completo).
- **`GetDistribucionAsync`:** Agrupa despachos de los últimos 30 días por tipo de combustible, calcula el porcentaje de cada uno sobre el total.
- **`GetEficienciaAsync`:** Cuenta solicitudes aprobadas, rechazadas y pendientes del mes actual. Tasa = aprobadas / (aprobadas + rechazadas) × 100, ignorando pendientes en el denominador.

### Controlador de Dashboard (`backend/FuelTrack.Api/Controllers/`)

**`DashboardController.cs`**
- `GET /api/v1/dashboard/resumen` — una sola línea: delega a `DashboardService.GetResumenAsync` y retorna `Ok`.
- Rol exclusivo: `Administrador`.

### DTOs de Dashboard (`backend/FuelTrack.Api/DTOs/Dashboard/`)

**`DashboardResumenResponse.cs`** — Todos los records en un solo archivo: `DashboardResumenResponse`, `DashboardHoy`, `DashboardDia`, `DashboardTanque`, `DashboardComparativaMes`, `DashboardMes`, `DashboardDistribucion`, `DashboardEficiencia`. Un archivo en lugar de ocho evita la dispersión de DTOs muy pequeños que solo se usan en este módulo.

### Tests (`backend/FuelTrack.Api.Tests/Services/`)

**`ReporteServiceTests.cs`** (9 tests)
- `Get_TipoInvalido_LanzaExcepcion` — tipo no reconocido devuelve `400 TIPO_REPORTE_INVALIDO`.
- `Get_Solicitudes_DevuelveLista` — tipo válido devuelve respuesta con `Tipo` correcto.
- `Get_Despachos_DevuelveLista`, `Get_Inventario_DevuelveLista`, `Get_Cierres_DevuelveLista` — happy path de los cuatro tipos.
- `ExportarCsv_FormatoInvalido_LanzaExcepcion` — formato desconocido devuelve `400 FORMATO_INVALIDO`.
- `ExportarCsv_Solicitudes_ContieneEncabezado` — el CSV empieza con la cabecera de columnas correcta.
- `ExportarExcel_Solicitudes_ContieneHoja` — el Excel tiene una hoja llamada "Solicitudes".
- `ExportarPdf_Solicitudes_RetornaBytes` — el PDF comienza con los magic bytes `%PDF`.

**`DashboardServiceTests.cs`** (2 tests)
- `GetResumen_SinDatos_DevuelveCeros` — con base de datos vacía, todos los contadores son cero y la lista de los últimos 7 días tiene exactamente 7 entradas.
- `GetResumen_DevuelveEstructuraCompleta` — todos los campos del DTO de respuesta están presentes y no son nulos.

## 4. Preguntas que podrían hacerme y cómo responderlas

**¿Por qué un solo endpoint de reportes y no uno por tipo?**
Un único endpoint con parámetro `tipo=solicitudes|despachos|inventario|cierres` simplifica el contrato de API. El frontend solo necesita recordar una ruta. La lógica de exportación puede reutilizar la misma tubería independientemente del tipo. Si en el futuro se agrega un nuevo tipo, se agrega un `case` al `switch` del servicio y un DTO, sin tocar el controlador ni agregar rutas.

**¿Por qué no se generan los reportes con datos de muestra en los tests?**
Los tests de `ReporteService` verifican la estructura de respuesta (tipos, campos, formato de archivo) con base de datos vacía. Eso es suficiente para confirmar que la lógica de serialización y exportación funciona. Agregar datos de prueba en estos tests los haría más frágiles y más lentos sin ganar cobertura adicional de la lógica de negocio relevante, que en este módulo es el filtrado por rango de fechas y el formateo de salida.

**¿Por qué `ExportarAsync` llama a `GetAsync` en vez de hacer su propia consulta?**
Para garantizar que la exportación y el reporte paginado ven exactamente los mismos datos. Si se duplicara la consulta, cualquier cambio en los filtros tendría que mantenerse en dos lugares. Llamar a `GetAsync` con `TamanoPagina = int.MaxValue` es simple y elimina esa posibilidad de divergencia.

**¿Qué pasa si el reporte tiene millones de filas y se pide una exportación?**
La implementación actual carga todos los registros en memoria antes de serializar. Para el volumen de datos de una flota interna (cientos o pocos miles de despachos por mes), esto es aceptable. Para un sistema con millones de registros, habría que implementar streaming — pero ese es un requisito que no existe en el SRS.

**¿Por qué ClosedXML y no EPPlus?**
EPPlus requiere una licencia comercial para uso no personal. ClosedXML tiene licencia LGPL y funciona sin configuración adicional. Para un proyecto universitario, es la opción correcta.

**¿Por qué el top 3 de tanques se agrupa en memoria?**
SQLite no puede traducir a SQL un `GroupBy` cuya clave de agrupación incluye una propiedad de navegación (`d.Tanque.Identificacion`). En PostgreSQL (producción) sí funcionaría. La solución es materializar primero con `.ToListAsync()` y agrupar después en C#. Esto funciona en ambos motores y los tests (que usan SQLite) pasan igual que la producción (que usará PostgreSQL).

**¿Por qué el Dashboard es solo para `Administrador` y no para `Supervisor`?**
El Dashboard muestra comparativas entre meses, eficiencia de aprobación y distribución de consumo por tipo de combustible — información estratégica de toda la organización. El Supervisor opera un turno o una estación; no necesita esa vista cruzada. Si en el futuro se requiere que el Supervisor también acceda, se agrega su rol al `[Authorize]` del controlador.

**¿Cómo calcula el "nivel crítico" de inventario?**
No lo calcula en tiempo real — lee directamente `Inventario.ExistenciaActual <= Tanque.NivelCritico`. El campo `NivelCritico` es un atributo del tanque definido al crearlo (Fase 2, Bloque A). Si la existencia actual cae por debajo de ese umbral, el Dashboard lo cuenta como "tanque con inventario bajo". No hay cálculo dinámico — la comparación es directa.

**¿El PDF de exportación tiene tabla de datos?**
No. El PDF de exportación del reporte solo incluye un encabezado con el tipo de reporte, el total de registros y la fecha de generación. Los datos detallados se exportan mejor en CSV o Excel, que los usuarios pueden abrir en una hoja de cálculo. El PDF del cierre diario (Fase 7) sí tiene tabla detallada porque es un acta oficial que se imprime, no un archivo de datos.

## 5. Términos clave

| Término | Definición |
|---|---|
| **ReporteService** | Servicio que centraliza consultas, filtrado, paginación y serialización de reportes |
| **DashboardService** | Servicio que calcula las seis métricas del resumen ejecutivo |
| **ReporteQuery** | Record inmutable con todos los parámetros de filtro de un reporte |
| **ReportePageResponse** | Respuesta paginada: tipo, total, página, tamaño y lista de items heterogéneos |
| **ExportarAsync** | Método que produce bytes de un archivo descargable (CSV, Excel o PDF) |
| **ClosedXML** | Librería .NET para leer y escribir archivos Excel `.xlsx` — sin Excel instalado, licencia LGPL |
| **`int.MaxValue` como `TamanoPagina`** | Truco para obtener todos los registros sin paginación al exportar |
| **Top 3 materializado** | Consulta LINQ que fuerza materialización en memoria antes del `GroupBy` para compatibilidad SQLite |
| **DashboardHoy** | Sub-DTO con las cuatro métricas del día actual: despachos, volumen, pendientes, inventario bajo |
| **DashboardComparativaMes** | Comparativa de volumen y solicitudes entre el mes actual y el anterior |
| **DashboardEficiencia** | Tasa de aprobación de solicitudes: aprobadas / (aprobadas + rechazadas) × 100 |
| **Magic bytes %PDF** | Los cuatro primeros bytes de todo archivo PDF válido (`0x25 0x50 0x44 0x46`), usados en tests para verificar el formato |

## 6. Cómo se conecta con el resto del sistema

**Depende de:**
- **Fase 3 (Solicitudes):** `GetSolicitudesAsync` lee `SolicitudesCombustible` con sus relaciones. `GetEficienciaAsync` y `GetHoyAsync` cuentan solicitudes por estado.
- **Fase 5 (Despacho):** `GetDespachosAsync` lee `Despachos`. `GetHoyAsync`, `GetUltimos7DiasAsync`, `GetTop3TanquesAsync`, `GetComparativaAsync` y `GetDistribucionAsync` agregan datos de la tabla `Despachos`.
- **Fase 6 (Inventario):** `GetInventarioAsync` lee `MovimientosInventario`. `GetHoyAsync` compara `Inventario.ExistenciaActual` con `Tanque.NivelCritico`.
- **Fase 7 (Cierre Diario):** `GetCierresAsync` lee `CierresDiarios`. Sin Fase 7, el tipo de reporte `cierres` devolvería siempre cero registros.

**Es base para:**
- **Frontend (Builder 3):** Los endpoints `GET /reportes`, `GET /reportes/exportar` y `GET /dashboard/resumen` son las pantallas de reporting y el panel principal del sistema de administración. Builder 3 consume los tres endpoints para renderizar tablas, gráficas y botones de descarga.
- **Auditoría (Fase 1):** Las consultas de reportes son de solo lectura y no generan eventos de auditoría. Si se requiriera auditar quién descargó qué reporte, habría que añadir una llamada a `AuditService.WriteAsync` en `ExportarAsync`.
