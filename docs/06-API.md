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

### Idioma de las rutas — decisión definitiva (2026-09-09)

**Las rutas de la API usan español.** Ejemplos: `/solicitudes`, `/empleados`,
`/tipos-combustible`, `/cierres-diarios`.

**Rationale:** las rutas en español ya están en `main`, el frontend las consume
directamente y el dominio del negocio es hispanohablante. Cambiarlas ahora
implicaría romper el frontend y todas las pruebas de integración.

**Regla de equipo:** cualquier renombre de ruta requiere consenso explícito de
los tres Builders antes de ejecutarse. Un PR que renombre rutas sin ese consenso
**debe rechazarse en code review**.

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

`GET /tickets`, `GET /tickets/{id}` y `GET /tickets/{id}/pdf` permiten además
`Solicitante`, filtrando por `Ticket.Empleado.UsuarioId == usuario autenticado`.
Un recurso ajeno devuelve `404`, igual que uno inexistente. Sin empleado
vinculado, el listado está vacío. Si el usuario posee además un rol operacional,
conserva el alcance de ese rol. Solicitante no puede validar QR operacional,
emitir, anular ni preparar envío.

- `POST /{id}/enviar`: Admin/Supervisor; crea una notificación `PENDIENTE` por
  correo/teléfono disponible y deja el Ticket en `Pendiente`. No ejecuta SMTP ni
  SMS. No duplica una notificación pendiente del mismo tipo, Ticket y canal;
  `notificacionesPendientes` cuenta los registros nuevos de esta invocación.
  PostgreSQL serializa preparaciones simultáneas por Ticket. `Enviado` queda
  reservado a F9 tras confirmar transporte real.
- `POST /{id}/anular`: Admin/Supervisor; requiere `{ "motivo": "..." }`,
  rechaza tickets consumidos/vencidos y es idempotente si ya estaba anulado.
- `GET /{id}/pdf`: roles operacionales o Solicitante propietario; devuelve `application/pdf` con el mismo
  QR emitido y registra auditoría.

Errores de negocio de emisión usan `400`, `404` o `409` con `code` y `message`.

### Despachos

Contrato F5: `POST /despachos` requiere exclusivamente rol `Despachador` activo.
Recibe `ticketId` (UUID), `qrPayload`, `tanqueId`, `estacionId`,
`galonesServidos` (positivo, máximo autorizado, hasta cuatro decimales) y
`observaciones` opcionales (máximo 500 caracteres). Un despacho parcial consume
el Ticket completo y no permite un segundo despacho por el saldo.

Devuelve `201` con `despachoId`, `ticketId`, `codigoTicket`, `fecha`, `hora` (UTC),
`galonesServidos`, `operadorId`, `operador`, `tanqueId`, `tanqueIdentificacion`,
`estacionId`, `estacionNombre`, `inventarioRestante`, `disponibilidadRestante`,
`estadoTicket` (enum numérico; Consumido = 5) y `observaciones`. El inventario restante
es el valor registrado al confirmar, no una lectura posterior del tanque.

En una sola transacción se revalida QR/estado/fecha, se crea Despacho, se consume
Ticket, se descuentan ExistenciaActual y Disponibilidad, se crea Movimiento de
tipo Salida con volumen negativo y se audita. PostgreSQL bloquea Ticket e Inventario; UNIQUE TicketId
impide doble consumo. La concurrencia de Inventario usa además `xmin` para que
escrituras antiguas de otros módulos fallen sin sobrescribir stock.

`GET /despachos` y `GET /despachos/{id}`: Admin/Supervisor/Auditor/Consulta leen;
Despachador consulta solo sus operaciones. Recursos fuera de alcance: `404`.
Listado paginado mediante `pagina` (1 por defecto) y `tamanoPagina` (20, máximo
100); `ticketId` opcional permite reconciliar una confirmación cuya respuesta
se perdió. No se reintenta automáticamente un POST tras error de red.

`GET /api/v1/estaciones`: lectura de estaciones activas para Despachador y los
roles de consulta de despachos. Es el único catálogo de lectura añadido;
`GET /api/v1/tanques` ya existe y se reutiliza filtrando activos/combustible.
`GET /api/v1/auth/me`: usuario local y roles de negocio resueltos por F1,
necesarios porque los roles externos de Keycloak no autorizan operaciones.

Errores `{code,message}`: `400` cantidad/formato inválido; `401` sesión/operador
inactivo; `403` rol no autorizado; `404` Ticket/tanque/estación/inventario
inexistente; `409` QR_INVALIDO, QR_NO_COINCIDE, TICKET_VENCIDO,
TICKET_ANULADO, TICKET_CONSUMIDO, TANQUE_INACTIVO, ESTACION_INACTIVA,
COMBUSTIBLE_INCORRECTO, INVENTARIO_INSUFICIENTE o CONCURRENCIA_CONFLICTO.
La validación de QR anterior a la confirmación no consume el Ticket.

Cantidad inválida: `GALONES_INVALIDOS` o `GALONES_EXCEDEN_AUTORIZACION` (`400`).
Operador inválido: `OPERADOR_INVALIDO` (`401`); sin rol: `OPERADOR_NO_AUTORIZADO` (`403`).
El operador, fechas, estado, movimiento e inventario se calculan en el servidor.

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
GET    /api/v1/cierres-diarios/{id}/pdf
POST   /api/v1/cierres-diarios
```

- `POST`: genera el cierre del día especificado en `{ "fecha": "YYYY-MM-DD" }`. Calcula inventario inicial/final por tanque a partir de `MovimientosInventario`, genera el PDF acta y lo persiste. Roles: `Administrador`, `Supervisor`.
- `GET /{id}/pdf`: devuelve el PDF acta en `application/pdf`. Roles: `Administrador`, `Supervisor`, `Auditor`.
- Errores de negocio: `400 FECHA_FUTURA`, `400 SIN_DESPACHOS`, `409 CIERRE_YA_EXISTE`.

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
