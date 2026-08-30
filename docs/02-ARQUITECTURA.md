# 02 - Arquitectura del Sistema

## 1. Objetivo

Definir la arquitectura técnica inicial de la solución a partir del SRS y de las decisiones tomadas por el equipo.

## 2. Estilo arquitectónico

Se propone una arquitectura cliente-servidor basada en API REST, con separación entre:

- Frontend web.
- Aplicación móvil.
- Backend.
- Base de datos.
- Servicios externos.
- Servicios de autenticación y seguridad.

## 3. Componentes principales

### Frontend web

**Tecnología:** React + Tailwind CSS.

Responsabilidades:

- Administración.
- Gestión de usuarios.
- Gestión de empleados.
- Gestión de vehículos.
- Gestión de departamentos.
- Gestión de inventario.
- Creación y emisión de tickets.
- Dashboard.
- Reportes.
- Auditoría.

El frontend consumirá exclusivamente la API REST del backend.

### Backend

**Tecnología:** .NET 10 Web API + Entity Framework Core.

Responsabilidades:

- Aplicar reglas de negocio.
- Exponer API REST.
- Gestionar autenticación y autorización.
- Gestionar tickets.
- Gestionar QR.
- Gestionar solicitudes.
- Gestionar inventario.
- Procesar despachos.
- Gestionar cierres diarios.
- Generar reportes.
- Registrar auditoría.
- Integrarse con correo y SMS.

### Base de datos

**Tecnología:** PostgreSQL.

Responsabilidades:

- Persistencia de datos transaccionales.
- Persistencia de configuraciones.
- Inventario.
- Tickets.
- Solicitudes.
- Despachos.
- Auditoría.
- Historial de movimientos.

### Aplicación móvil

**Tecnología:** Flutter.

Responsabilidades:

- Login seguro.
- Escaneo QR.
- Validación de ticket.
- Confirmación visual.
- Registro de despacho.
- Consulta de estado de tickets.
- Sincronización inmediata.

### Servicios externos

- SMTP para correo.
- SMS Gateway.
- Servicios de autenticación OAuth 2.0 cuando aplique.
- Generación/validación de QR, si se implementa como servicio independiente.

## 4. Flujo lógico de comunicación

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

## 6. Seguridad arquitectónica

- HTTPS/TLS 1.3.
- JWT para sesiones/API.
- OAuth 2.0 para autorización cuando corresponda.
- RBAC.
- Contraseñas almacenadas mediante mecanismos seguros de hashing.
- Cifrado en reposo según el requisito AES-256.
- QR con firma digital, SHA-256 y token de validación.
- Auditoría de accesos y cambios.

## 7. Disponibilidad

El SRS requiere disponibilidad 24/7. La arquitectura deberá considerar posteriormente:

- Despliegue redundante.
- Monitoreo.
- Backups.
- Recuperación ante fallos.
- Registro centralizado de errores.
- Health checks.

El proveedor y la estrategia concreta de infraestructura no están definidos por el SRS.

## 8. Principios de diseño

- Separación de responsabilidades.
- API como punto central de integración.
- Seguridad por defecto.
- Trazabilidad.
- Validación de datos.
- Evitar duplicidad de tickets.
- Transacciones para operaciones sensibles de inventario.
- Configuración mediante variables de entorno.
- Independencia entre clientes web y móvil.

## 9. Decisiones pendientes

- Proveedor de hosting.
- Estrategia CI/CD.
- Proveedor de SMS.
- Proveedor SMTP.
- Mecanismo exacto de firma digital del QR.
- Estrategia de almacenamiento de PDFs.
- Estrategia de observabilidad.
- Política de backups.
