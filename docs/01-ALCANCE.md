# 01 - Alcance del Proyecto

## 1. Objetivo

Definir el alcance funcional y tÃ©cnico de la **Plataforma Web y AplicaciÃ³n MÃ³vil para GestiÃ³n de Tickets Digitales e Inventario de Combustible**, tomando como referencia el SRS versiÃ³n 1.0 de agosto de 2026.

El sistema busca controlar de forma integral las solicitudes, emisiÃ³n de tickets digitales con cÃ³digo QR, despacho de combustible, inventario, auditorÃ­a, cierres diarios y reportes.

## 2. Alcance funcional incluido

SegÃºn el SRS, el proyecto contempla:

### Plataforma web

- AdministraciÃ³n del sistema.
- GestiÃ³n de usuarios.
- GestiÃ³n de empleados.
- GestiÃ³n de vehÃ­culos.
- GestiÃ³n de departamentos.
- GestiÃ³n de inventario.
- ConfiguraciÃ³n de parÃ¡metros.
- CreaciÃ³n y emisiÃ³n de tickets.
- AnÃ¡lisis y reportes.

### AplicaciÃ³n mÃ³vil

- Inicio de sesiÃ³n seguro.
- Escaneo de cÃ³digos QR.
- ValidaciÃ³n de tickets.
- Registro de despacho.
- Consulta de tickets.
- SincronizaciÃ³n inmediata.
- OperaciÃ³n en estaciÃ³n de combustible.

### Servicios de integraciÃ³n

- GeneraciÃ³n de cÃ³digos QR.
- MensajerÃ­a SMS.
- Correo electrÃ³nico.
- API REST unificada.
- Servicio de autenticaciÃ³n.

## 3. Procesos principales cubiertos

1. Registrar y administrar usuarios, empleados, vehÃ­culos y departamentos.
2. Crear solicitudes de combustible manuales, automÃ¡ticas o recurrentes.
3. Emitir tickets digitales Ãºnicos.
4. Generar un cÃ³digo QR seguro asociado al ticket.
5. Enviar el ticket por correo electrÃ³nico y/o SMS.
6. Validar el ticket desde la aplicaciÃ³n mÃ³vil.
7. Registrar el despacho realizado.
8. Actualizar el inventario con entradas, salidas, transferencias, mermas y ajustes.
9. Registrar recepciones de combustible.
10. Ejecutar el cierre diario.
11. Generar reportes y exportarlos.
12. Mantener auditorÃ­a y trazabilidad de las operaciones.
13. Mostrar indicadores en un dashboard ejecutivo.
14. Generar notificaciones automÃ¡ticas.

## 4. Actores contemplados

- Administrador General.
- Supervisor de Combustible.
- Despachador.
- Solicitante.
- Auditor.
- Usuario de consulta, segÃºn la definiciÃ³n mÃ­nima de roles del RF-01.

## 5. Requisitos de calidad y seguridad incluidos

- Disponibilidad 24/7.
- AutenticaciÃ³n por usuario y contraseÃ±a.
- MFA opcional.
- Control de acceso basado en roles.
- TLS 1.3 para datos en trÃ¡nsito.
- AES-256 para datos en reposo.
- OAuth 2.0 y JWT para APIs.
- QR con firma digital, hash SHA-256 y token de validaciÃ³n.
- AuditorÃ­a de accesos y operaciones relevantes.

## 6. Decisiones tÃ©cnicas del equipo

El SRS propone varias alternativas. Para este proyecto el equipo ha definido:

- Backend: .NET 10 Web API.
- ORM: Entity Framework Core.
- Base de datos: PostgreSQL.
- Frontend web: React.
- Estilos web: Tailwind CSS.
- AplicaciÃ³n mÃ³vil: Flutter.
- AutenticaciÃ³n de APIs: JWT + OAuth 2.0.

Estas decisiones concretan la arquitectura sin modificar el contenido original del SRS.

## 7. Aspectos no definidos explÃ­citamente por el SRS

El SRS no especifica con suficiente detalle:

- Proveedor de infraestructura cloud.
- Proveedor especÃ­fico de SMS.
- Proveedor especÃ­fico de correo.
- Estrategia exacta de respaldo y recuperaciÃ³n.
- DiseÃ±o visual final.
- Esquema fÃ­sico de base de datos.
- Contrato detallado de cada endpoint.
- Reglas exactas de negocio para asignaciones automÃ¡ticas.
- PolÃ­tica detallada de retenciÃ³n de auditorÃ­a.
- Estrategia de operaciÃ³n sin conexiÃ³n.
- Soporte para iOS.

Por tanto, estos puntos requieren decisiones posteriores y no deben asumirse como cerrados sin aprobaciÃ³n.

## 8. Entregables documentales previstos

- Arquitectura.
- Requisitos detallados.
- Casos de uso.
- Modelo conceptual de datos.
- DiseÃ±o inicial de API REST.
- Estrategia de seguridad.
- Matriz de roles y permisos.
- Flujos de negocio.
- Plan de pruebas.
- Estrategia de despliegue.
- PlanificaciÃ³n de desarrollo.

## 9. Criterios generales de aceptaciÃ³n

La soluciÃ³n serÃ¡ aceptada cuando se cumplan los siete criterios definidos en el SRS:

1. Tickets QR Ãºnicos y sin duplicidad.
2. Despacho validado exclusivamente mediante QR vÃ¡lido.
3. Inventario actualizado en tiempo real.
4. Trazabilidad completa.
5. Reportes exportables.
6. AplicaciÃ³n mÃ³vil operativa en producciÃ³n.
7. Cumplimiento de los requisitos de seguridad.

