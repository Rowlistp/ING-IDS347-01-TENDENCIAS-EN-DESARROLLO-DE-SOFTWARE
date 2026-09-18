import { Link } from 'react-router-dom'
import { getUser } from '../services/auth'
import { landingRouteForRoles } from '../routes/accessMatrix'

// Distinta de NotFoundPage a propósito: la ruta sí existe, el problema es que
// el rol del usuario no tiene permiso para verla (ver RoleProtectedRoute).
// Decirle "página no encontrada" sería engañoso y dificultaría distinguir un
// enlace roto de un permiso insuficiente. Esta pantalla siempre se renderiza
// dentro del Layout protegido (el usuario ya pasó ProtectedRoute), por lo que
// no necesita decidir su propia presentación como NotFoundPage.
export default function AccesoNoAutorizadoPage() {
  const destino = landingRouteForRoles(getUser()?.roles)
  return (
    <div className="flex min-h-full items-center justify-center py-20 text-center">
      <div>
        <p className="font-mono text-6xl font-bold text-peligro">403</p>
        <h1 className="mt-2 text-xl font-semibold text-tinta">Acceso no autorizado</h1>
        <p className="mt-1 text-sm text-acero">Tu rol no tiene permiso para ver esta página.</p>
        <Link
          to={destino}
          className="mt-6 inline-block rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          Volver al inicio
        </Link>
      </div>
    </div>
  )
}
