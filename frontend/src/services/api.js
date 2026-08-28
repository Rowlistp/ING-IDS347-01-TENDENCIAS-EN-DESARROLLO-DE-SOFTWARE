const API_URL = import.meta.env.VITE_API_URL

function getToken() {
  return localStorage.getItem('token')
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

  const response = await fetch(`${API_URL}${endpoint}`, {
    ...options,
    headers,
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || `Error ${response.status}`)
  }

  if (response.status === 204) {
    return null
  }

  return response.json()
}

export default apiRequest
