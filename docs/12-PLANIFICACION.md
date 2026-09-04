# 12 - Planificación del Desarrollo

## 1. Objetivo

Proponer un orden de trabajo basado en dependencias y riesgo, sin modificar los requisitos del SRS.

## 2. Principios

- Construir primero la base de seguridad y datos.
- Implementar después los catálogos.
- Luego solicitudes y tickets.
- Después despacho e inventario.
- Finalmente reportes, notificaciones y endurecimiento.
- Mantener trazabilidad desde el inicio.

## 3. Fases propuestas

### Fase 0 - Preparación

Entregables:

- SRS en repositorio.
- Alcance.
- Arquitectura.
- Modelo de datos.
- Contrato de API.
- Estrategia de seguridad.
- Repositorio base.
- Convenciones.

### Fase 1 - Seguridad y administración — implementada y validada

Incluye:

- Autenticación.
- JWT/OAuth 2.0.
- RBAC.
- Usuarios.
- Roles.
- Auditoría base.

Requisitos relacionados:

- RF-01.
- RS-01 y RS-02 implementados según alcance.
- RS-05 implementado con Keycloak/OIDC + JWT.
- RS-06 con base transaccional y append-only; cobertura transversal continúa en fases posteriores.
- RS-03 depende del despliegue productivo y RS-04 pertenece a Fase 4.

### Fase 2 - Catálogos

Incluye:

- Empleados.
- Vehículos.
- Departamentos.
- Tipos de combustible.
- Tanques/estaciones si son aprobados como entidades.

Requisitos:

- RF-02.
- RF-03.
- RF-04.

### Fase 3 - Solicitudes

Incluye:

- Solicitudes manuales.
- Estados.
- Aprobaciones.
- Programación/recurrentes según refinamiento.

Requisitos:

- RF-05.
- RF-11.

### Fase 4 - Tickets y QR

Incluye:

- UUID.
- Secuencia.
- Prefijo.
- Expiración.
- QR seguro.
- Estados.
- PDF.
- Envío inicial.

Requisitos:

- RF-06 a RF-10.

### Fase 5 - Aplicación móvil y despacho

Incluye:

- Login.
- Escaneo.
- Validación.
- Confirmación.
- Registro de despacho.
- Sincronización.

Requisitos:

- RF-12.
- RF-13.

### Fase 6 - Inventario

Incluye:

- Recepciones.
- Entradas.
- Salidas.
- Ajustes.
- Transferencias.
- Inventario en tiempo real.
- Historial de movimientos.

Requisitos:

- RF-14 a RF-17.

### Fase 7 - Cierre diario

Incluye:

- Consolidación de despachos.
- Inventario final.
- Diferencias.
- Acta.
- PDF.

Requisito:

- RF-18.

### Fase 8 - Reportes y dashboard

Incluye:

- Filtros.
- Excel.
- CSV.
- PDF.
- Dashboard ejecutivo.

Requisitos:

- RF-19.
- RF-20.
- RF-22.

### Fase 9 - Notificaciones e integraciones

Incluye:

- SMTP.
- SMS.
- Alertas de vencimiento.
- Inventario bajo.
- Fallos de integración.
- Ajustes.

Requisitos:

- RF-09.
- RF-23.
- RF-24.

### Fase 10 - Estabilización

Incluye:

- Pruebas integrales.
- Seguridad.
- Rendimiento.
- Observabilidad.
- Backups.
- Despliegue.
- Aceptación.

## 4. Dependencias críticas

```text
Seguridad
   |
Catálogos
   |
Solicitudes
   |
Tickets/QR
   |
Despacho móvil
   |
Inventario
   |
Cierre
   |
Reportes
```

## 5. Riesgos principales

| Riesgo | Impacto |
|---|---|
| Reglas de negocio incompletas | Alto |
| Doble consumo de ticket | Alto |
| Descuadre de inventario | Alto |
| Integraciones SMS/SMTP | Medio |
| Requisito 24/7 sin SLA definido | Alto |
| Cobertura de auditoría aún pendiente en módulos de fases posteriores | Alto |
| Seguridad QR insuficientemente especificada | Alto |
| Cambios tardíos en modelo de datos | Alto |

## 6. Hitos sugeridos

1. Documentación base aprobada.
2. Autenticación + RBAC. **Completado y validado en Fase 1.**
3. Catálogos listos.
4. Solicitud funcional.
5. Ticket + QR seguro.
6. Despacho móvil.
7. Inventario integrado.
8. Cierre diario.
9. Reportes.
10. Pruebas y producción.

## 7. Definición de terminado por módulo

Un módulo no se considera terminado hasta que:

- Cumpla requisitos asociados.
- Tenga validaciones.
- Respete roles.
- Registre auditoría cuando aplique.
- Tenga pruebas.
- Tenga manejo de errores.
- Tenga documentación actualizada.

## 8. Pendientes antes de estimar calendario definitivo

- Disponibilidad semanal.
- Proveedor de infraestructura.
- Definición de reglas automáticas.
- Nivel de calidad esperado.
- Métricas de rendimiento.
- Proceso de aprobación del cliente.

La composición ya está definida: 6 integrantes, distribuidos en 3 builders y 3
testers, con responsabilidades registradas en `13-DIVISION-EQUIPO.md`.
