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
| Fase 1 | Auth JWT/OAuth2, RBAC, auditoría base | RF-01; RS-01, RS-02, RS-05 y base RS-06 |
| Fase 4 | Tickets + QR seguro, consulta propia Solicitante y cola lógica idempotente; transporte real en F9 | RF-06 a RF-10 (RF-09 parcial) |
| Fase 5 | Aplicación móvil Flutter | RF-12, RF-13 |
| Fase 9 | Outbox, SMTP/SMS, links seguros, alertas y REST transversal | RF-09, RF-23, RF-24 |

F9 entrega backend y pruebas locales de red. Tester valida proveedor real al
contar con credenciales. Builder1 conserva RF-11/reportes/dashboard/roles de
cierre; Builder3 frontend. Sus gaps se coordinan en doc.26, no se reescriben en F9.

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
| Fase 6-7 (frontend) | Pantallas de Inventario, Recepciones, Despachos y Cierre Diario | RF-14 a RF-18 |
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
- El frontend de Inventario/Recepciones/Despachos/Cierre Diario (RF-14 a RF-18) no tenía builder asignado; se resolvió asignándolo a Builder 3, ya que se conecta directamente con su Dashboard de Fase 8.
- Falta documento de cierre formal de Fase 6-7 (backend de Inventario), a diferencia de las demás fases que sí lo tienen.
- ~~RF-15 pide consumo diario y consumo mensual por tanque, pero no existe ningún endpoint que exponga esos datos agregados a nivel de tanque individual (solo agregados globales en el dashboard).~~ **RESUELTO:** `InventarioDto.cs` ya incluye `ConsumoDiario` y `ConsumoMensual` por tanque. Verificado en el código real durante la auditoría final de cierre del frontend.
- ~~Asimetría de validación en Recepciones: el backend valida TANQUE_INACTIVO al registrar una recepción, pero no valida que el Proveedor esté activo. Es posible registrar una recepción con un proveedor desactivado.~~ **RESUELTO:** `RecepcionesController.Create()` ahora valida `proveedor.Activo` y responde 400 con el código `PROVEEDOR_INACTIVO` antes de registrar la recepción. Verificado en el código real de `main` (`RecepcionesController.cs`).
- ~~Tanques y Vehículos no tienen forma de reactivarse una vez desactivados: `SaveTanqueRequest` y `SaveVehiculoRequest` no incluyen el campo `Activo`.~~ **RESUELTO:** ambos DTOs ahora incluyen `bool? Activo = null`, con default `null` (no `false`) para preservar el estado si el formulario no lo envía explícitamente — evita la regresión de desactivar por omisión que este mismo cambio podría haber introducido. Además, `TanquesController.Update()` y `VehiculosController.Update()` ahora validan dependencias (existencia > 0 / solicitudes activas del vehículo) antes de permitir la transición activo→inactivo por `PUT`, igual que ya hacía `DELETE`. Verificado en el código real de `main`.
- ~~Estaciones — asimetría de lectura: `GET /api/v1/estaciones` solo devolvía estaciones activas.~~ **RESUELTO:** `EstacionesController.GetAll()` ya no filtra por `Activo` — devuelve activas e inactivas, igual que el resto de catálogos. Verificado en el código real de `main`.
- ~~Estaciones — asimetría de validación: `PUT /api/v1/estaciones/{id}` podía desactivar una estación sin validar dependencias, a diferencia de `DELETE`.~~ **RESUELTO:** `EstacionesController.Update()` ahora valida `ESTACION_CON_DESPACHOS` (409 Conflict) cuando la transición es de activo a inactivo (`entity.Activo && !req.Activo`), igual que `DELETE`. Verificado en el código real de `main`.
- Empleados, Departamentos y Proveedores tienen el mismo patrón de riesgo que se corrigió para Estaciones/Tanques/Vehículos: `PUT` puede desactivar sin pasar por la misma validación de dependencias que aplica `DELETE` (`EmpleadosController.Update()`, `DepartamentosController.Update()` y `ProveedoresController.Update()` asignan `entity.Activo = req.Activo` directamente, sin la validación que sí tienen sus respectivos `Deactivate()`). No es explotable desde la interfaz actual, pero la brecha existe en el backend. Prioridad baja según Builder 1.
- No existe ningún endpoint para vincular `Empleado.UsuarioId` a un `Usuario` (el campo es `int? UsuarioId` en el modelo, pero ni `SaveEmpleadoRequest` ni `CreateUserRequest`/`UpdateUserRequest` lo exponen), lo que impide probar en vivo el escenario completo de Solicitante viendo solo sus propios registros. Confirmado durante las pruebas de RBAC del frontend: se pudo verificar que el listado degrada correctamente a "sin resultados" para un Solicitante sin empleado vinculado, pero no que el filtrado por dueño (`OwnerFilter`) devuelva los registros reales de ese usuario.
- ~~Gap RF-22: "Consumo por departamento" y "Consumo por vehículo" no tenían ningún endpoint agregado en el backend — solo existían como filas crudas en el pipeline de Reportes (RF-19/20).~~ **RESUELTO:** Builder 1 agregó `ConsumoPorDepartamento` y `ConsumoPorVehiculo` a `GET /api/v1/dashboard/resumen` (agrupación real de despachos por departamento/vehículo, ventana de 30 días, ordenado descendente, array vacío cuando no hay datos). Verificado en vivo contra el backend real y mediante `DashboardServiceTests`. Conectado en el Dashboard por Builder 3.
- ~~Gap de integridad de datos: Departamento.Deactivate() no valida si tiene empleados o vehículos activos asociados antes de desactivarse. Un departamento puede quedar inactivo mientras conserva personal y flota activa asignada, generando un estado de datos inconsistente. Confirmado en prueba real: se desactivó un departamento con 1 empleado y 1 vehículo activos sin ningún error del backend. Pendiente de decisión del equipo (Builder 1): bloquear la desactivación si hay dependientes activos, o permitirlo con advertencia.~~ **RESUELTO:** DepartamentosController.Deactivate() ahora valida empleados/vehículos activos y responde 409 Conflict con el código DEPARTAMENTO_CON_EMPLEADOS_ACTIVOS. Verificado en vivo.

- ~~Error 500 al actualizar empleados cambiando únicamente el departamento: `EmpleadosController.Update()` podía generar una excepción `NullReferenceException` cuando la propiedad de navegación `Departamento` quedaba desincronizada después de modificar `DepartamentoId`. El problema se reproducía al editar un empleado y cambiar solamente el departamento desde la interfaz.~~ **RESUELTO:** `EmpleadosController.Update()` ahora recarga explícitamente la referencia `Departamento` después de actualizar `DepartamentoId`, evitando utilizar una navegación desactualizada al construir la respuesta. Verificado en vivo: cambio únicamente de departamento, cambios combinados y actualización de otros campos. 
- ~~Gap de vinculación Empleado-Usuario: No existe ningún endpoint para vincular Empleado.UsuarioId a un Usuario — esto impide probar en vivo el escenario completo de un Solicitante viendo solo sus propios registros (el filtrado backend, OwnerFilter, ya existe, pero no hay forma de crear el vínculo Empleado↔️Usuario desde el producto para probarlo de punta a punta).~~ **RESUELTO:** Se implementó el endpoint `PUT /api/v1/empleados/{id}/vincular-usuario` (con validación de existencia de usuario, estado activo y bloqueo de duplicidad 409 `USUARIO_YA_VINCULADO`, permitiendo también desvincular con `null`), se extendió `SaveEmpleadoRequest` y `EmpleadoDto` para soportar `UsuarioId` y `UsuarioNombre`, se actualizó `UserService` para exponer `EmpleadoId`/`EmpleadoNombre`, se incorporó el filtrado de pertenencia `OwnerFilter` en `SolicitudesController` para el rol `Solicitante` impidiendo ver o crear solicitudes ajenas (403 `SOLICITANTE_EMPLEADO_NO_AUTORIZADO`), y se conectó la interfaz completa en `EmpleadosPage.jsx` (columna de usuario, asignación en modal y acción rápida de vinculación), `UsuariosPage.jsx` y `SolicitudesPage.jsx` / `TicketsPage.jsx` (detección automática y banners de contexto). 295/295 pruebas automatizadas y 0 errores de linter/build.
