# Ejecutar FuelTrack localmente desde GitHub

El alojamiento opcional no cambia este modo de ejecución. La web, API, base PostgreSQL, usuarios, inventario y claves de este entorno son independientes de Render y Neon. El código de aplicación es el mismo; la configuración se entrega al arrancar.

## Requisitos

Docker con Compose v2, Python 3 y OpenSSL. La primera compilación descarga imágenes y dependencias; después del arranque las operaciones locales no necesitan Render ni Neon.

Desde la raíz del repositorio:

```sh
python3 infra/local/setup.py
docker compose --env-file .env.local -f compose.local.yaml up --build -d
```

Abrir **http://127.0.0.1:5351**. Usuario inicial: `local.admin`. La contraseña está en `LOCAL_ADMIN_PASSWORD` dentro de `.env.local`. Cada clonación genera claves nuevas. Este archivo queda excluido de Git y de la imagen del contenedor; conservarlo entre reinicios y no compartirlo públicamente.

El primer arranque aplica las migraciones y crea el administrador. Desde Usuarios se crean las cuentas de los otros cinco roles. El entorno no inserta empleados ni tickets reales automáticamente. Los datos ficticios usados para ensayar en el equipo del autor no se incluyen en Git.

Para comprobar el arranque:

```sh
curl --fail http://127.0.0.1:5351/healthz
docker compose --env-file .env.local -f compose.local.yaml ps
```

Para detener y volver a iniciar conservando los datos:

```sh
docker compose --env-file .env.local -f compose.local.yaml stop
docker compose --env-file .env.local -f compose.local.yaml start
```

El volumen `fueltrack-local_local-data` conserva PostgreSQL. No usar `down -v` si se desea conservar la información. Para otro puerto, editar `LOCAL_PORT` en `.env.local` antes de arrancar.

## Android local

Compilar desde `mobile/` con Flutter y el SDK Android:

```sh
flutter build apk --debug --dart-define=APP_ENV=development --dart-define=AUTH_MODE=local --dart-define=API_BASE_URL=http://127.0.0.1:5351/api/v1
adb reverse tcp:5351 tcp:5351
adb install -r build/app/outputs/flutter-apk/app-debug.apk
```

La redirección USB permite que el dispositivo alcance el servidor del equipo. Para un emulador puede utilizarse alternativamente `http://10.0.2.2:5351/api/v1` con una publicación de puerto accesible para el emulador. El APK online es una compilación separada; su URL no se guarda como valor por defecto del código móvil.

## Desarrollo con recarga de código

El modo Compose anterior ejecuta el contenedor completo. Para editar la web y API por separado, usar PostgreSQL local, configurar las claves con `dotnet user-secrets`, aplicar migraciones y arrancar la API en el puerto que define la plantilla del frontend:

```sh
dotnet run --project backend/FuelTrack.Api --urls http://localhost:5000
```

En `frontend/`, copiar `.env.example` a `.env.local`, instalar con `npm ci` y ejecutar `npm run dev -- --port 5173`. La plantilla usa `VITE_API_URL=http://localhost:5000/api/v1`. No copiar la conexión de Neon para desarrollar.

## Comunicaciones y seguridad del entorno

El entorno local de Compose usa autenticación por usuario y contraseña; las notificaciones externas están desactivadas inicialmente. Keycloak y los transportes locales de correo se configuran por separado según `infra/keycloak/README.md` e `infra/notifications/README.md`. HTTP sobre loopback facilita el desarrollo; no acredita TLS ni cifrado del disco. Los requisitos de producción se evalúan en la matriz SRS.
