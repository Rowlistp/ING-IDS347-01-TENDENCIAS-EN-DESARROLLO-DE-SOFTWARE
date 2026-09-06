# Fase 1 — Seguridad y administración

## 1. Qué se construyó

La Fase 1 deja dos formas de autenticación detrás de una sola política de
seguridad del backend:

- login local con JWT interno y refresh token rotatorio;
- OAuth 2.0/OIDC con Keycloak 26.7.3 mediante Authorization Code + PKCE S256.

En ambos casos, FuelTrack autoriza con los roles guardados en PostgreSQL. Un
rol recibido desde Keycloak no concede permisos por sí solo. La fase también
incluye administración de usuarios, el catálogo cerrado de seis roles,
protección del último Administrador activo y auditoría consultable e
inalterable.

## 2. Por qué se diseñó así

El esquema dual permite conservar el login propio del proyecto y, a la vez,
integrar un proveedor OIDC estándar. `SecurityVersion` hace posible invalidar
un access token antes de su expiración cuando cambian roles, contraseña o
estado del usuario. Los refresh tokens se guardan como SHA-256, se rotan una
sola vez y su actualización es atómica para impedir replay concurrente.

La autorización se mantiene local porque los roles corporativos de Keycloak y
los permisos de negocio de FuelTrack tienen responsabilidades distintas. La
auditoría comparte transacción con las operaciones sensibles y PostgreSQL
bloquea `UPDATE` y `DELETE` sobre `Auditorias`.

## 3. Archivos principales

- `Program.cs`: selección inteligente entre JWT interno y JWT Keycloak.
- `Security/PasswordService.cs`: PBKDF2-HMAC-SHA-512 y política 12–128.
- `Security/TokenService.cs`: JWT, refresh aleatorio y hashes.
- `Security/SessionValidationService.cs`: valida usuario activo y versión.
- `Services/AuthService.cs`: login, rotación, logout y reset.
- `Services/UserService.cs`: usuarios, roles, revocación y último admin.
- `Services/KeycloakIdentityService.cs`: identidad externa a usuario local.
- `Controllers/UsersController.cs`: contrato español `/api/v1/usuarios`.
- `Controllers/RolesController.cs` y `AuditController.cs`: consulta protegida.
- `Migrations/20260904133655_IntegratePhase1Security.cs`: cambio incremental
  sobre el modelo actual, sin revertir Solicitudes ni Inventario.
- `infra/keycloak/`: realm y contenedor reproducible sin secretos de clientes.
- `FuelTrack.Api.Tests/Integration/`: PostgreSQL, pipeline JWT y OIDC/PKCE.

## 4. Flujo resumido

En el login local, la API verifica el hash de contraseña, emite un JWT con
`security_version` y entrega un refresh token cuyo valor plano solo recibe el
cliente. Al renovar, el token anterior queda revocado y se genera otro.

En OIDC, el cliente público obtiene un authorization code usando PKCE, lo
intercambia con Keycloak y presenta el access token a FuelTrack. La API valida
firma, issuer, audience y expiración; después busca el usuario local activo y
carga exclusivamente sus roles PostgreSQL.

## 5. Preguntas que puede hacer el profesor

### ¿Por qué no basta con esperar que expire el JWT?

Porque un usuario desactivado o con permisos retirados conservaría acceso
durante ese tiempo. La comparación de `SecurityVersion` lo invalida de
inmediato.

### ¿Por qué el refresh token se guarda como hash?

Si la base de datos se filtra, el valor persistido no sirve para renovar una
sesión. El servidor compara hashes, igual que con una credencial opaca.

### ¿PKCE sustituye el client secret?

PKCE protege el authorization code de clientes públicos que no pueden guardar
un secreto, como React o Flutter. El verifier se crea por intento y nunca se
versiona.

### ¿Keycloak decide los permisos de FuelTrack?

No. Keycloak prueba identidad; FuelTrack consulta al usuario y sus roles
locales. Las pruebas demuestran que un rol externo no eleva privilegios.

### ¿Cómo se evita quedar sin administradores?

Antes de desactivar o retirar el rol del último Administrador se toma un
advisory transaction lock en PostgreSQL y se verifica que exista otro activo.

### ¿La auditoría puede editarse?

La aplicación solo inserta. Además, un trigger PostgreSQL rechaza `UPDATE` y
`DELETE`, incluso si alguien intenta modificar la tabla fuera del servicio.

## 6. Glosario

- **JWT:** token firmado con identidad, roles y vencimiento.
- **OIDC:** capa de identidad sobre OAuth 2.0.
- **PKCE:** prueba criptográfica que vincula el inicio y final del login.
- **Issuer:** servidor que emitió el token.
- **Audience:** API para la cual fue emitido.
- **RBAC:** autorización basada en roles.
- **PBKDF2:** derivación lenta de claves usada para hashes de contraseña.
- **Replay:** reutilización de una credencial que debía ser de un solo uso.
- **Append-only:** datos que se agregan, pero no se editan ni eliminan.

## 7. Conexión con otras fases

Fase 4 reutiliza esta autenticación, RBAC y auditoría para Tickets/QR. Fase 5
usará el cliente público `fueltrack-mobile` y PKCE. Cada módulo futuro debe
registrar sus eventos sin debilitar la tabla append-only.

## 8. Qué no se construyó

- MFA opcional.
- TLS 1.3 y cifrado de disco productivos, que dependen de infraestructura.
- Tickets, QR y PDF, correspondientes a Fase 4.
- Flutter y despacho, correspondientes a Fase 5.
- transporte real SMTP/SMS, correspondiente a Fase 9.

## 9. Evidencia de cierre

La integración definitiva partió del `main` con Solicitudes e Inventario y fue
validada con build Release sin errores ni advertencias. La corrida conjunta
aprobó 130 de 130 pruebas, sin fallos ni omisiones, contra PostgreSQL 16 y
Keycloak 26.7.3 reales.
