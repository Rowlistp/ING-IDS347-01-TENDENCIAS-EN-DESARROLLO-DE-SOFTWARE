# 08 - Roles y Permisos

## 1. Objetivo

Definir una matriz inicial RBAC a partir de los actores y responsabilidades del SRS.

> La matriz es una propuesta técnica. El SRS define responsabilidades, pero no una tabla completa de permisos por operación.

## 2. Roles

- Administrador.
- Supervisor.
- Despachador.
- Auditor.
- Solicitante/Consulta.

## 3. Matriz inicial

| Función | Admin | Supervisor | Despachador | Auditor | Solicitante/Consulta |
|---|---:|---:|---:|---:|---:|
| Configurar sistema | Sí | No | No | No | No |
| Gestionar usuarios | Sí | No | No | No | No |
| Gestionar roles | Sí | No | No | No | No |
| Gestionar empleados | Sí | Según política | No | Lectura | No |
| Gestionar vehículos | Sí | Según política | No | Lectura | No |
| Gestionar departamentos | Sí | Según política | No | Lectura | No |
| Crear solicitud | Según política | Sí | No | No | Sí |
| Aprobar solicitudes | Sí/según política | Sí | No | No | No |
| Emitir ticket | Sí/según política | Sí | No | No | No |
| Consultar ticket | Sí | Sí | Sí | Sí | Propios |
| Escanear QR | No | Opcional | Sí | No | No |
| Registrar despacho | No | Según política | Sí | No | No |
| Registrar recepción | Sí | Sí | No | No | No |
| Ajustar inventario | Sí | Sí | No | No | No |
| Consultar inventario | Sí | Sí | Según necesidad | Sí | No |
| Cierre diario | Según política | Sí | Sí | Lectura | No |
| Consultar reportes | Sí | Sí | Limitado | Sí | Limitado |
| Exportar reportes | Sí | Sí | No | Sí | No |
| Consultar auditoría | Sí | Limitado | No | Sí | No |

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

- Diferencia exacta entre rol "Consulta" y actor "Solicitante".
- Permisos del Administrador sobre despachos.
- Alcance departamental.
- Permisos de anulación de tickets.
- Permisos sobre transferencias.
- Política de aprobación múltiple.
