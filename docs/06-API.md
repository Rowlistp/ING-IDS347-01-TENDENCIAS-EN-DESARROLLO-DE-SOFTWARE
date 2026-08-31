# 06 - Diseño Inicial de API REST

## 1. Objetivo

Definir una propuesta inicial de recursos REST. El SRS exige una API REST, pero no especifica rutas ni contratos exactos.

## 2. Convenciones

- Base sugerida: `/api/v1`.
- JSON como formato principal.
- HTTPS obligatorio.
- JWT interno o access token OIDC de Keycloak en `Authorization: Bearer <token>`.
- Autorización por roles.
- Códigos HTTP estándar.
- Identificadores de recursos en la URL.

## 3. Recursos propuestos

### Autenticación

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
POST /api/v1/auth/password/reset
```

### Usuarios

```text
GET    /api/v1/users
GET    /api/v1/users/{id}
POST   /api/v1/users
PUT    /api/v1/users/{id}
PATCH  /api/v1/users/{id}/status
```

### Roles

```text
GET /api/v1/roles
```

- Solo `Administrador`.
- Devuelve únicamente los seis roles permitidos que estén persistidos.
- No existe CRUD arbitrario de roles.

### Esquemas de autenticación

- Local: login de FuelTrack, JWT firmado por la API y sesión con refresh token rotatorio.
- Externo: Keycloak, Authorization Code + PKCE S256 en los clientes públicos `fueltrack-web` y `fueltrack-mobile`; audiencia `fueltrack-api`.
- `401`: token ausente, inválido, issuer/audience incorrectos, o identidad externa sin usuario local activo.
- `403`: identidad autenticada, pero sin un rol local autorizado para el endpoint.

### Auditoría

```text
GET    /api/v1/audit?pagina=1&tamanoPagina=50
```

- Solo `Administrador` y `Auditor`.
- Consulta paginada y de solo lectura.
- No expone `DatosRelevantes`, contraseñas, tokens ni secretos.

### Empleados

```text
GET    /api/v1/employees
GET    /api/v1/employees/{id}
POST   /api/v1/employees
PUT    /api/v1/employees/{id}
PATCH  /api/v1/employees/{id}/status
```

### Vehículos

```text
GET    /api/v1/vehicles
GET    /api/v1/vehicles/{id}
POST   /api/v1/vehicles
PUT    /api/v1/vehicles/{id}
PATCH  /api/v1/vehicles/{id}/status
```

### Departamentos

```text
GET    /api/v1/departments
POST   /api/v1/departments
PUT    /api/v1/departments/{id}
```

### Solicitudes

```text
GET    /api/v1/fuel-requests
GET    /api/v1/fuel-requests/{id}
POST   /api/v1/fuel-requests
POST   /api/v1/fuel-requests/{id}/approve
POST   /api/v1/fuel-requests/{id}/reject
```

> Aprobar/rechazar es una propuesta derivada de la responsabilidad de "Aprobaciones" del Supervisor. El SRS no define literalmente esos endpoints.

### Tickets

```text
GET    /api/v1/tickets
GET    /api/v1/tickets/{id}
POST   /api/v1/tickets
POST   /api/v1/tickets/{id}/send
POST   /api/v1/tickets/{id}/cancel
POST   /api/v1/tickets/validate
```

### Despachos

```text
GET    /api/v1/dispatches
GET    /api/v1/dispatches/{id}
POST   /api/v1/dispatches
```

### Inventario

```text
GET    /api/v1/inventory
GET    /api/v1/inventory/movements
POST   /api/v1/inventory/adjustments
POST   /api/v1/inventory/transfers
```

### Recepciones

```text
GET    /api/v1/receipts
POST   /api/v1/receipts
```

### Cierre diario

```text
GET    /api/v1/daily-closures
GET    /api/v1/daily-closures/{id}
POST   /api/v1/daily-closures
```

### Reportes

```text
GET /api/v1/reports
GET /api/v1/reports/export
```

### Auditoría

```text
GET /api/v1/audit
```

### Dashboard

```text
GET /api/v1/dashboard/summary
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
