# Cierre de Fase 4 — Tickets digitales y QR seguro

## Estado

`FASE 4: READY FOR PR`

## Alcance terminado

- Emisión de Ticket únicamente desde una Solicitud aprobada.
- UUID, prefijo configurable y numeración global concurrency-safe.
- Un solo Ticket utilizable por Solicitud, conservando el histórico terminal.
- QR firmado con ECDSA P-256, SHA-256 y token aleatorio de 256 bits.
- Validación criptográfica y de estado sin consumir el Ticket.
- Consulta, anulación, PDF y preparación lógica de envío.
- RBAC, auditoría, migración EF Core y pruebas reales de concurrencia.

## Requisitos

| Requisito | Estado de Fase 4 |
|---|---|
| RF-06 | Implementado en backend: emisión y PDF desde Solicitud aprobada |
| RF-07 | Implementado: QR firmado y verificable |
| RF-08 | Implementado: UUID, prefijo y secuencia PostgreSQL única |
| RF-09 | Parcial deliberado: registra notificaciones pendientes; transporte real queda en Fase 9 |
| RF-10 | Implementado en backend: consulta y estado efectivo |
| RS-04 | Implementado: ECDSA P-256, SHA-256 y token aleatorio de 256 bits |

## Contrato entregado

| Método | Ruta | Uso |
|---|---|---|
| `GET` | `/api/v1/tickets` | Lista Tickets y su estado efectivo |
| `GET` | `/api/v1/tickets/{id}` | Consulta un Ticket |
| `POST` | `/api/v1/tickets` | Emite desde `SolicitudId` |
| `POST` | `/api/v1/tickets/validar` | Verifica QR, registro y estado |
| `POST` | `/api/v1/tickets/{id}/anular` | Anula un Ticket utilizable |
| `POST` | `/api/v1/tickets/{id}/enviar` | Prepara notificaciones pendientes |
| `GET` | `/api/v1/tickets/{id}/pdf` | Genera el comprobante PDF |

El detalle de DTOs, respuestas, errores y permisos está en `06-API.md` y
`08-ROLES-PERMISOS.md`.

## Diseño de seguridad

El sobre QR tiene versión, payload canónico, hash y firma. El servidor copia al
Ticket los datos autoritativos de la Solicitud aprobada; el cliente no decide
empleado, vehículo, combustible, cantidad ni vencimiento. La base guarda el
hash del token, el hash del payload, la firma y el PNG, pero no el token en
claro como columna separada.

La clave privada PKCS#8 se recibe exclusivamente mediante
`Tickets__SigningPrivateKeyPkcs8Base64`. La clave pública SPKI puede configurarse
con `Tickets__SigningPublicKeySpkiBase64`. `backend/.env.example` contiene solo
marcadores y ninguna clave real.

## Persistencia y concurrencia

La migración `20260904135312_AddSecureTicketsQr` agrega los campos de seguridad,
la secuencia PostgreSQL `ticket_numero_seq` y el índice parcial único
`UX_Tickets_Solicitud_Utilizable`. Así se protege la regla de negocio aun cuando
dos procesos intenten emitir simultáneamente.

## Dependencias

- [QRCoder 1.8.0](https://www.nuget.org/packages/QRCoder), licencia MIT, genera el PNG.
- [QuestPDF 2026.8.0](https://www.nuget.org/packages/QuestPDF), licencia Community
  declarada para este uso académico, genera el comprobante PDF.

## Evidencia de cierre

- Build Release: 0 errores y 0 advertencias.
- Suite combinada: **174 aprobadas, 0 fallidas, 0 omitidas**.
- PostgreSQL 16 real: migraciones desde cero, constraints y concurrencia verdes.
- Keycloak 26.7.3 real: OIDC y PKCE verdes; Fase 1 sin regresiones.
- EF Core: modelo y snapshot sin cambios pendientes.
- Secretos: no se incorporó una clave de firma privada ni credenciales
  productivas.

## Protección del trabajo compartido

No se modificaron `SolicitudesController`, `InventarioController`,
`RecepcionesController`, `MovimientosController` ni el frontend React. El modelo
de Solicitud se consume mediante sus relaciones existentes y el snapshot de EF
Core fue extendido, no sustituido por una versión antigua.

## Diferidos explícitos

- Interfaz web de Tickets: Builder 3.
- Aplicación Flutter y despacho/consumo: Fase 5.
- Transporte SMTP/SMS y confirmación real de entrega: Fase 9.
- Rotación productiva y almacén administrado de claves: despliegue/operaciones.

## Gate humano

Antes del merge, revisar el contrato con Builder 1 y Builder 3, ejecutar los
casos manuales de `21-PRUEBAS-FASE4-TICKETS-QR.md` y exigir CI remoto verde. No
se debe interpretar `Notificacion.Pendiente` como entrega real.
