# 14 — Estrategia de pruebas

## Unitarias
- canonicalización;
- firma;
- verificación;
- expiración;
- transición de estados;
- device binding;
- offline lease.

## Integración
- API + motor criptográfico;
- API + base de datos;
- cliente + API;
- cliente + validador offline.

## Negativas
Modificar:
- user_id;
- product_id;
- expires_at;
- features;
- device_binding;
- key_id;
- firma.

Resultado esperado: rechazo.

## Corrupción
- JSON inválido;
- licencia truncada;
- firma vacía;
- campo obligatorio ausente;
- versión desconocida;
- algoritmo desconocido.

## Offline
- licencia válida sin Internet;
- offline lease vencido;
- licencia manipulada;
- rollback de reloj;
- revocación mientras el cliente está desconectado.

## Criterio
Las pruebas deben comprobar tanto el resultado como el código de error esperado.
