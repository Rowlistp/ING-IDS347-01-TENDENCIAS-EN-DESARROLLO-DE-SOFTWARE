# Pruebas de Fase 4 — Tickets y QR

## Objetivo

Demostrar con pruebas automatizadas que la emisión, consulta, validación,
anulación, preparación de envío y generación de PDF de un Ticket respetan el
contrato, el RBAC y las reglas criptográficas de la Fase 4 sin romper la Fase 1
ni los módulos de Builder 1.

## Cobertura implementada

| Área | Evidencia automatizada |
|---|---|
| Emisión | Solicitud aprobada, copia de datos autoritativos, inexistente, estados no aprobados, cantidad/vencimiento inválidos y relaciones inconsistentes |
| Unicidad | UUID y secuencia únicos; máximo un Ticket utilizable por Solicitud |
| Concurrencia | 24 emisiones paralelas para Solicitudes distintas y carrera sobre una misma Solicitud en PostgreSQL real |
| QR | Payload auténtico y rechazo de UUID, empleado, vehículo, departamento, combustible, cantidad, fechas, token, hash o firma alterados |
| Criptografía | SHA-256, ECDSA P-256, token aleatorio de 256 bits y verificación usando solo la clave pública |
| Estados | Estado efectivo por fecha y rechazo de vencido, consumido o anulado |
| Anulación | Operación válida, auditable e idempotente; consumido/vencido no anulable |
| Envío | Creación idempotente de `Notificacion` pendiente sin invocar SMTP ni SMS |
| PDF | Respuesta PDF no vacía, datos básicos, mismo QR persistido y auditoría |
| API/RBAC | `401`, `403`, roles operativos y ciclo HTTP completo |
| Persistencia | Migraciones desde cero, secuencia, columnas e índice parcial único en PostgreSQL 16 |
| Regresión F1 | JWT, refresh, auditoría append-only, Keycloak 26.7.3 y Authorization Code + PKCE S256 |

## Archivos principales de prueba

- `backend/FuelTrack.Api.Tests/Services/TicketServiceTests.cs`
- `backend/FuelTrack.Api.Tests/Integration/SecurityJwtPipelineTests.cs`
- `backend/FuelTrack.Api.Tests/Integration/PostgreSqlSecurityTests.cs`
- `backend/FuelTrack.Api.Tests/Integration/KeycloakOidcTests.cs`

## Gate reproducible

Con PostgreSQL de pruebas y el realm reproducible de Keycloak disponibles:

```bash
dotnet restore backend/FuelTrack.slnx
dotnet build backend/FuelTrack.slnx --no-restore --configuration Release
dotnet test backend/FuelTrack.slnx --no-build --configuration Release \
  --logger "console;verbosity=minimal"
git diff --check
```

Variables requeridas para activar las integraciones reales:

```text
FUELTRACK_TEST_CONNECTION=<base PostgreSQL exclusiva de pruebas>
FUELTRACK_KEYCLOAK_URL=http://localhost:18080
```

El fixture de Keycloak es `infra/keycloak/fueltrack-realm.json`. Las pruebas de
firma generan claves ECDSA efímeras; ninguna clave privada real se versiona.

## Resultado del cierre — 4 de septiembre de 2026

| Comprobación | Resultado |
|---|---|
| Restore | Aprobado |
| Build Release | Aprobado: 0 errores, 0 advertencias |
| Suite sin infraestructura | 156 aprobadas, 0 fallidas; 8 omitidas porque Keycloak no estaba configurado en esa corrida local |
| PostgreSQL 16 aislado | 10 pruebas de integración aprobadas |
| Keycloak 26.7.3 aislado | 8 pruebas OIDC/PKCE aprobadas |
| Gate combinado PostgreSQL + Keycloak | **174 aprobadas, 0 fallidas, 0 omitidas** |
| Modelo frente a última migración | Sin cambios pendientes |
| `git diff --check` | Aprobado |

Los conteos aislados describen subconjuntos de la misma suite y no deben
sumarse al total combinado.

## Criterio de aceptación manual previo al merge

1. Emitir desde una Solicitud aprobada y comprobar el código visible.
2. Descargar el PDF y escanear su QR contra `POST /api/v1/tickets/validar`.
3. Alterar el payload y comprobar una respuesta inválida sin detalles internos.
4. Anular el Ticket y confirmar que el QR deja de ser utilizable.
5. Preparar el envío y confirmar una notificación `PENDIENTE`, sin afirmar que
   fue entregada por correo o SMS.
6. Repetir acciones con Administrador, Supervisor, Despachador, Auditor,
   Consulta y Solicitante para confirmar la matriz RBAC.
