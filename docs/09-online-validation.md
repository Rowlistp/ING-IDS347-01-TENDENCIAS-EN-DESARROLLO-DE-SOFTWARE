# 09 — Validación online

## Flujo
1. Cliente presenta licencia.
2. API valida estructura.
3. Se canonicaliza el payload.
4. Se verifican firmas.
5. Se consulta estado.
6. Se valida usuario.
7. Se verifica expiración.
8. Se verifica device binding si aplica.
9. Se verifica revocación.
10. Se emite autorización temporal.

## Resultados
- VALID
- INVALID_SIGNATURE
- EXPIRED
- REVOKED
- SUSPENDED
- USER_MISMATCH
- DEVICE_MISMATCH
- UNSUPPORTED_VERSION
- UNKNOWN_KEY

## Acceso
Una validación correcta debe producir un token temporal, no una URL secreta permanente.
