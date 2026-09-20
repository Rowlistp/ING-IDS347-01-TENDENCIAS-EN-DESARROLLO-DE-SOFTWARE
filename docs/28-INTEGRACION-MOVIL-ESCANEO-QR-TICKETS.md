# 28 - Integración de Escaneo Móvil de Tickets QR y Despacho en Estación

## 1. Contexto y Objetivos

Conforme a las especificaciones de las **Fases 4 y 5 del SRS de FuelTrack ERP**, el sistema debe proveer una solución integral y desacoplada para la emisión, validación óptica mediante código QR y despacho físico de combustible desde terminales móviles de estación.

Este documento formaliza la arquitectura técnica de interoperabilidad entre la **Plataforma Web (React)**, la **API Central (ASP.NET Core / .NET 10)** y la **Aplicación Móvil (Flutter)**, garantizando que el flujo de escaneo sea 100% auténtico, en tiempo real y con estricta integridad transaccional.

---

## 2. Ciclo de Vida del Ticket y Flujo Transaccional

```text
[Solicitud Creada] ──> [Aprobación Supervisor] ──> [Emisión de Ticket + QR]
                                                              │
                                                              ▼
[Despacho y Consumo] <── [Validación Óptica Móvil] <── [Lectura QR en Estación]
```

1. **Aprobación:** La solicitud de combustible es evaluada y aprobada por el Administrador o Supervisor.
2. **Emisión:** Se genera un ticket oficial con código correlativo (`TCK-YYYY-XXXXXX` o `COM-YYYY-XXXXXX`) y un identificador único global (UUID/GUID).
3. **Representación Óptica (QR):** Se codifica el identificador unívoco en el comprobante digital/PDF y en la pantalla de tickets web.
4. **Escaneo en Terminal Móvil:** El operador de estación enfoca la cámara física sobre el código QR en pantalla o papel.
5. **Resolución e Inspección:** La aplicación móvil consulta la API en caliente, desplegando los datos de vehículo, placa, conductor y tipo de combustible.
6. **Confirmación y Despacho:** Al registrar los galones despachados, el backend descuenta el combustible del inventario del tanque de la estación y transiciona atómicamente el estado del ticket a **`Consumido`**.

---

## 3. Matriz Técnica de Interoperabilidad

| Componente | Rol / Responsabilidad | Protocolo / Puerto | Endpoint Clave |
| :--- | :--- | :--- | :--- |
| **Backend API** | Autorización, reglas de negocio, integridad de inventario y estados | HTTP/REST (`0.0.0.0:5298`) | `GET /api/v1/tickets/{id}/qr`<br>`POST /api/v1/despachos` |
| **Frontend Web** | Gestión de solicitudes, emisión de tickets y generación interactiva de QR | HTTP / SPA (`5173`) | `/tickets`<br>`/qr.html` |
| **App Móvil** | Captura óptica por cámara, verificación visual del vehículo y despacho | Flutter / Android (`ADB Reverse` o LAN) | `ScannerScreen`<br>`DispatchScreen` |

### 3.1. Binding y Conectividad de Red Local
Para posibilitar la comunicación entre el dispositivo físico (ej. Samsung Galaxy Note 10+) y la máquina de desarrollo:
- **Binding de Socket:** La API escucha en `http://0.0.0.0:5298` (permitiendo tráfico de red local LAN `10.0.0.x`).
- **Enrutamiento USB (ADB Reverse):** El script `iniciar-servicios.bat` ejecuta `adb reverse tcp:5298 tcp:5298` y `adb reverse tcp:5173 tcp:5173`, permitiendo al dispositivo móvil acceder al backend utilizando `http://127.0.0.1:5298/api/v1` de forma transparente a través del cable USB.

---

## 4. Implementación Backend (`FuelTrack.Api`)

### 4.1. Resolución Dual de Tickets (`TicketService.cs`)
El servicio de tickets soporta resolución unívoca tanto por GUID como por el código correlativo oficial de ticket:
- Si el código escaneado cumple formato GUID (`Guid.TryParse`), se localiza por su clave primaria.
- Si no es GUID, se resuelve por `NumeroTicket` normalizado (soporte para correlativos `TCK-2026-001008`, `COM-2026-000001`, etc.).
- Se realiza carga ansiosa (`Include`) de las relaciones `Vehiculo`, `Empleado` (Conductor), `Departamento` y `TipoCombustible`, serializando la respuesta completa en el DTO de validación.

### 4.2. Transición Atómica de Estado a `Consumido`
En `DespachoService.cs`, la confirmación del despacho valida:
1. Existencia del ticket y estado vigente (`Activo` / `Aprobado`).
2. Disponibilidad de inventario en el tanque de la estación seleccionada.
3. Actualización de inventario físico del tanque.
4. Registro de auditoría inmutable.
5. Marcado del ticket como `Consumido`, impidiendo dobles despachos o reutilización fraudulenta.

---

## 5. Implementación Frontend Web (`frontend/`)

### 5.1. Generación de Código QR Real (`TicketsPage.jsx`)
- Se integró la codificación dinámica del código de ticket en el visor modal y comprobantes oficiales.
- Se añadió actualización reactiva de la grilla de datos para reflejar inmediatamente el cambio de estado cuando un ticket es despachado desde el móvil.

### 5.2. Utilidad de Pruebas Móviles (`public/qr.html`)
- Página liviana de autoservicio que lista los tickets autorizados y renderiza códigos QR de alta definición para facilitar las pruebas de enfoque óptico desde la cámara del celular.

---

## 6. Implementación Móvil Flutter (`mobile/`)

### 6.1. Escaneo con Cámara Física Real (`scanner.dart`)
- **Eliminación de Simulación Mock:** Se removió el botón obsoleto "Simular QR" que inyectaba identificadores estáticos en memoria.
- **Detección Óptica:** Uso de `mobile_scanner` con encuadre guiado y retroalimentación táctil/visual.
- **Entrada Manual de Contingencia:** Se preservó un botón limpio para digitación manual del número de ticket en caso de deterioro del código impreso o condiciones de baja luminosidad.

### 6.2. Modularización y Ergonomía de Pantallas
Se descompuso la arquitectura monolítica en módulos especializados:
- `mobile/lib/screens/login_screen.dart`: Autenticación con soporte de refresh token y diálogo de conexión limpio.
- `mobile/lib/screens/scan_screen.dart`: Vista de escaneo con validación inmediata contra la API.
- `mobile/lib/screens/dispatch_screen.dart`: Formulario de confirmación con límites de galonaje autorizados.
- `mobile/lib/screens/settings_screen.dart`: Configuración de URL de servidor simplificada (campo único, sin chips confusos).

---

## 7. Verificación de Calidad

1. **Backend Tests:**
   - Comando: `dotnet test backend/FuelTrack.Api.Tests`
   - Resultado: **321 exitosas, 0 fallidas** (100% de éxito).
2. **Mobile Tests:**
   - Comando: `flutter test`
   - Resultado: **26 exitosas, 0 fallidas** (100% de éxito).
3. **Análisis Estático Mobile:**
   - Comando: `flutter analyze`
   - Resultado: **0 advertencias / 0 errores**.
4. **Frontend Quality Gates:**
   - `npm run lint` (`oxlint`): **0 errores**.
   - `npm run build` (`vite build`): **Compilación exitosa (97 módulos)**.
