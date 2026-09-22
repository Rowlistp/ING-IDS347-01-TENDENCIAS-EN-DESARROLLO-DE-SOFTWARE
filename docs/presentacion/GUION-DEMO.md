# FuelTrack: guion para presentar al cliente

## Presentación

La presentación editable tiene 19 diapositivas y notas para el expositor. No hay una duración obligatoria. Recorrido sugerido: explicar el problema y la solución, enseñar el flujo y sus controles, presentar tecnología y trabajo del equipo, ejecutar la demo y cerrar con la matriz de aceptación. Si el profesor interrumpe, responder usando el producto y retomar el paso pendiente.

Reparto sugerido para seis integrantes (ajustable, no asigna nombres): apertura y necesidades; roles y solicitudes; tickets y seguridad; móvil y despacho; inventario, reportes y cierre; tecnología, calidad y preguntas del cliente. Una sola persona controla el equipo durante cada tramo de la demo.

Frase de apertura: «FuelTrack permite solicitar, autorizar y despachar combustible, y después comprobar qué pasó con cada ticket y con el inventario. Vamos a mostrar una operación completa con distintos roles».

## Preparación del ensayo

- Usar la base de demostración con datos ficticios. No activar notificaciones hacia números o correos ajenos al equipo.
- Web local: http://127.0.0.1:5351. API local: http://127.0.0.1:5351/api/v1. En Android, configurar el mismo servidor y usar la conexión inversa USB/emulador explicada en mobile/README.md.
- Tener cuentas Administrador, Solicitante, Supervisor, Despachador y Auditor. Las credenciales van en un documento privado del equipo, nunca en diapositivas.
- Tener un empleado activo y asociado al solicitante, vehículo del mismo departamento, combustible activo, estación activa y tanque compatible con al menos 10 galones disponibles.
- Anotar la existencia y disponibilidad antes de empezar. Crear un ticket nuevo para cada ensayo; un ticket consumido no se reutiliza.
- Mantener abiertas pestañas o perfiles de navegador separados por rol para no confundir sesiones.
- El APK de QA usa acceso local y HTTP de desarrollo. No presentarlo como paquete firmado para producción.
- Verificar que el proyector permita leer los textos. Mantener un PDF de respaldo de la presentación si la herramienta del aula lo requiere.

## Demostración principal

| Paso | Quién | Acción | Resultado que se debe enseñar |
|---|---|---|---|
| 1 | Solicitante | Entrar y revisar el menú | Solo funciones autorizadas y datos propios |
| 2 | Solicitante | Crear solicitud por 2 galones para su empleado/vehículo | Pendiente, departamento coherente y cantidad correcta |
| 3 | Supervisor | Abrir esa solicitud y aprobar 2 galones | Estado aprobado y actor responsable |
| 4 | Supervisor | Emitir ticket con vigencia futura | Número correlativo, UUID, PDF y visor QR |
| 5 | Despachador | Abrir FuelTrack Android, iniciar sesión y validar el QR | Datos del ticket, empleado, vehículo, cantidad y vigencia |
| 6 | Despachador | Confirmar identidad y seleccionar estación/tanque compatible | La app permite confirmar solo con datos válidos |
| 7 | Despachador | Registrar exactamente 2 galones | Una confirmación y ticket consumido |
| 8 | Despachador | Intentar usar el mismo QR otra vez | Rechazo y ninguna segunda salida de inventario |
| 9 | Supervisor | Actualizar inventario y movimientos | Existencia anterior menos 2; movimiento ligado al despacho |
| 10 | Auditor | Revisar auditoría y reportes | Usuario, fecha/hora, operación y filtros aplicados |
| 11 | Auditor | Exportar CSV, Excel y PDF del reporte filtrado | Los archivos corresponden al mismo filtro |
| 12 | Despachador / Supervisor | Crear o revisar el cierre del día según permisos | Volumen, inventario final, diferencias y PDF de cierre |

En emulador, el ingreso manual debe recibir el **contenido completo del QR firmado**, no su UUID ni su número. Si se usa ese ingreso, decirlo expresamente: no es una demostración del lector físico. Para el teléfono real, mostrar el escaneo de cámara. Nunca proyectar tokens de sesión o claves.

## Flujos complementarios y casos límite

| Caso | Acción | Comprobación |
|---|---|---|
| Sin permiso | Auditor intenta una acción de escritura | No aparece la acción y la API rechaza una petición no autorizada |
| Privacidad | Solicitante revisa sus tickets | No puede descargar los del otro empleado |
| Sobrellenado | Recibir/ajustar más que el espacio libre | Rechazo; inventario intacto |
| Combustible incompatible | Transferir entre tanques de distintos tipos | Rechazo sin modificar ninguno |
| Formulario antiguo | Abrir formulario, desactivar el combustible en otra sesión y enviarlo | La API vuelve a validar y rechaza |
| Ticket alterado o vencido | Validar un QR modificado o con vigencia pasada | Sin despacho ni descuento |
| Sin red | Desconectar la conexión de prueba y consultar o validar | Mensaje de error; no se inventan datos ni se confirma un despacho |
| Histórico | Crear plantilla con «Calcular según consumo histórico» | La opción y el límite persisten; explicar cálculo y aprobación |
| Sin historial | Probar una plantilla histórica sin despachos anteriores | No genera cantidades inventadas |
| Doble ejecución | Ejecutar el programador dos veces el mismo día en prueba controlada | Una solicitud por plantilla/día |
| PWA | Instalar desde navegador compatible, apagar red y recargar | Aviso sin conexión; sin cachear datos de tickets o API |
| Sesión | Cerrar y volver a abrir | Requiere autenticación cuando se revoca la sesión |
| SMS | Vincular Textbee físico y emitir a un número del equipo | Confirmar recepción y descarga por HTTPS, no solo ENVIADA |

El programador procesa a medianoche UTC. Crear la plantilla no genera inmediatamente la solicitud. No cambiar la hora del equipo para fingir una ejecución en vivo. Las pruebas automatizadas verifican el proceso; para una demo programada preparar la plantilla con antelación.

## Respuestas a preguntas previsibles

**¿Qué lenguajes usaron?** C# en el servidor, JavaScript/JSX, HTML y CSS en web, Dart en Android y SQL en datos. React y Flutter son frameworks. PostgreSQL es el motor de base de datos.

**¿Por qué .NET 10 si el SRS dice .NET 8?** El equipo documentó esa decisión el 29/08/2026. Mostrar docs/16-DECISION-NET10.md. No esconder la diferencia ni atribuir aprobación al cliente sin acta.

**¿El QR se puede usar dos veces?** El servidor valida firma, vigencia y estado. El consumo y el inventario se confirman transaccionalmente; se probaron concurrencia y rollback.

**¿Funciona sin internet?** Las operaciones requieren conexión para validar el estado vigente y evitar consumo duplicado. La PWA muestra una página sin conexión; no registra despachos sin servidor.

**¿Está en producción?** La revisión acredita el entorno local y pruebas controladas. La operación productiva, el cifrado de infraestructura, las copias/restauración y los dispositivos reales tienen criterios de cierre propios en la matriz.

**¿Cuánto cuesta?** No se ha aprobado un presupuesto operativo. Textbee usa un teléfono y su plan SMS; revisar sus límites vigentes. No prometer costo cero ni ahorro porcentual no medido.

**¿Qué aportó la IA?** Apoyó desarrollo, diagnóstico y documentación. El equipo conserva responsabilidad sobre reglas, revisión de código, pruebas y explicación del resultado.

## Si algo falla durante la exposición

Mostrar el error y comprobar conexión/servicios antes de repetir. Verificar si la operación ya quedó registrada: no volver a enviar un despacho o SMS sin conocer su resultado. Para notificaciones usar el historial. Si falta el teléfono con SIM, enseñar la integración preparada y declarar la entrega externa pendiente. Las capturas de las diapositivas son respaldo ilustrativo, no sustituyen una operación en vivo.
