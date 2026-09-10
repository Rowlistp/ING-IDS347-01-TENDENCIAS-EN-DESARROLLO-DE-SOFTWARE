import { useEffect, useState } from 'react'
import apiRequest from '../services/api'

export function useDepartamentos() {
  const [departamentos, setDepartamentos] = useState([])

  useEffect(() => {
    apiRequest('/departamentos')
      .then(setDepartamentos)
      .catch(() => {})
  }, [])

  return departamentos
}
