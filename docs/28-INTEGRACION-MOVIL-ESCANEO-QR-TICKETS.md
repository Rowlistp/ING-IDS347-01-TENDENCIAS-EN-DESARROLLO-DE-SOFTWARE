# 28 — Integración móvil, QR y tickets

Actualización del 20/09/2026. Integra las funciones de `main` en `76c81ed` con las correcciones de la UAT local y las validaciones del main actual `d86e7c9`. La revisión está preparada en `fix/diagnostico-web`, para revisión por Pull Request.

## Flujo implementado

Solicitud coherente entre empleado, vehículo y departamento → aprobación → emisión de ticket con QR firmado persistido → lectura por web/móvil → validación de firma, vigencia y estado → despacho transaccional → ticket Consumido e inventario actualizado.

El QR transporta el contenido firmado **FTQR1**, no un GUID ni un correlativo. La API rechaza ambos identificadores sin firma. La emisión revalida el departamento actual del empleado y del vehículo; una solicitud aprobada no permite eludir un cambio posterior de catálogo.

`GET /api/v1/tickets/{id}/qr` devuelve exclusivamente el PNG persistido, con autenticación, alcance por propietario y `Cache-Control: no-store`. Si falta, devuelve 409 `QR_NO_DISPONIBLE`; no fabrica otro QR. Un ticket inexistente o ajeno al Solicitante devuelve 404. El QR visible no sustituye la validación de estado en el servidor.

## Web

El visor usa un solo diálogo, controla carga/error/reintento, cancela solicitudes pendientes y libera imágenes al cerrar. La tabla y el diálogo consultan cambios cada tres segundos mientras la página es visible y al recuperar foco. Una caída muestra «Sin actualizar»; al recuperarse vuelve el estado normal. Los tickets consumidos/anulados no muestran un QR operativo. `/qr.html` dirige a Tickets autenticado.

## Móvil

Se mantienen las pantallas modulares y el lector con cámara de `main`. La autenticación local requiere credenciales explícitas y respuesta válida de la API; Keycloak conserva PKCE. No hay acceso automático con credenciales incrustadas, token de bypass ni inventario ficticio tras un error. Catálogos activos y compatibles provienen de la API real.

La configuración se fija al compilar; se muestra el servidor sin permitir cambiarlo durante una sesión. Las fuentes se distribuyen dentro del APK. Ver [configuración móvil](../mobile/README.md).

## Red de desarrollo

El perfil explícito de desarrollo `http` permite LAN en 5298. Se conserva la configuración normal del host para los demás entornos; no se fuerza globalmente `0.0.0.0`. El lanzador Windows usa 5298/5173 y USB reverse, sin número de serie fijo. La copia QA local usa API 5300 y web 5175. Para el APK QA, `adb reverse tcp:5300 tcp:5300`.

## Evidencia y límites

- Servidor: 431 aprobadas, 0 fallidas y 8 omitidas de Keycloak, con PostgreSQL/buzón de pruebas.
- Web: 6 pruebas aprobadas, lint y build correctos; navegación real de 8 perfiles por 19 rutas, más flujo firmado y despacho real.
- Móvil: 34 aprobadas, análisis sin problemas y APK construido con Flutter 3.47.2/Dart 3.13.2.
- E2E Flutter → API → PostgreSQL aprobado: consumo único, stock, movimiento, auditoría y rollback. Identidad y entrada del escáner se sustituyen solo en ese test.
- Cámara/pistola física, USB, Keycloak nativo y SMS externo requieren verificación en destino. No se certifica producción con esas comprobaciones pendientes.

[Informe y evidencias actuales](qa/06-VERIFICACION-INTEGRACION.md) · [Guía manual](qa/07-GUIA-MANUAL-INTEGRACION.md).
