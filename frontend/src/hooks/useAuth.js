import { useCallback, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { clearSession, getUser, isAuthenticated } from '../services/auth'

export function useAuth() {
  const navigate = useNavigate()
  const [user] = useState(getUser)

  const logout = useCallback(() => {
    clearSession()
    navigate('/login', { replace: true })
  }, [navigate])

  return { isAuthenticated: isAuthenticated(), user, logout }
}
