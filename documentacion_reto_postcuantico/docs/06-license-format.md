# 06 — Formato de licencia

## Estructura propuesta

```json
{
  "payload": {
    "license_version": 1,
    "license_id": "LIC-...",
    "user_id": "USR-...",
    "product_id": "PRD-...",
    "issued_at": "...",
    "valid_from": "...",
    "expires_at": "...",
    "offline_until": "...",
    "status_hint": "ACTIVE",
    "device_binding": "...",
    "features": [],
    "key_id": "KEY-2026-001",
    "signature_policy": "HYBRID_V1"
  },
  "signatures": {
    "classic": "...",
    "post_quantum": "..."
  }
}
```

## Regla de firma
Se firma únicamente el `payload` canonicalizado.

## Canonicalización
**Propuesta:** utilizar JSON Canonicalization Scheme (JCS / RFC 8785) o una representación determinista equivalente.

## Regla crítica
Dos implementaciones deben producir exactamente los mismos bytes antes de firmar.

## Inmutabilidad
Modificar cualquier campo protegido obliga a emitir una nueva licencia y nuevas firmas.
