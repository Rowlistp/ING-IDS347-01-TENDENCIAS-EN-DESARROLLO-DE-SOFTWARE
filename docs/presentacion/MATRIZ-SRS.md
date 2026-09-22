# Matriz de cumplimiento SRS - FuelTrack

Revisión: 22/09/2026. Fuente: PDF original SRS Ticket Digitales v1.0 (agosto 2026), contrastado con docs/SRS.md sin modificarlo. Base: main c2deae0, más los cambios de preparación.

**Validado local** significa implementado y respaldado por pruebas del entorno local o evidencia de la revisión anterior conservada. No significa producción certificada. **Parcial** conserva una parte pendiente. **Pendiente externo** necesita infraestructura, cuentas o equipos. No se declara 100% del SRS.

| ID | Requisito | Estado | Resultado y límite | Evidencia |
|---|---|---|---|---|
| RF-01 | Usuarios y roles | Validado local | Crear, modificar, desactivar, restablecer contraseña y aplicar permisos. | UsuariosController; AuthService; suite de seguridad |
| RF-02 | Empleados | Validado local | Código, identidad, departamento, cargo, contacto y estado. Acceso restringido a datos personales. | EmpleadosController; recorrido web previo y regresión |
| RF-03 | Vehículos | Validado local | Placa, ficha, marca, modelo, año, tipo, departamento, capacidad, odómetro y estado. | VehiculosController; regresión y web |
| RF-04 | Departamentos | Validado local | Altas, edición y asociación con empleados/vehículos. | DepartamentosController; validaciones relacionales |
| RF-05 | Solicitudes | Validado local | Manual, automática y recurrente; datos y aprobación con coherencia de departamento. | SolicitudesController; SolicitudesRecurrentesControllerTests |
| RF-06 | Emisión de tickets | Validado local | UUID, secuencia, vigencia, empleado, vehículo, cantidad, PDF y QR. Correo externo se evalúa en RF-09. | TicketService; PDF y suite de tickets |
| RF-07 | QR seguro | Validado local | Firma, hash, autorización íntegra, vigencia y bloqueo de reutilización. | TicketQrService; pruebas criptográficas y concurrencia |
| RF-08 | Numeración | Validado local | Prefijo configurable, secuencia, reinicio anual opcional y restricción de duplicados. | TicketSequenceService/TicketService; pruebas PostgreSQL |
| RF-09 | Envío por correo y SMS | Parcial | SMTP/PDF y enlaces seguros verificados localmente. Textbee adaptado y probado con HTTP controlado; falta recepción SMS y correo real en entorno objetivo. | NotificationTransportTests; TextbeeTransportTests; docs/29-TEXTBEE.md |
| RF-10 | Estados de tickets | Validado local | Creado, enviado, pendiente, próximo a vencer, vencido, consumido y anulado; estado efectivo según vigencia. | TicketService; TicketsPage; tests de estados |
| RF-11 | Asignaciones | Validado local | Manuales y programadas. Nuevo modo histórico: promedio por despacho del vehículo/combustible en 90 días, acotado por límite y capacidad. Sin historial no genera. Siempre requiere aprobación. Regla a ratificar con el cliente. | SolicitudRecurrenteService; pruebas históricas; HIST-UI |
| RF-12 | Despacho | Validado local | QR válido, identidad, fecha/hora, galones, operador, estación y observaciones; transacción y consumo único. Lector físico pendiente. | DispatchService; integración PostgreSQL; E2E móvil y despacho nativo de 1 galón en emulador |
| RF-13 | Aplicación móvil | Parcial | Login local, cámara virtual, QR manual firmado, despacho y bloqueo de reutilización en emulador; APK y 35 pruebas. Falta completar el flujo con cámara física y Keycloak nativo en el despliegue objetivo. | mobile/lib; mobile-build.log; evidencia emulador |
| RF-14 | Inventario | Validado local | Recepciones/compras, despachos, mermas, ajustes y transferencias; bloqueos de sobrecapacidad e incompatibilidad. | InventarioController; RecepcionesController; suites de negocio |
| RF-15 | Inventario actualizado | Validado local | Persistencia inmediata del despacho y consultas de existencia, disponibilidad, consumo y nivel crítico. Web actualiza por consulta/refresco periódico; no se promete push instantáneo entre pantallas. | InventarioController; DashboardService; transacciones |
| RF-16 | Recepción | Validado local | Proveedor/RNC, factura, volumen, fecha y tanque con impacto en existencias. | RecepcionesController; pruebas de capacidad y entidades activas |
| RF-17 | Movimientos | Validado local | Historial de entradas, salidas, ajustes y transferencias. | InventarioController; suites de movimientos |
| RF-18 | Cierre diario | Validado local | Despachos, volumen, existencia final, diferencias y acta PDF. | CierreDiarioServiceTests; guía manual |
| RF-19 | Reportes | Validado local | Filtros por fecha, empleado, vehículo, departamento, combustible y estado según el tipo de reporte. | ReporteServiceTests; ReportesPage |
| RF-20 | Exportación | Validado local | Excel, CSV y PDF mantienen los filtros. | ReporteServiceTests; pruebas web previas |
| RF-21 | Trazabilidad | Validado local | Eventos de creación, modificación, despacho, ajustes, anulación y acceso. Se añade auditoría de plantillas y solicitudes automáticas. Jobs identificados como PROGRAMADOR, sin IP de usuario ficticia. | AuditService; controladores; SolicitudRecurrenteService |
| RF-22 | Dashboard | Validado local | Inventario, despachos, tickets activos/vencidos y consumo por departamento/vehículo. Se corrige distribución de tarjetas en escritorio. | DashboardServiceTests; DashboardPage; revisión visual |
| RF-23 | Notificaciones | Parcial | Reglas de próximo vencimiento, vencido, inventario bajo, fallo de integración y ajustes. Entrega externa requiere proveedor/destinatarios y validación real. | NotificationRuleService; suite F9 |
| RF-24 | API REST | Validado local | Servicios de tickets, inventario, despacho y reportes, con permisos y validaciones comunes. | 463 pruebas backend incluidas integración real y Keycloak |
| RS-01 | Autenticación y sesiones | Validado local | Usuario/contraseña, renovación, revocación y Keycloak. MFA es opcional en el SRS; no se presenta como probado. | Suite Auth/Keycloak sin omitidas |
| RS-02 | Autorización RBAC | Validado local | Roles y propiedad de recursos; revisión de ocho perfiles y 19 rutas. | Suite seguridad; matriz de navegador |
| RS-03 | TLS 1.3 y AES-256 | Parcial | TLS 1.3 verificado en la demo pública (TLS_AES_256_GCM_SHA384). Falta acreditar AES-256 en reposo para BD y copias. | Demo Render/Neon; comprobación TLS 22/09/2026 |
| RS-04 | Seguridad QR | Validado local | ECDSA P-256, SHA-256 y token de validación. | TicketQrService; pruebas de firma y manipulación |
| RS-05 | OAuth 2.0 y JWT | Validado local | Keycloak/PKCE y validación de tokens del servidor probados. Flujo nativo Android de producción permanece en RF-13. | Suite Keycloak y seguridad 22/09/2026 |
| RS-06 | Auditoría inalterable | Validado local | Protección append-only en PostgreSQL y eventos del negocio. No equivale a impedir acciones de un superusuario de infraestructura. | Migración ProtectAuditAppendOnly; pruebas PostgreSQL |
| ARQ-WEB | Web React | Validado local | React, JavaScript/JSX, Tailwind y Vite. | frontend/package.json |
| ARQ-API | Backend .NET | Desviación documentada | .NET 10 en vez de .NET 8. Decisión del equipo documentada; ratificar con el profesor/cliente. | docs/16-DECISION-NET10.md |
| ARQ-BD | Base de datos | Validado local | PostgreSQL y Entity Framework Core. | AppDbContext; suite con PostgreSQL 16 |
| ARQ-PWA | Web instalable | Validado local | Manifest, iconos y service worker de aviso sin conexión. No cachea API ni tickets, no despacha offline. Instalación final depende de HTTPS y navegador compatible. | frontend/public/manifest.webmanifest y sw.js; prueba navegador |
| ARQ-ANDROID | Android y lectores | Parcial | Flutter y APK QA ejecutado en emulador. Pendientes lector físico opcional, cámara real y firma/distribución productiva. | mobile; evidencia Android |
| OBJ-24H | Disponibilidad 24/7 | Pendiente externo | Requiere hosting, supervisión, recuperación y medición. La ejecución local no acredita 24/7; Render Free tampoco lo garantiza por su suspensión automática. | Sin infraestructura productiva validada |
| CA-01 | Tickets únicos | Validado local | Sin duplicidad y pruebas concurrentes. | Suite PostgreSQL de tickets |
| CA-02 | Solo QR válido | Validado local | Rechaza identificadores sin firma, alteración, expiración y reuso. | TicketQr/Dispatch tests |
| CA-03 | Inventario actualizado | Validado local | Despacho y saldo se confirman juntos; interfaz consulta datos vigentes. | Integración real PostgreSQL |
| CA-04 | Trazabilidad | Validado local | Operaciones auditadas, eventos nuevos del programador incluidos. | RF-21 y RS-06 |
| CA-05 | Reportes exportables | Validado local | Excel, CSV y PDF. | RF-20 |
| CA-06 | Móvil en producción | Pendiente externo | El emulador y APK debug no satisfacen la operación productiva exigida. | Pendiente piloto físico y despliegue |
| CA-07 | Seguridad establecida | Parcial | Controles de aplicación probados; RS-03 necesita verificación en infraestructura real. | RS-01 a RS-06 |

## Condiciones para el cierre final

1. Vincular un Android físico con SIM a Textbee, configurar claves privadas y URL HTTPS pública, y confirmar recepción y descarga real.
2. Completar validación de correo real en el entorno final, con destinos propios del equipo.
3. Ensayar cámara real, conectividad y login nativo Keycloak. Preparar APK firmado para distribución.
4. Verificar TLS 1.3, AES-256 en reposo, copias, restauración, monitoreo y disponibilidad. Asignar responsables operativos.
5. Ratificar con el cliente la decisión .NET 10 y la regla del promedio histórico de 90 días. Registrar aceptación con evidencias.

## Evidencia y repetición

Servidor: `bash backend/scripts/run-full-integration-tests.sh`. Web: `npm test`, `npm run lint`, `npm run build`. Móvil: `flutter analyze`, `flutter test`, APK QA. La nueva migración `AddHistoricalRecurringRequests` añade un booleano con valor false, conservando las plantillas existentes en modo fijo.

La guía de exposición y ensayo está en [GUION-DEMO.md](Guion-demostracion.md). Los detalles de pruebas anteriores permanecen en verificación de integración del repositorio (docs/qa/06-VERIFICACION-INTEGRACION.md), diferenciados de la ejecución actual.
