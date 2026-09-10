const TOKEN_KEY = 'token'
const REFRESH_TOKEN_KEY = 'refreshToken'
const USER_KEY = 'user'

export function getToken() {
  return localStorage.getItem(TOKEN_KEY)
}

export function getUser() {
  const raw = localStorage.getItem(USER_KEY)
  return raw ? JSON.parse(raw) : null
}

export function saveSession({ accessToken, refreshToken, usuarioId, nombreUsuario, roles }) {
  localStorage.setItem(TOKEN_KEY, accessToken)
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
  localStorage.setItem(USER_KEY, JSON.stringify({ usuarioId, nombreUsuario, roles }))
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
}

export function isAuthenticated() {
  return Boolean(getToken())
}
