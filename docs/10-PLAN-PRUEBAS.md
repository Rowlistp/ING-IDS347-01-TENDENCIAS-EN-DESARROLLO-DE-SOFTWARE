# 10 - Plan de Pruebas

El gate correctivo F4 verifica listado/consulta/PDF propios del Solicitante,
`404` ajeno, prohibición de acciones operacionales, estado `Pendiente` y una sola
notificación por canal con ocho preparaciones simultáneas en PostgreSQL real.

## 1. Objetivo

Definir una estrategia inicial de pruebas alineada con el SRS y sus criterios de aceptación.

## 2. Niveles de prueba

### Pruebas unitarias

Aplicables a:

- Validaciones.
- Cálculo de estados.
- Numeración.
- Reglas de vencimiento.
- Reglas de inventario.
- Servicios internos.

### Pruebas de integración

- API + PostgreSQL.
- Emisión de ticket + QR.
- Despacho + inventario.
- Recepción + inventario.
- Cierre + movimientos.
- SMTP.
- SMS.

### Pruebas de API

- Autenticación.
- Autorización.
- CRUD permitido.
- Códigos HTTP.
- Validación.
- Conflictos.
- Seguridad.

### Pruebas web

- Flujos administrativos.
- Formularios.
- Reportes.
- Dashboard.
- Restricciones por rol.

### Pruebas móviles

- Login.
- Cámara/escaneo QR.
- Validación online.
- Confirmación.
- Registro de despacho.
- Consulta de tickets.

### Pruebas de seguridad

- Acceso sin token.
- Token expirado.
- Roles incorrectos.
- Manipulación de QR.
- Reutilización de QR.
- Validación de entradas.
- Exposición de información.
- Auditoría.

## 3. Pruebas por criterio de aceptación

### CA-01 Tickets QR únicos

- Crear múltiples tickets.
- Verificar UUID único.
- Verificar secuencia única.
- Intentar duplicidad.
- Validar integridad del QR.

### CA-02 Despacho solo con QR válido

Casos:

- QR válido.
- QR vencido.
- QR consumido.
- QR anulado.
- QR manipulado.
- Token inválido.

### CA-03 Inventario en tiempo real

- Registrar despacho y comprobar decremento.
- Registrar recepción y comprobar incremento.
- Registrar ajuste.
- Registrar transferencia.
- Validar consistencia ante errores.

### CA-04 Trazabilidad

Verificar auditoría de:

- Creación.
- Modificación.
- Despacho.
- Ajuste.
- Anulación.
- Acceso.

### CA-05 Reportes exportables

- Excel.
- CSV.
- PDF.
- Filtros.
- Datos correctos.

### CA-06 Aplicación móvil en producción

- Login.
- QR.
- Despacho.
- Consulta.
- Conectividad real.
- Manejo de errores de red.

### CA-07 Seguridad

Validar RS-01 a RS-06.

## 4. Datos de prueba

Preparar:

- Usuarios por rol.
- Empleados.
- Vehículos.
- Departamentos.
- Tipos de combustible.
- Tanques.
- Solicitudes.
- Tickets en todos los estados.
- Inventario con diferentes niveles.

## 5. Criterios de salida

Antes de producción:

- Cero defectos críticos abiertos.
- Criterios de aceptación aprobados.
- Flujos principales verificados.
- Seguridad validada.
- Integraciones probadas.
- Reportes validados.
- Auditoría validada.

## 6. Pendientes

El SRS no define:

- Porcentaje mínimo de cobertura.
- SLA de rendimiento.
- Carga esperada.
- Usuarios concurrentes.
- Tiempo máximo por operación.

Se requiere definir estos parámetros para pruebas de rendimiento formales.

## 7. Gate ejecutado de Fase 4

La suite automatizada cubre emisión desde Solicitud aprobada, rechazo de
estados/cantidades/vencimientos inválidos, UUID, secuencia, un ticket utilizable,
payload auténtico y manipulación de cada campo, hash, firma, token, estados
terminales, vencimiento efectivo, anulación, PDF, cola de envío, RBAC y
auditoría.

PostgreSQL real aplica migraciones desde cero, verifica la secuencia y el índice
parcial, crea 24 tickets concurrentes sin UUID/secuencia duplicados y permite
solo un ganador para dos emisiones simultáneas de la misma Solicitud. Los
resultados exactos se registran en `21-PRUEBAS-FASE4-TICKETS-QR.md`.
