# 05 - Modelo Conceptual de Datos

## 1. Objetivo

Proponer un modelo conceptual inicial basado en los datos explícitamente requeridos por el SRS.

> Este documento conserva la vista conceptual. El esquema físico existente y su
> evolución se consultan en `backend/FuelTrack.Api/Migrations/`.

## 2. Entidades principales propuestas

### Usuario

- Id.
- Nombre de usuario.
- Credenciales.
- Estado.
- Datos de sesión.
- Relación con roles.

### Rol

- Id.
- Nombre.
- Permisos asociados.

### Empleado

- Código.
- Nombre completo.
- Cédula.
- Departamento.
- Cargo.
- Correo.
- Teléfono.
- Estado.

### Departamento

- Id.
- Nombre.
- Estado.

### Vehículo

- Id.
- Placa.
- Ficha.
- Marca.
- Modelo.
- Año.
- Tipo.
- Departamento.
- Capacidad de tanque.
- Odómetro.
- Estado.

### SolicitudCombustible

- Id.
- Empleado.
- Vehículo.
- Departamento.
- Cantidad autorizada/solicitada.
- Tipo de combustible.
- Fecha de solicitud.
- Fecha de vencimiento.
- Tipo de solicitud.
- Estado propuesto.

### Ticket

- UUID.
- Número secuencial.
- Prefijo.
- Fecha de creación.
- Fecha de vencimiento.
- Empleado.
- Vehículo.
- Departamento.
- Cantidad autorizada.
- Tipo de combustible.
- Estado.
- Hash SHA-256 del payload canónico.
- Hash SHA-256 del token de validación; el token no se guarda en claro.
- Firma digital ECDSA P-256.
- PNG del QR necesario para el documento, sin columna de token en claro.
- Motivo de anulación.
- Solicitud de origen.

### TipoCombustible

- Id.
- Nombre.
- Estado.

### Tanque

- Id.
- Identificación.
- Tipo de combustible.
- Capacidad.
- Nivel actual.
- Nivel crítico.

> El SRS menciona "Tanque" en la recepción, pero no define su estructura. Los campos anteriores son una propuesta conceptual.

### Inventario

- Id.
- Tanque/tipo de combustible.
- Existencia actual.
- Disponibilidad.
- Fecha de última actualización.

### MovimientoInventario

- Id.
- Tipo: entrada, salida, ajuste o transferencia.
- Volumen.
- Fecha/hora.
- Usuario.
- Referencia de operación.
- Observaciones.

### Proveedor

- Id.
- RNC.
- Nombre.

### RecepcionCombustible

- Id.
- Proveedor.
- Factura.
- Volumen recibido.
- Fecha.
- Tanque.

### Despacho

- Id.
- Ticket.
- Fecha.
- Hora.
- Galones servidos.
- Operador.
- Estación.
- Observaciones.

### Estacion

El SRS registra la estación durante el despacho, pero no define su catálogo. Se propone una entidad para evitar texto libre.

### CierreDiario

- Id.
- Fecha.
- Volumen despachado.
- Inventario final.
- Diferencias.
- Acta digital.
- Reporte.

### Auditoria

- Id.
- Evento.
- Entidad afectada.
- Identificador del registro.
- Usuario.
- Fecha.
- Hora.
- Dirección IP.
- Datos relevantes del cambio.

### Notificacion

- Id.
- Tipo.
- Destinatario.
- Estado.
- Fecha.
- Canal.
- Referencia del evento.

## 3. Relaciones conceptuales principales

```text
Departamento 1 --- N Empleado
Departamento 1 --- N Vehículo

Empleado 1 --- N Solicitud
Vehículo 1 --- N Solicitud

Solicitud 1 --- 0..N Ticket
Ticket 1 --- 0..1 Despacho

TipoCombustible 1 --- N Ticket
TipoCombustible 1 --- N Tanque

Tanque 1 --- N Recepción
Tanque 1 --- N MovimientoInventario

Proveedor 1 --- N Recepción

Usuario 1 --- N Auditoría
Usuario N --- N Rol
```

## 4. Restricciones importantes

- UUID del ticket único.
- Secuencia de ticket sin duplicidad.
- Solo un ticket no terminal por Solicitud mediante índice parcial único.
- Estados terminales: Vencido, Consumido y Anulado.
- La vigencia por fecha prevalece sobre un estado activo almacenado.
- Placa y ficha deberían evaluarse como valores únicos.
- Código de empleado debería evaluarse como único.
- Una operación de despacho debe ser transaccional con el movimiento de inventario.
- Un ticket consumido no debe reutilizarse.
- Los movimientos de inventario no deberían eliminarse físicamente si forman parte de la trazabilidad.

## 5. Pendientes antes del modelo físico

- La relación física Solicitud-Ticket es 1 a N histórico, con máximo uno
  utilizable simultáneamente.
- Estructura de estaciones.
- Manejo de múltiples tanques.
- Transferencias entre tanques.
- Unidad de medida única.
- Estados exactos de solicitudes.
- Política de borrado lógico.
- Política productiva de retención y archivo de auditoría.
