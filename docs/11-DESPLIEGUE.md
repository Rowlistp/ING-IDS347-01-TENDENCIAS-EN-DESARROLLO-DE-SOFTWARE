# 11 - Estrategia de Despliegue

## 1. Objetivo

Definir una estrategia inicial de despliegue sin seleccionar todavía un proveedor específico de infraestructura.

## 2. Entornos

### Desarrollo

- Uso por el equipo.
- Datos no productivos.
- Configuración flexible.
- Logs detallados.

### Pruebas / Staging

- Configuración similar a producción.
- Integraciones controladas.
- Pruebas funcionales y de aceptación.

### Producción

- HTTPS obligatorio.
- Datos reales.
- Backups.
- Monitoreo.
- Acceso restringido.
- Alta disponibilidad acorde al requisito 24/7.

## 3. Componentes a desplegar

- Frontend React.
- API .NET 8.
- PostgreSQL.
- Aplicación Flutter Android.
- Servicio SMTP.
- SMS Gateway.
- Servicios de autenticación.
- Almacenamiento de reportes/PDF si aplica.

## 4. Configuración

Separar configuración de código mediante variables de entorno:

```text
DATABASE_CONNECTION
JWT_*
OAUTH_*
SMTP_*
SMS_*
QR_SIGNING_*
```

Los nombres definitivos se decidirán durante implementación.

## 5. Seguridad de despliegue

- TLS 1.3.
- Secretos fuera del repositorio.
- Acceso administrativo restringido.
- Backups cifrados.
- Logs protegidos.
- Actualizaciones de dependencias.
- Separación de ambientes.

## 6. Base de datos

Considerar:

- Migraciones de EF Core.
- Backups automáticos.
- Pruebas de restauración.
- Monitoreo de capacidad.
- Control de acceso.
- Cifrado en reposo.

## 7. Observabilidad

El SRS exige disponibilidad y alertas ante fallo de integración.

Se recomienda incluir:

- Health checks.
- Logs estructurados.
- Métricas.
- Alertas.
- Seguimiento de errores.
- Monitoreo de integraciones.

## 8. CI/CD

No está especificado en el SRS.

Propuesta futura:

```text
Commit/PR
   |
Build
   |
Tests
   |
Security checks
   |
Artifact
   |
Deploy staging
   |
Approval
   |
Deploy production
```

## 9. Aplicación móvil

Para Flutter Android:

- Build firmado.
- Configuración de API por ambiente.
- Protección de secretos.
- Pruebas en dispositivo físico.
- Distribución definida antes de producción.

## 10. Alta disponibilidad

El SRS exige 24/7, pero no define SLA.

Antes de producción deben definirse:

- SLA.
- RPO.
- RTO.
- Redundancia.
- Estrategia de failover.
- Escalamiento.
- Ventanas de mantenimiento.

## 11. Pendientes

- Proveedor cloud/on-premise.
- Dominio.
- Certificados.
- Contenedores sí/no.
- Kubernetes sí/no.
- Servicio de almacenamiento.
- Gestión de secretos.
- Retención de backups.
