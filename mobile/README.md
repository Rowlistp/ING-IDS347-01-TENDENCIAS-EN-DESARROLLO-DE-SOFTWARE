# FuelTrack móvil

Flutter **3.47.2**, Dart **3.13.2**, Java 21 y SDK Android. Desde `mobile`:

```sh
flutter pub get --enforce-lockfile
flutter analyze
flutter test
```

## Acceso local para pruebas

La aplicación solicita usuario y contraseña reales de FuelTrack. No incluye cuentas ni claves, no crea usuarios al fallar y no inventa estaciones o tanques.

Emulador Android, API de desarrollo en el puerto 5298 del equipo:

```sh
flutter run --dart-define=APP_ENV=development \
  --dart-define=AUTH_MODE=local \
  --dart-define=API_BASE_URL=http://10.0.2.2:5298/api/v1
```

Teléfono conectado por USB, con depuración autorizada:

```sh
adb reverse tcp:5298 tcp:5298
flutter run --dart-define=APP_ENV=development \
  --dart-define=AUTH_MODE=local \
  --dart-define=API_BASE_URL=http://127.0.0.1:5298/api/v1
```

En la copia QA de este proyecto la API usa **5300** y la web **5175**. El APK de entrega se compiló para `http://127.0.0.1:5300/api/v1`, autenticación local y desarrollo. Para ese APK, usar `adb reverse tcp:5300 tcp:5300`. Es una compilación de depuración; no un paquete firmado para distribución. El lanzador Windows usa los puertos de desarrollo 5298/5173 y requiere configuración previa de base de datos y autenticación local.

## Keycloak

`AUTH_MODE=keycloak` es el valor predeterminado. Para ese entorno configurar:

```sh
flutter run --dart-define=APP_ENV=development \
  --dart-define=AUTH_MODE=keycloak \
  --dart-define=API_BASE_URL=http://10.0.2.2:5298/api/v1 \
  --dart-define=OIDC_AUTHORITY=http://10.0.2.2:18080/realms/fueltrack \
  --dart-define=OIDC_CLIENT_ID=fueltrack-mobile \
  --dart-define=OIDC_REDIRECT_URI=fueltrack://callback
```

Ajustar direcciones al despliegue. El issuer anunciado por Keycloak y `Oidc:Authority` de la API deben coincidir y ser accesibles desde ambos. No mezclar issuer localhost con otro 10.0.2.2. OIDC usa navegador y PKCE S256; requiere usuario externo vinculado por `preferred_username` a un usuario FuelTrack activo. No utiliza client secret.

La URL y el modo se fijan al compilar; las sesiones se aíslan por servidor/modo. Tokens en almacenamiento seguro, renovación coordinada y cierre ante renovación rechazada. Producción exige HTTPS. HTTP solo se permite en desarrollo y compilación no release. Compilar sin defines muestra configuración pendiente.

## Flujo y verificación

Acceso → Inicio → escaneo → validación de ticket → tanque/estación compatibles → cantidad → confirmación → comprobante. La entrada manual requiere el **contenido firmado completo FTQR1**; un número de ticket o GUID no autoriza despacho. Un despacho parcial consume el ticket completo. Sin conexión no se confirma; ante respuesta incierta se consulta antes de repetir.

Las fuentes Public Sans e IBM Plex Mono vienen incluidas en `assets/fonts`, con licencias y huellas; no se descargan al abrir la aplicación.

Desde la raíz del repositorio, Docker/.NET 10/Flutter disponibles:

```sh
bash backend/scripts/run-mobile-e2e.sh
```

El script usa PostgreSQL efímero y API real: emite QR firmado, recorre widgets y comprueba consumo único, inventario, movimiento, auditoría y rollback. Solo identidad y entrada del escáner se sustituyen en el test; no acredita cámara ni navegador nativo.

Verificación del 20/09/2026: **34 pruebas móviles aprobadas**, análisis sin incidencias, APK construido y E2E Flutter → API → PostgreSQL aprobado. Quedan por ejecutar cámara/USB y acceso Keycloak con dispositivos y servicios reales. El lanzador Windows se revisó estáticamente, no se ejecutó en Windows. Guía de entrega: [integración](../docs/qa/07-GUIA-MANUAL-INTEGRACION.md).

## Preparación de la demostración (22/09/2026)

La revisión en Pixel 8 emulado verificó login local y detectó/corrigió un aviso que tapaba el escáner. También corrigió la liberación prematura del controlador del diálogo de ingreso manual de QR. La nueva prueba reproduce el error previo y verifica su cierre correcto. El emulador no sustituye las pruebas de cámara física o el envío SMS por SIM. Ver [guion](../docs/presentacion/GUION-DEMO.md) y [matriz SRS](../docs/presentacion/MATRIZ-SRS.md).
