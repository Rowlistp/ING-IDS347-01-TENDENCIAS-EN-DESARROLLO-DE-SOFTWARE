import { Navigate, Route, Routes } from 'react-router-dom'
import Layout from '../components/Layout'
import AuditoriaPage from '../pages/AuditoriaPage'
import DashboardPage from '../pages/DashboardPage'
import CierreDiarioPage from '../pages/CierreDiarioPage'
import DepartamentosPage from '../pages/DepartamentosPage'
import DespachosPage from '../pages/DespachosPage'
import EmpleadosPage from '../pages/EmpleadosPage'
import EstacionesPage from '../pages/EstacionesPage'
import InventarioPage from '../pages/InventarioPage'
import LoginPage from '../pages/LoginPage'
import NotFoundPage from '../pages/NotFoundPage'
import NotificacionesPage from '../pages/NotificacionesPage'
import ProveedoresPage from '../pages/ProveedoresPage'
import RecepcionesPage from '../pages/RecepcionesPage'
import ReportesPage from '../pages/ReportesPage'
import SolicitudesPage from '../pages/SolicitudesPage'
import SolicitudesRecurrentesPage from '../pages/SolicitudesRecurrentesPage'
import TanquesPage from '../pages/TanquesPage'
import TicketsPage from '../pages/TicketsPage'
import TiposCombustiblePage from '../pages/TiposCombustiblePage'
import UsuariosPage from '../pages/UsuariosPage'
import VehiculosPage from '../pages/VehiculosPage'
import { getUser } from '../services/auth'
import { getDefaultRouteForUser } from '../utils/rbac'
import ProtectedRoute from './ProtectedRoute'
import RoleRoute from './RoleRoute'

function IndexRedirect() {
  const user = getUser()
  return <Navigate to={getDefaultRouteForUser(user)} replace />
}

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      {/* Único catch-all: NotFoundPage decide si mostrarse con sidebar/header
          (sesión activa) o en pantalla completa (sin sesión) — ver el comentario
          en NotFoundPage.jsx sobre por qué no hay un segundo "*" dentro del
          Layout protegido. */}
      <Route path="*" element={<NotFoundPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route path="/" element={<IndexRedirect />} />
          <Route path="/dashboard" element={<RoleRoute path="/dashboard"><DashboardPage /></RoleRoute>} />
          <Route path="/usuarios" element={<RoleRoute path="/usuarios"><UsuariosPage /></RoleRoute>} />
          <Route path="/empleados" element={<RoleRoute path="/empleados"><EmpleadosPage /></RoleRoute>} />
          <Route path="/vehiculos" element={<RoleRoute path="/vehiculos"><VehiculosPage /></RoleRoute>} />
          <Route path="/departamentos" element={<RoleRoute path="/departamentos"><DepartamentosPage /></RoleRoute>} />
          <Route path="/solicitudes" element={<RoleRoute path="/solicitudes"><SolicitudesPage /></RoleRoute>} />
          <Route path="/solicitudes-recurrentes" element={<RoleRoute path="/solicitudes-recurrentes"><SolicitudesRecurrentesPage /></RoleRoute>} />
          <Route path="/tickets" element={<RoleRoute path="/tickets"><TicketsPage /></RoleRoute>} />
          <Route path="/inventario" element={<RoleRoute path="/inventario"><InventarioPage /></RoleRoute>} />
          <Route path="/recepciones" element={<RoleRoute path="/recepciones"><RecepcionesPage /></RoleRoute>} />
          <Route path="/despachos" element={<RoleRoute path="/despachos"><DespachosPage /></RoleRoute>} />
          <Route path="/estaciones" element={<RoleRoute path="/estaciones"><EstacionesPage /></RoleRoute>} />
          <Route path="/cierres-diarios" element={<RoleRoute path="/cierres-diarios"><CierreDiarioPage /></RoleRoute>} />
          <Route path="/auditoria" element={<RoleRoute path="/auditoria"><AuditoriaPage /></RoleRoute>} />
          <Route path="/notificaciones" element={<RoleRoute path="/notificaciones"><NotificacionesPage /></RoleRoute>} />
          <Route path="/proveedores" element={<RoleRoute path="/proveedores"><ProveedoresPage /></RoleRoute>} />
          <Route path="/tanques" element={<RoleRoute path="/tanques"><TanquesPage /></RoleRoute>} />
          <Route path="/tipos-combustible" element={<RoleRoute path="/tipos-combustible"><TiposCombustiblePage /></RoleRoute>} />
          <Route path="/reportes" element={<RoleRoute path="/reportes"><ReportesPage /></RoleRoute>} />
        </Route>
      </Route>
    </Routes>
  )
}
