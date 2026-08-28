import { Navigate, Route, Routes } from 'react-router-dom'
import Layout from '../components/Layout'
import DashboardPage from '../pages/DashboardPage'
import DepartamentosPage from '../pages/DepartamentosPage'
import EmpleadosPage from '../pages/EmpleadosPage'
import LoginPage from '../pages/LoginPage'
import SolicitudesPage from '../pages/SolicitudesPage'
import TicketsPage from '../pages/TicketsPage'
import UsuariosPage from '../pages/UsuariosPage'
import VehiculosPage from '../pages/VehiculosPage'

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<Layout />}>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/usuarios" element={<UsuariosPage />} />
        <Route path="/empleados" element={<EmpleadosPage />} />
        <Route path="/vehiculos" element={<VehiculosPage />} />
        <Route path="/departamentos" element={<DepartamentosPage />} />
        <Route path="/solicitudes" element={<SolicitudesPage />} />
        <Route path="/tickets" element={<TicketsPage />} />
      </Route>
    </Routes>
  )
}
