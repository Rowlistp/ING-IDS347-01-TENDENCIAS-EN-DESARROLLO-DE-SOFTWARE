# 05 — Modelo de amenazas

## Activos
- Claves privadas.
- Licencias emitidas.
- Estados de licencia.
- Lista de revocación.
- Tokens de acceso.
- Base de datos.
- Claves públicas confiables.

## Amenazas principales
- Modificación de fecha de expiración.
- Cambio de usuario o producto.
- Cambio de permisos.
- Fabricación de licencias.
- Copia de licencias entre dispositivos.
- Replay de respuestas o tokens.
- Manipulación del reloj.
- Manipulación de archivos locales.
- Uso de licencia revocada.
- Robo de claves privadas.
- Sustitución de claves públicas.
- Manipulación de manifiestos de revocación.
- Cliente antiguo aceptando una política insegura.

## Resultado esperado
Cada amenaza debe producir un comportamiento conocido: rechazo, bloqueo temporal, necesidad de reconexión o evento auditable.

## No objetivo
No se promete seguridad absoluta contra un atacante con control total del sistema operativo y acceso físico ilimitado.
