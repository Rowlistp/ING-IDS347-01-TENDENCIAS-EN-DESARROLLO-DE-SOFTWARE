# Guía manual — integración validada sobre main d86e7c9

Esta guía corresponde a la copia QA del repositorio, servida en **http://127.0.0.1:5175**. Recorre las pantallas y operaciones con datos de prueba. El informe `06-VERIFICACION-INTEGRACION.md` distingue lo comprobado de las integraciones y dispositivos que requieren otro entorno.

## Comprobaciones actuales sobre d86e7c9

Se completaron 431 pruebas del servidor, 6 web, 34 móviles y el E2E móvil/API/PostgreSQL. El recorrido actual contiene 166 casos aprobados, incluidos 152 de rol/ruta. Los ejemplos y datos del recorrido anterior se conservan para repetir los demás flujos.

### Validaciones combinadas de main

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Ajuste con combustible inactivo | En un entorno desechable, preparar un tanque con combustible inactivo y abrir un ajuste | El tanque no aparece en el selector; enviar el ID directamente devuelve 409 |
| Recepción con combustible inactivo | Usar el mismo fixture y abrir Registrar recepción | Tanque excluido; solicitud directa rechazada sin recepción ni descuento |
| Transferencia con combustible inactivo | Probar el fixture como origen y luego como destino | Tanque excluido; API rechaza el lado inactivo sin movimiento ni cambios de saldo |
| Formulario anterior al cambio | Abrir cada formulario cuando el combustible está activo; desactivarlo solo en el fixture de QA; confirmar | El servidor vuelve a verificar y rechaza la operación; saldos iguales |
| Restauración | Volver a activar el combustible fixture y recargar | El tanque vuelve a estar disponible según los permisos |
| Capacidad | Probar ajustes, recepciones y transferencias por encima del espacio libre | Rechazo aun con combustible activo; mismo saldo |
| Compatibilidad | Transferir entre combustibles distintos | Rechazo aun con ambos combustibles activos |
| Departamento derivado | Elegir empleado en solicitud/plantilla | Departamento automático, control deshabilitado y vehículos coherentes |
| Vehículo ajeno | Intentar enviar una solicitud con vehículo de otro departamento | Rechazo; no guardar solicitud ni auditoría de éxito |
| Contrato de tanque | Consultar lista y detalle desde la pestaña Red | `tipoCombustibleActivo` refleja el dato del servidor |

Las pruebas históricas/inactivas requieren un fixture de QA: la interfaz normal impide algunas combinaciones. No cambies directamente datos de producción para reproducirlas. En esta sesión se usaron combustible 5 y tanques 8/9, con saldos 5/0; el combustible quedó activo. La plantilla 4 de 0.55 galones quedó inactiva. La preparación se realizó fuera del repositorio; reproduce esos datos únicamente en una base de pruebas desechable.

## Orden recomendado para esta entrega

1. Repetir acceso y permisos de los ocho perfiles.
2. Seguir catálogos → solicitud → aprobación → ticket → QR → despacho → inventario → reportes.
3. Revisar los casos límite de cada sección con registros nuevos.
4. Completar el bloque **Integración de main y aplicación móvil** al final.

Los pasos generales de esta guía proceden de la UAT anterior y siguen siendo aplicables. Sus números de ejemplo son históricos: los tickets consumidos/anulados no sirven para repetir un despacho válido. En la integración actual, **INT-2026-000007** ya se consumió por **1.23 galones** y el tanque 1 quedó en **82.36**. **MOV-2026-000008** está vigente por **3.14 galones**, mientras no se use, anule o venza. Puedes usarlo para una primera comprobación; crea una solicitud nueva para repetir todo el ciclo.

**Ejecutado en la revisión anterior (76c81ed):** 168 comprobaciones de navegador (152 de roles/rutas), flujo firmado con consumo real, privacidad QR, error/reintento, reutilización, sincronización, recuperación de conexión y visor móvil. Servidor 419 pruebas, web 6, móvil 34 y E2E real aprobados. **Pendiente de equipo físico:** cámara/pistola/USB y Keycloak nativo. El informe enlaza la evidencia y separa los resultados históricos.

## Preparación

1. Abre la aplicación y prepara cuentas sintéticas para Administrador, Supervisor, Despachador, dos Solicitantes, Auditor, Consulta y combinaciones de roles. Mantén sus credenciales fuera del repositorio; los nombres de perfil de esta guía identifican esas cuentas de prueba.
2. Para trabajar con dos personas a la vez, usa ventanas privadas o perfiles de navegador separados. Dos pestañas normales comparten la misma sesión; sirven para probar el cierre simultáneo de sesión.
3. Usa nombres nuevos con un prefijo propio, por ejemplo `MAN-20260920-01`. Los registros `UAT` y `QA` que ya ves son sintéticos. No reutilices como ticket válido un QR que figure consumido o anulado.
4. Anota el número de solicitud, el código del ticket y los saldos antes y después. Captura la pantalla si el resultado no coincide con lo indicado.
5. Los importes son **galones**. La pantalla muestra horas del navegador; los PDF indican UTC. Una fecha civil de cierre debe mantener su día.

### Cuentas y permisos esperados

| Perfil | Qué debe poder hacer | Qué debe quedar fuera |
|---|---|---|
| Administrador | Todas las pantallas, usuarios, auditoría y operaciones | No puede saltar restricciones de inventario, estados o dependencias |
| Supervisor | Organización, suministro, solicitudes, tickets, despachos, cierres, reportes, notificaciones | Usuarios, auditoría y dashboard; desactivar catálogos activos |
| Despachador | Tickets, despachos, cierres y consulta de estaciones | Crear solicitudes, emitir/anular tickets, modificar catálogos o usuarios |
| Solicitante | Crear solicitudes propias y consultar sus solicitudes/tickets | Datos de otro solicitante, aprobación, despacho, administración |
| Auditor | Lectura de operaciones, inventario, organización, estaciones, reportes, auditoría, notificaciones | Botones de escritura y administración |
| Consulta | Lectura de tickets, despachos, cierres, inventario, estaciones y reportes | Escritura, organización, auditoría y usuarios |
| Auditor + Consulta (`multi`) | Unión de los permisos de lectura | Ninguna operación de escritura |
| Supervisor + Auditor (`uat`) | Escritura de Supervisor y lectura de auditoría | Desactivación administrativa y gestión de usuarios |

La [matriz de permisos](../27-CONTROL-ACCESO-ROLES-RBAC.md) contiene las 19 rutas. Para cada cuenta, comprueba tanto el menú como escribir directamente una dirección prohibida: debe aparecer «Acceso no autorizado», sin ejecutar la operación.

## 1. Acceso, sesión y navegación

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Acceso de cada perfil | Ingresar con las ocho cuentas base; revisar nombre, rol y menú | Pantalla inicial y opciones correspondientes al perfil |
| Contraseña incorrecta | Cambiar un carácter al ingresar | Rechazo sin entrar ni mostrar datos privados |
| Salir | Abrir otra pestaña de la aplicación; pulsar «Salir» en la primera | Ambas vuelven al acceso; la sesión anterior deja de renovarse |
| Sesión vencida | Dejar vencer la sesión y abrir una pantalla que solicite datos | Regreso al login sin pantalla en blanco |
| Caché incompleta — caso avanzado | En herramientas del navegador, cambiar el valor de `user` en almacenamiento local a `{invalid`; recargar | Login recuperable, sin error que inutilice la interfaz |
| Ruta inexistente | Abrir `/ruta-que-no-existe` | Página 404 y opción «Volver al inicio» |
| Caída de conexión | Con Reportes abierto, desconectar la red, cambiar un filtro y aplicarlo; reconectar y limpiar filtros | Mensaje en español; después vuelve a cargar sin perder la sesión |

## 2. Catálogos: alta, edición y estado

Realiza las altas con Administrador. Usa las opciones de los selectores; conserva los identificadores de las nuevas filas. Para RNC y cédula se comprueba el formato; los datos de esta guía son ficticios.

| Pantalla | Datos sugeridos para una nueva prueba | Comprobación |
|---|---|---|
| Departamentos | Nombre `MAN-… Departamento` | Crear, editar el nombre, recargar y verificar persistencia |
| Empleados | Código propio, nombre, cédula ficticia de 11 dígitos, cargo, correo `…@example.test`, teléfono 809/829/849 de 10 dígitos, departamento activo | Alta y edición; obligatorios, formato de correo y teléfono |
| Vehículos | Placa única, ficha única, marca, modelo, año, tipo, departamento activo, capacidad 15, odómetro 0 | Alta, edición y recarga; impedir datos obligatorios vacíos |
| Proveedores | RNC ficticio único de 9 dígitos, nombre | Alta, edición y persistencia; formato del RNC |
| Tipos de combustible | Nombre único de combustible de prueba | Alta, edición y persistencia |
| Tanques | Identificación única como `MAN-DSL-01`, capacidad 200, nivel crítico 20, combustible activo | Alta con existencia cero; nunca seleccionar combustible inactivo |
| Estaciones | Nombre propio | Alta, edición, desactivación y reactivación |

Repite estas comprobaciones de estado:

- **Cancelar:** abrir «Desactivar» y cancelar. La fila debe seguir activa.
- **Sin dependientes:** desactivar un registro de prueba sin referencias que lo bloqueen, recargar y reactivar. En algunas pantallas se reactiva desde «Editar» marcando «Activo».
- **Con dependientes:** departamento con empleados/vehículos activos, proveedor con recepciones, combustible con tanque activo y tanque con existencia deben rechazar la desactivación. El mensaje debe explicar el motivo.
- **Supervisor:** abrir «Editar» de un registro activo. La casilla de actividad no permite desactivarlo. La protección también existe en el servidor.
- **Dos vías equivalentes:** con Administrador, intentar desactivar desde «Editar» un departamento con dependientes. Debe rechazarlo igual que el botón «Desactivar».
- **Tanque con existencia:** intentar reducir su capacidad por debajo del saldo o cambiarle el combustible. Debe rechazarlo sin modificar el saldo.

Las filas `UAT Integral … Editado` permiten revisar el resultado del recorrido ejecutado. `UAT-DIESEL-20` conserva combustible y sirve para comprobar bloqueos por existencia.

## 3. Solicitud → autorización → ticket

| Paso | Perfil y acción | Resultado esperado |
|---|---|---|
| Solicitud propia | Solicitante 1: «Nueva solicitud», vehículo de su departamento, combustible activo, 4.32 galones y 7 días | Empleado/departamento propios fijos; solicitud Pendiente |
| Aislamiento | Solicitante 2: abrir Solicitudes y Tickets | No aparecen los datos de Solicitante 1 |
| Privacidad del formulario | En la pestaña Red del navegador, abrir Nueva solicitud y revisar la respuesta de empleados | Solo contiene la ficha del empleado vinculado; pedir una ficha ajena por ID devuelve 404 |
| Cantidad inválida | Probar cero, cantidad negativa y campos vacíos | No se guarda una solicitud inválida |
| Aprobación excesiva | Supervisor: aprobar la solicitud de 4.32 intentando 5 | Confirmación bloqueada; no autoriza por encima de lo pedido |
| Aprobación parcial | Cambiar a 2.16 y confirmar | Aprobada con 2.16 autorizados |
| Rechazo | Crear otra solicitud; Supervisor escribe un motivo y rechaza | Rechazada con motivo; no se puede aprobar después |
| Decisión simultánea | Abrir una misma Pendiente en dos perfiles; uno confirma aprobación y otro rechazo | Solo una decisión se guarda; el otro recibe aviso de solicitud procesada |
| Emisión | Supervisor: Tickets → «Emitir ticket» → solicitud aprobada → prefijo propio | Ticket con código único y cantidad autorizada correcta |
| Emisión repetida | Volver a intentar elegir la misma solicitud | No debe generar un segundo ticket vigente para esa autorización |
| Detalle y PDF | Abrir «Ver» y descargar PDF | Empleado, vehículo, cantidad, fechas y código coinciden; QR presente |
| Envío local | Pulsar «Enviar» en una fila y desde el detalle | Confirma registro para envío; el estado del detalle coincide con el servidor |

En la UAT anterior se usaron la solicitud **6**, ticket **UAT-2026-000005** y despacho **4**. Ese ticket ya está **consumido**: crea uno nuevo para repetir el flujo válido.

## 4. Lector y despacho

1. Entra como **Despachador** y abre «Nuevo despacho (Lector QR)».
2. Prueba texto que no sea un QR en «Pistola USB / Manual»: debe rechazarlo.
3. Usa «Subir imagen QR» con una imagen legible del código de un PDF válido recién generado. Debe mostrar empleado, vehículo, combustible y volumen autorizado.
4. Selecciona estación y tanque compatibles. Anota el saldo inicial.
5. Intenta servir más de lo autorizado: debe impedirlo. Intenta un tanque sin saldo suficiente cuando exista uno compatible: debe rechazarlo sin consumo.
6. Confirma la cantidad válida. Debe aparecer comprobante, un solo despacho y el saldo anterior menos los galones servidos.
7. Descarga el HTML e inspecciona el comprobante. En la prueba ejecutada: **87 − 2.16 = 84.84**.
8. Vuelve a leer el mismo QR: debe indicar que el ticket está consumido y no descontar otra vez.
9. Anula otro ticket vigente desde **Tickets → Ver → Anular ticket**, con Administrador o Supervisor. Debe abrir **un único diálogo**, permitir escribir y recorrerlo con Tab, y guardar el motivo.
10. Lee el QR anulado desde Despachos: debe rechazarlo. El ticket **UAC-2026-000006** sirve como ejemplo anulado.
11. Abre «Cámara en vivo» sin cámara/permisos: debe explicar el problema y permitir cambiar a imagen o entrada manual.

**Prueba física pendiente:** leer un QR con la cámara y/o pistola USB reales del equipo de destino. Aquí se verificaron el QR real desde imagen y el manejo de cámara no disponible; no se simuló una lectura física como si hubiera ocurrido.

## 5. Inventario, ajustes, transferencias y recepción

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Ajuste positivo | En tanque de prueba vacío, agregar 10 con motivo | Existencia y disponibilidad aumentan a 10; movimiento y auditoría |
| Ajuste negativo | Restar 3 con motivo | Ambos saldos quedan en 7 |
| Saldo negativo | Intentar restar más de lo disponible | Rechazo; mismo saldo y sin movimiento |
| Sobrecapacidad | Intentar agregar más que el espacio libre | Rechazo en servidor y mensaje de capacidad |
| Transferencia válida | Mismo combustible: mover 1.25 entre dos tanques | Uno resta 1.25, otro suma 1.25; el total se conserva |
| Mismo origen/destino | Seleccionar el mismo tanque en ambos campos | Rechazo sin cambios |
| Combustibles distintos | Seleccionar tanques de combustibles diferentes | Rechazo por incompatibilidad |
| Origen insuficiente | Mover más de lo que tiene el origen | Rechazo sin afectar ninguno de los tanques |
| Destino sin espacio | Transferir más que el espacio del destino | Rechazo sin sobrecapacidad |
| Historial | Abrir «Historial de movimientos» y filtrar tanque | Entradas, salidas y ajustes corresponden a lo realizado |
| Recepción válida | Proveedor activo, tanque activo, factura propia, cantidad dentro de capacidad | Recepción, comprobante y aumento exacto del inventario |
| Recepción con formulario anterior | A prepara una recepción que llena el tanque; B agrega 1 mediante ajuste; A guarda el formulario anterior | Rechazo por capacidad actual, aunque el formulario mostrara espacio suficiente |
| Recuperación de recepción | Tras el rechazo anterior, reducir a una cantidad permitida y guardar | Se guarda una sola recepción válida |
| Comprobante | Abrir recepción y descargar comprobante | Factura, proveedor, tanque y volumen correctos |

El caso concurrente se reprodujo con el tanque `UAT-DIESEL-20`. La carga que demostró el fallo fue compensada mediante un ajuste identificado como restauración de QA. Se conservó su rastro para auditoría.

## 6. Plantillas recurrentes

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Selección compatible | Supervisor: «Nueva plantilla», seleccionar empleado | Departamento completado y vehículos compatibles |
| Valores inválidos | Cero/negativo, campos vacíos, fecha de fin anterior o igual a inicio | Rechazo sin plantilla inválida |
| Alta válida | Cantidad decimal, periodicidad mensual, inicio y fin coherentes | Plantilla activa; no crea una solicitud inmediatamente |
| Pausar/reactivar | Desactivar y activar una plantilla válida | Cambia el estado; no duplica solicitudes |
| Datos que ya no sirven | Intentar activar plantilla con catálogo inactivo o relación incompatible | Rechazo claro |
| Plantilla vencida | Intentar activar una cuya fecha de fin ya pasó | Rechazo; requiere nueva plantilla |

El proceso automático se ejecuta a medianoche UTC. Se probaron automáticamente la generación, las periodicidades, la revalidación y la prevención de duplicado del mismo día. Las plantillas sintéticas creadas durante el recorrido quedaron inactivas.

## 7. Cierres, reportes, auditoría y notificaciones

| Área | Prueba manual | Resultado esperado |
|---|---|---|
| Cierre | Fecha futura | Rechazo sin cierre |
| Cierre | Fecha sin despachos | Aviso «No hay despachos» y sin acta vacía |
| Cierre | Fecha que ya tiene cierre | Rechazo por duplicado |
| Cierre | Abrir Detalle y Acta PDF de un cierre existente | Misma fecha civil y totales registrados; instantánea estable |
| Cierre nuevo válido | En una jornada con despachos y sin cierre previo, generar acta | Un único cierre y PDF. La base de este recorrido ya tenía cerrados sus días con despachos; la creación válida está cubierta por regresión y por la etapa anterior |
| Reportes | Consultar Solicitudes, Tickets, Despachos, Inventario y Cierres | Datos reales de la operación |
| Exportaciones | En cada uno de esos cinco tipos, descargar CSV, Excel y PDF | 15 archivos legibles con columnas y registros coherentes |
| Filtros | Aplicar empleado, tanque, combustible, estado y fechas cuando estén disponibles | Vista y exportación usan los mismos filtros |
| Filtros vacíos | Buscar rango sin resultados | Estado vacío comprensible y navegación correcta |
| Paginación | Inventario con más de 20 movimientos: avanzar, volver, cambiar filtro | Página válida y resultados actualizados |
| Auditoría | Auditor: buscar eventos de las altas, ajustes, solicitudes y despachos de prueba | Actor, evento, referencia y fecha correspondientes; sin edición |
| Notificaciones | Filtrar Email, SMS y estado | Solo el canal/estado seleccionado; no confundir Pendiente con Enviada |
| Entrega local | Ver correo del ticket en el buzón de pruebas http://127.0.0.1:18030 | Correo sintético recibido; sin usar destinatarios externos |

No reenvíes a direcciones reales como parte de esta prueba. SMS está deshabilitado en esta configuración; pendiente no equivale a entrega.

## 8. Usuarios y revocación

1. Administrador: crear un usuario con contraseña que cumpla la política y roles Supervisor + Auditor. La contraseña corta debe rechazarse.
2. Ingresar con él: debe poder crear catálogos y leer auditoría. Auditor no debe quitar los permisos de Supervisor.
3. Con esa sesión abierta, Administrador restablece la contraseña. Al pedir datos, la sesión anterior debe volver al acceso. La contraseña nueva debe funcionar.
4. Desactivar ese usuario. Su siguiente petición debe ser rechazada y tampoco debe poder iniciar sesión.
5. Reactivarlo: recupera sus permisos. Evita usar cuentas principales para estas pruebas.

## 9. Pantalla pequeña y teclado

- Recorre las 19 pantallas a **390 × 844** y luego en escritorio. La página no debe desbordarse horizontalmente; las tarjetas deben permitir consultar detalles y acciones.
- Abre/cierra el menú móvil y entra a otra sección. Debe cerrarse al navegar.
- En formularios, pulsa Tab y Shift+Tab varias veces: el foco permanece dentro del diálogo. Escape cierra sin guardar.
- Abre la anulación desde el detalle del ticket: un solo diálogo y escritura normal del motivo.
- Pulsa las etiquetas de filtros en Notificaciones y Despachos: llevan al control correcto. Un lector de pantalla puede identificar esos campos.
- La revisión realizada cubre estructura, etiquetas y teclado; no representa una certificación completa de accesibilidad.

## Criterio de aceptación y registro

Marca cada fila como **Pasa**, **Falla** o **No aplica**. Un rechazo esperado por capacidad, autorización, estado o falta de datos cuenta como Pasa. Una pantalla en blanco, error inesperado, saldo incorrecto, dato ajeno o escritura no autorizada cuenta como Falla.

Conserva para cualquier fallo: perfil, pantalla, datos usados, pasos, resultado esperado, resultado obtenido, hora y captura. El [resumen de verificación](06-VERIFICACION-INTEGRACION.md) documenta el alcance y los resultados; las evidencias completas y los registros de reproducciones se conservan en el entorno QA.


## Integración de main y aplicación móvil

### QR, firma, errores y actualización

| Caso | Pasos manuales | Resultado esperado |
|---|---|---|
| Visor vigente | Tickets → MOV-2026-000008 → QR, antes de consumirlo | Imagen oficial, conductor, placa, combustible y galones correctos |
| Desde detalle | Ver → Ver Código QR | Un solo diálogo; Tab permanece dentro; Escape cierra y permite seguir trabajando |
| Pantalla estrecha | Repetir con ancho 390 px o navegador del teléfono | QR completo, datos y botones visibles; página sin desbordamiento que impida operar |
| Estado terminal | Abrir QR de INT consumido o UAC anulado | Muestra el estado, sin imagen QR operativa |
| Privacidad | Solicitante 2 intenta abrir el QR de un ticket de Persona 1 | No aparece en su lista; la descarga ajena devuelve 404 |
| Número sin firma | Despachos → Nuevo despacho → Pistola USB / Manual; escribir solo código MOV o GUID | Rechazo de QR inválido; sin despacho |
| Firma alterada | En una copia del contenido firmado de un QR de prueba, cambiar un carácter; validar | Rechazo sin descuento; no compartir el contenido firmado |
| Imagen auténtica | Subir una imagen legible del QR del ticket vigente | Validación real; conductor, placa y volumen coinciden |
| Descarga QR fallida | Bloquear temporalmente la petición `/tickets/{id}/qr` con herramientas del navegador; abrir QR | Error visible, sin QR alternativo; desbloquear y pulsar Reintentar QR recupera la imagen |
| Actualización fallida | Con Tickets abierto, interrumpir temporalmente GET `/tickets` | Indica Sin actualizar y error; puede ocultar la tabla durante la interrupción |
| Recuperación | Restablecer la petición anterior | Vuelve Actualización automática y la tabla, sin recargar |
| Dos sesiones | Dejar QR abierto en una sesión y consumirlo en otra | La primera cambia a Consumido y retira el QR; actualización periódica en página visible |
| Reutilización | Volver a subir el QR que acaba de consumirse | Rechazo; mismo saldo y número de despachos |
| Enlace antiguo | Abrir `/qr.html` | Dirige a Tickets; exige sesión si no existe |
| Coherencia posterior | Aprobar una solicitud de prueba; cambiar el departamento de su empleado; intentar emitir | Rechazo por incoherencia; restaurar el departamento y comprobar el caso válido |

### APK y acceso móvil

1. Compilar el APK de depuración siguiendo [mobile/README](../../mobile/README.md). Requiere Android y depuración USB autorizada para este modo de conexión.
2. Confirmar que la API local responde en el puerto **5300**. Conectar dispositivo y ejecutar `adb reverse tcp:5300 tcp:5300`; instalar el APK de depuración. Con más de un dispositivo, seleccionar el destino explícitamente al ejecutar ADB.
3. Abrir FuelTrack: debe mostrar servidor `http://127.0.0.1:5300/api/v1` y pedir usuario/contraseña reales. Usar una cuenta de QA con rol Despachador para confirmar consumos. Usa las cuentas sintéticas preparadas para este entorno.
4. Intentar una clave incorrecta: no debe entrar ni inventar un usuario. Cortar conexión antes de entrar: debe explicar el error, sin otorgar acceso. Restablecer y entrar correctamente.
5. Revisar perfil, roles y servidor; cerrar sesión y volver a entrar. Reiniciar la app para comprobar restauración de una sesión válida. Una sesión vencida o revocada no debe autorizar operaciones.
6. Repetir escaneo → validación → tanque compatible → cantidad → comprobante usando un ticket nuevo. Anotar saldo inicial y confirmar descuento exacto en la web.
7. Repetir con perfil Consulta: podrá consultar/validar según sus permisos, sin confirmar despacho.

### Casos físicos y poco comunes pendientes de repetir en destino

| Caso | Acción | Resultado esperado |
|---|---|---|
| Permiso de cámara | Denegarlo, volver a abrir lector y probar recuperación/entrada manual | Mensaje comprensible, sin bloqueo permanente |
| Lectura real | Escanear pantalla y PDF, en luz normal y baja | Una validación por lectura; datos del ticket correcto |
| Dispositivo sin linterna | Pulsar linterna/cambio de cámara si están disponibles | Error manejado, sin cierre inesperado |
| Repetición rápida | Mantener el mismo QR enfocado y pulsar confirmar dos veces rápidamente | Un solo despacho, un solo descuento |
| Campo decimal | Cantidad con punto; cuando el teclado lo permita, también con coma | Previsualización y cantidad coherentes; no sobrepasa autorización |
| API sin estaciones/tanques utilizables | Usar entorno de pruebas vacío o catálogos inactivos | Ningún catálogo inventado y despacho bloqueado |
| Caída antes de confirmar | Interrumpir conexión y confirmar | No informa éxito sin respuesta; permite recuperación segura |
| Respuesta incierta | Interrumpir después de enviar confirmación; consultar estado antes de repetir | Reconciliación; no duplicar un consumo que ya se guardó |
| Reabrir tras consumo | Cerrar/reabrir app y consultar ticket | Consumido según servidor, no disponible otra vez |
| USB desconectado | Retirar cable cuando API usa reverse | Error de conexión recuperable, sin datos de demostración |
| Cambio de entorno | Compilar para otra API/modo de autenticación | No reutiliza sesión del servidor anterior |
| Keycloak | Compilar con AUTH_MODE=keycloak y configurar issuer/cliente/callback; ingresar en navegador nativo | PKCE, usuario local vinculado y permisos reales; rechazo a identidad desconocida/inactiva |

La configuración QA descrita usa acceso local. Keycloak exige otra configuración de compilación y servicios activos, como indica [mobile/README](../../mobile/README.md). Las pruebas automatizadas de esta entrega cubren varios errores y concurrencia, pero no reemplazan la observación física de esta tabla.

Registra cada resultado con perfil, dispositivo, versión, pasos, esperado, observado y captura sin contraseñas ni QR vigente. Si cambias datos para forzar un caso, restaura catálogos y saldos mediante operaciones auditadas de prueba.
