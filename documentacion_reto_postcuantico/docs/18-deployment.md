# 18 — Despliegue y operación inicial

## Entornos
- development
- test
- production

## Configuración
Usar variables de entorno o mecanismo de secretos.

## Requisitos mínimos
- HTTPS en producción.
- Secretos fuera del repositorio.
- Logs sin claves privadas.
- Base de datos con respaldo.
- Política de rotación de claves.
- Manejo de errores sin exponer información sensible.

## CI/CD inicial
Se recomienda automatizar:
- pruebas;
- linting;
- build;
- benchmark opcional.

## No requerido para MVP
- HSM;
- multi-región;
- alta disponibilidad avanzada;
- autoscaling empresarial.

## Operación
Debe existir procedimiento para:
- generar nuevas claves;
- rotarlas;
- revocar licencias;
- revisar eventos;
- restaurar respaldos;
- responder ante compromiso de clave.
