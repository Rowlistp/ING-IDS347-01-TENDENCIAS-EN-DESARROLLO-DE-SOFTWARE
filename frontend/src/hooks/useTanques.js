import { useEffect, useState } from 'react'
import apiRequest from '../services/api'

export function useTanques() {
  const [tanques, setTanques] = useState([])

  useEffect(() => {
    apiRequest('/tanques')
      .then(setTanques)
      .catch(() => {})
  }, [])

  return tanques
}
