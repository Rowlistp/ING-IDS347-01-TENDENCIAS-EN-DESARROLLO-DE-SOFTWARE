# Verificación de preparación del producto

Fecha: 22/09/2026. Base revisada: main `c2deae0`. La PR #65 anterior ya está integrada y sus tres comprobaciones de GitHub aprobaron. La revisión actual conserva las mejoras de correo y las validaciones añadidas después.

| Verificación actual | Resultado |
|---|---|
| Servidor, PostgreSQL, Mailpit y Keycloak | 463 aprobadas, 0 fallidas y 0 omitidas |
| Web | 6 pruebas aprobadas; análisis y compilación correctos |
| Navegador | 156 comprobaciones aprobadas: 152 de ocho perfiles × 19 rutas, plantilla histórica, PWA sin conexión y dashboard móvil/escritorio |
| Errores de aplicación durante ese recorrido web | 0 excepciones JavaScript y 0 respuestas 5xx |
| Flutter | 35 pruebas aprobadas; análisis sin incidencias y APK construido |
| Regresión del diálogo QR | La prueba falla con el controlador anterior y aprueba tras corregir su ciclo de vida |
| Flutter con API y PostgreSQL reales | Consumo único, saldo, movimiento, auditoría y rollback aprobados; identidad y fuente de QR sustituidas solo en ese test |
| Android emulado | APK instalado, login local, cámara virtual, navegación, rechazo de código sin firma, validación de QR firmado, despacho de 1 galón con descuento exacto de inventario y rechazo de reutilización |
| Presentación | 19 diapositivas con notas, tabla tecnológica editable, capturas reales y revisión visual |

## Cambios de esta revisión

- Adaptador Textbee: contrato y acuses específicos, configuración privada, sin repetir automáticamente resultados ambiguos.
- RF-11: cálculo opcional por historial de 90 días, límite configurado/capacidad del vehículo, aprobación obligatoria y auditoría de generación. Sin historial no se inventan cantidades.
- Auditoría transaccional de creación/activación/desactivación de plantillas recurrentes.
- PWA con manifest e iconos, aviso sin conexión y sin cachear respuestas de API, tickets o datos personales.
- Idioma/título/marca de la web y distribución de tarjetas del dashboard.
- Escáner Android: instrucciones visibles y corrección del error al cerrar el diálogo de QR manual.

## Base de datos y operación

`AddHistoricalRecurringRequests` añade `UsarConsumoHistorico` con valor predeterminado false. Las plantillas existentes conservan su cantidad fija. Se aplicó en la base local y la suite completa valida las migraciones en PostgreSQL desechable. Para otro entorno, respaldar y aplicar la migración antes de ejecutar esta versión.

La regla histórica promedia despachos positivos del mismo vehículo/combustible desde hoy menos 90 días hasta ayer. Limita el resultado por capacidad del vehículo y cantidad de la plantilla. El programador mantiene una solicitud por plantilla/día. La regla es una implementación explícita para ratificar con el cliente, porque el SRS no prescribe el algoritmo.

La evidencia detallada de comandos, TRX, capturas y resultados se conserva en `qa/preparacion-cliente` del espacio de trabajo; no se publican configuraciones privadas ni QR vigentes. Los datos de prueba no representan la operación real de INTEC.

## Pendientes que estas pruebas no sustituyen

SMS con Android físico/SIM y recepción real, entrega de correo en el entorno definitivo, lector/cámara físicos, login Keycloak nativo y distribución Android firmada. La infraestructura productiva debe acreditar TLS 1.3, AES-256, respaldo/restauración y disponibilidad. Persisten avisos no bloqueantes de tamaño del bundle y futura migración Kotlin del plugin de cámara.

Ver [matriz SRS](MATRIZ-SRS.md), [guion](GUION-DEMO.md) y [Textbee](../29-TEXTBEE.md). No se declara cumplimiento productivo del 100%.

## Ampliación para despliegue

Contenedor completo probado con PostgreSQL: inicio de sesión, catálogos, solicitud, aprobación, emisión y PDF. Se corrigió el rechazo de fechas con offset por PostgreSQL y se comprobaron cinco variantes de zona horaria. La API normaliza las fechas JSON a UTC; entradas sin zona usan UTC. Suite completa: 463 aprobadas, ninguna omitida.
