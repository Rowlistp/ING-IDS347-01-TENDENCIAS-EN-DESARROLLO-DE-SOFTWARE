# 11 — Revocación

## Tipos
### Manual
Ejecutada por administrador.

### Automática
Ejecutada por reglas como expiración.

## Estados
- ACTIVE
- SUSPENDED
- EXPIRED
- REVOKED

## Manifiesto de revocación
Propuesta:

```json
{
  "version": 1,
  "generated_at": "...",
  "next_update": "...",
  "revoked_license_ids": ["LIC-..."],
  "signature": "..."
}
```

El manifiesto debe estar firmado.

## Revocación offline
La revocación será efectiva en el cliente:
- inmediatamente si está online;
- al actualizar el manifiesto;
- o al expirar el offline lease.

## Suspensión vs revocación
- Suspensión: reversible.
- Revocación: definitiva.
