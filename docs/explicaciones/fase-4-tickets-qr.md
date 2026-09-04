# Fase 4 explicada — Tickets digitales y QR seguro

## 1. ¿Qué se construyó?

Se construyó el backend que transforma una Solicitud aprobada en un Ticket
digital verificable. El Ticket recibe un UUID, un número visible, fechas,
estado, datos autorizados, un QR seguro y un PDF. También puede consultarse,
validarse, anularse y prepararse para envío.

Validar un QR responde si el Ticket es auténtico y utilizable; no registra un
despacho ni lo consume. Esa transición pertenece a la Fase 5.

## 2. ¿Por qué se diseñó así?

La Solicitud es la fuente autoritativa. El endpoint de emisión recibe solo su
identificador para impedir que un cliente cambie la cantidad, el empleado o el
vehículo ya aprobados.

La numeración usa una secuencia PostgreSQL porque dos emisiones simultáneas no
deben obtener el mismo número. Un índice parcial permite conservar Tickets
anulados, vencidos o consumidos y, a la vez, impide más de uno utilizable para
la misma Solicitud.

El QR combina cuatro controles:

1. Un payload canónico evita interpretaciones distintas de los mismos datos.
2. SHA-256 detecta cambios accidentales o maliciosos.
3. ECDSA P-256 demuestra que el emisor poseía la clave privada.
4. Un token aleatorio de 256 bits hace cada emisión impredecible, incluso si
   los datos de negocio se parecen.

La validación puede hacerse con la clave pública; la clave privada solo es
necesaria para emitir. Esto reduce la exposición del secreto criptográfico.

## 3. Archivos importantes

| Archivo | Responsabilidad |
|---|---|
| `Controllers/TicketsController.cs` | Contrato HTTP y permisos |
| `Services/TicketService.cs` | Reglas de emisión, estado, anulación, envío y PDF |
| `Security/TicketQrService.cs` | Payload, hash, firma, token y PNG QR |
| `Services/TicketNumberService.cs` | Secuencia de numeración |
| `Services/TicketPdfService.cs` | Documento PDF |
| `Security/TicketOptions.cs` | Prefijo y claves configurables |
| `Models/Ticket.cs` | Persistencia del Ticket y evidencia criptográfica |
| `Data/AppDbContext.cs` | Mapeo, secuencia e índice parcial |
| `Migrations/20260904135312_AddSecureTicketsQr.cs` | Cambio reproducible de base de datos |
| `FuelTrack.Api.Tests/Services/TicketServiceTests.cs` | Reglas y manipulación criptográfica |
| `FuelTrack.Api.Tests/Integration/PostgreSqlSecurityTests.cs` | Migración y concurrencia reales |

Todos los paths de la tabla son relativos a `backend/FuelTrack.Api` o
`backend/FuelTrack.Api.Tests`, según corresponda.

## 4. Preguntas que podría hacer el profesor

### ¿Por qué no usar `MAX(numero) + 1`?

Porque dos transacciones podrían leer el mismo máximo. La secuencia de
PostgreSQL asigna valores distintos de forma atómica bajo concurrencia.

### ¿SHA-256 por sí solo prueba autenticidad?

No. Cualquiera puede recalcular un hash después de alterar datos. La firma
ECDSA es la que demuestra autenticidad; el hash aporta una representación fija
para verificar integridad.

### ¿Dónde está la clave privada?

Fuera de Git, entregada al proceso por configuración segura mediante
`Tickets__SigningPrivateKeyPkcs8Base64`. El archivo `.env.example` no contiene
una clave funcional.

### ¿Por qué no guardar el token en una columna en claro?

Porque la base solo necesita comparar su SHA-256. Si alguien leyera la tabla,
no obtendría directamente ese factor secreto del QR.

### ¿Escanear consume el Ticket?

No. Escanear/validar comprueba autenticidad y vigencia. El despacho y consumo
son otra operación de negocio que se implementará en Fase 5.

### ¿El endpoint `enviar` manda correo o SMS?

No. En Fase 4 prepara registros `PENDIENTE` para los canales disponibles. El
transporte y su confirmación pertenecen a Fase 9.

### ¿Cómo se evita una doble emisión simultánea?

El servicio revisa la regla para dar un error claro y PostgreSQL la refuerza
con un índice parcial único. La base decide correctamente aun si hay una carrera.

## 5. Glosario

- **Payload canónico:** texto con campos y orden definidos de manera única.
- **Hash:** huella determinista usada para detectar modificaciones.
- **ECDSA:** algoritmo de firma digital de curva elíptica.
- **P-256:** curva criptográfica utilizada por la firma.
- **Token:** valor aleatorio e impredecible incluido en cada QR.
- **Base64url:** codificación segura para transportar bytes en texto/URL.
- **PKCS#8:** formato usado para importar la clave privada.
- **SPKI:** formato usado para importar la clave pública.
- **Índice parcial:** restricción que aplica únicamente a filas que cumplen una
  condición; aquí, Tickets no terminales.
- **Idempotencia:** repetir una operación no crea efectos duplicados.

## 6. Conexión con otras fases

- Fase 1 aporta identidad, JWT/OIDC, RBAC y auditoría.
- Fase 3 aporta la Solicitud aprobada y sus datos autoritativos.
- Fase 4 produce y verifica el Ticket.
- Fase 5 usará la validación desde Flutter y añadirá el despacho/consumo.
- Fase 9 transportará por SMTP/SMS las notificaciones preparadas.
- Builder 3 puede construir la vista web usando `docs/06-API.md` sin conocer la
  implementación criptográfica interna.

## 7. ¿Qué no se construyó?

- Aplicación móvil Flutter.
- Registro de despacho o consumo del Ticket.
- Interfaz React de Tickets.
- Envío real por SMTP o SMS.
- Confirmación de entrega externa.
- Infraestructura productiva de custodia y rotación de claves.

Estos puntos están fuera del alcance de Fase 4 y no se presentan como
funcionalidades terminadas.
