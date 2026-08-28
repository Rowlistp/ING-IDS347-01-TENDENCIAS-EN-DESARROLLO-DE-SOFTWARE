# 14 - Reglas de Trabajo Simultáneo

## 1. Objetivo

Definir cómo los 3 builders pueden trabajar en paralelo desde el día 1 sin pisarse el código ni bloquearse entre sí.

## 2. Ramas separadas por builder

- Cada builder crea su rama desde `main`:
  - `feature/backend-datos`
  - `feature/seguridad-movil`
  - `feature/frontend`
- Nadie hace push directo a `main` – todo entra por Pull Request.
- El dueño de cada capa es quien hace merge de su propia rama (evita pisar cambios ajenos).

Como cada builder toca carpetas distintas (`backend/`, `frontend/`, `mobile/`), los conflictos reales de código serán mínimos. El riesgo está en archivos compartidos (README raíz, `.gitignore`, `docker-compose`, si se usa).

## 3. El contrato de API manda

Este es el único punto real de acoplamiento entre los tres:

- Antes de escribir un endpoint nuevo, revisar si ya está en `06-API.md`; si no está, agregarlo ahí primero.
- Si alguien necesita un endpoint que aún no existe, puede mockearlo temporalmente sin esperar a que el backend lo tenga listo.
- Cualquier cambio al contrato de API se avisa al equipo antes de implementarlo, no se hace por sorpresa.

Mientras todos respeten las rutas y formatos ya definidos, el frontend puede avanzar sin depender de que el backend tenga todo terminado.

## 4. Sincronización liviana, no constante

- Sync corto (15 min) al cerrar cada fase propia – no hace falta diario si no es necesario.
- Usar Issues de GitHub o un board simple para marcar en qué fase está trabajando cada quien.
- Los 3 testers prueban apenas una fase se sube a `main`, antes de que el siguiente módulo dependiente arranque sobre ella.

## 5. Resumen rápido

| Regla | Por qué |
|---|---|
| Rama propia por builder | Evita pisar código ajeno |
| PR obligatorio a main | Nadie rompe el trabajo de otro sin revisión |
| Respetar 06-API.md | Es el contrato que conecta backend, frontend y móvil |
| Avisar cambios de contrato | Evita que otro builder trabaje sobre algo desactualizado |
| Sync al cerrar fase, no diario | Suficiente para mantenerse alineados sin perder tiempo |
| Testers prueban apenas se sube a main | Detecta problemas antes de que otra fase dependa de una rota |
