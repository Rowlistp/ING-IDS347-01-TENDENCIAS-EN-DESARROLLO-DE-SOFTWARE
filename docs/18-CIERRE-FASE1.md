# Cierre de Fase 1

## Estado

`FASE 1: READY FOR PR`

## Objetivo

Cerrar y validar la seguridad y administración base de FuelTrack sin anticipar
Tickets/QR, despacho móvil ni notificaciones de fases posteriores.

## Alcance terminado

- Autenticación local.
- OAuth 2.0/OIDC Keycloak.
- JWT y sesiones revocables.
- RBAC, usuarios y catálogo cerrado de roles.
- Auditoría consultable y append-only.
- Hardening de contraseñas, tokens, concurrencia y último administrador.

## Arquitectura de autenticación

- Autenticación local con usuario/contraseña y JWT interno independiente.
- Sesiones mediante refresh tokens almacenados como hash, rotatorios y revocables.
- OAuth2/OIDC con Keycloak 26.7.3, realm `fueltrack` y audiencia `fueltrack-api`.
- Authorization Code + PKCE S256 para `fueltrack-web` y `fueltrack-mobile`, ambos
  clientes públicos sin client secret.
- Implicit Flow y password/direct access grants deshabilitados.
- Identidad Keycloak vinculada con un usuario local activo; autorización de
  negocio exclusivamente mediante roles locales de PostgreSQL.

## Requisitos

| Requisito | Estado de Fase 1 |
|---|---|
| RF-01 | Implementado para usuarios, autenticación y roles |
| RS-01 | Implementado para usuario/contraseña y gestión de sesiones. MFA es opcional según el SRS y queda diferido |
| RS-02 | Implementado: RBAC evaluado en backend con roles locales |
| RS-03 | Pendiente de infraestructura/despliegue productivo: TLS 1.3 en tránsito y AES-256 en reposo |
| RS-04 | No pertenece a Fase 1. El QR seguro se implementará en Fase 4 |
| RS-05 | Implementado: OAuth 2.0/OIDC con Keycloak + JWT |
| RS-06 | Implementado para la base de auditoría: transaccional, consultable y append-only en PostgreSQL. Retención operativa diferida |

## Evidencias

- Gestión de usuarios: crear, modificar, activar/desactivar, asignar roles y
  restablecer contraseñas, con protección del último Administrador activo.
- Catálogo cerrado de seis roles y `GET /api/v1/roles` solo para Administrador.
- Contraseñas PBKDF2-HMAC-SHA-512, política fuerte y validación defensiva de hashes.
- Tests de JWT, sesiones, refresh rotation, revocación, 401/403 y pipeline HTTP.
- PostgreSQL real: migraciones desde cero, restricciones, concurrencia y trigger append-only.
- Keycloak real `26.7.3`: metadata, Authorization Code + PKCE S256 y validación API.
- Auditoría transaccional, consulta segura y bloqueo PostgreSQL de update/delete.
- CI `Backend Security` ejecuta restore, build y la suite con PostgreSQL y Keycloak.
- Migraciones EF completas desde base vacía.
- Endpoints: auth, users, `GET /api/v1/roles` y auditoría paginada.

## Resultado final

- Build: 0 errores y 0 warnings en el gate de cierre.
- Tests: 59 aprobados, 0 fallidos y 0 omitidos en el gate completo.
- PostgreSQL y Keycloak/OIDC reales: aprobados.
- Alcance diferido documentado sin marcar requisitos futuros como terminados.

## Diferidos explícitos

- MFA opcional.
- TLS 1.3 productivo.
- AES-256 en reposo.
- QR seguro → Fase 4.
- SMTP/SMS → Fase 9.

## Gate

Antes de aprobar el PR, testers humanos deben verificar:

1. Login/logout local y revocación visible después de desactivar, cambiar roles o resetear contraseña.
2. Login web y móvil por Keycloak usando PKCE, sin secreto de cliente.
3. Un usuario Keycloak desconocido o localmente inactivo recibe `401`.
4. Un rol externo de Keycloak no eleva permisos; el rol local incorrecto recibe `403`.
5. Administrador consulta usuarios/roles; Auditor consulta auditoría sin datos sensibles.
6. No puede eliminarse el último administrador activo ni asignarse una lista de roles vacía/duplicada/desconocida.
7. Las migraciones se aplican a una base PostgreSQL vacía y la API inicia.

El gate automático exige build sin errores, suite completa verde, `git diff --check`,
CI remoto verde y ausencia de secretos reales en el diff.
