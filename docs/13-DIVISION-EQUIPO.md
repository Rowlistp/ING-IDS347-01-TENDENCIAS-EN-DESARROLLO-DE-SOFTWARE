# 13 - División de Trabajo del Equipo

## 1. Objetivo

Definir cómo se distribuye el desarrollo entre los 3 integrantes que construyen con IA, alineado con las fases y dependencias ya establecidas en `12-PLANIFICACION.md`.

## 2. Composición del equipo

- **6 integrantes** en total.
- **3 builders**: construyen el sistema apoyándose en IA (Claude Code u otra).
- **3 testers**: prueban cada fase entregada antes de avanzar a la siguiente.

## 3. Criterio de asignación

La división es **por capa técnica**, alineada a las fortalezas de cada builder, para minimizar fricción y permitir trabajo en paralelo desde la Fase 0 sin bloquearse entre sí.

## 4. Asignación

### Builder 1 – Backend + Datos

**Perfil:** fuerte en base de datos.

**Responsabilidades:**
- Modelo de datos real en PostgreSQL (basado en `05-MODELO-DATOS.md`).
- DbContext, entidades y migraciones con Entity Framework Core.
- Lógica de negocio del backend para catálogos e inventario.

**Fases / Requisitos a cargo:**
| Fase | Contenido | Requisitos |
|---|---|---|
| Fase 0 | DbContext, entidades, migraciones iniciales | – |
| Fase 2 | Catálogos: empleados, vehículos, departamentos | RF-02, RF-03, RF-04 |
| Fase 3 | Solicitudes de combustible | RF-05, RF-11 |
| Fase 6-7 | Inventario completo + cierre diario | RF-14 a RF-18 |
| Fase 8 | Reportes y exportación | RF-19, RF-20 |

---

### Builder 2 – Seguridad + Tickets/QR + Móvil

**Perfil:** experto en uso de IA en general.

**Responsabilidades:**
- Autenticación, autorización y auditoría.
- Lógica criptográfica de tickets y QR.
- Aplicación móvil completa en Flutter.
- Integraciones externas (SMTP, SMS).

**Fases / Requisitos a cargo:**
| Fase | Contenido | Requisitos |
|---|---|---|
| Fase 1 | Auth JWT/OAuth2, RBAC, auditoría base | RF-01 |
| Fase 4 | Tickets + QR seguro (hash, firma digital) | RF-06 a RF-10 |
| Fase 5 | Aplicación móvil Flutter | RF-12, RF-13 |
| Fase 9 | Notificaciones e integraciones (SMTP/SMS) | RF-23, RF-24 |

---

### Builder 3 – Frontend + Dashboard + UX

**Perfil:** fuerte en frontend.

**Responsabilidades:**
- Interfaz web completa en React + Tailwind CSS.
- Consumo de la API expuesta por el backend.
- Dashboard ejecutivo y vistas de reportes.

**Fases / Requisitos a cargo:**
| Fase | Contenido | Requisitos |
|---|---|---|
| Fase 0 | Setup de React + Tailwind, estructura de rutas/componentes | – |
| Fase 1-3 | Pantallas de login, usuarios, catálogos, solicitudes | RF-01 a RF-05 |
| Fase 4 | Vista de tickets (emisión, estados, QR visual) | RF-06 a RF-10 |
| Fase 8 | Dashboard ejecutivo + vistas de reportes | RF-22 |

## 5. Por qué esta división

- Coincide con el orden de fases ya definido en `12-PLANIFICACION.md`: nadie queda bloqueado esperando a otro, todos pueden arrancar en paralelo desde la Fase 0/1.
- Cada builder lleva su capa de principio a fin, evitando que varias personas toquen el mismo código al mismo tiempo.
- Los módulos de mayor riesgo o complejidad técnica (autenticación, criptografía del QR) quedan con quien más domina el uso de IA, ya que probablemente requieran más iteración.
- El frontend puede avanzar con datos simulados (mocks) mientras el backend expone endpoints reales, sin quedar detenido.

## 6. Punto único de sincronización

Todo el equipo debe respetar desde el día 1 el contrato definido en `06-API.md` (rutas, formatos de request/response, códigos de error). Es el único acoplamiento fuerte entre los tres builders – cualquier cambio a ese contrato debe comunicarse al equipo antes de implementarlo.

## 7. Rol de los testers

Después de cada fase entregada por cualquiera de los tres builders, los 3 testers deben:
- Verificar que el módulo cumple los requisitos funcionales asociados (según `03-REQUISITOS.md`).
- Ejecutar los casos de prueba correspondientes a esa fase (según `10-PLAN-PRUEBAS.md`).
- Reportar hallazgos antes de que el equipo avance a la siguiente fase dependiente.

## 8. Pendiente de definir

- Si Builder 2 necesita apoyo puntual de otro builder para la app móvil, dado que concentra varios módulos de alta complejidad (seguridad + QR + Flutter).
- Frecuencia de sincronización entre builders (ej. daily corto, o solo al cerrar cada fase).
- Herramienta de gestión de tareas (issues de GitHub, Trello, etc.) para dar seguimiento visual a esta división.
- La Fase 3 (Solicitudes, RF-05/RF-11) no tenía builder asignado explícitamente; se resolvió asignándola a Builder 1 junto con el resto del backend.
- RF-21 (Trazabilidad, actor Auditor) no aparece asignado a ningún builder ni en ninguna fase de `12-PLANIFICACION.md`. Pendiente de que el equipo decida a quién corresponde.
- RF-22 (Dashboard ejecutivo): el backend (endpoint `GET /api/v1/dashboard/summary` y su lógica) no está asignado a ningún builder, aunque el frontend sí lo tiene Builder 3 en Fase 8. Pendiente de que el equipo decida a quién corresponde.
