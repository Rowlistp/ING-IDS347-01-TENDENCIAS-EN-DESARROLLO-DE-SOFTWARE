import { useEffect, useState } from 'react'
import apiRequest from '../services/api'

export function useProveedores() {
  const [proveedores, setProveedores] = useState([])

  useEffect(() => {
    apiRequest('/proveedores')
      .then(setProveedores)
      .catch(() => {})
  }, [])

  return proveedores
}
