# 03 — Requisitos

## Requisitos funcionales

### RF-01 Generar licencia
El sistema debe permitir crear una licencia asociada a usuario, producto, vigencia y política de uso.

### RF-02 Firmar licencia
El sistema debe proteger criptográficamente los campos relevantes de la licencia.

### RF-03 Validar online
El cliente debe poder solicitar validación al servidor.

### RF-04 Validar offline
El cliente debe poder validar localmente una licencia durante una ventana autorizada.

### RF-05 Revocación manual
Un administrador debe poder revocar una licencia.

### RF-06 Revocación automática
El sistema debe poder invalidar licencias por reglas como expiración.

### RF-07 Verificar usuario
La licencia debe corresponder al usuario autorizado.

### RF-08 Redirección válida
Una licencia válida debe producir un mecanismo de acceso temporal a la aplicación.

### RF-09 Redirección inválida
Una licencia inválida, vencida, suspendida o revocada debe producir una respuesta de error diferenciada.

## Requisitos no funcionales

### RNF-01 Integridad
Modificar un campo protegido debe invalidar la firma.

### RNF-02 Autenticidad
Solo el servidor autorizado debe poder emitir licencias válidas.

### RNF-03 Crypto agility
Los algoritmos deben poder sustituirse sin reescribir todo el sistema.

### RNF-04 Rendimiento
Se medirán tiempos de firma/verificación y tamaños.

### RNF-05 Compatibilidad
Deben poder coexistir distintas versiones de licencia.

### RNF-06 Auditabilidad
Los eventos críticos deben quedar registrados.

### RNF-07 Mantenibilidad
La criptografía debe estar desacoplada de API, base de datos y UI.
