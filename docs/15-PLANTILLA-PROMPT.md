# Plantilla para armar tu prompt de Fase 0 (o cualquier fase)

Usa esta estructura como base al pedirle algo a Claude Code. Ajusta las secciones según tu módulo.

---

Voy a construir [TU PARTE: backend+datos / seguridad+tickets+móvil] del sistema descrito en docs/SRS.md. Antes de escribir código, LEE la documentación en docs/ para tener contexto completo, especialmente:
- docs/02-ARQUITECTURA.md
- docs/05-MODELO-DATOS.md (si es backend/datos)
- docs/06-API.md (contrato de endpoints – TODOS deben respetarlo)
- docs/07-SEGURIDAD.md (si es seguridad/auth)
- docs/13-DIVISION-EQUIPO.md y docs/14-REGLAS-TRABAJO-SIMULTANEO.md (mi rol y las reglas de colaboración)

CONTEXTO DE EQUIPO:
Somos 3 builders trabajando en paralelo sobre este mismo repo. Yo llevo [TU PARTE]. Un compañero ya completó el frontend (React + Tailwind, en main). Otro compañero lleva [LA OTRA PARTE QUE NO ES TUYA].

REGLAS A SEGUIR (según docs/14-REGLAS-TRABAJO-SIMULTANEO.md):
1. Antes de nada, crea (o cámbiate a) una rama llamada `feature/[NOMBRE-DE-TU-RAMA]` partiendo de `main`. Todo el trabajo debe hacerse en esa rama, NUNCA directo en main.
2. No modifiques nada dentro de frontend/ [ni de la otra carpeta que no es tuya], solo tu carpeta correspondiente (y docs/ si necesitas documentar algo de tu parte).
3. Si cambias o agregas algo al contrato de docs/06-API.md, avísamelo explícitamente al terminar para que yo se lo comunique al equipo.
4. Al terminar, haz commit en tu rama (NO push a main, NO merge automático) con un mensaje claro, y déjalo listo para que yo cree el Pull Request manualmente en GitHub.

STACK DEFINIDO:
[copia el stack que te corresponda desde docs/01-ALCANCE.md sección 6]

TAREA - Fase [NÚMERO] ([NOMBRE DE LA FASE, según docs/12-PLANIFICACION.md]):

[Describe aquí, en pasos numerados, exactamente qué necesitas que construya. Sé específico: qué carpetas, qué archivos, qué debe funcionar al final, y qué NO debe implementar todavía (para no adelantarte a fases futuras).]

Al terminar, confirma que el proyecto compila/corre sin errores, y haz commit en tu rama con un mensaje descriptivo del tipo "chore: [descripción] (Fase [N])".

---

## Notas importantes

- Revisa docs/12-PLANIFICACION.md para saber exactamente qué entra en tu fase y qué es de una fase posterior – no te adelantes a construir cosas que dependen de otro módulo que aún no existe.
- Revisa docs/13-DIVISION-EQUIPO.md para confirmar qué RF (requisitos funcionales) y qué fases son tuyas.
- Si tu módulo requiere algo del backend que aún no existe (o viceversa), coordina con el equipo antes de asumir un contrato que no está en docs/06-API.md.
