# Integraciones F9 — ejecución segura

Las integraciones están deshabilitadas por defecto. Nunca activar destinos o
credenciales productivas en CI. La plantilla `.env.example` contiene únicamente
placeholders; .NET no carga ese archivo automáticamente: exportar variables o
usar user-secrets/secret manager. No versionar `.env`.

## Prueba completa reproducible

Desde la raíz, con Docker y .NET 10:

```sh
bash backend/scripts/run-full-integration-tests.sh
```

Levanta PostgreSQL 16, Keycloak 26.7.3 y Mailpit 1.30.0 con puertos loopback
efímeros; los tests crean gateways SMS HTTP locales reales. Al terminar elimina
solo esos contenedores. No usar FUELTRACK_TEST_CONNECTION con datos operativos.
El script PostgreSQL-only excluye explícitamente NotificationTransport; para
acreditar F9 ejecutar siempre la suite completa.

Mailpit manual opcional: `docker compose -f infra/notifications/compose.yml up -d`.
SMTP localhost:1025; buzón http://localhost:8025. No tiene relay externo.
Para el API local configurar Notifications__Smtp__Enabled=true, Host=127.0.0.1,
Port=1025, StartTls=false, FromAddress=fueltrack@example.test,
Notifications__AllowInsecureLocalTransport=true y ASPNETCORE_ENVIRONMENT=Development.
Mantener SMS deshabilitado salvo gateway local expresamente configurado.
Activar Notifications__WorkerEnabled=true solo después de migrar y verificar la cola.

## SMTP productivo

MailKit 4.17.0, TLS obligatorio: StartTls=true (normalmente587) o UseSsl=true
(normalmente465), no ambos. Validación normal de certificados, sin bypass.
Username y password si el servidor requiere autenticación. Password exclusivamente
Notifications__Smtp__Password/env, user-secrets o secret manager. No protocol logger.
Aceptar el mensaje SMTP equivale a ENVIADA, no acredita entrega al inbox final.
El PDF se genera con el renderer F4 y el mismo PNG QR persistido; no nueva firma.

## Contrato del gateway SMS genérico

No se eligió Twilio/Vonage/SNS. BaseUrl es la URL HTTPS completa del endpoint POST.
Sin redirects, credenciales en URI ni query. Header configurado AuthHeaderName
(por defecto Authorization), valor exacto de Notifications__Sms__ApiKey; incluir
el prefijo Bearer en el secreto si lo requiere el gateway.

```json
{"to":"+18095550101","message":"...","reference":"clave lógica","sender":"FuelTrack"}
```

Header Idempotency-Key: SHA-256 hexadecimal de la clave lógica. Acuse esperado:

```json
{"messageId":"opaque-provider-id"}
```

Acuse 2xx con messageId de1–128 caracteres alfanuméricos/guion/underscore.
JSON inválido/acuse ausente es resultado ambiguo reintentable, no éxito inventado.
Timeout, conexión,429,5xx reintentan;400/401/403 y otros rechazos definitivos fallan.
El gateway real debe implementar este contrato o aportar un adaptador revisado.
No existe evidencia de credenciales/proveedor SMS productivo:
`TRANSPORT IMPLEMENTED — REAL PROVIDER CREDENTIAL GATE PENDING`.

## Workers y operación

Opciones: BatchSize10 (máximo50), PollIntervalSeconds10, RuleIntervalSeconds60,
LockSeconds120, TransportTimeoutSeconds30, MaxAttempts4, BaseRetrySeconds60,
MaxRetrySeconds3600, TicketExpiringSoonHours24, TicketLinkHours48,
LowInventoryPeriodHours24. ValidateOnStart exige lease mayor que el doble del timeout
más10 segundos. Canales deshabilitados permanecen PENDIENTE sin consumir intentos.
Destinatarios operativos: Operations:Emails/Phones, vacíos por defecto.

At-least-once con mitigación: un crash después de aceptación externa y antes de
ENVIADA puede duplicar mensaje. SMTP Message-Id estable no obliga a deduplicar;
Idempotency-Key SMS solo funciona si lo respeta el proveedor. No exactamente-once.
ReservaId impide que un worker viejo complete una reserva recuperada. Los intentos
abandonados cuentan y el máximo termina en FALLIDA. Reintento manual conserva
IntentosTotales, reinicia Intentos y audita. Fallos generan alerta INTERNO visible
en la API, sin bucle de correos/SMS de error.

PublicBaseUrl es el origen/base público HTTPS, sin `/api/v1`; la app añade
`/api/v1/tickets/descargar/{token}`. Configurar proxy, trazas y access logs para
omitir/redactar esa ruta; la API suprime logs de framework que incluirían tokens.
Enlace bearer de256 bits, solo hash SHA-256 persistido, sin cache/referrer.
No compartirlo en logs, incidencias o capturas. Expira como máximo con el Ticket.

## Migración e historial

Aplicar AddPhase9NotificationsIntegration antes de activar workers. Preflight
detecta duplicados TICKET_EMITIDO por tipo/referencia/canal/destinatario; aborta
sin borrar filas. Diagnóstico no destructivo (solo IDs, no destinatarios):

```sql
SELECT array_agg("Id" ORDER BY "Id") AS ids
FROM "Notificaciones"
WHERE "Tipo" = 'TICKET_EMITIDO' AND "ReferenciaEvento" IS NOT NULL
GROUP BY "Tipo", "ReferenciaEvento", "Canal", btrim("Destinatario")
HAVING count(*) > 1;
```

Operaciones debe revisar historial/aceptación de proveedor y aprobar un mapeo
explícito antes de migrar. No automatizar borrado ni reenviar filas ambiguas.
Backfill de filas no duplicadas conserva IDs/estado/destino. NULL permitido solo
por compatibilidad histórica; todas las notificaciones nuevas F4/F9 tienen clave.

## Referencias de transporte

- [MailKit SmtpClient](https://mimekit.net/docs/html/T_MailKit_Net_Smtp_SmtpClient.htm): cliente SMTP empleado.
- [Mailpit API v1](https://mailpit.axllent.org/docs/api-v1/): inspección del correo recibido en las pruebas.
- [Mailpit releases](https://github.com/axllent/mailpit/releases): versión local fijada en 1.30.0.
