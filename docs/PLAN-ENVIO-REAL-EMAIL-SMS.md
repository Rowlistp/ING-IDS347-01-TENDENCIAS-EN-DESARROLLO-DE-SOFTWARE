# Plan: activar envío real de Email/SMS (Fase 9)

**Para:** Builder 2
**De:** revisión de repo (ver `docs/26-CIERRE-FASE9.md` e `infra/notifications/README.md`)
**Estado de partida:** el transporte YA está implementado y probado (285 tests aprobados). Lo que falta es **configuración con credenciales reales**, no código nuevo — excepto si el gateway SMS elegido no calza con el contrato genérico (ver Paso 3).

## 0. Qué ya existe (no hay que reescribir esto)

- `backend/FuelTrack.Api/Notifications/NotificationTransports.cs`
  - `SmtpEmailSender`: envío SMTP real vía MailKit (TLS, adjunta PDF del ticket).
  - `HttpSmsGatewaySender`: POST HTTP genérico a un gateway SMS con idempotencia y validación de acuse.
- `NotificationWorkers.cs` / `NotificationQueue.cs`: cola con reintentos, backoff, lease, at-least-once.
- `NotificationRuleService.cs`: genera las 5 alertas (vencimiento próximo, vencido, inventario bajo, ajustes, integración fallida).
- Todo detrás de flags apagados por defecto (`Notifications:Smtp:Enabled=false`, `Notifications:Sms:Enabled=false`, `Notifications:WorkerEnabled=false`) para no disparar envíos por accidente.
- Contrato completo documentado en `infra/notifications/README.md` — **leerlo primero**, tiene el detalle exacto de cada campo.

## 1. Decisión que Builder 2 debe tomar (bloqueante)

Elegir proveedor de **SMTP** y de **SMS**. Sugerencias para un proyecto de curso (gratis o casi gratis):

| Canal | Opción sugerida | Por qué |
|---|---|---|
| Email | Cuenta de Gmail con "contraseña de aplicación", o Brevo/SendGrid free tier | Gmail es rápido de armar para pruebas; Brevo/SendGrid si quieren remitente propio con dominio |
| SMS | Buscar un gateway que devuelva `{"messageId": "..."}` en JSON tras un POST simple | El código NO tiene un adaptador de Twilio/Vonage — es un contrato HTTP genérico a propósito (ver `infra/notifications/README.md` línea 41) |

**Importante sobre SMS**: si el proveedor real (Twilio, Vonage, AWS SNS, etc.) no responde exactamente `{"messageId": "..."}` a un POST simple con `{to, message, reference, sender}`, **hay que escribir un adaptador pequeño** que traduzca la respuesta de ese proveedor al contrato esperado, o implementar una clase nueva que implemente `ISmsSender` directamente contra su SDK/API. Esto sí sería código nuevo, acotado a una clase.

## 2. Provisión de credenciales (Builder 2, con su propia cuenta)

1. Crear la cuenta SMTP (Gmail/Brevo/SendGrid) y generar credenciales de aplicación (nunca la contraseña personal).
2. Crear la cuenta del gateway SMS elegido y obtener API key.
3. **Nunca** commitear estas credenciales. Configurarlas localmente con `dotnet user-secrets` (ya usado en el proyecto):
   ```bash
   cd backend/FuelTrack.Api
   dotnet user-secrets set "Notifications:Smtp:Enabled" "true"
   dotnet user-secrets set "Notifications:Smtp:Host" "smtp.gmail.com"
   dotnet user-secrets set "Notifications:Smtp:Port" "587"
   dotnet user-secrets set "Notifications:Smtp:StartTls" "true"
   dotnet user-secrets set "Notifications:Smtp:Username" "tu-correo@gmail.com"
   dotnet user-secrets set "Notifications:Smtp:Password" "app-password-de-16-caracteres"
   dotnet user-secrets set "Notifications:Smtp:FromAddress" "tu-correo@gmail.com"
   dotnet user-secrets set "Notifications:Smtp:FromName" "FuelTrack"

   dotnet user-secrets set "Notifications:Sms:Enabled" "true"
   dotnet user-secrets set "Notifications:Sms:BaseUrl" "https://api.tu-gateway.com/send"
   dotnet user-secrets set "Notifications:Sms:ApiKey" "tu-api-key"
   ```
4. Configurar destinatarios operativos de las alertas internas (sin esto, las 5 alertas no tienen a quién avisar):
   ```bash
   dotnet user-secrets set "Notifications:Operations:Emails:0" "correo-del-equipo@dominio.com"
   ```

## 3. Validación local antes de "real" (recomendado, ya soportado)

Antes de gastar cuota real del proveedor, probar con **Mailpit** (SMTP falso local, ya incluido):

```bash
docker compose -f infra/notifications/compose.yml up -d
```

Config para esa prueba (ver `infra/notifications/README.md` líneas 24–28):
```bash
dotnet user-secrets set "Notifications:Smtp:Enabled" "true"
dotnet user-secrets set "Notifications:Smtp:Host" "127.0.0.1"
dotnet user-secrets set "Notifications:Smtp:Port" "1025"
dotnet user-secrets set "Notifications:Smtp:StartTls" "false"
dotnet user-secrets set "Notifications:Smtp:FromAddress" "fueltrack@example.test"
dotnet user-secrets set "Notifications:AllowInsecureLocalTransport" "true"
```
Ver los correos en http://localhost:8025. Cuando esto funcione, repetir el mismo flujo apuntando a las credenciales reales del Paso 2.

Para SMS sin gastar cuota real, se puede montar un mock HTTP simple (cualquier servidor que responda `{"messageId":"test-1"}`) y apuntar `Sms:BaseUrl` ahí primero.

## 4. Activar el worker

Solo después de migrar y de que SMTP/SMS respondan bien en pruebas:
```bash
dotnet user-secrets set "Notifications:WorkerEnabled" "true"
```
La migración `AddPhase9NotificationsIntegration` ya está aplicada en la base de datos actual — no hace falta correrla de nuevo salvo en un entorno nuevo.

## 5. Checklist de salida (para cerrar el gap)

- [ ] Proveedor SMTP elegido y probado con Mailpit primero
- [ ] Proveedor SMTP real probado enviando a un correo de prueba propio (no productivo)
- [ ] Proveedor SMS elegido; si no calza con el contrato genérico, adaptador escrito e implementando `ISmsSender`
- [ ] SMS real probado con un número propio
- [ ] `Notifications:Operations:Emails/Phones` configurados con destinatarios reales del equipo (para las 5 alertas)
- [ ] `Notifications:WorkerEnabled=true` activado y verificado que la cola procesa (`GET /notificaciones`)
- [ ] Credenciales SOLO en user-secrets o variables de entorno — nunca en `appsettings.json` ni en git
- [ ] Actualizar `docs/26-CIERRE-FASE9.md` quitando el gate `REAL PROVIDER CREDENTIAL GATE PENDING` una vez validado con evidencia (captura o log de envío real, sin exponer credenciales)

## 6. Qué NO hacer

- No commitear `.env`, API keys ni passwords (el `.gitignore` ya cubre `.env`, pero revisar antes de cualquier commit).
- No activar `WorkerEnabled=true` en CI ni con destinatarios reales masivos — solo local, con destinatarios de prueba propios.
- No inventar un adaptador que "asuma éxito" si la respuesta del proveedor SMS no es clara — el contrato exige tratar respuesta ambigua como reintentable, no como éxito (ver `NotificationTransports.cs` línea 79-82).
