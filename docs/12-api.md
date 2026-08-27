# 12 — API inicial

## Endpoints propuestos

### Licencias
- `POST /api/licenses`
- `GET /api/licenses/{id}`
- `POST /api/licenses/{id}/validate`
- `POST /api/licenses/{id}/revoke`
- `POST /api/licenses/{id}/suspend`
- `POST /api/licenses/{id}/renew`

### Revocación
- `GET /api/revocations`

### Acceso
- `POST /api/access-token`

## Respuesta de validación
Ejemplo:

```json
{
  "valid": false,
  "code": "LIC-003",
  "reason": "EXPIRED"
}
```

## Códigos internos iniciales
- LIC-001 INVALID_FORMAT
- LIC-002 INVALID_SIGNATURE
- LIC-003 EXPIRED
- LIC-004 REVOKED
- LIC-005 SUSPENDED
- LIC-006 USER_MISMATCH
- LIC-007 DEVICE_MISMATCH
- LIC-008 OFFLINE_LEASE_EXPIRED
- LIC-009 UNKNOWN_KEY
- LIC-010 UNSUPPORTED_VERSION
