# 17 - Pruebas de Fase 1: Seguridad

## Objetivo

Registrar las pruebas automatizadas incorporadas por Builder 2 para la Fase 1 de seguridad.

## Alcance actual

Las pruebas cubren:

- hashing y verificación de contraseñas;
- uso de salt diferente para la misma contraseña;
- rechazo de contraseñas incorrectas y hashes inválidos;
- generación de JWT con identidad y roles;
- generación aleatoria de refresh tokens;
- hash SHA-256 de refresh tokens antes de persistencia;
- catálogo de roles base;
- rechazo de login para usuarios desactivados;
- rotación de refresh token;
- rechazo de reutilización del refresh token anterior.

## Hardening de sesiones

La rotación de refresh token utiliza una actualización condicional atómica.

El token anterior solo puede ser reclamado si:

- existe;
- no fue revocado;
- no está vencido.

La actualización de revocación, creación del token reemplazo y auditoría se ejecutan dentro de una transacción de base de datos.

Si dos solicitudes intentan usar el mismo refresh token, solamente la que consiga actualizar la fila activa puede completar la rotación.

## Comandos

Desde `backend/`:

```powershell
dotnet restore .\FuelTrack.slnx
dotnet build .\FuelTrack.slnx --no-restore
dotnet test .\FuelTrack.slnx --no-build
```

## Criterio antes del PR

La Fase 1 no debe enviarse a `main` si:

- la solución no compila;
- existe una prueba fallida;
- `git diff --check` reporta errores;
- la rama está detrás de `main` y requiere integración.
