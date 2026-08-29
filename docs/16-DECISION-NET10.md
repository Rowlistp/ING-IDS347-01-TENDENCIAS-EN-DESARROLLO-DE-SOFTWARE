# 16 - Decisión de Plataforma Backend: .NET 10

## Estado

**Aprobada por el equipo:** 29 de agosto de 2026.

## Contexto

El SRS original del proyecto menciona .NET 8 Web API como tecnología de backend. El SRS se conserva sin modificaciones como documento fuente.

Durante la preparación técnica del proyecto, el backend de Fase 0 fue creado sobre .NET 10. El equipo decidió continuar el desarrollo utilizando .NET 10 en lugar de regresar el proyecto a .NET 8.

## Decisión

La implementación del proyecto utilizará:

- .NET 10 Web API.
- Entity Framework Core.
- PostgreSQL.
- React + Tailwind CSS para web.
- Flutter para móvil.
- JWT + OAuth 2.0 para autenticación/autorización.

## Consecuencias

- `docs/SRS.md` no se modifica.
- La documentación técnica del equipo debe referirse a .NET 10.
- Los nuevos paquetes ASP.NET Core agregados por el equipo deben ser compatibles con .NET 10.
- Builder 2 implementará la Fase 1 de seguridad sobre el backend existente en `main`.
- Cualquier cambio de versión de Entity Framework Core o Npgsql debe coordinarse con Builder 1, responsable de backend y datos.

## Alcance de Builder 2

Builder 2 es responsable de:

- Fase 1: autenticación, JWT, RBAC, usuarios/roles y auditoría base.
- Fase 4: tickets y QR seguro.
- Fase 5: aplicación móvil Flutter y flujo de validación/despacho.
- Fase 9: SMTP, SMS y notificaciones.

Esta decisión no autoriza a Builder 2 a modificar lógica de inventario o catálogos perteneciente a Builder 1 salvo coordinación explícita.
