# Plataforma de GestiÃ³n de Tickets Digitales e Inventario de Combustible

Proyecto desarrollado para la asignatura **ING-IDS347-01 - Tendencias en Desarrollo de Software**.

## DescripciÃ³n

La soluciÃ³n tiene como propÃ³sito gestionar de forma integral las solicitudes, emisiÃ³n y validaciÃ³n de tickets digitales de combustible mediante cÃ³digos QR Ãºnicos, junto con el control de inventario, despachos, recepciones, cierres diarios, reportes y trazabilidad de las operaciones.

El proyecto contempla una plataforma web administrativa y una aplicaciÃ³n mÃ³vil destinada principalmente al proceso de validaciÃ³n y despacho de combustible.

## Stack tecnolÃ³gico definido

| Componente | TecnologÃ­a |
|---|---|
| Backend | .NET 10 Web API |
| ORM | Entity Framework Core |
| Base de datos | PostgreSQL |
| Frontend web | React |
| Estilos web | Tailwind CSS |
| AplicaciÃ³n mÃ³vil | Flutter |
| AutenticaciÃ³n / API | JWT + OAuth 2.0 |
| Integraciones | API REST, SMTP y SMS Gateway |

## DocumentaciÃ³n

La documentaciÃ³n del proyecto se encuentra en [`docs/`](docs/).

### Documento fuente

- [SRS - EspecificaciÃ³n de Requisitos](docs/SRS.md)

### AnÃ¡lisis y diseÃ±o

1. [Alcance del proyecto](docs/01-ALCANCE.md)
2. [Arquitectura del sistema](docs/02-ARQUITECTURA.md)
3. [Requisitos del sistema](docs/03-REQUISITOS.md)
4. [Casos de uso](docs/04-CASOS-DE-USO.md)
5. [Modelo conceptual de datos](docs/05-MODELO-DATOS.md)
6. [DiseÃ±o inicial de API REST](docs/06-API.md)
7. [Estrategia de seguridad](docs/07-SEGURIDAD.md)
8. [Roles y permisos](docs/08-ROLES-PERMISOS.md)
9. [Flujos de negocio](docs/09-FLUJOS-NEGOCIO.md)
10. [Plan de pruebas](docs/10-PLAN-PRUEBAS.md)
11. [Estrategia de despliegue](docs/11-DESPLIEGUE.md)
12. [PlanificaciÃ³n del desarrollo](docs/12-PLANIFICACION.md)

TambiÃ©n existe un [Ã­ndice interno de documentaciÃ³n](docs/README.md).

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

Actualmente el repositorio contiene la **documentaciÃ³n base de anÃ¡lisis, arquitectura y planificaciÃ³n** del proyecto.

El SRS se conserva como documento fuente. Los documentos adicionales organizan los requisitos y registran las decisiones tÃ©cnicas del equipo sin sustituir el contenido original.

## Alcance funcional principal

- GestiÃ³n de usuarios, empleados, vehÃ­culos y departamentos.
- Solicitudes de combustible.
- Tickets digitales Ãºnicos.
- CÃ³digos QR seguros.
- ValidaciÃ³n y despacho desde aplicaciÃ³n mÃ³vil.
- Control de inventario en tiempo real.
- RecepciÃ³n y movimientos de combustible.
- Cierre diario.
- Reportes y exportaciones.
- Dashboard ejecutivo.
- Notificaciones.
- AuditorÃ­a y trazabilidad.

## Seguridad

La arquitectura contempla los requisitos establecidos en el SRS, incluyendo:

- RBAC.
- JWT.
- OAuth 2.0.
- TLS 1.3.
- AES-256 para datos en reposo.
- SHA-256 y firma/token para cÃ³digos QR.
- AuditorÃ­a de operaciones sensibles.


