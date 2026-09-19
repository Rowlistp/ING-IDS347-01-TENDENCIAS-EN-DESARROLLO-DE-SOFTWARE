# Guía de Estándares: Convención de Commits y Documentación de Defectos (QA)

Este documento define la metodología oficial y los lineamientos técnicos aplicados en **FuelTrack ERP** para la realización de **commits en Git** y la **documentación de defectos/errores de QA**. Está diseñado para que cualquier integrante del equipo o agente de Inteligencia Artificial (IA) pueda replicar exactamente el mismo estándar de trabajo.

---

## 1. Convención de Commits (Conventional Commits)

### 1.1. Estructura General del Mensaje

Cada commit debe seguir la siguiente estructura:

```text
<tipo>(<alcance>): <descripción concisa del cambio>
```

- **`<tipo>`**: Categoría técnica del cambio (en minúsculas).
- **`(<alcance>)`**: Módulo o subsistema afectado (ej. `rbac`, `tanques`, `ui`, `notifications`, `tickets`, `auth`, `qa`).
- **`<descripción>`**: Explicación clara en **español**, en tiempo presente/imperativo, en minúsculas y sin punto final.

### 1.2. Tipos Permitidos

| Tipo | Propósito | Ejemplo |
| :--- | :--- | :--- |
| **`feat`** | Nueva funcionalidad o capacidad para el usuario. | `feat(tanques): permitir reactivacion de tanques inactivos y soporte en frontend` |
| **`fix`** | Corrección de un error o defecto funcional. | `fix(jwt): resolver redireccion al expirar token de sesion` |
| **`docs`** | Cambios exclusivamente en documentación (Markdown, specs). | `docs(rbac): documentacion de roles, permisos y separacion de modulos en docs/27` |
| **`refactor`** | Reestructuración de código sin alterar comportamiento externo. | `refactor(auth): extraer verificador de politicas de contrasena` |
| **`ui` / `style`**| Ajustes de diseño, estilos visuales, CSS o ergonomía. | `feat(ui): componentes responsivos, tabla adaptable y modal unificado` |
| **`test`** | Adición o corrección de pruebas unitarias o de integración. | `test(cierres): agregar validacion concurrente de cierre diario` |
| **`chore`** | Mantenimiento, dependencias o tareas de build. | `chore(deps): actualizar configuracion de bundler vite` |

### 1.3. Reglas de Oro para Commits

1. **Atomicidad estricta:** Un commit debe contener **una sola unidad lógica de trabajo**. Nunca mezclar cambios de backend con frontend no relacionados, ni mezclar correcciones de bugs con rediseños visuales.
2. **Puerta de Calidad previa (Pre-commit checklist):** Antes de ejecutar `git commit`, es **obligatorio** validar:
   - Backend: `dotnet test backend/FuelTrack.Api.Tests` (**100% de tests aprobados**).
   - Frontend: `npm run lint` (**0 errores**).
   - Frontend: `npm run build` (**compilación de producción exitosa**).
3. **Revisión de cambios:** Ejecutar `git status` y `git diff` antes de hacer `git add` para no incluir archivos temporales, logs o credenciales secretas.
4. **Comandos en Windows / PowerShell:**
   - Usar comillas simples para los mensajes de commit para evitar problemas de escape con caracteres especiales o signos de exclamación:
     ```powershell
     git add archivo1.cs archivo2.jsx
     git commit -m 'feat(modulo): descripcion clara del cambio'
     ```

---

## 2. Metodología de Documentación de Errores y Defectos (Defect Log QA)

Toda anomalía, vulnerabilidad o discrepancia detectada respecto a las especificaciones o al SRS debe registrarse en la bitácora técnica consolidada (`docs/qa/04-DEFECT-LOG.md`).

### 2.1. Nomenclatura de Defectos
- Formato: **`DEF-YYYY-NNN`** (ejemplo: `DEF-2026-001`, `DEF-2026-002`).

### 2.2. Niveles de Severidad y Prioridad

- **Severidad:**
  - **Crítica:** Fuga de seguridad, bloqueo de autenticación, corrupción o pérdida de datos.
  - **Mayor:** Flujo de negocio principal interrumpido sin alternativa inmediata.
  - **Media:** Defecto funcional con alternativa viable o validación de datos faltante.
  - **Menor:** Inconsistencia estética menor o mejora de redacción.
- **Prioridad:**
  - **P1 (Alta):** Debe corregirse de inmediato antes de cualquier release.
  - **P2 (Media):** Debe resolverse en la iteración o fase en curso.
  - **P3 (Baja):** Mejora postergable.

### 2.3. Estructura de Ficha Técnica de un Defecto

Cada entrada en la bitácora debe contener los siguientes 8 campos obligatorios:

```markdown
---
### DEF-2026-XXX: <Título Descriptivo del Defecto>
- **Severidad:** Crítica | Mayor | Media | Menor (Explicar impacto en una frase)
- **Prioridad:** P1 (Alta) | P2 (Media) | P3 (Baja)
- **Estado:** ✅ Resuelto | ⏳ En Progreso | ⚠️ Mitigado
- **Componente:** `<Ruta/archivo_backend.cs>`, `<Ruta/archivo_frontend.jsx>`
- **Descripción:** Detallar qué sucedía, qué acción provocaba el fallo y los códigos o mensajes de error exactos obtenidos.
- **Causa Raíz (RCA - Root Cause Analysis):** Explicar técnicamente por qué se producía el error en el código o arquitectura (no quedarse en el síntoma superficial).
- **Resolución / Acciones Correctivas:**
  1. Paso técnico 1 aplicado (especificando función o componente modificado).
  2. Paso técnico 2 aplicado.
  3. Validaciones preventivas agregadas.
- **Verificación:** Pruebas ejecutadas para comprobar la corrección (tests unitarios, pruebas manuales y linter).
```

---

## 3. Estructura de Documentación de Nuevas Funcionalidades (`docs/XX-...md`)

Cuando se implementa un módulo grande (como RBAC, Cierre Diario o Despachos), se crea un documento en `docs/` con el siguiente esquema:

1. **Contexto y Objetivos:** Referencia a los requisitos del SRS que cubre.
2. **Definiciones del Dominio:** Explicación funcional (roles, entidades, estados).
3. **Matriz Técnica:** Tablas comparativas (rutas vs roles, acciones vs permisos).
4. **Implementación Frontend:** Explicación de componentes, hooks y utilidades creadas.
5. **Implementación Backend:** Controladores, servicios, DTOs y atributos de autorización.
6. **Verificación de Calidad:** Evidencia de pruebas unitarias y compilación.

---

## 4. Prompt para el Asistente / IA del Compañero

Copia y pega el siguiente bloque en la IA de tu compañero para que adopte inmediatamente este comportamiento:

```markdown
Actúa como desarrollador senior y especialista en Aseguramiento de Calidad (QA) para el proyecto FuelTrack ERP.
Debes regirte estrictamente por las siguientes directrices de commits y documentación de defectos:

1. ESTÁNDAR DE COMMITS:
   - Sigue la especificación Conventional Commits en español: `<tipo>(<alcance>): <descripción>`
   - Tipos: `feat`, `fix`, `docs`, `refactor`, `ui`, `test`, `chore`.
   - Realiza commits atómicos: un solo cambio lógico por commit.
   - Antes de commitear, siempre ejecuta y verifica:
     a) `dotnet test backend/FuelTrack.Api.Tests` (100% aprobado).
     b) `npm run lint` (0 errores).
     c) `npm run build` (build de producción exitoso).

2. DOCUMENTACIÓN DE DEFECTOS (DEFECT LOG):
   - Cada bug o fallo resuelto debe documentarse con el código `DEF-2026-XXX`.
   - Estructura obligatoria:
     - Código y Título
     - Severidad y Prioridad
     - Estado y Componentes afectados
     - Descripción detallada con mensajes/códigos de error
     - Causa Raíz (RCA - Root Cause Analysis a nivel de código)
     - Resolución paso a paso
     - Verificación realizada

3. DOCUMENTACIÓN ARQUITECTÓNICA:
   - Toda nueva funcionalidad debe actualizar la matriz en `docs/08-ROLES-PERMISOS.md` o generar un documento en `docs/` detallando componentes de backend, frontend y pruebas.
   - Nunca expongas términos técnicos de depuración en la interfaz del usuario final.
```
