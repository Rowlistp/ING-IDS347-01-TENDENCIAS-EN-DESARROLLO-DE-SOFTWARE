import { Navigate, Route, Routes } from 'react-router-dom'
import Layout from '../components/Layout'
import AuditoriaPage from '../pages/AuditoriaPage'
import DashboardPage from '../pages/DashboardPage'
import CierreDiarioPage from '../pages/CierreDiarioPage'
import DepartamentosPage from '../pages/DepartamentosPage'
import DespachosPage from '../pages/DespachosPage'
import EmpleadosPage from '../pages/EmpleadosPage'
import InventarioPage from '../pages/InventarioPage'
import LoginPage from '../pages/LoginPage'
import NotFoundPage from '../pages/NotFoundPage'
import ProveedoresPage from '../pages/ProveedoresPage'
import RecepcionesPage from '../pages/RecepcionesPage'
import SolicitudesPage from '../pages/SolicitudesPage'
import TanquesPage from '../pages/TanquesPage'
import TicketsPage from '../pages/TicketsPage'
import TiposCombustiblePage from '../pages/TiposCombustiblePage'
import UsuariosPage from '../pages/UsuariosPage'
import VehiculosPage from '../pages/VehiculosPage'
import ProtectedRoute from './ProtectedRoute'

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
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/usuarios" element={<UsuariosPage />} />
          <Route path="/empleados" element={<EmpleadosPage />} />
          <Route path="/vehiculos" element={<VehiculosPage />} />
          <Route path="/departamentos" element={<DepartamentosPage />} />
          <Route path="/solicitudes" element={<SolicitudesPage />} />
          <Route path="/tickets" element={<TicketsPage />} />
          <Route path="/inventario" element={<InventarioPage />} />
          <Route path="/recepciones" element={<RecepcionesPage />} />
          <Route path="/despachos" element={<DespachosPage />} />
          <Route path="/cierres-diarios" element={<CierreDiarioPage />} />
          <Route path="/auditoria" element={<AuditoriaPage />} />
          <Route path="/proveedores" element={<ProveedoresPage />} />
          <Route path="/tanques" element={<TanquesPage />} />
          <Route path="/tipos-combustible" element={<TiposCombustiblePage />} />
        </Route>
      </Route>
    </Routes>
  )
}
