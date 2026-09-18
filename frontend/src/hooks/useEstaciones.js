import { useEffect, useState } from 'react'
import apiRequest from '../services/api'

export function useEstaciones() {
  const [estaciones, setEstaciones] = useState([])

  useEffect(() => {
    apiRequest('/estaciones')
      .then(setEstaciones)
      .catch(() => {})
  }, [])

  return estaciones
}
