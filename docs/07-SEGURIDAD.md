# 07 - Estrategia de Seguridad

## 1. Objetivo

Traducir los requisitos RS-01 a RS-06 del SRS en una estrategia técnica inicial.

## 2. Autenticación

El SRS requiere:

- Usuario/contraseña.
- MFA opcional.
- Gestión de sesiones.

JWT con access token corto y refresh token rotatorio es el mecanismo activo de
Fase 1.

Fase 1 integra Keycloak `26.7.2` como proveedor OAuth 2.0/OIDC. El realm es
`fueltrack`; `fueltrack-web` y `fueltrack-mobile` son clientes públicos sin
secreto, con Authorization Code + PKCE S256 obligatorio. Implicit Flow y Direct
Access Grants están deshabilitados. `fueltrack-api` representa la audiencia.

La API mantiene dos validadores explícitos: JWT interno y Keycloak. Para Keycloak
valida firma, issuer, audience y vigencia; luego resuelve `preferred_username`
contra `Usuario.NombreUsuario`. Solo un usuario local activo puede entrar. Los
roles externos se ignoran y la autorización usa exclusivamente roles locales de
PostgreSQL. No se crean usuarios ni administradores automáticamente.

**MFA pendiente: no implementado ni configurado en Fase 1.**

### Propuesta

- Access token de corta duración.
- Refresh token protegido.
- Revocación de sesiones.
- Bloqueo/desactivación de usuarios.
- MFA configurable para roles sensibles cuando se apruebe su política.

Los JWT incluyen una versión de seguridad del usuario. Cada petición autenticada
comprueba en base de datos que el usuario siga activo y que la versión coincida.
Desactivar, restablecer contraseña o cambiar roles incrementa esta versión e
invalida inmediatamente los access tokens anteriores, además de revocar refresh
tokens cuando corresponde.

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

### Decisión de Fase 1

- PBKDF2-HMAC-SHA-512 con salt aleatorio de 128 bits.
- 210,000 iteraciones y hash de 256 bits.
- Comparación en tiempo constante.
- Entre 12 y 128 caracteres, sin espacios, con mayúscula, minúscula, número y carácter especial.
- La política se aplica en el servicio al crear usuarios, restablecer contraseñas y crear el administrador inicial.

La verificación solo acepta el formato versionado `PBKDF2-SHA512`, entre
100,000 y 1,000,000 iteraciones, salt de 16 bytes y hash de 32 bytes. Los hashes
corruptos, sobredimensionados o con parámetros fuera de rango se rechazan sin
propagar errores internos.

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

`GET /api/v1/audit` permite consulta paginada únicamente a `Administrador` y
`Auditor`. La respuesta omite `DatosRelevantes` para no exponer accidentalmente
hashes, tokens u otros secretos históricos.

Una migración PostgreSQL instala un trigger append-only que permite `INSERT` y
rechaza `UPDATE`/`DELETE` sobre `Auditorias`. Las escrituras sensibles y su
auditoría comparten transacción. La política de retención sigue siendo una
decisión operativa posterior.

## 9. Seguridad de API

- JWT.
- OAuth 2.0/OIDC Keycloak con Authorization Code + PKCE S256.
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
- Política e implementación MFA.
- Rotación de secretos.
- Alta disponibilidad y endurecimiento productivo de Keycloak.
- Retención de logs.
- Respuesta a incidentes.
