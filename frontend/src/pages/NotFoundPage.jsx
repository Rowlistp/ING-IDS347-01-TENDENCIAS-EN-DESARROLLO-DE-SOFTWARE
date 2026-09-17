import { Link } from 'react-router-dom'
import Layout from '../components/Layout'
import { isAuthenticated } from '../services/auth'

function NotFoundContent() {
  return (
    <div className="flex min-h-full items-center justify-center py-20 text-center">
      <div>
        <p className="font-mono text-6xl font-bold text-tanque">404</p>
        <h1 className="mt-2 text-xl font-semibold text-tinta">Página no encontrada</h1>
        <p className="mt-1 text-sm text-acero">La ruta que buscas no existe o fue movida.</p>
        <Link
          to="/dashboard"
          className="mt-6 inline-block rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          Volver al Dashboard
        </Link>
      </div>
    </div>
  )
}

// React Router no puede elegir entre una ruta catch-all dentro del Layout protegido
// y una fuera según el estado de autenticación en tiempo de ejecución — el empate de
// especificidad ("*" en ambas) lo resuelve por orden de declaración, no por sesión.
// Por eso hay una sola ruta catch-all (ver AppRoutes.jsx) y esta página decide su
// propia presentación: con sidebar/header si hay sesión, en pantalla completa si no.
export default function NotFoundPage() {
  if (isAuthenticated()) {
    return (
      <Layout>
        <NotFoundContent />
      </Layout>
    )
  }

  return (
    <div className="flex h-screen items-center justify-center bg-fondo">
      <NotFoundContent />
    </div>
  )
}
