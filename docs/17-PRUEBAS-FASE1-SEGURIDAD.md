# 17 - Pruebas de Fase 1: Seguridad

## Objetivo

Registrar las pruebas automatizadas incorporadas por Builder 2 para la Fase 1 de seguridad.

## Alcance actual

Las pruebas cubren:

- hashing y verificación de contraseñas;
- uso de salt diferente para la misma contraseña;
- rechazo de contraseñas incorrectas y hashes inválidos;
- generación de JWT con identidad y roles;
- generación aleatoria de refresh tokens;
- hash SHA-256 de refresh tokens antes de persistencia;
- catálogo de roles base;
- rechazo de login para usuarios desactivados;
- rotación de refresh token;
- rechazo de reutilización del refresh token anterior;
- política de contraseña fuerte;
- dos renovaciones simultáneas con un único ganador;
- reset de contraseña con revocación de sesiones y auditoría;
- `401` sin autenticación, `403` con rol incorrecto y acceso correcto como Administrador.
- versión de seguridad incluida en JWT;
- rechazo de access tokens de usuarios desactivados o con versión obsoleta;
- rechazo de espacios en contraseñas fuertes;
- límites de algoritmo, iteraciones, salt, hash y tamaño de entrada al verificar contraseñas;
- catálogo separado de roles `Consulta` y `Solicitante`;
- `GET /api/v1/audit` permitido para Administrador/Auditor, denegado para otros roles y sin `DatosRelevantes`;
- pipeline JWT real: token ausente o inválido `401`, rol incorrecto `403`, Administrador permitido;
- invalidación inmediata del access token tras desactivar el usuario;
- bloqueo de auto-retiro del rol Administrador y de desactivación del último administrador activo;
- revocación de refresh tokens al desactivar un usuario.
- catálogo `GET /api/v1/roles` filtrado y protegido para Administrador;
- migración PostgreSQL append-only: inserta auditoría y rechaza update/delete;
- metadata OIDC real de Keycloak;
- Authorization Code + PKCE S256 real y audiencia `fueltrack-api`;
- rechazo de issuer/audience incorrectos y de identidades locales desconocidas/inactivas;
- roles locales positivos y ausencia de elevación mediante roles externos.

## Hardening de sesiones

La rotación de refresh token utiliza una actualización condicional atómica.

El token anterior solo puede ser reclamado si:

- existe;
- no fue revocado;
- no está vencido.

La actualización de revocación, creación del token reemplazo y auditoría se ejecutan dentro de una transacción de base de datos.

Si dos solicitudes intentan usar el mismo refresh token, solamente la que consiga actualizar la fila activa puede completar la rotación.

## Comandos

Desde `backend/`:

```powershell
dotnet restore .\FuelTrack.slnx
dotnet build .\FuelTrack.slnx --no-restore
dotnet test .\FuelTrack.slnx --no-build
```

### PostgreSQL real

Las pruebas marcadas `PostgreSQL` usan exclusivamente una base cuyo nombre
contenga `test`. Aplican todas las migraciones desde cero y destruyen esa base al
terminar. Nunca deben apuntar a desarrollo o producción.

Desde la raíz del repositorio:

```bash
./backend/scripts/run-postgres-security-tests.sh
```

El script crea un contenedor efímero `postgres:16-alpine` con contraseña aleatoria,
ejecuta las pruebas y elimina el contenedor mediante `trap` incluso ante fallo.
Se verifican:

- tabla, FK e índices de `RefreshTokens`;
- índice único de `Roles.Nombre`;
- seed idempotente bajo concurrencia;
- login, refresh, rotación y rechazo de reutilización;
- revocación del refresh token tras desactivar al usuario.
- trigger append-only de `Auditorias`.

### Keycloak real

```bash
docker compose -f infra/keycloak/compose.yml up -d
FUELTRACK_KEYCLOAK_URL=http://localhost:18080 dotnet test backend/FuelTrack.slnx
```

La suite usa Keycloak `26.7.3`, navega el flujo Authorization Code, inicia sesión
en el formulario real, intercambia el code con PKCE S256 y presenta el access
token a la API. CI levanta la misma imagen fijada y el mismo realm importable.

## Resultado de la integración definitiva

La integración sobre `main` fue validada el 4 de septiembre de 2026 con .NET
SDK `10.0.111`, PostgreSQL `16-alpine` y Keycloak `26.7.3`:

- build Release: 0 errores y 0 advertencias;
- suite conjunta: 130 aprobadas, 0 fallidas y 0 omitidas;
- PostgreSQL real: 7 pruebas, incluidas migraciones desde cero, trigger
  append-only y concurrencia;
- Keycloak/OIDC real: 8 pruebas, incluido Authorization Code + PKCE S256;
- regresión local y módulos actuales de Builder 1: 115 pruebas aprobadas.

La migración de integración es
`20260904133655_IntegratePhase1Security`: parte del snapshot actual de `main` y
solo agrega `Usuarios.SecurityVersion`, la unicidad de `Roles.Nombre` y la
protección append-only de `Auditorias`.

## Criterio antes del PR

La Fase 1 no debe enviarse a `main` si:

- la solución no compila;
- existe una prueba fallida;
- `git diff --check` reporta errores;
- la rama no parte del `main` vigente o requiere una integración adicional.

## Decisiones pendientes

MFA opcional, TLS 1.3 productivo y AES-256 en reposo dependen de infraestructura
posterior. JWT local y Keycloak/OIDC son mecanismos activos de Fase 1.
