# 19 - Matriz de Trazabilidad

## 1. Propósito

Relacionar los requisitos originales de `SRS.md` con fases, responsables,
casos de uso, módulos y evidencia. La matriz no cambia el SRS ni presume que la
existencia de una entidad o pantalla complete un requisito.

## 2. Requisitos funcionales

| Requisito | Descripción | Fase | Responsable | Caso de uso | API/Módulo | Pruebas | Estado |
|---|---|---|---|---|---|---|---|
| RF-01 | Gestión de usuarios | 1 | Builder 2 | CU-02 | `/api/v1/usuarios`, `/roles`, auth | Suite Fase 1 + PostgreSQL | **Implementado/Validado** |
| RF-02 | Gestión de empleados | 2 | Builder 1 + Builder 3 web | CU-03 | `/api/v1/empleados` | Gate propio de Fase 2 por confirmar | Implementado en backend; validación de fase pendiente |
| RF-03 | Gestión de vehículos | 2 | Builder 1 + Builder 3 web | CU-04 | `/api/v1/vehiculos` | Gate propio de Fase 2 por confirmar | Implementado en backend; validación de fase pendiente |
| RF-04 | Gestión de departamentos | 2 | Builder 1 + Builder 3 web | CU-05 | `/api/v1/departamentos` | Gate propio de Fase 2 por confirmar | Implementado en backend; validación de fase pendiente |
| RF-05 | Solicitudes de combustible | 3 | Builder 1 backend + Builder 3 web | CU-06, CU-07 | `/api/v1/solicitudes` | Suite actual de Solicitudes | Backend implementado/validado; web según Builder 3 |
| RF-06 | Emisión de tickets digitales | 4 | Builder 2 + Builder 3 web | CU-08 | Tickets/PDF | Pendiente | Pendiente por fase |
| RF-07 | QR seguro | 4 | Builder 2 | CU-09, CU-12 | Seguridad QR | Pendiente | Pendiente por fase |
| RF-08 | Numeración de tickets | 4 | Builder 2 | CU-08 | Secuencia/prefijo | Pendiente | Pendiente por fase |
| RF-09 | Envío de tickets | 4 y 9 | Builder 2 | CU-10 | Ticket + SMTP/SMS | Pendiente | Pendiente por fase |
| RF-10 | Consulta de estado | 4 | Builder 2 + Builder 3 web | CU-11 | Tickets | Pendiente | Pendiente por fase |
| RF-11 | Asignaciones manuales/automáticas | 3 | Builder 1 backend + Builder 3 web | CU-06, CU-07 | Solicitudes/reglas | Pendiente | Pendiente por fase |
| RF-12 | Despacho de combustible | 5 | Builder 2 | CU-12, CU-13 | Despacho móvil/API | Pendiente | Pendiente por fase |
| RF-13 | Aplicación móvil para despacho | 5 | Builder 2 | CU-01, CU-12, CU-13 | Flutter | Pendiente | Pendiente por fase |
| RF-14 | Control de inventario | 6 | Builder 1 | CU-14 a CU-16 | Inventario | Suite actual de Inventario | Backend implementado/validado |
| RF-15 | Inventario en tiempo real | 6 | Builder 1 | CU-16 | Inventario/consultas | Suite actual de Inventario | Backend implementado/validado |
| RF-16 | Recepción de combustible | 6 | Builder 1 | CU-14 | Recepciones | Suite actual de Recepciones | Backend implementado/validado |
| RF-17 | Movimientos de inventario | 6 | Builder 1 | CU-14, CU-15 | Movimientos | Suite actual de Movimientos | Backend implementado/validado |
| RF-18 | Cierre diario | 7 | Builder 1 | CU-17 | Cierres/PDF | Pendiente | Pendiente por fase |
| RF-19 | Reportes | 8 | Builder 1 + Builder 3 web | CU-18 | Reportes | Pendiente | Pendiente por fase |
| RF-20 | Exportación de reportes | 8 | Builder 1 + Builder 3 web | CU-19 | Excel/CSV/PDF | Pendiente | Pendiente por fase |
| RF-21 | Trazabilidad | 1 y transversal | Builder 2 base; todos por módulo | CU-20 | `/api/v1/audit` + eventos | Suite Fase 1/append-only | Base implementada; cobertura futura por fase |
| RF-22 | Dashboard ejecutivo | 8 | Builder 3 | CU-21 | Dashboard web | Pendiente | Pendiente por fase |
| RF-23 | Notificaciones | 9 | Builder 2 | CU-22 | Notificaciones | Pendiente | Pendiente por fase |
| RF-24 | API REST | Transversal | Todos; integración Builder 2 en Fase 9 | Todos | API .NET 10 | Seguridad API validada; resto pendiente | Parcial por fases |

## 3. Requisitos de seguridad

| Requisito | Descripción | Fase | Responsable | Caso de uso | API/Módulo | Pruebas | Estado |
|---|---|---|---|---|---|---|---|
| RS-01 | Usuario/contraseña, MFA opcional y sesiones | 1 | Builder 2 | CU-01, CU-02 | Auth, usuarios, refresh tokens | Contraseña, login, rotación, revocación | **Implementado/Validado** salvo MFA opcional diferido |
| RS-02 | RBAC | 1 | Builder 2 | Transversal | Roles locales PostgreSQL | 401/403, catálogo, no elevación externa | **Implementado/Validado** |
| RS-03 | TLS 1.3 y AES-256 en reposo | 10/despliegue | Infraestructura por definir | Transversal | Plataforma productiva | Pendiente de entorno productivo | Pendiente por infraestructura |
| RS-04 | Firma, SHA-256 y token QR | 4 | Builder 2 | CU-09, CU-12 | Tickets/QR | Pendiente | Pendiente por fase |
| RS-05 | OAuth 2.0 + JWT | 1 | Builder 2 | CU-01 | Keycloak 26.7.3 + JWT interno | OIDC/PKCE real, issuer, audience | **Implementado/Validado** |
| RS-06 | Auditoría inalterable | 1 y transversal | Builder 2 base; todos por módulo | CU-20 | Auditoría PostgreSQL | Transacciones y trigger append-only | Base **Implementada/Validada**; eventos futuros por fase |

## 4. Notas de integración

- `docs/SRS.md` permanece como fuente de descripción y alcance.
- Los estados “pendiente por fase” no implican ausencia de entidades preliminares.
- Las rutas vigentes de usuarios y catálogos están en español y son el contrato
  autoritativo para los consumidores web y móvil.
- RS-03 no se declara satisfecho por código de aplicación: requiere evidencia del
  despliegue productivo.
