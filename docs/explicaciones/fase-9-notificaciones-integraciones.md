# Fase 9 — Notificaciones e integraciones

## Qué se construyó y por qué

Antes F4 dejaba una intención de envío en PostgreSQL. F9 toma esa intención,
envía correo/SMS, guarda resultado y avisa automáticamente sobre tickets e
inventario. Separar intención de transporte permite que una caída del proveedor
no pierda el ticket ni mantenga una transacción abierta esperando Internet.

## Recorrido por los componentes

- Notificacion: outbox durable, estados e intentos. Una outbox es una cola
  persistida en la misma base de datos donde se realiza el negocio.
- TicketService: sigue encolando, no envía. UNIQUE ClaveIdempotencia evita repetir
  una intención lógica, incluso con varios procesos.
- NotificationDeliveryService: reserva con FOR UPDATE SKIP LOCKED; cada worker
  toma filas distintas. Lease fija hasta cuándo posee el trabajo; ReservaId
  impide a un worker atrasado confirmar una reserva que ya fue recuperada.
- NotificationDeliveryWorker: genera contenido, llama SMTP/SMS fuera de la
  transacción y registra resultado. Backoff aumenta la espera; MaxAttempts limita.
- SmtpEmailSender: MailKit construye MIME, texto y PDF adjunto. Reutiliza el
  PDF/PNG de F4; SMTP es el protocolo real, Mailpit el buzón local de pruebas.
- HttpSmsGatewaySender: POST JSON a un gateway configurable, separado del dominio.
  No se inventa un proveedor comercial ni se oculta falta de credenciales.
- TicketDeliveryLinkService: token nuevo e independiente del QR; guarda solo hash,
  limita expiración y devuelve PDF. Quien posee URL tiene acceso: no debe loggearse.
- NotificationRuleService/Worker: detecta vencimiento cercano/real, stock bajo y
  ajustes ya persistidos. Dedupe evita avisar cada minuto sobre el mismo hecho.
- NotificacionesController: operaciones consulta estado/error seguro y reintenta
  FALLIDA con permisos. Auditor solo lee. INTEGRACION_FALLIDA es alerta interna,
  no un correo que falle y genere otro correo infinitamente.

## Preguntas del profesor

¿Por qué no SMTP dentro de la transacción? Retendría locks durante una llamada
que puede tardar o fallar, bloqueando otras operaciones de combustible.

¿Cómo se evita el doble envío? UNIQUE evita doble encolado; SKIP LOCKED y lease
evitan procesar fácilmente la misma fila; Message-Id e Idempotency-Key mitigan
duplicados externos. Pero un crash después de aceptación y antes de guardar
ENVIADA puede repetir. Eso es at-least-once, no exactamente-once.

¿Y si SMS falla pero correo llega? Ticket sigue Pendiente. Solo todos los canales
requeridos aceptados permiten Enviado. Consumido/Anulado/Vencido nunca reviven.

¿Qué es un error permanente? Destinatario inválido o rechazo de configuración;
se marca FALLIDA. Timeout,429,5xx y SMTP4xx se reintentan hasta el límite.
No guardamos cuerpo del error externo: podría contener credenciales.

¿Por qué otro token? Un enlace de descarga no debe exponer JWT, refresh token
o token criptográfico del QR. Es una capacidad aleatoria de256 bits, solo hash
persistido; expira/revoca y no vive más que el Ticket.

¿Cómo sabe el worker que hubo ajuste? Lee MovimientoInventario Tipo=Ajuste de
Builder1. No cambia ese controlador ni su lógica. Stock bajo usa la fuente
Inventario.ExistenciaActual, no otro saldo paralelo.

¿Cómo se demuestra? PostgreSQL real para locks/migraciones, Mailpit por SMTP TCP
y API de buzón para ver PDF, gateway HTTP local para capturar solicitudes y
provocar errores, pruebas de caída/recovery y recorrido REST hasta reporte.
Mocks ni credenciales de prueba equivalen a proveedor productivo validado.

## Seguridad, equipo y pendientes

Env/user-secrets/secret manager guardan contraseñas/API keys; Git solo placeholders.
No se loggean enlaces bearer, tokens ni headers sensibles. También hay que
configurar reverse proxy y trazas para no registrar URL completa.
F1/F4/F5/F6/F7/F8 se preservan. RF-11/RF-19/RF-22/roles de cierre/UI y gate
físico Android tienen pendientes propios; F9 no los presenta como resueltos.
