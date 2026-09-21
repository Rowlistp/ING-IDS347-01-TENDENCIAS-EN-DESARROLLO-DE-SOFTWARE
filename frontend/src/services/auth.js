const TOKEN_KEY = 'token'
const REFRESH_TOKEN_KEY = 'refreshToken'
const USER_KEY = 'user'

export function getToken() {
  return localStorage.getItem(TOKEN_KEY)
}

export function getUser() {
  try {
    const user = JSON.parse(localStorage.getItem(USER_KEY))
    return user && Number.isInteger(user.usuarioId) && typeof user.nombreUsuario === 'string'
      && Array.isArray(user.roles) && user.roles.every((role) => typeof role === 'string') ? user : null
  } catch {
    return null
  }
}

export function getRefreshToken() {
  return localStorage.getItem(REFRESH_TOKEN_KEY)
}

export function sessionSnapshot() {
  return JSON.stringify([getToken(), localStorage.getItem(USER_KEY)])
}

export function subscribeSession(callback) {
  window.addEventListener('storage', callback)
  window.addEventListener('fueltrack-session', callback)
  return () => {
    window.removeEventListener('storage', callback)
    window.removeEventListener('fueltrack-session', callback)
  }
}

function notifySession() {
  window.dispatchEvent(new Event('fueltrack-session'))
}

export function saveSession({ accessToken, refreshToken, usuarioId, nombreUsuario, roles }) {
  localStorage.setItem(TOKEN_KEY, accessToken)
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
  localStorage.setItem(USER_KEY, JSON.stringify({ usuarioId, nombreUsuario, roles }))
  notifySession()
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
  notifySession()
}

export function isAuthenticated() {
  return Boolean(getToken() && getUser())
}
