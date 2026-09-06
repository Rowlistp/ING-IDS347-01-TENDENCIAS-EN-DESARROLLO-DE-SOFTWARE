# FuelTrack móvil — Fase 5

Android, Flutter **3.47.2**, Dart **3.13.2**. Requiere SDK Android y Java 21.
Desde mobile:

```sh
flutter pub get --enforce-lockfile
flutter analyze
flutter test
flutter build apk --debug
```

El APK sin defines acredita compilación y muestra configuración pendiente;
no contiene servidores/credenciales productivos. Ejemplo para emulador:

```sh
flutter run --dart-define=APP_ENV=development \
  --dart-define=API_BASE_URL=http://10.0.2.2:5000/api/v1 \
  --dart-define=OIDC_AUTHORITY=http://10.0.2.2:18080/realms/fueltrack \
  --dart-define=OIDC_CLIENT_ID=fueltrack-mobile \
  --dart-define=OIDC_REDIRECT_URI=fueltrack://callback
```

Ajustar direcciones al despliegue. El issuer anunciado por Keycloak y
Oidc:Authority de la API deben coincidir y ser accesibles desde ambos;
no mezclar issuer localhost con otro 10.0.2.2. En teléfono usar hostname/IP LAN,
nunca localhost del PC. Producción exige HTTPS. Cleartext solo Android debug
y APP_ENV=development. Cliente/callback deben coincidir con realm/manifiesto;
cambiarlos requiere revisión coordinada, no crear otro cliente.

OIDC abre navegador con PKCE S256. Requiere usuario Keycloak vinculado por
preferred_username a usuario FuelTrack activo con rol local Despachador.
No se usa login/password local de API ni client secret; tokens solo Secure Storage.
Un segundo401 o refresh fallido cierra sesión.

Flujo: login → Inicio → QR → datos Ticket → Continuar al despacho → estación/tanque
del API → galones/identidad → confirmar → resultado/Finalizar. Parcial consume
todo el Ticket. Sin conexión no confirma; respuesta perdida exige consulta.

## Persistencia real y gate físico

Desde raíz, Docker/.NET10/Flutter en PATH:

```sh
bash backend/scripts/run-full-integration-tests.sh
bash backend/scripts/run-mobile-e2e.sh
```

El segundo script levanta PostgreSQL efímero/API real, emite QR firmado, recorre
widgets y verifica consumo, stock, movimiento, auditoría y rollback. Login y
entrada scanner se sustituyen solo en el test: no acredita hardware/navegador
nativo. El protocolo PKCE móvil tiene test real independiente en Keycloak.
Fixtures/tokens efímeros nunca se versionan.

APK: build/app/outputs/flutter-apk/app-debug.apk; firma debug, no distribución.
CI publica artefacto sin defines: para ensayo operativo recompilar con URLs de
prueba. Checklist humano obligatorio antes de merge en docs/23.
