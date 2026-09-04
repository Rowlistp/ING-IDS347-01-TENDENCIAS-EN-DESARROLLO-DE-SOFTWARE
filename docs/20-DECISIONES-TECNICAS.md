# 20 - Índice de Decisiones Técnicas

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

## 3. Decisiones pendientes

| Área | Decisión pendiente | Motivo |
|---|---|---|
| Infraestructura | Proveedor cloud/on-premise y topología | El SRS no selecciona proveedor |
| SMTP | Proveedor y credenciales operativas | Fase 9 |
| SMS | Gateway, costos y SLA | Fase 9 |
| QR | Mecanismo definitivo de firma y gestión de claves | Fase 4 |
| PDF | Almacenamiento, retención y acceso | Requiere fases 4/7/8 |
| Observabilidad | Logs centralizados, métricas, tracing y alertas | Estabilización |
| Disponibilidad | SLA, RPO y RTO | Requiere acuerdo de infraestructura |
| Backups | Frecuencia, retención, cifrado y restauración | Infraestructura productiva |

## 4. Límites

- MFA es opcional en el SRS y sigue diferido; no se presenta como deuda obligatoria.
- TLS 1.3 y AES-256 en reposo requieren evidencia de infraestructura productiva.
- Tickets/QR, Flutter/despacho y SMTP/SMS permanecen en Fases 4, 5 y 9.
- Ninguna credencial productiva debe versionarse. Los valores de
  `infra/keycloak` son fixtures reproducibles de testing.
