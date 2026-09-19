# 27 - Control de Acceso Basado en Roles (RBAC) y Separación de Módulos

## 1. Contexto y Objetivos

Conforme a las directrices de seguridad y diseño de las Secciones 3 y 4 del SRS de FuelTrack, la aplicación debe garantizar que cada actor únicamente tenga acceso a los módulos y capacidades operativas pertinentes a su función.

Este documento formaliza la implementación de RBAC tanto en el frontend como en el backend, los guardianes de ruta, la navegación adaptativa y la protección de acciones en la interfaz.

---

## 2. Definición de Roles

1. **Administrador (`Administrador`)**: Control total del sistema, configuración global, gestión de usuarios, visualización de auditoría y reportes gerenciales.
2. **Supervisor (`Supervisor`)**: Gestión operativa y de abastecimiento, aprobación/rechazo de solicitudes, emisión/anulación de tickets, gestión de inventario/recepciones y mantenimiento de catálogos maestros.
3. **Despachador (`Despachador`)**: Personal de estación responsable de la lectura de tickets QR, ejecución de despachos físicos y cierres diarios de turno.
4. **Solicitante (`Solicitante`)**: Empleado de la organización que solicita vales de combustible para vehículos asignados y consulta el estado de sus tickets autorizados.
5. **Auditor (`Auditor`)**: Rol de fiscalización técnica y de cumplimiento. Dispone de acceso transversal de **solo lectura** sobre auditoría, transacciones, catálogos y reportes.
6. **Consulta (`Consulta`)**: Perfil de consulta pasiva sobre existencias de combustible, históricos de despacho y reportes.

---

## 3. Matriz de Autorización y Rutas

| Ruta Frontend | Endpoint Principal Backend | Roles Autorizados |
| :--- | :--- | :--- |
| `/dashboard` | `GET /api/v1/dashboard/*` | `Administrador` |
| `/usuarios` | `/api/v1/usuarios/*` | `Administrador` |
| `/auditoria` | `GET /api/v1/auditoria` | `Administrador`, `Auditor` |
| `/solicitudes` | `/api/v1/solicitudes/*` | `Administrador`, `Supervisor`, `Solicitante` *(propias)*, `Auditor` *(lectura)* |
| `/solicitudes-recurrentes` | `/api/v1/solicitudes-recurrentes/*` | `Administrador`, `Supervisor` |
| `/tickets` | `/api/v1/tickets/*` | `Administrador`, `Supervisor`, `Despachador`, `Solicitante` *(propios)*, `Auditor`, `Consulta` |
| `/despachos` | `/api/v1/despachos/*` | `Administrador`, `Supervisor`, `Despachador`, `Auditor` *(lectura)*, `Consulta` *(lectura)* |
| `/cierres-diarios` | `/api/v1/cierres-diarios/*` | `Administrador`, `Supervisor`, `Despachador`, `Auditor` *(lectura)*, `Consulta` *(lectura)* |
| `/inventario` | `/api/v1/inventario/*` | `Administrador`, `Supervisor`, `Auditor` *(lectura)*, `Consulta` *(lectura)* |
| `/recepciones` | `/api/v1/recepciones/*` | `Administrador`, `Supervisor` |
| `/tanques` | `/api/v1/tanques/*` | `Administrador`, `Supervisor` |
| `/tipos-combustible` | `/api/v1/tipos-combustible/*` | `Administrador`, `Supervisor` |
| `/proveedores` | `/api/v1/proveedores/*` | `Administrador`, `Supervisor` |
| `/estaciones` | `/api/v1/estaciones/*` | `Administrador`, `Supervisor`, `Despachador`, `Auditor`, `Consulta` |
| `/empleados` | `/api/v1/empleados/*` | `Administrador`, `Supervisor`, `Auditor` *(lectura)* |
| `/vehiculos` | `/api/v1/vehiculos/*` | `Administrador`, `Supervisor`, `Auditor` *(lectura)* |
| `/departamentos` | `/api/v1/departamentos/*` | `Administrador`, `Supervisor`, `Auditor` *(lectura)* |
| `/reportes` | `GET /api/v1/reportes/*` | `Administrador`, `Supervisor`, `Auditor`, `Consulta` |
| `/notificaciones` | `GET /api/v1/notificaciones/*` | `Administrador`, `Supervisor`, `Auditor` |

---

## 4. Implementación en Frontend

### 4.1. Módulo Central RBAC (`src/utils/rbac.js`)
- `ROLES`: Enumeración de constantes para evitar errores tipográficos.
- `ROUTE_ROLES`: Mapeo explícito ruta-roles.
- `canAccessRoute(user, path)`: Validador de acceso a rutas.
- `getDefaultRouteForUser(user)`: Redirección inteligente al iniciar sesión.
- `canManageCatalogs(user)`: Admin y Supervisor.
- `canApproveRequests(user)`: Admin y Supervisor.
- `canEmitTickets(user)`: Admin y Supervisor.
- `canPerformDispatch(user)`: Admin, Supervisor y Despachador.
- `canAdjustInventory(user)`: Admin y Supervisor.
- `canCreateDailyClose(user)`: Admin, Supervisor y Despachador.

### 4.2. Guardián de Rutas (`src/routes/RoleRoute.jsx`)
Intercepta intentos de navegación manual por URL hacia rutas fuera del perfil del usuario y lo redirige automáticamente a su ruta inicial por defecto (`getDefaultRouteForUser`).

### 4.3. Menús y Navegación Adaptativa
- **Sidebar (`src/components/Sidebar.jsx`)**: Filtra cada opción de navegación y elimina categorías vacías. Modifica el texto de tickets a *"Mis Tickets"* para el rol `Solicitante`.
- **BottomNav (`src/components/BottomNav.jsx`)**: Despliega pestañas táctiles móviles específicas para cada rol (Despachador ve Despachos, Tickets, Cierres; Solicitante ve Solicitudes y Mis Tickets; Auditor ve Reportes, Auditoría, Despachos, Tickets).
- **Header (`src/components/Header.jsx`)**: Incluye insignia distintiva con el rol activo del usuario autenticado.

### 4.4. Protección de Vistas y Supresión de Lenguaje Técnico
- En pantallas de catálogos y transacciones, las columnas de acciones ("Editar", "Desactivar", "Nuevo...") se ocultan por completo para perfiles de solo lectura (`Auditor`, `Consulta`).
- La acción de desactivación física o lógica queda restringida a `Administrador`.
- Se eliminó cualquier etiqueta de depuración interna (`OwnerFilter Activo`), sustituyéndose por mensajes amigables y funcionales.

---

## 5. Implementación en Backend

- Se ajustaron los atributos `[Authorize(Roles = ...)]` en:
  - `CierresDiariosController.cs`: Incorporación de `Roles.Despachador` para creación y `Roles.Consulta` para lectura.
  - `InventarioController.cs`: Habilitación de `Ajustar` para `Roles.Supervisor`.
  - `ReportesController.cs`: Habilitación para `Roles.Supervisor` y `Roles.Consulta`.
- Mantenimiento del filtro de propiedad de tickets y solicitudes en base de datos para usuarios con rol `Solicitante`.

---

## 6. Verificación de Calidad

- **Pruebas Unitarias Backend**: 299 tests aprobados al 100% (`dotnet test backend/FuelTrack.Api.Tests`).
- **Análisis Estático Frontend**: 0 errores en linter (`npm run lint`).
- **Compilación de Producción Frontend**: Exitosa (`npm run build`).
