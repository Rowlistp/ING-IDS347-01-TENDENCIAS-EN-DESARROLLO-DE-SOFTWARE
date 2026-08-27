# 13 — Modelo de datos

## Entidades

### User
- id
- external_reference
- status

### Product
- id
- name
- status

### License
- id
- user_id
- product_id
- status
- issued_at
- valid_from
- expires_at
- revoked_at
- revocation_reason
- signature_policy
- key_id

### Device
- id
- user_id
- fingerprint
- status

### KeyMetadata
- key_id
- algorithm
- created_at
- active
- compromised

### Revocation
- license_id
- revoked_at
- reason

### ValidationEvent
- id
- license_id
- timestamp
- mode
- result
- reason
- duration_ms

## Nota
El esquema final dependerá del stack y ORM seleccionados.
