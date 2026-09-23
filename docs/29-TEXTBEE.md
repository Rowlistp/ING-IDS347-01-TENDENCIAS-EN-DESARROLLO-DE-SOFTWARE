# Textbee: configuración y prueba de aceptación

El proveedor `Textbee` usa `POST https://api.textbee.dev/api/v1/gateway/send-sms`, cabecera `x-api-key` y cuerpo con `recipients`, `message` y `deviceId`. Se conserva el proveedor `Generic` para instalaciones anteriores. Documentación consultada el 22/09/2026: https://textbee.dev/docs/sending-sms/sending-sms y https://textbee.dev/docs/api-reference.

## Requisitos externos

1. El titular crea su cuenta en https://textbee.dev y verifica su correo.
2. Instala la app oficial en un Android físico con SIM, servicio SMS y acceso a internet. Vincula el dispositivo desde el QR del panel. Conserva API Key y Device ID.
3. El emulador sirve para demostrar FuelTrack, pero no tiene una SIM del operador para enviar SMS reales. El envío usa el plan del teléfono. Consultar los límites vigentes del plan de Textbee y del operador.
4. FuelTrack necesita una URL HTTPS accesible desde el teléfono destinatario para descargar el ticket. `127.0.0.1` solo funciona dentro del equipo donde se abre, no es una URL pública.

## Configurar sin publicar secretos

Desde la raíz del repositorio:

```sh
python3 backend/scripts/configure-textbee.py
```

La clave se solicita sin eco y se pasa a .NET por entrada estándar. Se guarda en user-secrets del usuario, fuera del repositorio. Este almacén local no está cifrado: proteger la cuenta del equipo. Producción debe usar el gestor de secretos del despliegue. La API carga user-secrets en Development; variables de entorno existentes tienen prioridad y pueden anular esta configuración. Nunca adjuntar la clave a una presentación, captura o commit.

El script deja `Sms:Enabled=false` y `WorkerEnabled=false`. Antes de activarlos revisar la cola: contiene también notificaciones antiguas y al encender el worker podrían enviarse. Para la primera prueba usar una base de demostración y un único destinatario propio del equipo. Mantener SMTP apagado si no forma parte de la prueba.

Tras comprobar los destinos, habilitar `Notifications:Sms:Enabled` y `Notifications:WorkerEnabled` en la configuración privada, reiniciar la API y emitir un ticket para ese destinatario. No ejecutar esta activación en CI.

## Criterios observables

| Paso | Evidencia requerida |
|---|---|
| Emitir ticket | Solicitud aprobada, ticket único y notificación SMS pendiente |
| Aceptación API | La notificación pasa a ENVIADA y conserva el identificador de lote si Textbee lo proporciona |
| Entrega real | El teléfono destinatario recibe el SMS, Textbee confirma su estado y el enlace HTTPS abre el PDF con QR |
| Usar QR | Despachador valida identidad y registra una sola vez; inventario disminuye por los galones servidos |
| Repetir | Segundo intento rechaza ticket consumido sin otra salida de inventario |

`ENVIADA` significa aceptación del proveedor, no recepción confirmada por el operador. Textbee puede aceptar un lote mientras el teléfono está sin conexión. Revisar el panel para confirmar entrega. La API también admite el acuse de modo inmediato (`successCount=1`, `failureCount=0`).

No se presupone deduplicación de Textbee por `Idempotency-Key`. Ante timeout, respuesta ambigua o 5xx, se detiene el reintento automático y se revisa el historial antes de reintentar manualmente. 429 permite reintento con espera; 400/401/403 requieren corregir configuración o destinatario. No se registran claves ni cuerpos de respuestas externas.

## Estado de esta entrega

Adaptador y contrato probados contra HTTP local con respuestas controladas. Envío externo **pendiente**: el equipo informó que todavía solo dispone del emulador. La cuenta, SIM, clave, dispositivo y URL pública no se simulan ni se declaran verificados.
