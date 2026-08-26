# 16 — Limitaciones de seguridad

## Revocación offline
No existe revocación instantánea en un cliente completamente desconectado. Se mitiga con offline lease y manifiestos firmados.

## Control total del cliente
Un atacante con privilegios elevados puede intentar modificar:
- reloj;
- archivos;
- binarios;
- memoria;
- configuración.

El sistema debe aumentar el costo del ataque y detectar manipulación, pero el MVP no promete resistencia absoluta contra un host totalmente comprometido.

## Device binding
Puede reducir copias, pero también puede causar falsos bloqueos después de cambios legítimos de hardware.

## Claves privadas
La seguridad global depende fuertemente de protegerlas.

## Dependencia de librerías
La implementación criptográfica debe usar librerías mantenidas y evitar algoritmos escritos manualmente.

## Compatibilidad
Soportar clientes antiguos puede obligar a mantener políticas más débiles durante una transición. Esto debe controlarse mediante versionado explícito.
