# 26 — Cierre de Fase 9

## Alcance y base

RF-09 envío real EMAIL/SMS, RF-23 cinco alertas y RF-24 API REST transversal.
Rama feature/builder2-fase9-notificaciones desde main
db248b6170ad7b6c13a8c9dffafaa74de7a7fd19. F1/F4/F5 ya fusionadas por PR#9;
F7/F8 por PR#10, documentación por PR#11. F9 no reimplementa esas fases.
Sin PR automático, merge ni push a main en esta entrega.

## Entrega técnica

Notificacion existente ampliada con intentos, próximo/último intento, fecha de
envío, error sanitizado, ID proveedor, clave única, lease/ReservaId y mensaje
de alerta. No hay segunda cola. F4 solo prepara; dedupe contempla todos los
estados, pues un canal enviado/procesando/fallido no debe recrearse.

DeliveryWorker reclama lotes SKIP LOCKED en transacción corta; ejecuta transporte
fuera y finaliza con fencing de ReservaId. Lote bounded inicia en paralelo para
no agotar leases esperando una cola local. Timeout menor que lease, recuperación
de PROCESANDO abandonado y backoff acotado hasta FALLIDA. Auditoría y estado se
guardan juntos. Ticket se bloquea antes que Notificacion, consistente con F4.
Solo pasa Pendiente→Enviado si todos sus TICKET_EMITIDO están ENVIADA y sigue
vigente/no terminal; auditoría TICKET_ENVIADO una vez.

MailKit SMTP con TLS configurable, MIME y PDF generado por F4 con QR persistido.
Message-Id determinista; SMS HTTP genérico con Idempotency-Key y acuse validado.
Semántica at-least-once: crash tras aceptación externa puede duplicar; no se
promete exactamente-once ni deduplicación universal del correo.

SMS incluye código/cantidad/vencimiento y URL segura, no QR raw. TicketDeliveryLink
guarda hash SHA-256 de token aleatorio de256 bits; vida menor o igual al Ticket.
Descarga anónima por capacidad con404/410, PDF, no-store/no-referrer y auditoría
sin token. Proxy/telemetría deben respetar la misma política de redacción.

RuleWorker separado del transporte: próximo vencimiento, vencimiento, inventario
bajo y ajustes. Quinta alerta: INTEGRACION_FALLIDA, INTERNO visible en API y
auditoría, sin reenviar al canal roto. Destinatarios operativos por configuración,
no se inventa email en Usuario. Inventario usa ExistenciaActual y NivelCritico;
ajustes observan movimientos persistidos sin tocar Builder1.

Reglas Ticket por lotes keyset con filtro de alertas ya generadas; índice
tipo/referencia y UNIQUE lógico. Stock bajo dedupe por bucket UTC de período
configurable (24h por defecto), no ventana deslizante; puede avisar al cruzar
un límite de período. Ajustes hasta500 pendientes por ciclo. Destinatarios nuevos
reciben alertas futuras; no se reenvía todo el historial al cambiar configuración.

## API y configuración

GET /notificaciones y /{id}: Admin/Supervisor/Auditor; filtros y paginación.
POST /{id}/reintentar: Admin/Supervisor, solo FALLIDA; conserva IntentosTotales.
Options tipadas/ValidateOnStart; deshabilitadas por defecto para no enviar datos
accidentalmente. Credenciales exclusivamente env/user-secrets/secret manager.
Operación y contrato completo: [infra/notifications](../infra/notifications/README.md).

## Pruebas y clasificación de archivos

[25 — Pruebas](25-PRUEBAS-FASE9-NOTIFICACIONES.md):285 aprobadas, sin fallos ni
omisiones; PostgreSQL real, SMTP TCP/Mailpit, SMS HTTP, concurrencia, retry,
reglas, links y REST transversal. CI dedicado y Backend Security incluyen main.

F9 PROPIO: Notifications/*, dos controladores, TicketDeliveryLink, migración F9,
tests F9, infra notifications y workflow notifications.
COMPARTIDO NECESARIO: Notificacion, AppDbContext/snapshot, TicketService (dedupe
y reutilización del mapeo PDF), Program/options/DI/log filter, csproj MailKit,
appsettings sin secretos, scripts de pruebas, tests RBAC, CI/documentación.
AJENO MODIFICADO: ninguno de los controladores/servicios protegidos. React y
Flutter sin cambios; no eliminaciones de trabajo ajeno ni migraciones previas.

## KNOWN PROJECT GAPS — fuera de esta rama

- RF-11: automatización/recurrentes incompletos; aprobar/rechazar no los completa.
- RF-19: filtros funcionales SRS incompletos; la existencia del endpoint no los resuelve.
- RF-22: indicadores dashboard no cubren todo el SRS; UI web pendiente.
- Rol Despachador en cierre diario: decisión pendiente de coordinación.
- Frontend React atrasado: Builder3; no corregido en F9.
- F5: gate físico Android/login nativo todavía pendiente, aunque ya esté mergeado.
- Dependencia preexistente de tests F7/F8: System.IO.Packaging8.0.0, avisos
  GHSA-f32c-w444-8ppv y GHSA-qj66-m88j-hmgj. Coordinar actualización; no se ocultan warnings.

## Pendientes reales de despliegue

Elegir proveedor SMS/gateway y validar credenciales reales; SMTP productivo,
DNS/TLS/cuentas/remitente, red y políticas de logs. No se enviaron mensajes a
destinatarios reales ni se acreditó entrega productiva.
`TRANSPORT IMPLEMENTED — REAL PROVIDER CREDENTIAL GATE PENDING`.
La migración aborta ante duplicados históricos; revisión humana preservando
historial antes de continuar, nunca borrado automático.
