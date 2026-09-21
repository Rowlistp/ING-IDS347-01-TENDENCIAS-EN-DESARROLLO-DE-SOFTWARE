import { useCallback, useSyncExternalStore } from 'react'
import { useNavigate } from 'react-router-dom'
import { clearSession, getUser, getRefreshToken, isAuthenticated, sessionSnapshot, subscribeSession } from '../services/auth'
import apiRequest from '../services/api'

export function useAuth() {
  const navigate = useNavigate()
  useSyncExternalStore(subscribeSession, sessionSnapshot)
  const user = getUser()

  const logout = useCallback(async () => {
    const refreshToken = getRefreshToken()
    // Iniciar la revocación antes de borrar las credenciales locales.
    const revoke = refreshToken ? apiRequest('/auth/logout', {
      method: 'POST', body: JSON.stringify({ refreshToken }), signal: AbortSignal.timeout(5000),
    }).catch(() => null) : Promise.resolve()
    clearSession()
    navigate('/login', { replace: true })
    await revoke
  }, [navigate])

  return { isAuthenticated: isAuthenticated(), user, logout }
}
