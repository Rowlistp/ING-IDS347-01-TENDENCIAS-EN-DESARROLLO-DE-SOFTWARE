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
    <div className="flex h-screen items-center justify-center bg-tanque">
      <div className="w-full max-w-sm rounded-sm border border-acero/30 bg-white p-8 shadow-xl">
        <p className="text-xs font-semibold uppercase tracking-widest text-acero">FuelTrack</p>
        <h1 className="mt-1 text-xl font-semibold text-tanque">Iniciar sesión</h1>

        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <div>
            <label htmlFor="nombreUsuario" className="mb-1 block text-sm font-medium text-acero">
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
              className="w-full rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta focus:outline-none focus:ring-2 focus:ring-tanque/50 focus:border-tanque"
            />
          </div>

          <div>
            <label htmlFor="contrasena" className="mb-1 block text-sm font-medium text-acero">
              Contraseña
            </label>
            <input
              id="contrasena"
              type="password"
              value={contrasena}
              onChange={(e) => setContrasena(e.target.value)}
              required
              autoComplete="current-password"
              className="w-full rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta focus:outline-none focus:ring-2 focus:ring-tanque/50 focus:border-tanque"
            />
          </div>

          {error && <p className="rounded-sm border border-peligro/40 bg-peligro/10 p-2 text-sm text-peligro">{error}</p>}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
          >
            {loading ? 'Ingresando...' : 'Ingresar'}
          </button>
        </form>
      </div>
    </div>
  )
}
