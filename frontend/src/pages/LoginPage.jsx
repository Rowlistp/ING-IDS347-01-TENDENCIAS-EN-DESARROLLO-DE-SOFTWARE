import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import apiRequest from '../services/api'
import { saveSession } from '../services/auth'

export default function LoginPage() {
  const navigate = useNavigate()
  const [nombreUsuario, setNombreUsuario] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  async function handleSubmit(e) {
    e.preventDefault()
    setLoading(true)
    setError(null)

    try {
      const auth = await apiRequest('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ nombreUsuario, contrasena }),
      })
      saveSession(auth)
      navigate('/dashboard', { replace: true })
    } catch (err) {
      setError(
        err instanceof TypeError
          ? 'No se pudo conectar con el servidor. Verifica tu conexión o que el backend esté corriendo.'
          : err.message
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex h-screen items-center justify-center bg-gray-50">
      <div className="w-full max-w-sm rounded-lg border border-gray-200 bg-white p-8 shadow-sm">
        <h1 className="text-xl font-semibold text-gray-800">Iniciar sesión</h1>

        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <div>
            <label htmlFor="nombreUsuario" className="mb-1 block text-sm font-medium text-gray-700">
              Usuario
            </label>
            <input
              id="nombreUsuario"
              type="text"
              value={nombreUsuario}
              onChange={(e) => setNombreUsuario(e.target.value)}
              required
              autoFocus
              autoComplete="username"
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label htmlFor="contrasena" className="mb-1 block text-sm font-medium text-gray-700">
              Contraseña
            </label>
            <input
              id="contrasena"
              type="password"
              value={contrasena}
              onChange={(e) => setContrasena(e.target.value)}
              required
              autoComplete="current-password"
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {loading ? 'Ingresando...' : 'Ingresar'}
          </button>
        </form>
      </div>
    </div>
  )
}
