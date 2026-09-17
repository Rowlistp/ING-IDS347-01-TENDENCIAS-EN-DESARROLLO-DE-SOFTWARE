import { useEffect, useState } from 'react'
import apiRequest from '../services/api'

export function useTiposCombustible() {
  const [tiposCombustible, setTiposCombustible] = useState([])

  useEffect(() => {
    apiRequest('/tipos-combustible')
      .then(setTiposCombustible)
      .catch(() => {})
  }, [])

  return tiposCombustible
}
