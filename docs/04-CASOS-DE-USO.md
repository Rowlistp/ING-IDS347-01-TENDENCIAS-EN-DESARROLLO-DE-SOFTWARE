# 04 - Casos de Uso

## 1. Objetivo

Describir los principales casos de uso identificados a partir del SRS.

## 2. Actores

- Administrador General.
- Supervisor de Combustible.
- Despachador.
- Solicitante.
- Auditor.
- Sistema.
- Servicios externos.

## 3. Casos de uso principales

### CU-01 Iniciar sesión

**Actor:** Usuario autorizado.  
**Precondición:** Usuario activo.  
**Flujo principal:**

1. El usuario introduce sus credenciales.
2. El sistema valida identidad.
3. El sistema aplica las políticas de acceso.
4. Se crea la sesión/token correspondiente.
5. El usuario accede según su rol.

### CU-02 Gestionar usuarios

**Actor:** Administrador.  
Incluye crear, modificar, desactivar, gestionar roles y restablecer contraseñas.

### CU-03 Gestionar empleados

**Actor:** Administrador.  
Permite registrar y actualizar los datos establecidos en RF-02.

### CU-04 Gestionar vehículos

**Actor:** Administrador.  
Permite registrar y actualizar placa, ficha, marca, modelo, año, tipo, departamento, capacidad, odómetro y estado.

### CU-05 Gestionar departamentos

**Actor:** Administrador.  
Permite crear, modificar y asociar empleados y vehículos.

### CU-06 Crear solicitud de combustible

**Actor:** Solicitante / usuario autorizado.  
**Resultado:** Solicitud registrada para revisión o procesamiento.

### CU-07 Aprobar o gestionar solicitud

**Actor:** Supervisor de Combustible.  
**Resultado:** Solicitud autorizada, rechazada o procesada según reglas definidas posteriormente.

> El SRS asigna al Supervisor la responsabilidad de aprobaciones, pero no detalla estados ni flujo exacto de rechazo.

### CU-08 Emitir ticket digital

**Actor:** Supervisor / sistema.  
**Precondición:** Solicitud válida/autorizada.  
**Resultado:** Ticket único con UUID, secuencia, fechas, vehículo, empleado, departamento, cantidad y combustible.

### CU-09 Generar QR seguro

**Actor:** Sistema.  
**Resultado:** QR único, no editable, no reutilizable y verificable criptográficamente.

### CU-10 Enviar ticket

**Actor:** Sistema.  
**Canales:** correo electrónico y SMS.

### CU-11 Consultar estado de ticket

**Actores:** Solicitante, Supervisor, Despachador, Auditor según permisos.  
Estados definidos: Creado, Enviado, Pendiente, Próximo a vencer, Vencido, Consumido, Anulado.

### CU-12 Escanear y validar QR

**Actor:** Despachador.  
**Flujo:**

1. Escanea el QR.
2. La app envía los datos a la API.
3. El sistema valida firma/token, estado y vigencia.
4. Se muestra confirmación visual.

### CU-13 Registrar despacho

**Actor:** Despachador.  
**Datos:** fecha, hora, galones, operador, estación y observaciones.  
**Resultado:** despacho registrado e inventario afectado.

### CU-14 Registrar recepción de combustible

**Actor:** Supervisor.  
**Datos:** RNC, suplidor, factura, volumen, fecha y tanque.  
**Resultado:** entrada de inventario.

### CU-15 Registrar ajuste de inventario

**Actor:** Usuario autorizado.  
Tipos: positivo o negativo.

### CU-16 Consultar inventario

**Actor:** Supervisor / Administrador.  
Muestra existencia, disponibilidad, consumo diario, consumo mensual y nivel crítico.

### CU-17 Ejecutar cierre diario

**Actor:** Despachador / Supervisor.  
Registra despachos, volumen, inventario final y diferencias. Genera acta y reporte PDF.

### CU-18 Generar reporte

**Actor:** Auditor / Administración.  
Filtros: fecha, empleado, vehículo, departamento, combustible y estado.

### CU-19 Exportar reporte

**Actor:** Auditor / Administración.  
Formatos: Excel, CSV y PDF.

### CU-20 Revisar auditoría

**Actor:** Auditor.  
Consulta creaciones, modificaciones, despachos, ajustes, anulaciones y accesos.

### CU-21 Consultar dashboard

**Actor:** Administración.  
Muestra indicadores definidos en RF-22.

### CU-22 Recibir notificación

**Actor:** Usuario/Sistema.  
Eventos: vencimiento, inventario bajo, fallo de integración y ajustes.

## 4. Casos pendientes de refinamiento

- Reglas de aprobación/rechazo.
- Reglas de asignación automática.
- Anulación de tickets.
- Transferencias entre inventarios/tanques.
- Gestión detallada de estaciones.
- Resolución de diferencias en cierre diario.
