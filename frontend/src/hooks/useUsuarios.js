import { useEffect, useState } from 'react'
import apiRequest from '../services/api'

export function useUsuarios() {
  const [usuarios, setUsuarios] = useState([])

  useEffect(() => {
    apiRequest('/usuarios')
      .then(setUsuarios)
      .catch(() => {})
  }, [])

  return usuarios
}
