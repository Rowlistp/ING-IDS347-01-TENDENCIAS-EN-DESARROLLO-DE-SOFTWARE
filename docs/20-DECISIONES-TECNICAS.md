# 20 - Índice de Decisiones Técnicas

## Decisiones F9

MailKit4.17.0 SMTP/Mailpit1.30.0 local; SMS sin proveedor elegido, adapter HTTP
configurable y gate de credenciales. Cola existente, UNIQUE determinista,
SKIP LOCKED, lease/ReservaId, timeout y transacciones cortas. Externo at-least-once;
Message-Id/Idempotency-Key mitigan, no garantizan exactamente-once. Reglas separadas,
fallo→alerta interna sin recursión. Link independiente256 bits, solo hash,
expira con Ticket; migración aborta duplicados históricos sin borrar.
Guía PostgreSQL aplicada a índices y bloqueos. [Operación](../infra/notifications/README.md).

## 1. Propósito

Resumir decisiones vigentes y pendientes sin reemplazar el SRS ni duplicar el
detalle de documentos especializados. La decisión de plataforma se desarrolla
en `16-DECISION-NET10.md`.

## 2. Decisiones aprobadas

| Área | Decisión | Evidencia/Referencia |
|---|---|---|
| Backend | .NET 10 Web API | `16-DECISION-NET10.md`, proyectos `net10.0` |
| ORM | Entity Framework Core | Backend y migraciones existentes |
| Base de datos | PostgreSQL | `AppDbContext`, Npgsql y pruebas reales |
| Web | React + Tailwind CSS | Arquitectura y división del equipo |
| Móvil | Flutter | SRS y arquitectura seleccionada |
| Autenticación local | JWT interno independiente | Fase 1 y pruebas del pipeline JWT |
| OAuth2/OIDC | Keycloak 26.7.3 | `infra/keycloak/`, configuración API y CI |
| Flujo OIDC | Authorization Code + PKCE S256 | Realm importable y pruebas reales |
| Clientes OIDC | Web/móvil públicos, sin client secret | `fueltrack-web`, `fueltrack-mobile` |
| Flujos rechazados | Implicit Flow y password/direct access grants deshabilitados | Realm y pruebas Keycloak |
| Audiencia | `fueltrack-api` | Realm, opciones tipadas y pruebas de audience |
| Identidad externa | Se vincula con un usuario local activo | Resolución por `preferred_username` |
| Autorización | Roles de negocio locales en PostgreSQL | RBAC de Fase 1 |
| Roles externos | No elevan privilegios FuelTrack | Pipeline OIDC y prueba negativa |
| Contraseñas | PBKDF2-HMAC-SHA-512 y política fuerte | `07-SEGURIDAD.md` y tests |
| Sesiones | Refresh tokens rotatorios, hash y revocación | Servicios y tests de concurrencia |
| Auditoría | Escritura transaccional y append-only PostgreSQL | `IntegratePhase1Security` y pruebas PostgreSQL |
| API administrativa | Contratos en español | `/api/v1/usuarios` y `/{id}/estado` |
| CI | GitHub Actions para backend/security | `.github/workflows/backend-security.yml` |
| Ticket desde Solicitud | El cliente envía solo `SolicitudId`; el servidor copia los datos aprobados | `TicketService` |
| Numeración Ticket | Secuencia global PostgreSQL y código `PREFIJO-AÑO-000001` | `ticket_numero_seq` |
| Multiplicidad | Histórico 1:N, máximo un Ticket no terminal por Solicitud | Índice parcial único |
| Criptografía QR | ECDSA P-256 + SHA-256 + token aleatorio de 256 bits | `TicketQrService` |
| Persistencia QR | Hashes/firma y PNG; no columna con token en claro | Modelo `Ticket` |
| PDF | QuestPDF 2026.8.0 bajo licencia Community para uso académico | `TicketPdfService` |
| Generación QR | QRCoder 1.8.0, licencia MIT | `TicketQrService` |
| Envío F4/F9 | F4 crea cola idempotente y Ticket `Pendiente`; F9 confirma transporte y `Enviado` | `PrepareSendAsync`, bloqueo por Ticket PostgreSQL |
| Ownership F4 | Solicitante lee solo sus Tickets/PDF; recurso ajeno responde `404` | Filtro `Empleado.UsuarioId` en SQL y tests HTTP |

## 3. Decisiones pendientes

### Decisiones adicionales F5

- Flutter 3.47.2 / Dart 3.13.2 fijados en CI; paquetes exactos en pubspec.lock.
- Dio, Riverpod, AppAuth, Secure Storage, mobile_scanner; Android como plataforma F5.
- Secure Storage 10.3.1: versión11 exigía android-37 incompatible con el target
  disponible local. APK verificado con 10.3.1 y compile SDK de Flutter.
- TanqueId obligatorio RESTRICT, Estación independiente, sin inventar asignación
  histórica. La migración aborta sin alterar datos si existen despachos previos:
  exige mapeo de tanques revisado explícitamente.
- Movimiento Salida negativo; ExistenciaActual/Disponibilidad son fuente de saldo,
  NivelActual no se toca. Despacho conserva instantáneas de ambos saldos.
- Locks PostgreSQL y UNIQUE TicketId; xmin protege inventario/anulaciones obsoletos
  sin reescribir controladores de Builder1.
- Refresh single-flight y un replay tras401. Timeout POST exige reconciliación;
  no hay cola offline. Auth/me expone roles locales para RBAC visual.

| Área | Decisión pendiente | Motivo |
|---|---|---|
| Infraestructura | Proveedor cloud/on-premise y topología | El SRS no selecciona proveedor |
| SMTP | Proveedor y credenciales operativas | Fase 9 |
| SMS | Gateway, costos y SLA | Fase 9 |
| Rotación de firma QR | Procedimiento productivo de rotación y confianza multiclave | Infraestructura/despliegue |
| PDF | Retención y almacenamiento externo a largo plazo | Requiere política operativa |
| Observabilidad | Logs centralizados, métricas, tracing y alertas | Estabilización |
| Disponibilidad | SLA, RPO y RTO | Requiere acuerdo de infraestructura |
| Backups | Frecuencia, retención, cifrado y restauración | Infraestructura productiva |

## 4. Límites

- MFA es opcional en el SRS y sigue diferido; no se presenta como deuda obligatoria.
- TLS 1.3 y AES-256 en reposo requieren evidencia de infraestructura productiva.
- F5 fusionada PR#9 con gate físico pendiente; F9 implementa SMTP/SMS,
  proveedor/credenciales productivos siguen como gate de despliegue.
- Ninguna credencial productiva debe versionarse. Los valores de
  `infra/keycloak` son fixtures reproducibles de testing.
