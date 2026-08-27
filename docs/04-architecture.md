# 04 — Arquitectura

## Vista general

```text
Administrador
    |
    v
API de Licencias
    |
    +--> Base de datos
    |
    +--> Motor criptográfico
    |       +--> ECDSA Provider
    |       +--> ML-DSA Provider
    |
    +--> Servicio de revocación

Cliente
    |
    +--> Validación Online --> API
    |
    +--> Validación Offline --> Claves públicas + política offline
```

## Componentes

### API de licencias
Gestiona creación, consulta, validación, suspensión, revocación y renovación.

### Motor criptográfico
Encapsula generación de claves, firma y verificación.

### Motor de licencias
Aplica reglas de estado, expiración, usuario, dispositivo y política offline.

### Servicio de revocación
Mantiene revocaciones y manifiestos firmados.

### Cliente validador
Ejecuta validación online u offline.

## Principio
La API no debe depender directamente de un algoritmo concreto. Se usará una abstracción tipo `SignatureProvider`.
