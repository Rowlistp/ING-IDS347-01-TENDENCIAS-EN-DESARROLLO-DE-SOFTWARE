# Despliegue gratuito y prueba de comunicaciones

Revisión de fuentes oficiales: 22/09/2026. El objetivo es una demostración académica con datos ficticios. Los planes gratuitos tienen límites y no acreditan disponibilidad 24/7.

## Demo publicada

URL: https://fueltrack-intec-demo.onrender.com. Render Free y Neon Free, región Ohio. Versión de aplicación `7abc474`, publicada el 22/09/2026. Usuarios y claves se entregan por archivo privado y no forman parte del repositorio.

Se verificaron login de cinco roles, permisos, solicitud/aprobación/ticket/PDF, bloqueo de sobrecapacidad y QR inválido, cola vacía y TLS 1.3. El APK de demostración online usa esta URL; continúa siendo una compilación debug. La demo incluye solo datos ficticios.

El correo SMTP local se probó con Mailpit. La cuenta Brevo ya verificó teléfono; la integración externa y recepción real siguen pendientes. SMS real requiere Android físico con SIM. No se declara cumplimiento productivo total.

## Opciones revisadas

| Opción | Uso | Límite relevante |
|---|---|---|
| Render Free + Neon Free | Web/API .NET en contenedor y PostgreSQL administrado | Render suspende la instancia tras 15 minutos sin tráfico. El arranque siguiente puede tardar alrededor de un minuto. Neon tiene cuotas y suspensión de cómputo. |
| Cloudflare Quick Tunnel | Enlace HTTPS temporal hacia el equipo local, sin cuenta | Requiere equipo y procesos encendidos, dirección temporal y sin garantía de disponibilidad. No es alojamiento permanente. |
| Brevo Free | Correo transaccional con PDF usando el SMTP existente | 300 correos/día según el plan consultado. Requiere cuenta, remitente verificado y habilitación transaccional. |
| Textbee Free | SMS por un Android del equipo | 1 dispositivo, 50 mensajes/día y 300/mes según el plan consultado. El operador puede cobrar SMS. |
| Twilio Trial | Prueba de proveedor sin Android propio | 30 días, destinatarios verificados, país de registro y contenido predefinido. La prueba actual no admite el texto personalizado con enlace del ticket. No sustituye Textbee sin otro adaptador. |

Fuentes: [Render](https://render.com/docs/free), [Neon](https://neon.com/blog/how-to-make-the-most-of-neons-free-plan), [Cloudflare](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/do-more-with-tunnels/trycloudflare/), [Brevo](https://help.brevo.com/hc/en-us/articles/208580669-FAQs-What-are-the-limits-of-the-Free-plan), [Textbee](https://textbee.dev/), [Twilio](https://www.twilio.com/docs/usage/trials). Confirmar las cuotas mostradas por cada cuenta antes de activar servicios. No añadir un plan de pago para esta demo.

## iPhone, SIM y emulador

El iPhone sirve como destinatario del SMS y permite abrir el ticket en Safari. Textbee es un gateway Android, no una app para convertir el iPhone en gateway.

- SIM física: puede trasladarse a un Android compatible y desbloqueado para el operador. La línea pasa a ese teléfono mientras la SIM esté allí. Es preferible usar el Android/SIM de un compañero y conservar el iPhone como receptor.
- eSIM: no se copia al emulador. Un traslado entre teléfonos depende del operador y la compatibilidad de ambos equipos. [Apple](https://support.apple.com/en-us/123878) documenta transferencias entre determinados modelos y operadores.
- Emulador: simula telefonía. Un SMS inyectado desde sus controles o consola llega a su app de mensajes, pero no circula por una red móvil real. [Android](https://developer.android.com/studio/run/emulator-console).
- GitHub: [Textbee](https://github.com/textbee/textbee) requiere Android/SIM y [Gammu](https://github.com/gammu/gammu) requiere teléfono o módem GSM compatible. No se encontró una opción verificada que proporcione una SIM de operador gratuita al emulador. El código abierto no sustituye el hardware ni el servicio del operador.

## Publicar en Render y Neon

1. Crear un proyecto Neon Free dedicado a FuelTrack. Elegir una región cercana al servicio Render y conservar la cadena de conexión en el gestor de secretos.
2. Crear un servicio Render Free desde este repositorio y la rama de esta entrega. El archivo `render.yaml` define el contenedor. El `Dockerfile` compila React y .NET y sirve ambos bajo el mismo origen. No hay que publicar la base ni abrir CORS globalmente.
3. Ejecutar `python3 infra/deploy/generate-keys.py`. Crea una sola vez `~/.config/fueltrack/deployment-keys.env` con permisos privados. Introducir esas claves como secretos de Render y conservarlas entre reinicios. No regenerarlas en cada arranque.
4. Configurar `ConnectionStrings__DefaultConnection` en formato Npgsql, no pegar directamente el URI `postgresql://`: `Host=<host>;Database=<db>;Username=<rol>;Password=<secreto>;SSL Mode=VerifyFull;Channel Binding=Require`. Usar un endpoint directo para la migración inicial de esta demo.
5. `Database__MigrateOnStartup=true` aplica las migraciones antes de crear el administrador, únicamente para una instancia de demostración. Para producción, respaldo y migración como paso controlado de publicación.
6. Verificar `/healthz`, abrir `/login`, entrar con el administrador privado y crear usuarios con sus roles. `healthz` confirma el proceso, no mide disponibilidad histórica ni sustituye una consulta funcional a la base.
7. Comprobar que una ruta protegida sin sesión devuelve 401 y una ruta `/api` inexistente devuelve 404. La navegación directa de React debe funcionar.
8. Configurar `Notifications__PublicBaseUrl=https://<servicio>.onrender.com` antes de generar enlaces de SMS. Mantener ambos canales y worker desactivados hasta revisar destinatarios y cola.

La imagen usa `Production`, no publica Swagger y corre como usuario no privilegiado. No contiene credenciales ni archivos `.env`. El hosting termina HTTPS en su borde. Deben comprobarse por separado TLS 1.3, cifrado en reposo, backups y restauración antes de cerrar RS-03 y disponibilidad.

El plan gratuito puede dormir mientras un envío o una solicitud recurrente esperan ejecución. No prometer notificaciones puntuales con un proceso suspendido ni usar pings artificiales para evadir las restricciones del proveedor.

## Correo real sin cambiar la aplicación

Brevo documenta SMTP con STARTTLS en `smtp-relay.brevo.com:2525`. Render Free bloquea 25, 465 y 587; la alternativa 2525 debe comprobarse desde el servicio. [Puertos oficiales de Brevo](https://help.brevo.com/hc/en-us/articles/10905415650322-Which-SMTP-port-should-I-use-Port-587-465-or-2525).

Para desarrollo local: `python3 backend/scripts/configure-brevo.py`. Solicita Login SMTP, clave SMTP y remitente verificado sin imprimir la clave. Usa user-secrets, cargados en Development. El almacén no está cifrado. En Render introducir las mismas opciones en Environment, usando `__` en lugar de `:`; las variables del entorno tienen prioridad.

Antes de habilitar el worker, utilizar una base de demo, revisar todas las notificaciones pendientes y limitar los destinos al correo y teléfono que el equipo autorice. No habilitar una cola antigua por accidente.

## Pruebas y evidencia de aceptación

| Prueba | Qué demuestra | Qué no demuestra |
|---|---|---|
| SMTP real hacia Mailpit | Emisión, MIME, destinatario, PDF adjunto y recepción en buzón local | Llegada a Gmail/iCloud/Outlook |
| HTTP controlado de SMS | Contrato, autenticación, acuses, errores y reintentos | Recepción por la red del operador |
| SMS simulado en Android | Visualización y apertura desde la app de mensajes | SIM real ni entrega del proveedor |
| Correo externo autorizado | Recepción, adjunto y contenido en el buzón propio | Entrega a todos los proveedores |
| Textbee con Android/SIM e iPhone receptor | Entrega real y apertura del enlace HTTPS del ticket | Disponibilidad garantizada del operador |

Para la aceptación externa: emitir un ticket nuevo de prueba, registrar su identificador, enviar solo a los destinos autorizados, comprobar recepción y PDF, validar el QR y despachar una vez. Repetir el QR debe fallar sin otra variación de inventario. No confundir `ENVIADA` (aceptación del transporte) con recepción final.

## Fechas de la API

La API acepta fechas ISO 8601 con `Z` u offset (`-04:00`, `+00:00`) y conserva el instante al guardarlo en UTC. Las fechas JSON sin zona se interpretan como UTC, independientemente del huso del servidor.

Para mostrar la simulación explícita en Mensajes del emulador: `python3 backend/scripts/demo-sms-emulator.py https://<demo>`. Solo admite un dispositivo `emulator-*` y una URL sin secretos. El texto indica SIMULACION LOCAL. No usa Textbee ni demuestra recepción real.
