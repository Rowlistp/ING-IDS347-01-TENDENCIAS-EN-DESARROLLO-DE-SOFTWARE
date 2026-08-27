# 10 — Validación offline

## Objetivo
Permitir uso controlado sin conexión.

## Datos disponibles en cliente
- licencia;
- claves públicas;
- política offline;
- último estado válido conocido;
- manifiesto de revocación previamente descargado.

## Flujo
1. Leer licencia.
2. Canonicalizar.
3. Verificar firmas.
4. Verificar expiración.
5. Verificar `offline_until`.
6. Verificar device binding.
7. Verificar información local de revocación.
8. Permitir o rechazar.

## Offline Lease
**Propuesta inicial:** permitir una ventana de 7 días sin conexión.

La duración final deberá ser configurable.

## Limitación
Un dispositivo completamente desconectado no puede conocer instantáneamente una revocación nueva del servidor.
