import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { getUser } from '../services/auth'
import { rolesAllowRoute } from './accessMatrix'

// Se monta dentro de <ProtectedRoute> (ya se verificó la sesión), y además
// valida que alguno de los roles del usuario tenga acceso a la ruta actual
// según accessMatrix.js. Esto cierra el hueco de que escribir la URL a mano
// permitía saltarse el control que solo existía en el Sidebar.
export default function RoleProtectedRoute() {
  const location = useLocation()
  const userRoles = getUser()?.roles ?? []

  return rolesAllowRoute(userRoles, location.pathname)
    ? <Outlet />
    : <Navigate to="/acceso-no-autorizado" replace />
}
