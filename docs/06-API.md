# 06 - Diseño Inicial de API REST

## 1. Objetivo

Registrar rutas implementadas y propuestas de recursos REST. El SRS exige una
API REST, pero no especifica rutas ni contratos exactos. Las secciones de Fase 1
y catálogos reflejan el backend actual; los módulos de fases posteriores siguen
siendo propuestas hasta su implementación.

## 2. Convenciones

- Base sugerida: `/api/v1`.
- JSON como formato principal.
- HTTPS obligatorio.
- JWT interno o access token OIDC de Keycloak en `Authorization: Bearer <token>`.
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

Rutas implementadas actualmente por Builder 1:

```text
GET    /api/v1/empleados
GET    /api/v1/empleados/{id}
POST   /api/v1/empleados
PUT    /api/v1/empleados/{id}
DELETE /api/v1/empleados/{id}  # desactivación lógica
```

### Vehículos

Rutas implementadas actualmente por Builder 1:

```text
GET    /api/v1/vehiculos
GET    /api/v1/vehiculos/{id}
POST   /api/v1/vehiculos
PUT    /api/v1/vehiculos/{id}
DELETE /api/v1/vehiculos/{id}  # desactivación lógica
```

### Departamentos

Rutas implementadas actualmente por Builder 1:

```text
GET    /api/v1/departamentos
GET    /api/v1/departamentos/{id}
POST   /api/v1/departamentos
PUT    /api/v1/departamentos/{id}
DELETE /api/v1/departamentos/{id}  # desactivación lógica
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
GET    /api/v1/tickets/{id}/pdf
```

#### Contrato de emisión

`POST /api/v1/tickets`, autorizado para `Administrador` y `Supervisor`, recibe:

```json
{
  "solicitudId": 42,
  "prefijo": "COM"
}
```

`prefijo` es opcional; por defecto usa `Tickets:Prefix`. Empleado, vehículo,
departamento, combustible, cantidad y vencimiento se leen de la Solicitud
aprobada. No se aceptan copias editables de esos campos desde el cliente.

La respuesta `201` contiene UUID, código visible `COM-2026-000001`, datos
autorizados, estado e indicador `qrDisponible`; no expone token, hashes, firma
ni clave privada. Una Solicitud solo puede tener un Ticket no terminal.

#### Validación

`POST /api/v1/tickets/validar` recibe `{ "qrPayload": "FTQR1..." }`. Requiere
un rol operacional (`Administrador`, `Supervisor`, `Despachador`, `Auditor` o
`Consulta`) y devuelve `200` con `valido`, `codigo`, `mensaje` y los datos del
Ticket únicamente cuando es válido. Validar no consume el Ticket.

La API comprueba versión, estructura, UUID, SHA-256, firma, token, coincidencia
con PostgreSQL, estado y fecha de vencimiento. Los códigos operacionales
incluyen `QR_INVALIDO`, `QR_NO_COINCIDE`, `TICKET_VENCIDO`, `TICKET_CONSUMIDO`
y `TICKET_ANULADO`.

#### Envío, anulación y PDF

- `POST /{id}/enviar`: Admin/Supervisor; crea una notificación `PENDIENTE` por
  correo/teléfono disponible. No ejecuta SMTP ni SMS.
- `POST /{id}/anular`: Admin/Supervisor; requiere `{ "motivo": "..." }`,
  rechaza tickets consumidos/vencidos y es idempotente si ya estaba anulado.
- `GET /{id}/pdf`: roles operacionales; devuelve `application/pdf` con el mismo
  QR emitido y registra auditoría.

Errores de negocio de emisión usan `400`, `404` o `409` con `code` y `message`.

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
- Convención definitiva español/inglés para las rutas de catálogos de Builder 1.
