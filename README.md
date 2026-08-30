# Plataforma de Gestión de Tickets Digitales e Inventario de Combustible

Proyecto desarrollado para la asignatura **ING-IDS347-01 - Tendencias en Desarrollo de Software**.

## Descripción

La solución tiene como propósito gestionar de forma integral las solicitudes, emisión y validación de tickets digitales de combustible mediante códigos QR únicos, junto con el control de inventario, despachos, recepciones, cierres diarios, reportes y trazabilidad de las operaciones.

El proyecto contempla una plataforma web administrativa y una aplicación móvil destinada principalmente al proceso de validación y despacho de combustible.

## Stack tecnológico definido

| Componente | Tecnología |
|---|---|
| Backend | .NET 10 Web API |
| ORM | Entity Framework Core |
| Base de datos | PostgreSQL |
| Frontend web | React |
| Estilos web | Tailwind CSS |
| Aplicación móvil | Flutter |
| Autenticación / API | JWT + OAuth 2.0 |
| Integraciones | API REST, SMTP y SMS Gateway |

## Documentación

La documentación del proyecto se encuentra en [`docs/`](docs/).

### Documento fuente

- [SRS - Especificación de Requisitos](docs/SRS.md)

### Análisis y diseño

1. [Alcance del proyecto](docs/01-ALCANCE.md)
2. [Arquitectura del sistema](docs/02-ARQUITECTURA.md)
3. [Requisitos del sistema](docs/03-REQUISITOS.md)
4. [Casos de uso](docs/04-CASOS-DE-USO.md)
5. [Modelo conceptual de datos](docs/05-MODELO-DATOS.md)
6. [Diseño inicial de API REST](docs/06-API.md)
7. [Estrategia de seguridad](docs/07-SEGURIDAD.md)
8. [Roles y permisos](docs/08-ROLES-PERMISOS.md)
9. [Flujos de negocio](docs/09-FLUJOS-NEGOCIO.md)
10. [Plan de pruebas](docs/10-PLAN-PRUEBAS.md)
11. [Estrategia de despliegue](docs/11-DESPLIEGUE.md)
12. [Planificación del desarrollo](docs/12-PLANIFICACION.md)

También existe un [índice interno de documentación](docs/README.md).

## Arquitectura general

```text
[React Web] --------\
                     \
                      >---- [API .NET 10] ---- [PostgreSQL]
                     /
[Flutter Mobile] ---/

                            |---- [SMTP]
                            |---- [SMS Gateway]
                            |---- [OAuth 2.0]
```

## Estado actual

Actualmente el repositorio contiene la **documentación base de análisis, arquitectura y planificación** del proyecto.

El SRS se conserva como documento fuente. Los documentos adicionales organizan los requisitos y registran las decisiones técnicas del equipo sin sustituir el contenido original.

## Alcance funcional principal

- Gestión de usuarios, empleados, vehículos y departamentos.
- Solicitudes de combustible.
- Tickets digitales únicos.
- Códigos QR seguros.
- Validación y despacho desde aplicación móvil.
- Control de inventario en tiempo real.
- Recepción y movimientos de combustible.
- Cierre diario.
- Reportes y exportaciones.
- Dashboard ejecutivo.
- Notificaciones.
- Auditoría y trazabilidad.

## Seguridad

La arquitectura contempla los requisitos establecidos en el SRS, incluyendo:

- RBAC.
- JWT.
- OAuth 2.0.
- TLS 1.3.
- AES-256 para datos en reposo.
- SHA-256 y firma/token para códigos QR.
- Auditoría de operaciones sensibles.
