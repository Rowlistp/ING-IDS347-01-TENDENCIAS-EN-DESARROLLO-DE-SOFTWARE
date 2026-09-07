# 25 — Pruebas de Fase 9

Base main verificada: db248b6170ad7b6c13a8c9dffafaa74de7a7fd19, 2026-09-07.
Baseline:235 pruebas aprobadas; F1/F4/F5 y F6/F7/F8 incluidas. F9:285 pruebas
aprobadas,0 fallidas,0 omitidas en la ejecución completa local. Build Release
sin errores; conserva dos NU1903 preexistentes de System.IO.Packaging8.0.0
en el proyecto de tests F7/F8 (ver cierre). No confundir warnings con fallos.

## Reproducción y cobertura

`bash backend/scripts/run-full-integration-tests.sh` con Docker/.NET10.
PostgreSQL16, Keycloak26.7.3, Mailpit1.30.0 y servidores SMS/TCP loopback efímeros.
Sin credenciales ni proveedores productivos. No basta ejecutar solamente SQLite.

| Área | Evidencia |
|---|---|
| Migraciones | Cadena completa; backfill conserva pendientes; duplicados abortan sin borrar; UNIQUE lógico |
| Outbox | Dos workers, mismo lote, un claim por fila; SKIP LOCKED no espera otra fila bloqueada |
| Recovery | Lease vencida recuperada con nueva ReservaId; completador obsoleto no modifica estado |
| Retry | Backoff acotado, máximo, intento histórico, fallo permanente, reintento manual/RBAC |
| Transición | EMAIL sola no marca Enviado; ambos canales sí; auditoría única bajo concurrencia; terminales no reviven |
| Atomicidad | Fallo de auditoría revierte finalización; no locks Ticket/Inventario durante HTTP externo |
| SMTP | MIME, destinatario, asunto/body, Message-Id estable, PDF; entrega TCP y lectura buzón Mailpit;451/550 reales sanitizados |
| SMS | HTTP real: headers/to/message/reference;2xx,429,500,400,401,403; timeout/conexión rechazada/acuse inválido |
| Fallo integración | Gateway500 hasta4 intentos: FALLIDA, alerta INTERNO, auditoría, Ticket Pendiente, sin recursión |
| Link |256 bits, solo hash, válido/incorrecto/expirado/revocado/consumido; FK rechaza Ticket inexistente; PDF y no-store |
| Logs | Captura ILogger sin token del enlace, API key ni cuerpo sensible de proveedor; auditoría sin token raw |
| Reglas |25h no alerta,23h sí;10 ejecuciones sin duplicar; expirados/terminales; stock101/100/50; Ajuste sí, otros movimientos no |
| RF24 | Login, solicitud HTTP/consulta/aprobación/emisión, cola, SMTP/SMS, Enviado, QR, despacho, inventario y reporte |

RF24 se divide dentro del mismo escenario: creación/aprobación/emisión HTTP y
la etapa transporte/dispatch usan un fixture firmado previo para disponer del
payload de escaneo sin exponerlo en una nueva API ni regenerar criptografía.
Mailpit confirma recepción y PDF adjunto; no se simula como entrega Gmail.

## Gates

Workflows Backend Security y Notifications Phase9 incluyen ramaF9 y main
(al integrar el workflow). Ambos ejecutan pruebas completas con SMTP local.
La ejecución CI del SHA final se enlaza en el informe de entrega; logs TRX en
artefacto phase9-test-results. Snapshot coincide con modelo EF.

Pendiente productivo SMTP/SMS: proveedor/credenciales/DNS/TLS/red/gateway real.
`TRANSPORT IMPLEMENTED — REAL PROVIDER CREDENTIAL GATE PENDING`.
Gate físico Android sigue pendiente; no se ejecutó ni modificó Flutter en F9.
