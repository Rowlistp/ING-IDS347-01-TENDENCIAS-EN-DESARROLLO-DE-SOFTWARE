import { useEffect, useState } from 'react'
import apiRequest from '../services/api'

export function useEmpleados() {
  const [empleados, setEmpleados] = useState([])

  useEffect(() => {
    apiRequest('/empleados')
      .then(setEmpleados)
      .catch(() => {})
  }, [])

  return empleados
}
