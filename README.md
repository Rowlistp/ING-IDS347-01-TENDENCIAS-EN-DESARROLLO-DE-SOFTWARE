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

## Arranque local reproducible

Con Docker Compose, Python 3 y OpenSSL, desde la raíz:

```sh
python3 infra/local/setup.py
docker compose --env-file .env.local -f compose.local.yaml up --build -d
```

Abrir **http://127.0.0.1:5351**. Usuario `local.admin`; contraseña generada en `.env.local` (`LOCAL_ADMIN_PASSWORD`). El archivo no se publica en Git. Este modo usa PostgreSQL y claves locales independientes del alojamiento opcional. Ver [ejecución local y Android](docs/31-EJECUCION-LOCAL.md).

## Documentación del proyecto

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
23. [Pruebas Fase 5 — Móvil y despacho](docs/23-PRUEBAS-FASE5-MOVIL-DESPACHO.md)
24. [Cierre de Fase 5](docs/24-CIERRE-FASE5.md)
25. [Pruebas Fase 9 — Notificaciones](docs/25-PRUEBAS-FASE9-NOTIFICACIONES.md)
26. [Cierre de Fase 9](docs/26-CIERRE-FASE9.md)

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
                            |---- [SMTP MailKit / Mailpit local]
                            |---- [SMS HTTP Gateway configurable]
```

## Estado actual

La aplicación implementa catálogos, solicitudes manuales y recurrentes, tickets PDF/QR, validación y despacho, inventario, recepciones, cierres, reportes, notificaciones y auditoría. Web y Android comparten la API y sus reglas. El acceso admite cuentas locales y OAuth2/OIDC mediante Keycloak.

La última suite completa del servidor registró 463 pruebas aprobadas, ninguna fallida ni omitida. La evidencia y los límites de aceptación están en [verificación](docs/presentacion/VERIFICACION.md) y [matriz SRS](docs/presentacion/MATRIZ-SRS.md). Entrega externa de SMS/correo, validación física y garantías productivas se acreditan por separado. El SRS fuente se conserva sin modificaciones; la decisión de usar .NET 10 está documentada.

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
productivo. Los transportes SMTP/SMS están probados contra servidores locales
reales; esto no acredita entrega productiva ni la prueba física del móvil.
