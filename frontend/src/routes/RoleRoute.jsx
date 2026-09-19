import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { canAccessRoute, getDefaultRouteForUser } from '../utils/rbac'

export default function RoleRoute({ children, path, allowedRoles }) {
  const { user } = useAuth()
  const location = useLocation()
  const targetPath = path || location.pathname

  const isAllowed = allowedRoles
    ? user?.roles?.includes('Administrador') || user?.roles?.some((r) => allowedRoles.includes(r))
    : canAccessRoute(user, targetPath)

  if (!isAllowed) {
    const defaultRoute = getDefaultRouteForUser(user)
    return <Navigate to={defaultRoute} replace />
  }

  return children
}
