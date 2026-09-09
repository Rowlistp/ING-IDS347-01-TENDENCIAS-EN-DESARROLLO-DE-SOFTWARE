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
| RF-06 | Emisión de tickets digitales | 4 | Builder 2 + Builder 3 web | CU-08 | Tickets/PDF | Suite F4 servicio + HTTP | **Implementado/Validado backend** |
| RF-07 | QR seguro | 4 | Builder 2 | CU-09, CU-12 | Seguridad QR | Manipulación, firma, hash y token | **Implementado/Validado** |
| RF-08 | Numeración de tickets | 4 | Builder 2 | CU-08 | Secuencia/prefijo | PostgreSQL concurrente 24 tickets | **Implementado/Validado** |
| RF-09 | Envío de tickets | 4 y 9 | Builder 2 | CU-10 | Cola persistente, SMTP/PDF y SMS/link | Suite F4/F9, SMTP y HTTP reales | Implementado/validado integración; credenciales productivas pendientes |
| RF-10 | Consulta de estado y Tickets propios por Solicitante | 4 | Builder 2 + Builder 3 web | CU-11 | `/api/v1/tickets` | Suite F4 estado efectivo/RBAC/ownership/PDF | **Implementado/Validado backend** |
| RF-11 | Asignaciones manuales/automáticas | 3 | Builder 1 backend + Builder 3 web | CU-06, CU-07 | `POST /solicitudes/{id}/aprobar`, `POST /solicitudes/{id}/rechazar` | Suite de Solicitudes (PR #5) | Parcial: flujo manual validado; automatización/recurrentes y web pendientes |
| RF-12 | Despacho de combustible | 5 | Builder 2 | CU-12, CU-13 | DispatchService, `/api/v1/despachos` | HTTP/PostgreSQL: concurrencia, rollback, FK/UNIQUE | Implementado; evidencia en doc. 23 |
| RF-13 | Aplicación móvil para despacho | 5 | Builder 2 | CU-01, CU-12, CU-13 | Flutter Android OIDC/scanner/formulario | Unit/widget, E2E API/BD, analyze, APK | Integrado en main por PR #9; cámara/login nativo pendientes de tester físico |
| RF-14 | Control de inventario | 6 | Builder 1 | CU-14 a CU-16 | Inventario | Suite actual de Inventario | Backend implementado/validado |
| RF-15 | Inventario en tiempo real | 6 | Builder 1 | CU-16 | Inventario/consultas | Suite actual de Inventario | Backend implementado/validado |
| RF-16 | Recepción de combustible | 6 | Builder 1 | CU-14 | Recepciones | Suite actual de Recepciones | Backend implementado/validado |
| RF-17 | Movimientos de inventario | 6 | Builder 1 | CU-14, CU-15 | Movimientos | Suite actual de Movimientos | Backend implementado/validado |
| RF-18 | Cierre diario | 7 | Builder 1 | CU-17 | `POST /cierres-diarios`, `GET /cierres-diarios`, `GET /cierres-diarios/{id}`, `GET /cierres-diarios/{id}/pdf` | 8 tests unitarios MSTest + SQLite (PR #10) | **Implementado/Validado backend** |
| RF-19 | Reportes | 8 | Builder 1 + Builder 3 web | CU-18 | `GET /reportes?tipo={solicitudes\|despachos\|inventario\|cierres}` | 9 tests unitarios MSTest + SQLite (PR #10), REST F9 | Parcial: reportes base validados; filtros SRS y web pendientes |
| RF-20 | Exportación de reportes | 8 | Builder 1 + Builder 3 web | CU-19 | `GET /reportes/exportar?tipo=...&formato={csv\|excel\|pdf}` | Incluido en suite de Reportes (PR #10) | **Implementado/Validado backend**; web según Builder 3 |
| RF-21 | Trazabilidad | 1 y transversal | Builder 2 base; todos por módulo | CU-20 | `/api/v1/audit` + eventos | Suite Fase 1/append-only | Base implementada; cobertura futura por fase |
| RF-22 | Dashboard ejecutivo | 8 | Builder 1 API + Builder 3 web | CU-21 | `GET /dashboard/resumen` | 2 tests unitarios MSTest + SQLite (PR #10) | Parcial: resumen base validado; indicadores SRS y visualización web pendientes |
| RF-23 | Notificaciones | 9 | Builder 2 | CU-22 | Workers, cinco alertas, API RBAC/reintento | Doc. 25: PostgreSQL, concurrencia, reglas, transportes reales | Implementado/validado integración; despliegue y credenciales pendientes |
| RF-24 | API REST | Transversal | Todos; integración Builder 2 en Fase 9 | Todos | API .NET 10 | Login, solicitud/aprobación/emisión, envío, validación, despacho, stock y reporte (doc. 25) | Integración transversal validada; no acredita completar gaps RF-11/RF-19/RF-22 |

## 3. Requisitos de seguridad

| Requisito | Descripción | Fase | Responsable | Caso de uso | API/Módulo | Pruebas | Estado |
|---|---|---|---|---|---|---|---|
| RS-01 | Usuario/contraseña, MFA opcional y sesiones | 1 | Builder 2 | CU-01, CU-02 | Auth, usuarios, refresh tokens | Contraseña, login, rotación, revocación | **Implementado/Validado** salvo MFA opcional diferido |
| RS-02 | RBAC | 1 | Builder 2 | Transversal | Roles locales PostgreSQL | 401/403, catálogo, no elevación externa | **Implementado/Validado** |
| RS-03 | TLS 1.3 y AES-256 en reposo | 10/despliegue | Infraestructura por definir | Transversal | Plataforma productiva | Pendiente de entorno productivo | Pendiente por infraestructura |
| RS-04 | Firma, SHA-256 y token QR | 4 | Builder 2 | CU-09, CU-12 | ECDSA P-256 + QR | Suite criptográfica y PostgreSQL | **Implementado/Validado** |
| RS-05 | OAuth 2.0 + JWT | 1 | Builder 2 | CU-01 | Keycloak 26.7.3 + JWT interno | OIDC/PKCE real, issuer, audience | **Implementado/Validado** |
| RS-06 | Auditoría inalterable | 1 y transversal | Builder 2 base; todos por módulo | CU-20 | Auditoría PostgreSQL | Transacciones y trigger append-only | Base **Implementada/Validada**; eventos futuros por fase |

## 4. Notas de integración

- Base F9: main `db248b6`, con F1/F4/F5 (PR #9), F7/F8 (PR #10) y su documentación (PR #11).
- El merge de F5 no sustituye el gate físico Android pendiente. El rol Despachador
  en cierre diario y el frontend React requieren coordinación fuera de F9.
- SMTP/Mailpit y SMS HTTP local reales prueban transporte, no entrega productiva.
- `docs/SRS.md` permanece como fuente de descripción y alcance.
- Los estados “pendiente por fase” no implican ausencia de entidades preliminares.
- Las rutas vigentes de usuarios y catálogos están en español y son el contrato
  autoritativo para los consumidores web y móvil.
- RS-03 no se declara satisfecho por código de aplicación: requiere evidencia del
  despliegue productivo.
