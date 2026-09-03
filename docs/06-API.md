# 06 - Diseño Inicial de API REST

## 1. Objetivo

Definir una propuesta inicial de recursos REST. El SRS exige una API REST, pero no especifica rutas ni contratos exactos.

## 2. Convenciones

- Base sugerida: `/api/v1`.
- JSON como formato principal.
- HTTPS obligatorio.
- JWT en `Authorization: Bearer <token>`.
- Autorización por roles.
- Códigos HTTP estándar.
- Identificadores de recursos en la URL.

## 3. Recursos

### Autenticación

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
POST /api/v1/auth/password/reset
```

### Usuarios

```text
GET    /api/v1/usuarios
GET    /api/v1/usuarios/{id}
POST   /api/v1/usuarios
PUT    /api/v1/usuarios/{id}
PATCH  /api/v1/usuarios/{id}/estado
```

### Empleados

```text
GET    /api/v1/empleados
GET    /api/v1/empleados/{id}
POST   /api/v1/empleados
PUT    /api/v1/empleados/{id}
DELETE /api/v1/empleados/{id}
```

### Vehículos

```text
GET    /api/v1/vehiculos
GET    /api/v1/vehiculos/{id}
POST   /api/v1/vehiculos
PUT    /api/v1/vehiculos/{id}
DELETE /api/v1/vehiculos/{id}
```

### Departamentos

```text
GET    /api/v1/departamentos
GET    /api/v1/departamentos/{id}
POST   /api/v1/departamentos
PUT    /api/v1/departamentos/{id}
DELETE /api/v1/departamentos/{id}
```

### Tipos de Combustible

```text
GET    /api/v1/tipos-combustible
GET    /api/v1/tipos-combustible/{id}
POST   /api/v1/tipos-combustible
PUT    /api/v1/tipos-combustible/{id}
DELETE /api/v1/tipos-combustible/{id}
```

### Tanques

```text
GET    /api/v1/tanques
GET    /api/v1/tanques/{id}
POST   /api/v1/tanques
PUT    /api/v1/tanques/{id}
DELETE /api/v1/tanques/{id}
```

> POST crea el Tanque y su registro de Inventario (existencia = 0) en una sola transacción.

### Proveedores

```text
GET    /api/v1/proveedores
GET    /api/v1/proveedores/{id}
POST   /api/v1/proveedores
PUT    /api/v1/proveedores/{id}
DELETE /api/v1/proveedores/{id}
```

### Solicitudes de Combustible

```text
GET    /api/v1/solicitudes
GET    /api/v1/solicitudes/{id}
POST   /api/v1/solicitudes
POST   /api/v1/solicitudes/{id}/aprobar
POST   /api/v1/solicitudes/{id}/rechazar
```

### Tickets

```text
GET    /api/v1/tickets
GET    /api/v1/tickets/{id}
POST   /api/v1/tickets
POST   /api/v1/tickets/{id}/enviar
POST   /api/v1/tickets/{id}/anular
POST   /api/v1/tickets/validar
```

### Despachos

```text
GET    /api/v1/despachos
GET    /api/v1/despachos/{id}
POST   /api/v1/despachos
```

### Inventario

```text
GET    /api/v1/inventario
GET    /api/v1/inventario/movimientos
POST   /api/v1/inventario/ajustes
POST   /api/v1/inventario/transferencias
```

### Recepciones de Combustible

```text
GET    /api/v1/recepciones
POST   /api/v1/recepciones
```

### Cierres Diarios

```text
GET    /api/v1/cierres-diarios
GET    /api/v1/cierres-diarios/{id}
POST   /api/v1/cierres-diarios
```

### Reportes

```text
GET /api/v1/reportes
GET /api/v1/reportes/exportar
```

### Auditoría

```text
GET /api/v1/auditoria
```

### Dashboard

```text
GET /api/v1/dashboard/resumen
```

## 4. Respuesta de error sugerida

```json
{
  "code": "TICKET_INVALID",
  "message": "El ticket no es válido.",
  "details": []
}
```

## 5. Códigos HTTP esperados

- `200 OK`
- `201 Created`
- `204 No Content`
- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

## 6. Reglas críticas

- Nunca confiar en datos del QR sin validación en servidor.
- No permitir doble consumo de ticket.
- Proteger operaciones de inventario mediante transacciones.
- Auditar operaciones sensibles.
- Validar roles en backend.
- Evitar exponer información sensible en errores.

## 7. Pendientes

- Contratos DTO definitivos.
- Paginación.
- Filtros.
- Versionado exacto.
- Idempotencia.
- Rate limiting.
- OpenAPI/Swagger final.
