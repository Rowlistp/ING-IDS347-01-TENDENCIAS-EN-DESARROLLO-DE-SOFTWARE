# Verificación de la integración FuelTrack

Base revisada: `d86e7c998c55be352cc4b2d2651ca0bea63d203b` (20/09/2026). Se combinan las validaciones de combustible inactivo de main con las correcciones de capacidad, compatibilidad, privacidad, permisos, auditoría, sesiones y clientes. Se conserva el visor QR y el móvil modular de la línea 76c81ed con firma y autenticación obligatorias.

## Resultados

| Capa | Comprobación |
|---|---|
| Servidor con PostgreSQL y buzón local | 431 aprobadas, 0 fallidas, 8 omitidas de Keycloak |
| Casos que fallaban al comparar ramas | 9/9 aprobados juntos; 7/7 casos límite adicionales |
| Web | 6 pruebas aprobadas; análisis y compilación correctos |
| Móvil | 34 pruebas aprobadas; análisis sin incidencias y APK construido |
| Flutter → API → PostgreSQL | Consumo único, inventario, movimiento, auditoría y rollback aprobados |
| Navegador real | 166 comprobaciones aprobadas; 152 corresponden a ocho perfiles por 19 rutas |

El recorrido no registró excepciones JavaScript ni respuestas 5xx. Se verificó que formularios abiertos antes de una desactivación rechacen ajustes, transferencias y recepciones sin modificar saldos. La plantilla creada para QA quedó inactiva. Las evidencias completas se conservan en el entorno de QA; este repositorio no distribuye cuentas, base de datos, capturas de QR vigentes ni configuraciones privadas.

## Repetición

- Backend: `bash backend/scripts/run-full-integration-tests.sh`, con Docker/.NET disponibles. Consultar las variables y requisitos del script para PostgreSQL, Mailpit y Keycloak; deshabilitar una integración no equivale a probarla.
- Web, desde `frontend`: `npm ci`, `npm test`, `npm run lint`, `npm run build`.
- Móvil, Flutter 3.47.2/Dart 3.13.2: seguir [mobile/README](../../mobile/README.md) para dependencias, pruebas, servidor y APK.
- E2E móvil: `bash backend/scripts/run-mobile-e2e.sh`; levanta API y PostgreSQL desechables. Sustituye identidad y entrada del escáner únicamente en el test.
- [Guía manual completa](07-GUIA-MANUAL-INTEGRACION.md): perfiles, reglas, límites, concurrencia, QR, exportaciones y dispositivos.

## Alcance de revisión

Dos commits dependientes: servidor/reglas/pruebas y clientes/documentación. Revisar y ejecutar el conjunto completo. El contrato `tipoCombustibleActivo` de main se mantiene; QR es firmado y limitado por permisos. No se añaden migraciones de base de datos. Los identificadores DEF del grupo se conservan; los hallazgos propios de la integración usan INT.

Quedan pendientes cámara/pistola/USB físicos, Keycloak nativo y SMS externo. Las 8 pruebas omitidas corresponden a Keycloak deshabilitado. El lanzador Windows tiene revisión estática. El APK QA es de depuración y no se publica como binario de distribución. Persisten avisos no bloqueantes de tamaño de bundle web y futura migración Kotlin del plugin de cámara. No se presenta esta revisión como certificación de producción.
