# 01 - Alcance del Proyecto

## 1. Objetivo

Definir el alcance funcional y técnico de la **Plataforma Web y Aplicación Móvil para Gestión de Tickets Digitales e Inventario de Combustible**, tomando como referencia el SRS versión 1.0 de agosto de 2026.

El sistema busca controlar de forma integral las solicitudes, emisión de tickets digitales con código QR, despacho de combustible, inventario, auditoría, cierres diarios y reportes.

## 2. Alcance funcional incluido

Según el SRS, el proyecto contempla:

### Plataforma web

- Administración del sistema.
- Gestión de usuarios.
- Gestión de empleados.
- Gestión de vehículos.
- Gestión de departamentos.
- Gestión de inventario.
- Configuración de parámetros.
- Creación y emisión de tickets.
- Análisis y reportes.

### Aplicación móvil

- Inicio de sesión seguro.
- Escaneo de códigos QR.
- Validación de tickets.
- Registro de despacho.
- Consulta de tickets.
- Sincronización inmediata.
- Operación en estación de combustible.

### Servicios de integración

- Generación de códigos QR.
- Mensajería SMS.
- Correo electrónico.
- API REST unificada.
- Servicio de autenticación.

## 3. Procesos principales cubiertos

1. Registrar y administrar usuarios, empleados, vehículos y departamentos.
2. Crear solicitudes de combustible manuales, automáticas o recurrentes.
3. Emitir tickets digitales únicos.
4. Generar un código QR seguro asociado al ticket.
5. Enviar el ticket por correo electrónico y/o SMS.
6. Validar el ticket desde la aplicación móvil.
7. Registrar el despacho realizado.
8. Actualizar el inventario con entradas, salidas, transferencias, mermas y ajustes.
9. Registrar recepciones de combustible.
10. Ejecutar el cierre diario.
11. Generar reportes y exportarlos.
12. Mantener auditoría y trazabilidad de las operaciones.
13. Mostrar indicadores en un dashboard ejecutivo.
14. Generar notificaciones automáticas.

## 4. Actores contemplados

- Administrador General.
- Supervisor de Combustible.
- Despachador.
- Solicitante.
- Auditor.
- Usuario de consulta, según la definición mínima de roles del RF-01.

## 5. Requisitos de calidad y seguridad incluidos

- Disponibilidad 24/7.
- Autenticación por usuario y contraseña.
- MFA opcional.
- Control de acceso basado en roles.
- TLS 1.3 para datos en tránsito.
- AES-256 para datos en reposo.
- OAuth 2.0 y JWT para APIs.
- QR con firma digital, hash SHA-256 y token de validación.
- Auditoría de accesos y operaciones relevantes.

## 6. Decisiones técnicas del equipo

El SRS propone varias alternativas. Para este proyecto el equipo ha definido:

- Backend: .NET 10 Web API.
- ORM: Entity Framework Core.
- Base de datos: PostgreSQL.
- Frontend web: React.
- Estilos web: Tailwind CSS.
- Aplicación móvil: Flutter.
- Autenticación de APIs: JWT interno + OAuth2/OIDC con Keycloak 26.7.3,
  Authorization Code y PKCE S256.

Estas decisiones concretan la arquitectura sin modificar el contenido original del SRS.

## 7. Aspectos no definidos explícitamente por el SRS

El SRS no especifica con suficiente detalle:

- Proveedor de infraestructura cloud.
- Proveedor específico de SMS.
- Proveedor específico de correo.
- Estrategia exacta de respaldo y recuperación.
- Diseño visual final.
- Esquema físico de base de datos.
- Contrato detallado de cada endpoint.
- Reglas exactas de negocio para asignaciones automáticas.
- Política detallada de retención de auditoría.
- Estrategia de operación sin conexión.
- Soporte para iOS.

Por tanto, estos puntos requieren decisiones posteriores y no deben asumirse como cerrados sin aprobación.

## 8. Entregables documentales previstos

- Arquitectura.
- Requisitos detallados.
- Casos de uso.
- Modelo conceptual de datos.
- Diseño inicial de API REST.
- Estrategia de seguridad.
- Matriz de roles y permisos.
- Flujos de negocio.
- Plan de pruebas.
- Estrategia de despliegue.
- Planificación de desarrollo.

## 9. Criterios generales de aceptación

La solución será aceptada cuando se cumplan los siete criterios definidos en el SRS:

1. Tickets QR únicos y sin duplicidad.
2. Despacho validado exclusivamente mediante QR válido.
3. Inventario actualizado en tiempo real.
4. Trazabilidad completa.
5. Reportes exportables.
6. Aplicación móvil operativa en producción.
7. Cumplimiento de los requisitos de seguridad.
