import { useEffect, useState } from 'react'
import apiRequest from '../services/api'

export function useEmpleados(optionsOnly = false) {
  const [empleados, setEmpleados] = useState([])

  useEffect(() => {
    apiRequest(optionsOnly ? '/empleados/opciones' : '/empleados')
      .then(setEmpleados)
      .catch(() => {})
  }, [optionsOnly])

  return empleados
}
