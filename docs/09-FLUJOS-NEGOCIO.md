# 09 - Flujos de Negocio

## 1. Objetivo

Documentar los flujos principales derivados de los requisitos funcionales.

## 2. Flujo: Solicitud → Ticket

```text
Solicitante
   |
Crear solicitud
   |
Validar datos
   |
Revisión / aprobación
   |
Generar ticket
   |
Asignar UUID + secuencia
   |
Generar QR seguro
   |
Crear notificación pendiente
   |
Ticket disponible
```

Fase 4 no transporta mensajes: `POST /tickets/{id}/enviar` crea registros
`Notificacion` con estado `PENDIENTE` para los canales disponibles. Fase 9
implementará SMTP/SMS y actualizará el resultado de entrega.

La emisión recibe únicamente `SolicitudId`, valida que la Solicitud esté
aprobada y copia sus datos autorizados. PostgreSQL asigna la secuencia y evita
dos tickets utilizables simultáneos para la misma Solicitud.

## 3. Flujo: Ticket → Despacho

```text
Despachador
   |
Escanea QR
   |
App móvil consulta API
   |
Validar firma/token
   |
Validar vigencia
   |
Validar estado
   |
Confirmación visual
   |
Registrar galones
   |
Confirmar despacho
   |
Marcar ticket consumido
   |
Registrar salida de inventario
   |
Auditar operación
```

La actualización del ticket y del inventario debe ejecutarse de forma consistente para evitar dobles consumos o desbalances.

En Fase 4 solo está implementada la parte hasta la validación y visualización.
`POST /tickets/validar` no consume; el despacho transaccional pertenece a Fase 5.

## 4. Flujo: Recepción → Inventario

```text
Supervisor
   |
Registrar suplidor/factura
   |
Registrar volumen/tanque
   |
Validar datos
   |
Crear recepción
   |
Crear entrada de inventario
   |
Actualizar existencia
   |
Auditar
```

## 5. Flujo: Ajuste de inventario

```text
Usuario autorizado
   |
Seleccionar inventario
   |
Indicar ajuste +/- 
   |
Registrar justificación
   |
Aplicar movimiento
   |
Actualizar existencia
   |
Auditar
```

El SRS no especifica obligatoriedad de justificación, pero se recomienda por trazabilidad.

## 6. Flujo: Cierre diario

```text
Despachos del día
      |
Calcular volumen despachado
      |
Obtener inventario final
      |
Detectar diferencias
      |
Confirmar cierre
      |
Generar acta digital
      |
Generar PDF
      |
Auditar
```

## 7. Flujo: Vencimiento de ticket

```text
Ticket pendiente
   |
Evaluar fecha de expiración
   |
Próximo a vencer -> Notificar
   |
Vencido -> Cambiar estado / impedir uso
```

## 8. Flujo: Anulación

La implementación de Fase 4 usa el siguiente proceso:

```text
Usuario autorizado
   |
Solicitar anulación
   |
Validar que no esté consumido
   |
Registrar motivo
   |
Cambiar estado
   |
Auditar
```

## 9. Flujo: Reportes

```text
Usuario autorizado
   |
Seleccionar filtros
   |
Consultar datos
   |
Visualizar
   |
Opcional: exportar Excel/CSV/PDF
```

## 10. Reglas transversales

- Toda operación sensible debe comprobar autorización.
- Toda operación relevante debe auditarse.
- Un ticket consumido no puede reutilizarse.
- Un ticket vencido o anulado no puede despacharse.
- Los cambios de inventario deben tener movimiento asociado.
- Las integraciones fallidas deben poder generar alertas.
