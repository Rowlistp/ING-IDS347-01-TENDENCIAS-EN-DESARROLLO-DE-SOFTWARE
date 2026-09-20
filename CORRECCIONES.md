# Correcciones y verificación de FuelTrack

Base histórica: `77e8fba8d56301486bcb1f4e1503f5aa7d9d3f2d`; integradas las validaciones del main actual `d86e7c998c55be352cc4b2d2651ca0bea63d203b` y las funciones QR/móviles corregidas de `76c81ed`. Rama local `fix/diagnostico-web`. Integración preparada para revisión mediante Pull Request.

## Resultado actual

La versión incorpora las correcciones de la primera etapa y de la UAT integral posterior. El [informe actualizado](docs/qa/06-VERIFICACION-INTEGRACION.md) contiene el resultado final, evidencias y límites. La [guía manual](docs/qa/07-GUIA-MANUAL-INTEGRACION.md) permite repetir los flujos por perfil.

- Ajustes, transferencias y recepciones respetan capacidad y saldos. Las transferencias exigen el mismo combustible. La recepción revalida el espacio aunque el formulario sea anterior a otro movimiento.
- Un tanque con existencias no puede cambiar de combustible ni reducir su capacidad por debajo del saldo. Un tanque activo requiere combustible activo.
- La edición y la desactivación de catálogos comparten restricciones de dependencias; Supervisor no puede eludir el permiso administrativo mediante la casilla Activo.
- Solicitudes y plantillas recurrentes validan catálogos activos y relaciones entre empleado, vehículo y departamento. El proceso recurrente revalida antes de generar y reserva su ejecución en una transacción.
- Aprobar y rechazar solo modifican una solicitud todavía Pendiente; una lectura obsoleta no sobrescribe la decisión de otro operador. Las decisiones y su auditoría se confirman juntas.
- Solicitante solo recibe su propia ficha personal. Reportes carga una lista de identificadores y nombres; Consulta no puede descargar fichas completas.
- Una sesión local corrupta vuelve al acceso sin pantalla en blanco. Salir borra la sesión en las otras pestañas y solicita la revocación del permiso de renovación. Si no hay conexión, el acceso local se cierra igualmente.
- La anulación desde el detalle de un ticket usa un solo diálogo. Los formularios y filtros tienen etiquetas asociadas y los diálogos conservan el foco.
- Inventario actualiza saldos; los reportes aplican filtros también al exportar; las fechas civiles preservan el día. El lector de imagen QR usa un respaldo con carga diferida y sigue validando firma, vigencia y estado en el servidor.
- El envío muestra el estado real de cada canal y del ticket. Los problemas de conexión se explican en español.

## Verificación

431 pruebas del servidor aprobadas; ninguna fallida. Ocho pruebas de Keycloak omitidas porque la integración está deshabilitada. Se usaron PostgreSQL y un buzón de pruebas aislados. Seis pruebas de interfaz aprobadas, análisis estático y compilación correctos.

La evidencia de navegador incluye los perfiles Administrador, Supervisor, Despachador, dos Solicitantes, Auditor, Consulta y combinaciones de roles; el ciclo solicitud → aprobación → ticket/PDF → QR → despacho; anulación y reutilización; concurrencia entre operadores; catálogos; usuarios; inventario; reportes; vista móvil y teclado. El informe conserva la reproducción inicial de cada fallo y su repetición corregida.

Keycloak, SMS externo y dispositivos físicos deben verificarse en el entorno de destino. El procesamiento nocturno y la creación válida de cierre cuentan con regresión automatizada; los días con despachos de la base local ya estaban cerrados. La advertencia de tamaño del paquete principal permanece. No se presenta esta copia local como un despliegue de producción certificado.

Para repetir la regresión aislada, consultar [verificación](docs/qa/06-VERIFICACION-INTEGRACION.md) y `backend/scripts/run-full-integration-tests.sh`. En `frontend`: `npm test`, `npm run lint`, `npm run build`.

## Integración de main — 20/09/2026

Se conservaron el nuevo visor QR, la actualización de tickets, las pantallas móviles modulares y la cámara. Se corrigieron regresiones de firma QR, autenticación móvil, datos ficticios y revalidación del empleado. Se incluyeron fuentes en el APK y se actualizó el bloqueo de dependencias para Flutter 3.47.2.

34 pruebas móviles, análisis estático y construcción APK correctos. E2E Flutter/API/PostgreSQL aprobado. El recorrido adicional repitió las 152 combinaciones rol/ruta y el ciclo con QR firmado: el despacho de 1.23 galones descontó 83.59 → 82.36, y otra sesión observó Consumido sin recargar. Los resultados anteriores son evidencia histórica; el informe de integración contiene las comprobaciones actuales y sus límites.

## Resolución del main actual d86e7c9

Se combinan los bloqueos nuevos de combustible inactivo con capacidad y compatibilidad. Se mantiene `tipoCombustibleActivo` en tanques y sus filtros; departamento derivado del empleado, validación del vehículo y auditoría local. La documentación y generadores del grupo se conservan. Los hallazgos QR/móvil usan el prefijo INT para evitar colisiones con DEF-2026-012 a 016.

La regresión actual contiene 431 pruebas aprobadas y 8 omitidas de Keycloak, además de 6 web, 34 móviles y E2E Flutter/API/PostgreSQL aprobado. El informe de integración contiene las pruebas de navegador y paquetes separados para revisión. La integración requiere revisión antes de fusionarse en main.

La rama local `fix/diagnostico-web` queda basada en `d86e7c9`, con las correcciones separadas para revisión. La referencia `backup/qa-antes-main-d86` conserva la base previa. Los archivos se verificaron idénticos antes y después de alinear la base.
