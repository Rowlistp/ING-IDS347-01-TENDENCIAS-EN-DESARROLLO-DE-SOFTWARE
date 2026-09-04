# 03 - Requisitos del Sistema

## 1. Objetivo

Organizar los requisitos del SRS para facilitar trazabilidad, diseño, implementación y pruebas.

## 2. Requisitos funcionales

| ID | Requisito | Actor principal | Prioridad |
|---|---|---|---|
| RF-01 | Gestión de usuarios | Administrador | Alta |
| RF-02 | Gestión de empleados | Administrador | Alta |
| RF-03 | Gestión de vehículos | Administrador | Alta |
| RF-04 | Gestión de departamentos | Administrador | Media |
| RF-05 | Solicitudes de combustible | Solicitante / Supervisor | Alta |
| RF-06 | Emisión de tickets digitales | Supervisor | Alta |
| RF-07 | QR seguro | Sistema | Alta |
| RF-08 | Numeración de tickets | Sistema | Alta |
| RF-09 | Envío de tickets | Sistema | Media |
| RF-10 | Consulta de estado | Varios | Alta |
| RF-11 | Asignaciones manuales/automáticas | Supervisor | Media |
| RF-12 | Despacho de combustible | Despachador | Alta |
| RF-13 | Aplicación móvil para despacho | Despachador | Alta |
| RF-14 | Control de inventario | Supervisor / Administrador | Alta |
| RF-15 | Inventario en tiempo real | Administrador / Supervisor | Alta |
| RF-16 | Recepción de combustible | Supervisor | Alta |
| RF-17 | Movimientos de inventario | Supervisor / Auditor | Alta |
| RF-18 | Cierre diario | Despachador / Supervisor | Alta |
| RF-19 | Reportes | Auditor / Administración | Media |
| RF-20 | Exportación de reportes | Auditor / Administración | Media |
| RF-21 | Trazabilidad | Auditor | Alta |
| RF-22 | Dashboard ejecutivo | Administración | Media |
| RF-23 | Notificaciones | Sistema | Media |
| RF-24 | API REST | Sistema / Integraciones | Alta |

> La prioridad es una propuesta de planificación del equipo, no una clasificación incluida explícitamente en el SRS.

## 3. Requisitos de seguridad

| ID | Requisito |
|---|---|
| RS-01 | Autenticación por usuario/contraseña, MFA opcional y gestión de sesiones |
| RS-02 | RBAC |
| RS-03 | TLS 1.3 en tránsito y AES-256 en reposo |
| RS-04 | QR con firma digital, SHA-256 y token |
| RS-05 | OAuth 2.0 + JWT |
| RS-06 | Auditoría inalterable de accesos, cambios, despachos y ajustes |

## 4. Dependencias principales

- RF-06 depende de RF-05.
- RF-07 y RF-08 forman parte de la emisión segura del ticket.
- RF-09 depende de RF-06.
- RF-12 depende de RF-07 y RF-10.
- RF-13 consume los servicios necesarios para RF-12.
- RF-14 se actualiza con RF-12 y RF-16.
- RF-17 registra las operaciones de RF-14.
- RF-18 depende de los despachos e inventario del día.
- RF-19 y RF-20 dependen de los datos generados por los procesos anteriores.
- RF-21 es transversal.
- RF-23 depende de eventos del sistema.
- RF-24 es transversal para clientes e integraciones.

## 5. Criterios de aceptación trazables

| Criterio SRS | Requisitos relacionados |
|---|---|
| QR único sin duplicidad | RF-06, RF-07, RF-08 |
| Despacho solo con QR válido | RF-07, RF-10, RF-12, RF-13 |
| Inventario en tiempo real | RF-12, RF-14, RF-15, RF-16, RF-17 |
| Trazabilidad completa | RF-21, RS-06 |
| Reportes exportables | RF-19, RF-20 |
| App móvil operativa | RF-13 |
| Seguridad cumplida | RS-01 a RS-06 |

## 6. Requisitos no funcionales derivados directamente del SRS

- Disponibilidad 24/7.
- Seguridad de datos.
- Sincronización inmediata.
- Integridad de inventario.
- Unicidad de tickets.
- Auditoría.
- Protección criptográfica.
- Soporte multiusuario y multirol.

## 7. Información que requiere refinamiento

El SRS no define métricas concretas para:

- Tiempo máximo de respuesta.
- Número de usuarios concurrentes.
- Volumen máximo de tickets.
- RPO/RTO.
- Retención de auditoría.
- SLA de integraciones.
- Tamaño máximo de reportes.
- Límites de solicitudes API.

Estos puntos deben convertirse en requisitos medibles antes de producción.

## 8. Estado de implementación de Fase 4

Esta sección registra trazabilidad sin modificar el texto original del SRS:

| Requisito | Estado al cierre de Fase 4 |
|---|---|
| RF-06 | Implementado: emisión desde una Solicitud aprobada y PDF con QR |
| RF-07 | Implementado: QR firmado, verificable y resistente a manipulación |
| RF-08 | Implementado: UUID, prefijo y secuencia PostgreSQL sin duplicidad |
| RF-09 | Parcial: se crean notificaciones pendientes; SMTP/SMS real queda para Fase 9 |
| RF-10 | Implementado: consulta y validación de estado efectivo |
| RS-04 | Implementado: ECDSA P-256, SHA-256 y token aleatorio de 256 bits |
