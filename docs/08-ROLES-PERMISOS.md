# 08 - Roles y Permisos

## 1. Objetivo

Definir una matriz inicial RBAC a partir de los actores y responsabilidades del SRS.

> La matriz es una propuesta técnica. El SRS define responsabilidades, pero no una tabla completa de permisos por operación.

## 2. Roles

- Administrador.
- Supervisor.
- Despachador.
- Auditor.
- Consulta.
- Solicitante.

`Consulta` es el rol mínimo de lectura definido por el SRS. `Solicitante` se
conserva como rol técnico distinto porque el actor aparece en los flujos de
solicitudes. En F4 consulta exclusivamente Tickets propios mediante
`Ticket.Empleado.UsuarioId`; un Ticket/PDF ajeno responde `404`. No son alias.

Este catálogo es cerrado. `GET /api/v1/roles` permite al Administrador consultar
los roles permitidos persistidos, pero no crearlos, renombrarlos ni eliminarlos.
Todo usuario debe conservar al menos un rol válido y único. Roles vacíos,
duplicados, desconocidos o externos a este catálogo se rechazan. En accesos OIDC,
los roles de Keycloak nunca conceden permisos: la API carga los roles locales de
PostgreSQL después de resolver al usuario activo.

## 3. Matriz consolidada de módulos y operaciones

| Módulo / Operación | Admin | Supervisor | Despachador | Auditor | Consulta | Solicitante |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| **Dashboard** (`/dashboard`) | Sí | No | No | No | No | No |
| **Usuarios** (`/usuarios`) | Sí | No | No | No | No | No |
| **Auditoría** (`/auditoria`) | Sí | No | No | Lectura | No | No |
| **Solicitudes** (`/solicitudes`) | Gestión | Aprob/Rech | No | Lectura | No | Crear / Propias |
| **Solicitudes recurrentes** (`/solicitudes-recurrentes`) | Sí | Sí | No | No | No | No |
| **Tickets** (`/tickets`) | Gestión/Emit | Emit/Anul | Lectura | Lectura | Lectura | Solo propios |
| **Despachos** (`/despachos`) | Oper/Lect | Oper/Lect | Lector QR/Oper | Lectura | Lectura | No |
| **Cierres diarios** (`/cierres-diarios`) | Gestión | Crear/Lect | Crear/Lect | Lectura | Lectura | No |
| **Inventario** (`/inventario`) | Ajustar/Lect | Ajustar/Lect | No | Lectura | Lectura | No |
| **Recepciones** (`/recepciones`) | Sí | Sí | No | No | No | No |
| **Tanques** (`/tanques`) | Sí | Sí | No | No | No | No |
| **Tipos combustible** (`/tipos-combustible`) | Sí | Sí | No | No | No | No |
| **Proveedores** (`/proveedores`) | Sí | Sí | No | No | No | No |
| **Estaciones** (`/estaciones`) | Sí (Desact) | Crear/Editar | Lectura | Lectura | Lectura | No |
| **Empleados** (`/empleados`) | Sí (Desact) | Crear/Editar | No | Lectura | No | No |
| **Vehículos** (`/vehiculos`) | Sí (Desact) | Crear/Editar | No | Lectura | No | No |
| **Departamentos** (`/departamentos`) | Sí (Desact) | Crear/Editar | No | Lectura | No | No |
| **Reportes** (`/reportes`) | Sí | Sí | No | Lectura/Desc | Lectura/Desc | No |
| **Notificaciones** (`/notificaciones`) | Sí | Sí | No | Lectura | No | No |

> Corregido 2026-09-18 tras contrastar contra `docs/SRS.md` (documento fuente)
> y el código real, porque `CierresDiariosController`/`ReportesController`
> (implementados el 06 y 17-sep) habían divergido de esta matriz sin
> actualizarla:
> - **Cierre diario**: el SRS (sección 3.3, Actores) asigna "Cierre diario"
>   explícitamente como responsabilidad del Despachador. El código lo excluía
>   por completo — eso era un bug del backend, no de esta tabla. Ya se corrigió
>   `CierresDiariosController` para incluir a Despachador (lectura y creación).
> - **Reportes**: el SRS no le da a Supervisor ninguna responsabilidad de
>   reportes (solo Auditor: "Exportar reportes"), así que restringir
>   `ReportesController` a Admin/Auditor sí calza con el documento fuente. Se
>   corrige la tabla, no el código.
>
> Antes de asumir cualquier otra celda de esta matriz como vigente, verificar
> contra el SRS y el `[Authorize]` real del controller: es un documento de
> planificación y ya divergió del código al menos en estos casos.

## 4. Principio de mínimo privilegio

Cada rol recibe exclusivamente los módulos y acciones requeridas para sus tareas operativas:
- **Administrador**: 19 módulos completos. Control del sistema, creación y desactivación de usuarios y entidades maestras.
- **Supervisor**: 16 módulos operativos y de abastecimiento. Aprobación y rechazo de solicitudes, emisión de tickets, recepción de combustible, ajustes de inventario y gestión de catálogos. Bloqueado de Dashboard, Usuarios y Auditoría.
- **Despachador**: 4 módulos de servicio en estación. Escáner de tickets QR, registro atómico de despachos y cierres diarios de turno.
- **Solicitante**: 2 módulos de autoservicio. Emisión de solicitudes de combustible y visualización de tickets propios (*"Mis Tickets"*).
- **Auditor**: 11 módulos de inspección y trazabilidad. Acceso 100% de solo lectura sin botones de modificación o acción destructiva.
- **Consulta**: 6 módulos de lectura de combustible. Histórico y reportes de combustible en solo lectura.

## 5. Separación técnica y arquitectónica

1. **Backend**: Filtros `[Authorize(Roles = ...)]` por controlador y endpoint específico. Validación de identidad y pertenencia `OwnerFilter` para Solicitantes en Tickets y Solicitudes.
2. **Frontend Router**: Componente `RoleRoute` interceptando navegación manual por URL hacia rutas no autorizadas con redirección a `getDefaultRouteForUser`.
3. **Navegación Visual**: `Sidebar` y `BottomNav` filtrados dinámicamente con `canAccessRoute`.
4. **Protección de Acciones en UI**: Columnas de acciones, botones de nuevo registro, edición y desactivación condicionados por capacidades operativas (`canManageCatalogs`, `canApproveRequests`, `canEmitTickets`, `canAdjustInventory`, `canCreateDailyClose`).

