# Integración corregida de QR y móvil

Los identificadores INT son propios de esta integración y no reemplazan DEF-2026-012 a 016 de main. Se conservan los resultados históricos de la revisión anterior; la verificación actual está en [informe](06-VERIFICACION-INTEGRACION.md).



### INT-2026-001

- **Severidad:** Crítica
- **Prioridad:** P1
- **Estado:** Resuelto en la integración; pendiente de revisión del equipo.
- **Componente:** TicketService / QR
- **Descripción:** El validador aceptaba GUID/correlativo sin firma.
- **Causa raíz (RCA):** Fallback de búsqueda añadido tras la validación criptográfica.
- **Resolución:** Eliminar fallback; conservar PNG firmado persistido, propiedad y error si no hay imagen.
- **Verificación:** Pruebas de GUID/código, dueño/no dueño y PNG ausente; navegador INT-QR-SIN-FIRMA, GUID, PRIVACIDAD e IMAGEN.

### INT-2026-002

- **Severidad:** Crítica
- **Prioridad:** P1
- **Estado:** Resuelto en la integración; pendiente de revisión del equipo.
- **Componente:** Autenticación móvil
- **Descripción:** Un acceso automático y token de bypass podían representar una sesión no verificada.
- **Causa raíz (RCA):** La rama móvil trataba errores de acceso como modo de demostración y alteraba el proveedor seguro.
- **Resolución:** Credenciales explícitas, respuesta real, caducidad/renovación/logout y separación por servidor; conservar OIDC.
- **Verificación:** Pruebas móviles de éxito, 401/503, descarte de bypass y aislamiento de sesiones dentro de las 34 aprobadas.

### INT-2026-003

- **Severidad:** Mayor
- **Prioridad:** P1
- **Estado:** Resuelto en la integración; pendiente de revisión del equipo.
- **Componente:** Catálogos móviles
- **Descripción:** Fallos de servidor podían mostrar estaciones/tanques inventados.
- **Causa raíz (RCA):** Fallback de datos de demostración en el cliente.
- **Resolución:** Eliminar fallback; conservar error o lista vacía y filtrar catálogos reales activos/compatibles.
- **Verificación:** Pruebas móviles de listas vacías, desconexión y compatibilidad; E2E API real.

### INT-2026-004

- **Severidad:** Media
- **Prioridad:** P2
- **Estado:** Resuelto en la integración; pendiente de revisión del equipo.
- **Componente:** Tickets web
- **Descripción:** Visor QR desde detalle superponía diálogos; la carga y actualización no reflejaban bien errores/cierre.
- **Causa raíz (RCA):** Nuevo visor no integrado con foco y ciclo de solicitudes.
- **Resolución:** Un diálogo, cancelación, liberación de imagen, reintento y aviso de actualización fallida.
- **Verificación:** INT-QR-FIRMADO, ERROR, REINTENTO y TICKETS-SINCRONIZACION en navegador real.

### INT-2026-005

- **Severidad:** Mayor
- **Prioridad:** P1
- **Estado:** Resuelto en la integración; pendiente de revisión del equipo.
- **Componente:** Emisión de tickets / Solicitudes
- **Descripción:** Se podía emitir una solicitud cuyo empleado ya no pertenecía al departamento.
- **Causa raíz (RCA):** Main había retirado esa revalidación; el formulario también perdía coherencia de selección.
- **Resolución:** Revalidar empleado y vehículo; seleccionar departamento a partir del empleado.
- **Verificación:** Regresión del servicio e INT-SOLICITUD-COHERENCIA.

### INT-2026-006

- **Severidad:** Media
- **Prioridad:** P2
- **Estado:** Resuelto en la integración; pendiente de revisión del equipo.
- **Componente:** Dependencias y recursos móviles
- **Descripción:** El bloqueo de dependencias no correspondía al SDK declarado y las fuentes dependían de descarga al abrir.
- **Causa raíz (RCA):** Archivo lock anterior y Google Fonts con carga remota; el E2E no avanzaba IO del inicio modular.
- **Resolución:** Actualizar cuatro dependencias del SDK, empaquetar fuentes/licencias y adaptar reloj del test a IO real sin retirar aserciones.
- **Verificación:** 34 pruebas, análisis, APK y mobile-api-e2e-verificado.log aprobados.
