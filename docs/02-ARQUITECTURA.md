# 02 - Arquitectura del Sistema

## 1. Objetivo

Definir la arquitectura tÃ©cnica inicial de la soluciÃ³n a partir del SRS y de las decisiones tomadas por el equipo.

## 2. Estilo arquitectÃ³nico

Se propone una arquitectura cliente-servidor basada en API REST, con separaciÃ³n entre:

- Frontend web.
- AplicaciÃ³n mÃ³vil.
- Backend.
- Base de datos.
- Servicios externos.
- Servicios de autenticaciÃ³n y seguridad.

## 3. Componentes principales

### Frontend web

**TecnologÃ­a:** React + Tailwind CSS.

Responsabilidades:

- AdministraciÃ³n.
- GestiÃ³n de usuarios.
- GestiÃ³n de empleados.
- GestiÃ³n de vehÃ­culos.
- GestiÃ³n de departamentos.
- GestiÃ³n de inventario.
- CreaciÃ³n y emisiÃ³n de tickets.
- Dashboard.
- Reportes.
- AuditorÃ­a.

El frontend consumirÃ¡ exclusivamente la API REST del backend.

### Backend

**TecnologÃ­a:** .NET 10 Web API + Entity Framework Core.

Responsabilidades:

- Aplicar reglas de negocio.
- Exponer API REST.
- Gestionar autenticaciÃ³n y autorizaciÃ³n.
- Gestionar tickets.
- Gestionar QR.
- Gestionar solicitudes.
- Gestionar inventario.
- Procesar despachos.
- Gestionar cierres diarios.
- Generar reportes.
- Registrar auditorÃ­a.
- Integrarse con correo y SMS.

### Base de datos

**TecnologÃ­a:** PostgreSQL.

Responsabilidades:

- Persistencia de datos transaccionales.
- Persistencia de configuraciones.
- Inventario.
- Tickets.
- Solicitudes.
- Despachos.
- AuditorÃ­a.
- Historial de movimientos.

### AplicaciÃ³n mÃ³vil

**TecnologÃ­a:** Flutter.

Responsabilidades:

- Login seguro.
- Escaneo QR.
- ValidaciÃ³n de ticket.
- ConfirmaciÃ³n visual.
- Registro de despacho.
- Consulta de estado de tickets.
- SincronizaciÃ³n inmediata.

### Servicios externos

- SMTP para correo.
- SMS Gateway.
- Servicios de autenticaciÃ³n OAuth 2.0 cuando aplique.
- GeneraciÃ³n/validaciÃ³n de QR, si se implementa como servicio independiente.

## 4. Flujo lÃ³gico de comunicaciÃ³n

```text
[React Web] --------\
                     \
                      >---- [API .NET 10] ---- [PostgreSQL]
                     /
[Flutter Mobile] ---/

                            |---- [SMTP]
                            |---- [SMS Gateway]
                            |---- [Servicio OAuth 2.0]
```

## 5. Capas sugeridas para el backend

```text
API / Controllers
        |
Application / Services
        |
Domain / Models + reglas
        |
Infrastructure / EF Core + integraciones
        |
PostgreSQL / Servicios externos
```

En la primera fase puede mantenerse una estructura simple, evitando sobrearquitectura.

## 6. Seguridad arquitectÃ³nica

- HTTPS/TLS 1.3.
- JWT para sesiones/API.
- OAuth 2.0 para autorizaciÃ³n cuando corresponda.
- RBAC.
- ContraseÃ±as almacenadas mediante mecanismos seguros de hashing.
- Cifrado en reposo segÃºn el requisito AES-256.
- QR con firma digital, SHA-256 y token de validaciÃ³n.
- AuditorÃ­a de accesos y cambios.

## 7. Disponibilidad

El SRS requiere disponibilidad 24/7. La arquitectura deberÃ¡ considerar posteriormente:

- Despliegue redundante.
- Monitoreo.
- Backups.
- RecuperaciÃ³n ante fallos.
- Registro centralizado de errores.
- Health checks.

El proveedor y la estrategia concreta de infraestructura no estÃ¡n definidos por el SRS.

## 8. Principios de diseÃ±o

- SeparaciÃ³n de responsabilidades.
- API como punto central de integraciÃ³n.
- Seguridad por defecto.
- Trazabilidad.
- ValidaciÃ³n de datos.
- Evitar duplicidad de tickets.
- Transacciones para operaciones sensibles de inventario.
- ConfiguraciÃ³n mediante variables de entorno.
- Independencia entre clientes web y mÃ³vil.

## 9. Decisiones pendientes

- Proveedor de hosting.
- Estrategia CI/CD.
- Proveedor de SMS.
- Proveedor SMTP.
- Mecanismo exacto de firma digital del QR.
- Estrategia de almacenamiento de PDFs.
- Estrategia de observabilidad.
- PolÃ­tica de backups.


