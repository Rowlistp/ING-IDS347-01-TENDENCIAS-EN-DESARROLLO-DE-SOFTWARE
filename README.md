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
| Autenticación / API | JWT interno + OAuth2/OIDC con Keycloak 26.7.3 |
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
13. [División de trabajo](docs/13-DIVISION-EQUIPO.md)
14. [Reglas de trabajo simultáneo](docs/14-REGLAS-TRABAJO-SIMULTANEO.md)
15. [Plantilla de prompt](docs/15-PLANTILLA-PROMPT.md)
16. [Decisión .NET 10](docs/16-DECISION-NET10.md)
17. [Pruebas de Fase 1](docs/17-PRUEBAS-FASE1-SEGURIDAD.md)
18. [Cierre de Fase 1](docs/18-CIERRE-FASE1.md)
19. [Matriz de trazabilidad](docs/19-MATRIZ-TRAZABILIDAD.md)
20. [Decisiones técnicas](docs/20-DECISIONES-TECNICAS.md)
21. [Pruebas de Fase 4 — Tickets/QR](docs/21-PRUEBAS-FASE4-TICKETS-QR.md)
22. [Cierre de Fase 4](docs/22-CIERRE-FASE4.md)

Explicación pedagógica: [Fase 1 — Seguridad y administración](docs/explicaciones/fase-1-seguridad-administracion.md).

También existe un [índice interno de documentación](docs/README.md).

## Arquitectura general

```text
[React Web] --------\
                     \
                      >---- [API .NET 10] ---- [PostgreSQL]
                     /
[Flutter Mobile] ---/

                            |---- [Keycloak 26.7.3]
                            |---- [SMTP, pendiente]
                            |---- [SMS Gateway, pendiente]
```

## Estado actual

Las **Fases 1 y 4 están implementadas y validadas en backend**. Fase 1 incluye
autenticación local, OAuth2/OIDC con Keycloak 26.7.3, Authorization Code + PKCE
S256, JWT interno, RBAC local, usuarios, roles, sesiones y auditoría append-only.
Fase 4 incorpora emisión desde Solicitudes aprobadas, secuencia PostgreSQL, QR
ECDSA P-256/SHA-256, validación, estados, PDF, anulación y preparación de
notificaciones. Flutter/despacho y transporte SMTP/SMS siguen pendientes.

El repositorio también contiene el backend base y catálogos desarrollados por
otros builders. Esto no significa que el sistema completo ni las fases
posteriores estén terminados. La implementación usa .NET 10 y dispone del
workflow GitHub Actions `Backend Security` para compilar y ejecutar pruebas con
PostgreSQL y Keycloak reales.

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
- OAuth2/OIDC con Keycloak 26.7.3 y JWT interno.
- TLS 1.3.
- AES-256 para datos en reposo.
- SHA-256 y firma/token para códigos QR.
- Auditoría de operaciones sensibles.

TLS 1.3 y AES-256 en reposo requieren configuración y evidencia del despliegue
productivo. SMTP/SMS real y móvil pertenecen a fases posteriores.
