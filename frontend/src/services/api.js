import { getToken, clearSession } from './auth'

const API_URL = import.meta.env.VITE_API_URL

async function fetchWithConnectionMessage(url, options) {
  try {
    return await fetch(url, options)
  } catch (error) {
    if (error.name === 'AbortError' || error.name === 'TimeoutError') throw error
    throw new Error('No se pudo conectar con el servidor. Comprueba tu conexión e inténtalo de nuevo.')
  }
}

export async function apiRequest(endpoint, options = {}) {
  const token = getToken()
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers,
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetchWithConnectionMessage(`${API_URL}${endpoint}`, {
    ...options,
    headers,
  })

  if (!response.ok) {
    if (response.status === 401 && endpoint !== '/auth/login') {
      clearSession()
      if (window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || `Error ${response.status}`)
  }

  if (response.status === 204) {
    return null
  }

  return response.json()
}

export async function apiDownload(endpoint, options = {}) {
  const token = getToken()
  const headers = {}
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetchWithConnectionMessage(`${API_URL}${endpoint}`, { ...options, headers })

  if (!response.ok) {
    if (response.status === 401) {
      clearSession()
      if (window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || `Error ${response.status}`)
  }

  const disposition = response.headers.get('Content-Disposition') || ''
  const match = disposition.match(/filename="?([^";]+)"?/i)

  return {
    blob: await response.blob(),
    filename: match ? match[1] : 'documento.pdf',
  }
}

export default apiRequest
