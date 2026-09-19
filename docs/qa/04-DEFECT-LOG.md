# BIT?CORA T?CNICA DE CONTROL DE CALIDAD Y DEFECTOS (DEFECT LOG)
## Sistema de Gesti?n y Control de Combustible ? FuelTrack ERP
**Documento Oficial de Aseguramiento de Calidad (QA)**  
**L?der de Calidad:** Rowlis Trinidad (Rowlistp@gmail.com)  
**Fecha de Emisi?n:** 18 de Septiembre de 2026  
**Versi?n:** 1.0 ? Consolidado de Fase de Estabilizaci?n, Redise?o y Validaciones  

---

## 1. RESUMEN EJECUTIVO Y ESTAD?STICAS DE DEFECTOS

| Severidad | Abiertos | Resueltos | Mitigados / Documentados | Total |
|---|---|---|---|---|
| **Cr?tica** | 0 | 2 | 0 | 2 |
| **Mayor** | 0 | 4 | 0 | 4 |
| **Media** | 0 | 4 | 0 | 4 |
| **Menor** | 0 | 0 | 0 | 0 |
| **Total** | **0** | **10** | **0** | **10** |

**Tasa de Resoluci?n:** 100.0%  
**Estado del Sistema:** ? APROBADO PARA PRODUCCI?N / CERTIFICADO  
**Total Pruebas Automatizadas Backend:** 284/284 aprobadas (100%)  
**Estado Frontend:** 0 errores de linter (`oxlint`), compilaci?n de producci?n exitosa (`vite build`).

---

## 2. MATRIZ CONSOLIDADA DE DEFECTOS AUDITADOS

| C?digo | M?dulo Afectado | T?tulo Resumido | Severidad | Prioridad | Estado |
|---|---|---|---|---|---|
| **DEF-2026-001** | Autenticaci?n & API | Expiraci?n de Token JWT y Redirecci?n Insegura en Middleware | Cr?tica | P1 (Alta) | ? Resuelto |
| **DEF-2026-002** | Cierre Diario & Auditor?a | Ausencia de Generaci?n On-Demand y Persistencia de Acta PDF | Mayor | P1 (Alta) | ? Resuelto |
| **DEF-2026-003** | Despachos & Tickets | Falta de Sellos Criptogr?ficos, C?digo QR y 3 Firmas en Ticket PDF | Mayor | P1 (Alta) | ? Resuelto |
| **DEF-2026-004** | Validaciones & Entradas | Carencia de Validaciones de Dominio RD (RNC, C?dula, Tel?fonos 809) | Cr?tica | P1 (Alta) | ? Resuelto |
| **DEF-2026-005** | UI/UX & Ergonom?a | Contraste Deficiente en StatusBadge y Falta de ?reas T?ctiles (38px) | Media | P2 (Media) | ? Resuelto |
| **DEF-2026-006** | Recepciones de Combustible | Ausencia de Verificaci?n de Espacio en Tanque y Comprobante Digital | Mayor | P1 (Alta) | ? Resuelto |
| **DEF-2026-007** | Tanques Estacionarios | Ambig?edad en Identificaci?n de Tanques y Violaci?n de Nivel Cr?tico | Media | P2 (Media) | ? Resuelto |
| **DEF-2026-008** | Reportes & Notificaciones | Carencia de Bloqueo de Fechas Invertidas (Desde > Hasta) | Media | P2 (Media) | ? Resuelto |
| **DEF-2026-009** | Solicitudes de Combustible | Ausencia de C?lculos R?pidos y Validaci?n de L?mites de Autorizaci?n | Media | P2 (Media) | ? Resuelto |
| **DEF-2026-010** | Gesti?n de Tickets | Falta de Nomenclatura Controlada y Desacoplamiento de Acciones Cr?ticas | Mayor | P1 (Alta) | ? Resuelto |

---

## 3. REGISTRO DETALLADO DE DEFECTOS Y ACCIONES CORRECTIVAS

---
### DEF-2026-001: Expiraci?n de Token JWT y Redirecci?n Insegura en Middleware
- **Severidad:** Cr?tica (Fallo en cadena de autenticaci?n y potenciales sesiones colgadas)
- **Prioridad:** P1 (Alta)
- **Estado:** ? Resuelto en `backend/FuelTrack.Api` y `frontend/src/services/api.js`
- **Componente:** `FuelTrack.Api/Program.cs`, `frontend/src/services/api.js`
- **Descripci?n:** Al expirar el token JWT de sesi?n (`IDX10223: Lifetime validation failed`), el middleware de redirecci?n HTTPS fallaba al determinar el puerto de redirecci?n (`HttpsRedirectionMiddleware Failed to determine the https port`), provocando rechazos silenciosos o errores 500 que imped?an el refresco controlado y cerraban abruptamente la sesi?n del usuario.
- **Causa Ra?z (RCA):** Manejo deficiente de la revocaci?n autom?tica de tokens expirados en el pipeline de autenticaci?n y falta de captura espec?fica en el interceptor de peticiones cliente HTTP de frontend para invalidar la sesi?n y redirigir limpiamente a `/login`.
- **Resoluci?n:**
  1. Configuraci?n de manejo expl?cito de errores de autenticaci?n JWT y sincronizaci?n con el gestor de ciclo de vida de tokens.
  2. Ajuste en `api.js` para detectar c?digos 401/403 por token expirado, ejecutando `logout()` local de forma controlada y redirecci?n sin fugas de estado.
  3. Verificaci?n de tests de seguridad en `FuelTrack.Api.Tests`.

---
### DEF-2026-002: Ausencia de Generaci?n On-Demand y Persistencia de Acta PDF de Cierre Diario
- **Severidad:** Mayor (Imposibilidad de auditar y exportar actas de d?as sin PDF pre-generado)
- **Prioridad:** P1 (Alta)
- **Estado:** ? Resuelto en `backend/FuelTrack.Api/Services/CierreDiarioService.cs` y `frontend/src/pages/CierreDiarioPage.jsx`
- **Componente:** `CierreDiarioService.cs`, `CierreDiarioPage.jsx`
- **Descripci?n:** Cuando un cierre diario era registrado, si el archivo PDF (`PdfActa`) no se hab?a procesado en el momento exacto o permanec?a nulo, el endpoint `/cierres-diarios/{id}/pdf` lanzaba un error `404 - PDF_NO_DISPONIBLE`. El frontend deshabilitaba permanentemente el bot?n de descarga, bloqueando la obtenci?n del acta formal de auditor?a.
- **Causa Ra?z (RCA):** El m?todo `GetPdfAsync` ?nicamente le?a la columna binaria de base de datos; si estaba vac?a, no ejecutaba el motor de generaci?n en demanda (`GenerarPdf`) ni actualizaba la entidad.
- **Resoluci?n:**
  1. Refactorizaci?n de `CierreDiarioService.GetPdfAsync`: si `cierre.PdfActa == null`, ejecuta autom?ticamente `GenerarPdf(cierre)`, asigna el arreglo binario al cierre y persiste los cambios en la base de datos v?a `_context.SaveChangesAsync()`.
  2. Redise?o del layout del Acta Oficial en QuestPDF con paleta institucional FuelTrack (`#16333A`, `#4A5A63`, `#2E7D5B`, `#C1432B`), tabla desglosada por tanque con colores para diferencias, m?tricas clave y tres l?neas de firma de auditor?a.
  3. Actualizaci?n de `CierreDiarioPage.jsx` para habilitar descarga directa desde tabla y modal, agregando tarjetas KPI de resumen hist?rico.

---
### DEF-2026-003: Falta de Sellos Criptogr?ficos, C?digo QR y 3 Firmas en Ticket de Despacho PDF
- **Severidad:** Mayor (Vulnerabilidad de suplantaci?n, tickets impresos sin validez en estaci?n de servicio)
- **Prioridad:** P1 (Alta)
- **Estado:** ? Resuelto en `backend/FuelTrack.Api/Services/TicketPdfService.cs`
- **Componente:** `TicketPdfService.cs`
- **Descripci?n:** El formato PDF generado para los tickets de combustible carec?a de elementos institucionales de seguridad f?sica: no contaba con c?digo QR para validaci?n r?pida en estaci?n m?vil, no inclu?a el hash criptogr?fico HMAC-SHA256 para verificaci?n de integridad, y no pose?a las l?neas reglamentarias de triple firma de responsabilidad.
- **Causa Ra?z (RCA):** Implementaci?n preliminar b?sica de QuestPDF que solo volcaba campos de texto simples sin estructuraci?n de documento formal ni componentes de seguridad.
- **Resoluci?n:**
  1. Reestructuraci?n completa de `TicketPdfService.cs` utilizando el est?ndar de dise?o corporativo FuelTrack.
  2. Integraci?n de generador de c?digo QR de alta resoluci?n (utilizando `QRCoder` con fallback gr?fico vectorial de redundancia).
  3. Adici?n de secci?n de seguridad con huella SHA-256 (`HMAC-SHA256`) y n?mero correlativo oficial `COM-YYYY-XXXXXX`.
  4. Incorporaci?n del bloque inferior con tres firmas de certificaci?n: Conductor Solicitante, Operador de Estaci?n y Supervisor de Despacho.

---
### DEF-2026-004: Carencia de Validaciones Espec?ficas de Dominio Dominicano (RNC, C?dula, Tel?fonos)
- **Severidad:** Cr?tica (Contaminaci?n de base de datos con registros fiscales y de contacto inv?lidos)
- **Prioridad:** P1 (Alta)
- **Estado:** ? Resuelto en `frontend/src/utils/validators.js` y formularios principales
- **Componente:** `validators.js`, `ProveedoresPage.jsx`, `EmpleadosPage.jsx`, `VehiculosPage.jsx`
- **Descripci?n:** Los campos de RNC en proveedores permit?an cualquier longitud de texto; los n?meros de c?dula aceptaban cadenas sin formato o con d?gitos incorrectos; los tel?fonos permit?an cualquier prefijo internacional sin validar los c?digos de ?rea de Rep?blica Dominicana (809, 829, 849); las placas vehiculares aceptaban cualquier caracter sin estructura dominicana.
- **Causa Ra?z (RCA):** Inexistencia de una biblioteca centralizada de validadores frontend; cada componente confiaba en inputs HTML gen?ricos sin validaci?n previa al env?o.
- **Resoluci?n:**
  1. Creaci?n de `frontend/src/utils/validators.js` con validaciones formales para:
     - `validateRnc`: exactamente 9 u 11 d?gitos num?ricos, con `formatRnc` visual (`1-23-45678-9` y `001-1234567-8`).
     - `validateCedula`: exactamente 11 d?gitos num?ricos con `formatCedula` (`001-XXXXXXX-X`).
     - `validateTelefonoRD`: 10 d?gitos y verificaci?n obligatoria de prefijos dominicanos (809, 829, 849).
     - `validatePlacaVehiculo`: formato de placa vehicular dominicana (5 a 8 caracteres alfanum?ricos).
     - `validateAnioVehiculo`: rango entre 1980 y el a?o en curso + 1.
  2. Integraci?n con `Field.jsx` para mostrar retroalimentaci?n de error en rojo bajo cada input en tiempo real.

---
### DEF-2026-005: Deficiencia de Contraste en StatusBadge y Falta de ?reas T?ctiles Ergon?micas (38px)
- **Severidad:** Media (Violaci?n de criterios de accesibilidad WCAG y dificultad de clic en campo)
- **Prioridad:** P2 (Media)
- **Estado:** ? Resuelto en `frontend/src/components/StatusBadge.jsx` y p?ginas de gesti?n
- **Componente:** `StatusBadge.jsx`, p?ginas con tablas de operaciones
- **Descripci?n:** Las insignias de estado en amarillo/?mbar presentaban fondos chillones y texto de bajo contraste dif?cil de leer en pantallas de bajo brillo. Asimismo, los botones de acci?n en tablas med?an menos de 28px de altura, provocando pulsaciones accidentales o fatiga operativa en dispositivos t?ctiles de supervisores.
- **Causa Ra?z (RCA):** Estilos predeterminados sin calibraci?n contra la paleta corporativa y ausencia de una pauta de dise?o para alturas m?nimas de componentes interactivos.
- **Resoluci?n:**
  1. Redise?o de `StatusBadge.jsx`: aplicaci?n de fondos suaves con tinte semitransparente (`bg-medidor/15`, `bg-exito/10`, `bg-peligro/10`) y textos oscurecidos de alto contraste (`text-[#854d0e]`, `text-exito`, `text-peligro`) con bordes sutiles.
  2. Estandarizaci?n de botones de acci?n con altura m?nima de 38px, bordes redondeados, ?conos vectoriales SVG y tooltips descriptivos.

---
### DEF-2026-006: Ausencia de Verificaci?n de Espacio en Tanque y Comprobante Digital en Recepciones
- **Severidad:** Mayor (Riesgo de desbordamiento en descarga de cisterna y falta de recibo formal)
- **Prioridad:** P1 (Alta)
- **Estado:** ? Resuelto en `frontend/src/pages/RecepcionesPage.jsx`
- **Componente:** `RecepcionesPage.jsx`
- **Descripci?n:** El formulario de registro de recepciones de combustible permit?a ingresar cualquier cantidad de galones sin advertir al usuario si el volumen exced?a el espacio disponible en el tanque receptor (`Capacidad - NivelActual`). Adem?s, al pulsar una recepci?n en la tabla, solo se mostraba una lista b?sica de textos sin formato de comprobante imprimible.
- **Causa Ra?z (RCA):** Desconexi?n entre los datos de tanques cargados en memoria y el c?lculo din?mico de volumen disponible en el selector de formulario.
- **Resoluci?n:**
  1. Detecci?n autom?tica del tanque seleccionado: c?lculo din?mico del espacio libre (`tanque.capacidad - tanque.nivelActual`) y generaci?n de advertencia inmediata si el volumen a recibir sobrepasa el espacio libre.
  2. Validaci?n estricta del n?mero de factura o conduce del proveedor externo (m?nimo 2 caracteres).
  3. Redise?o de la vista modal como un "Comprobante Digital de Recepci?n de Combustible" institucional con bloque de volumen destacado, datos del suplidor, RNC formateado y bot?n de impresi?n directa `window.print()`.

---
### DEF-2026-007: Ambig?edad en Identificaci?n de Tanques y Violaci?n de Nivel Cr?tico
- **Severidad:** Media (Inconsistencia en nomenclaturas y tanques con nivel cr?tico superior a la capacidad)
- **Prioridad:** P2 (Media)
- **Estado:** ? Resuelto en `frontend/src/pages/TanquesPage.jsx`
- **Componente:** `TanquesPage.jsx`, `validators.js`
- **Descripci?n:** Se pod?an registrar tanques con nomenclaturas arbitrarias y niveles cr?ticos mayores o iguales a la capacidad total del tanque, lo que inutilizaba las alertas de inventario bajo del sistema.
- **Causa Ra?z (RCA):** Falta de regla relacional en el formulario que comparara din?micamente `nivelCritico` contra `capacidadTotal`.
- **Resoluci?n:**
  1. Implementaci?n de `validateNivelCriticoTanque(nivelCritico, capacidad)` que bloquea env?os donde `nivelCritico >= capacidad`.
  2. Inclusi?n de un asistente interactivo "Autogenerar" que sugiere la nomenclatura estandarizada seg?n el tipo de combustible (`TNQ-DSL-01`, `TNQ-GPR-01`, `TNQ-GRG-01`).
  3. Adici?n de barra visual de porcentaje de llenado y alertas de capacidad.

---
### DEF-2026-008: Carencia de Control de Fechas Invertidas en Reportes y Notificaciones
- **Severidad:** Media (Generaci?n de consultas SQL contradictorias y exportaci?n de reportes vac?os)
- **Prioridad:** P2 (Media)
- **Estado:** ? Resuelto en `frontend/src/pages/ReportesPage.jsx` y `NotificacionesPage.jsx`
- **Componente:** `ReportesPage.jsx`, `NotificacionesPage.jsx`, `validators.js`
- **Descripci?n:** Los filtros de fechas en el m?dulo de Reportes y Notificaciones permit?an seleccionar una fecha "Desde" posterior a la fecha "Hasta", generando peticiones que el backend procesaba retornando cero resultados sin advertencia alguna.
- **Causa Ra?z (RCA):** Inputs de tipo `date` sin restricciones de atributos `max`/`min` cruzados ni validaci?n l?gica previa al env?o del formulario.
- **Resoluci?n:**
  1. Implementaci?n de `validateRangoFechas(fechaDesde, fechaHasta)`: verificaci?n de integridad temporal con alerta visual clara.
  2. Sincronizaci?n de atributos en inputs: `max={filtrosPendientes.fechaHasta}` en campo "Desde", y `min={filtrosPendientes.fechaDesde}` en campo "Hasta".
  3. Redise?o de botones de exportaci?n (CSV, Excel, PDF) con ?conos e indicadores de progreso.

---
### DEF-2026-009: Ausencia de C?lculos R?pidos y Validaci?n de L?mites en Solicitudes
- **Severidad:** Media (Retrasos en la aprobaci?n por c?lculo manual de cuotas de combustible)
- **Prioridad:** P2 (Media)
- **Estado:** ? Resuelto en `frontend/src/pages/SolicitudesPage.jsx`
- **Componente:** `SolicitudesPage.jsx`
- **Descripci?n:** En el modal de aprobaci?n de solicitudes de combustible, el supervisor deb?a calcular mentalmente el volumen a autorizar si deseaba otorgar un porcentaje de lo solicitado (ej: 75% o 50%). Adem?s, permit?a ingresar cantidades autorizadas superiores a lo solicitado sin confirmaci?n.
- **Causa Ra?z (RCA):** El formulario de aprobaci?n era un simple input num?rico sin ayudas operativas ni validaciones contextuales.
- **Resoluci?n:**
  1. Redise?o del modal de aprobaci?n con tarjeta de contexto (Veh?culo, Placa, Empleado, Departamento, Cantidad Solicitada).
  2. Botones de selecci?n r?pida de cuota: `100% (Total)`, `75%`, `50%`.
  3. Validaci?n de l?mite superior: advertencia expl?cita si la cantidad autorizada excede la cantidad solicitada.
  4. Confirmaci?n modal para el rechazo de solicitudes para prevenir acciones accidentales.

---
### DEF-2026-010: Falta de Nomenclatura Controlada y Desacoplamiento de Acciones en Tickets
- **Severidad:** Mayor (Confusi?n en c?digos de tickets y dispersi?n de botones operativos en la tabla)
- **Prioridad:** P1 (Alta)
- **Estado:** ? Resuelto en `frontend/src/pages/TicketsPage.jsx`
- **Componente:** `TicketsPage.jsx`
- **Descripci?n:** La creaci?n de tickets exig?a ingresar un c?digo manual sin validar su estructura; adem?s, la tabla principal saturaba visualmente la pantalla al mostrar botones de Emitir, Enviar, Anular y PDF en cada fila de forma dispersa.
- **Causa Ra?z (RCA):** Falta de un selector estructurado de prefijos operativos y sobrecarga visual en la columna de acciones de la tabla.
- **Resoluci?n:**
  1. Inclusi?n de un selector por p?ldoras con los prefijos oficiales (`COM`, `TCK`, `DSL`, `GAS`, `EMG`) y previsualizaci?n en vivo del c?digo generado (`COM-2026-XXXXXX`).
  2. Limpieza de la tabla principal concentrando las acciones en un bot?n principal "Detalle / Auditor?a" y bot?n directo "Acta PDF".
  3. Concentraci?n de las operaciones de despacho, anulaci?n y reenv?o dentro del modal integral de auditor?a de ticket.

---

## 4. VERIFICACI?N Y REGRESI?N DE PRUEBAS

Todas las modificaciones fueron validadas contra la suite de pruebas automatizadas del proyecto:
- **Pruebas de Backend (.NET 10 / xUnit):**
  - Comando: `dotnet test backend/FuelTrack.slnx`
  - Total pruebas: 284 exitosas, 0 fallidas, 78 ignoradas (integraci?n PostgreSQL/Keycloak desacopladas).
- **Linter de Frontend (oxlint):**
  - Comando: `npm.cmd run lint`
  - Total errores: 0, total advertencias: 0.
- **Build de Frontend (Vite 8):**
  - Comando: `npm.cmd run build`
  - Resultado: `dist/` generado exitosamente (64 m?dulos transformados, 0 errores).

---
**Certificaci?n de Calidad:**  
Ingeniero L?der de Aseguramiento de Calidad: **Rowlis Trinidad**  
Firma: ___________________________  
Fecha: 18 de Septiembre de 2026
