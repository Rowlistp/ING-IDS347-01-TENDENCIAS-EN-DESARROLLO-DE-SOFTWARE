# 08 — Gestión de claves

## Regla principal
Las claves privadas nunca deben distribuirse al cliente.

## Servidor
Puede contener:
- clave privada clásica;
- clave privada postcuántica;
- metadatos de versión;
- key_id.

## Cliente
Contiene únicamente claves públicas confiables.

## Rotación
Ejemplo:
- KEY-2026-001
- KEY-2027-001

Las licencias indican qué `key_id` fue utilizado.

## Compromiso de clave
Debe existir un procedimiento para:
1. marcar la clave como comprometida;
2. impedir nuevas firmas;
3. generar nueva clave;
4. actualizar confianza del cliente;
5. reemitir licencias cuando sea necesario.

## Secretos
Nunca deben incluirse en:
- repositorio;
- frontend;
- logs;
- licencia.
