# Fase 5 — Móvil y despacho explicado al equipo

## 1. Qué se construyó

App Android: escanear ticket, comprobar empleado/vehículo y registrar combustible
servido. API descuenta existencias y deja operación auditable. Escanear no consume.

## 2. Por qué

El teléfono puede repetir peticiones o perder conexión. Por eso no decide si
el ticket sigue válido ni calcula saldo definitivo. El servidor decide en una
transacción: se guardan todas las piezas o ninguna.

## 3. Recorrido archivo por archivo

Rutas API y Tests relativas a backend/FuelTrack.Api y backend/FuelTrack.Api.Tests.

| Archivo | Responsabilidad |
|---|---|
| API/Models/Despacho.cs | Conserva ticket/operador/estación; añade tanque/saldos |
| API/Data/AppDbContext.cs | FK, precisión, concurrencia xmin |
| API/Migrations/20260906195749_AddPhase5DispatchIntegration.cs | Evolución sin inventar tanques históricos |
| API/DTOs/Dispatch/* | Entrada permitida y respuesta calculada |
| API/Services/DispatchService.cs | Revalidación, locks, consumo, stock, movimiento, auditoría |
| API/Controllers/DespachosController.cs | Rutas/autorización sin negocio duplicado |
| API/Controllers/EstacionesController.cs | Catálogo activo de lectura |
| API/Controllers/AuthController.cs | me expone roles locales F1 |
| API/Program.cs | DI y conflicto409 |
| Tests/Integration/PostgreSqlDispatchTests.cs | BD real, carreras y rollback |
| Tests/Integration/SecurityJwtPipelineTests.cs | Recorrido HTTP/permisos |
| Tests/Integration/KeycloakOidcTests.cs | PKCE móvil real |
| mobile/lib/core/config.dart | URLs/cliente/callback/HTTPS |
| mobile/lib/core/auth.dart | AppAuth, tokens seguros, refresh/logout |
| mobile/lib/core/api.dart | Dio, un replay401 |
| mobile/lib/core/models.dart, errors.dart | JSON, validadores, errores seguros |
| mobile/lib/app/providers.dart | Dependencias Riverpod reemplazables en tests |
| mobile/lib/app/app.dart | Login, inicio, consulta, validación, formulario/resultado |
| mobile/lib/features/scanner.dart | Cámara QR y bloqueo captura repetida |
| mobile/test/* | Unitarios/widgets sin cámara |
| mobile/e2e/api_e2e_test.dart + FuelTrack.MobileHarness | Widgets/API/BD con fixture efímero |
| .github/workflows/mobile.yml | Analyze/tests/E2E/APK/artefacto |

## 4. Preguntas del profesor

¿Por qué validar dos veces? Entre escaneo y confirmación otro dispositivo puede
usar el QR. La comprobación definitiva ocurre bloqueando el ticket en servidor.

¿Dos pedidos simultáneos? El primero bloquea Ticket; el segundo espera y luego
encuentra Consumido. UNIQUE refuerza esa regla. Tickets distintos esperan por
la misma fila de inventario para no servir más de lo que existe.

¿Por qué no NivelActual? F6 ya tiene la fuente autoritativa de saldo. Dos saldos
independientes facilitarían desajustes. ¿Si falla auditoría? Rollback incluso del
despacho insertado. El test provoca ese fallo y comprueba que no haya descuento.

¿Un parcial permite volver? No, consume todo el ticket por decisión de alcance.
¿Por qué no contraseña Keycloak en app? Autentica el navegador. PKCE vincula
el código con la app que inició el flujo; una app pública no protege secretos.
Los permisos reales siguen viniendo de roles locales PostgreSQL.

¿Timeout significa fallo? No: pudo confirmarse y perderse la respuesta. Se
consulta antes de reintentar. 401 es distinto: se intenta renovar una sola vez.

## 5. Términos clave

Transacción: cambios indivisibles. Rollback: deshacer cambios si falla algo.
Row lock: turno exclusivo sobre fila. UNIQUE: regla BD contra duplicados.
xmin: versión PostgreSQL que detecta actualizaciones obsoletas.
PKCE: vincula inicio OAuth/canje del código. RBAC: permisos por rol.
Online-first: confirmar exige servidor; no acumula despachos offline.

## 6. Cómo conecta F1/F4/F6

F1 reconoce usuario/roles; F4 emite/verifica QR; F5 confirma y escribe entidades
de inventario F6. Auditoría F1 comparte la transacción. Movimiento negativo
permite después cierres/reportes sin inventar otro sistema de stock.

## 7. Qué NO se construyó

SMTP/SMS(F9), cierre(F7), reportes(F8), cola offline ni firma Android productiva.
Mocks/E2E no sustituyen cámara física/login nativo: checklist tester obligatorio
antes del merge. No se modifica el SRS ni se reescriben controladores de Builder1.
