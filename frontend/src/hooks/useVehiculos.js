import { useEffect, useState } from 'react'
import apiRequest from '../services/api'

export function useVehiculos() {
  const [vehiculos, setVehiculos] = useState([])

  useEffect(() => {
    apiRequest('/vehiculos')
      .then(setVehiculos)
      .catch(() => {})
  }, [])

  return vehiculos
}
