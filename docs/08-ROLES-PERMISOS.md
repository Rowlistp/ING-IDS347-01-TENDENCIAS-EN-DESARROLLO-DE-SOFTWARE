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
solicitudes y podrá operar sobre recursos propios cuando Fase 3 defina esa
relación. No son alias: el alcance exacto de ambos sigue pendiente de aprobación.

## 3. Matriz inicial

| Función | Admin | Supervisor | Despachador | Auditor | Consulta | Solicitante |
|---|---:|---:|---:|---:|---:|---:|
| Configurar sistema | Sí | No | No | No | No | No |
| Gestionar usuarios | Sí | No | No | No | No | No |
| Gestionar roles | Sí | No | No | No | No | No |
| Gestionar empleados | Sí | Según política | No | Lectura | Lectura pendiente | No |
| Gestionar vehículos | Sí | Según política | No | Lectura | Lectura pendiente | No |
| Gestionar departamentos | Sí | Según política | No | Lectura | Lectura pendiente | No |
| Crear solicitud | Según política | Sí | No | No | No | Sí |
| Aprobar solicitudes | Sí/según política | Sí | No | No | No | No |
| Emitir ticket | Sí/según política | Sí | No | No | No | No |
| Consultar ticket | Sí | Sí | Sí | Sí | Alcance pendiente | Propios |
| Escanear QR | No | Opcional | Sí | No | No | No |
| Registrar despacho | No | Según política | Sí | No | No | No |
| Registrar recepción | Sí | Sí | No | No | No | No |
| Ajustar inventario | Sí | Sí | No | No | No | No |
| Consultar inventario | Sí | Sí | Según necesidad | Sí | Alcance pendiente | No |
| Cierre diario | Según política | Sí | Sí | Lectura | Lectura pendiente | No |
| Consultar reportes | Sí | Sí | Limitado | Sí | Alcance pendiente | Limitado |
| Exportar reportes | Sí | Sí | No | Sí | No | No |
| Consultar auditoría | Sí | No | No | Sí | No | No |

## 4. Principio de mínimo privilegio

Cada rol debe recibir únicamente los permisos necesarios para su función.

## 5. Separación de funciones

Se recomienda evitar que un mismo rol pueda realizar todas las acciones sensibles sin control:

- Emitir ticket.
- Modificar inventario.
- Registrar despacho.
- Alterar auditoría.

## 6. Permisos sobre datos

Además del permiso de operación, puede ser necesario restringir el alcance:

- Solicitudes propias.
- Departamento propio.
- Estación asignada.
- Reportes globales o parciales.

El SRS no especifica estas restricciones, por lo que requieren aprobación.

## 7. Pendientes

- Alcance funcional exacto de `Consulta` y alcance de recursos propios de `Solicitante`.
- Permisos del Administrador sobre despachos.
- Alcance departamental.
- Permisos de anulación de tickets.
- Permisos sobre transferencias.
- Política de aprobación múltiple.
