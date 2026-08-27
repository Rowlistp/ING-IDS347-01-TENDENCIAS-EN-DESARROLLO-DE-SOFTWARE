# 17 — Escenarios de demostración

## Demo 1 — Crear licencia
Crear licencia nueva.
Resultado: `VALID`.

## Demo 2 — Alterar expiración
Editar manualmente `expires_at`.
Resultado: `INVALID_SIGNATURE`.

## Demo 3 — Offline válido
Desconectar Internet.
Validar licencia dentro del offline lease.
Resultado: acceso permitido.

## Demo 4 — Revocación
Revocar licencia en servidor mientras el cliente está offline.
Explicar que el cliente aún no conoce el cambio.

## Demo 5 — Reconexión
Reconectar.
Actualizar estado/manifiesto.
Resultado: `REVOKED`.

## Demo 6 — Copia
Copiar licencia a otro dispositivo si device binding está activo.
Resultado: `DEVICE_MISMATCH`.

## Demo 7 — Benchmark
Mostrar tiempos y tamaños de:
- ECDSA;
- ML-DSA;
- híbrido.

## Objetivo
La demo debe probar funcionalidad y también explicar limitaciones reales.
