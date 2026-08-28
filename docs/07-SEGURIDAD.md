# 07 - Estrategia de Seguridad

## 1. Objetivo

Traducir los requisitos RS-01 a RS-06 del SRS en una estrategia técnica inicial.

## 2. Autenticación

El SRS requiere:

- Usuario/contraseña.
- MFA opcional.
- Gestión de sesiones.

El equipo ha definido JWT + OAuth 2.0 para la API.

### Propuesta

- Access token de corta duración.
- Refresh token protegido.
- Revocación de sesiones.
- Bloqueo/desactivación de usuarios.
- MFA configurable para roles sensibles.

## 3. Autorización

El SRS exige RBAC.

Los permisos deben evaluarse siempre en backend. El frontend puede ocultar opciones, pero no sustituye la autorización del servidor.

## 4. Seguridad de comunicaciones

- TLS 1.3.
- HTTPS obligatorio.
- No permitir credenciales o tokens por HTTP sin cifrar.

## 5. Datos en reposo

El SRS exige AES-256.

La implementación concreta debe definirse según infraestructura:

- cifrado de almacenamiento;
- cifrado de backups;
- protección de secretos;
- protección de campos especialmente sensibles cuando sea necesario.

## 6. Seguridad de contraseñas

Aunque el SRS no define el algoritmo de password hashing, las contraseñas no deben almacenarse cifradas de forma reversible ni en texto plano.

La selección del algoritmo y parámetros debe formalizarse durante implementación.

## 7. Seguridad del QR

El SRS requiere:

- Firma digital.
- Hash SHA-256.
- Token de validación.
- Unicidad.
- No reutilización.
- Verificación criptográfica.

### Principios

- El QR no debe ser considerado válido solo porque pueda leerse.
- El backend debe verificar integridad, vigencia, estado y token.
- Un ticket consumido debe fallar en validaciones posteriores.
- Debe existir protección contra alteración.
- Debe evitarse colocar datos sensibles innecesarios dentro del QR.

## 8. Auditoría

Registrar como mínimo:

- Accesos.
- Cambios.
- Despachos.
- Ajustes.
- Creaciones.
- Modificaciones.
- Anulaciones.

Datos mínimos:

- Usuario.
- Fecha.
- Hora.
- IP.

La condición de "registro inalterable" del SRS requiere una estrategia específica que debe definirse antes de producción.

## 9. Seguridad de API

- JWT.
- OAuth 2.0.
- Autorización por rol.
- Validación estricta de entradas.
- Protección contra exposición de errores internos.
- CORS restringido.
- Rate limiting a definir.
- Logs sin secretos.
- Rotación de credenciales.

## 10. Gestión de secretos

Nunca almacenar en Git:

- Contraseñas.
- Connection strings reales.
- Secretos JWT.
- Claves OAuth.
- API keys de SMS.
- Credenciales SMTP.
- Claves de firma.

Usar variables de entorno o un gestor de secretos.

## 11. Riesgos prioritarios

- Reutilización de QR.
- Manipulación de tickets.
- Doble despacho.
- Elevación de privilegios.
- Cambios no autorizados de inventario.
- Fuga de tokens.
- Alteración de auditoría.
- Exposición de datos personales.

## 12. Pendientes de definición

- Duración de tokens.
- Política MFA.
- Política de contraseñas.
- Rotación de secretos.
- Estrategia de firma digital.
- Estrategia de auditoría inmutable.
- Retención de logs.
- Respuesta a incidentes.
