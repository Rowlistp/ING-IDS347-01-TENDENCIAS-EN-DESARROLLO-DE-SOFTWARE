import { useEffect, useState } from 'react'
import Field, { inputCls, inputClsError } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import { useDepartamentos } from '../hooks/useDepartamentos'
import apiRequest from '../services/api'
import {
  validatePlacaVehiculo,
  validateAnioVehiculo,
  validateCapacidadTanqueVehiculo,
  validateTextoMinimo,
} from '../utils/validators'

const EMPTY_FORM = {
  placa: '',
  ficha: '',
  marca: '',
  modelo: '',
  año: '',
  tipo: '',
  departamentoId: '',
  capacidadTanque: '',
  odometro: '',
}

export default function VehiculosPage() {
  const [vehiculos, setVehiculos] = useState([])
  const departamentos = useDepartamentos()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [fieldErrors, setFieldErrors] = useState({})
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [deactivatingId, setDeactivatingId] = useState(null)
  const [actionError, setActionError] = useState(null)

  async function cargarVehiculos() {
    try {
      const data = await apiRequest('/vehiculos')
      setVehiculos(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/vehiculos')
      .then((data) => { if (!cancelado) setVehiculos(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  function validateAllFields(f) {
    const errs = {}
    const placaErr = validatePlacaVehiculo(f.placa)
    if (placaErr) errs.placa = placaErr

    const fichaErr = validateTextoMinimo(f.ficha, 'La ficha o número interno', 2)
    if (fichaErr) errs.ficha = fichaErr

    const marcaErr = validateTextoMinimo(f.marca, 'La marca', 2)
    if (marcaErr) errs.marca = marcaErr

    const modeloErr = validateTextoMinimo(f.modelo, 'El modelo', 2)
    if (modeloErr) errs.modelo = modeloErr

    const anioErr = validateAnioVehiculo(f.año)
    if (anioErr) errs.año = anioErr

    const tipoErr = validateTextoMinimo(f.tipo, 'El tipo de vehículo', 2)
    if (tipoErr) errs.tipo = tipoErr

    if (!f.departamentoId) {
      errs.departamentoId = 'Debe seleccionar un departamento.'
    }

    const capErr = validateCapacidadTanqueVehiculo(f.capacidadTanque)
    if (capErr) errs.capacidadTanque = capErr

    if (f.odometro !== '' && f.odometro !== undefined && Number(f.odometro) < 0) {
      errs.odometro = 'El odómetro no puede ser negativo.'
    }

    setFieldErrors(errs)
    return Object.keys(errs).length === 0
  }

  function handleFormChange(e) {
    const { name, value } = e.target
    setForm((f) => ({ ...f, [name]: value }))

    // Inline validation
    if (name === 'placa') {
      setFieldErrors((prev) => ({ ...prev, placa: validatePlacaVehiculo(value) }))
    } else if (name === 'ficha') {
      setFieldErrors((prev) => ({ ...prev, ficha: validateTextoMinimo(value, 'La ficha', 2) }))
    } else if (name === 'año') {
      setFieldErrors((prev) => ({ ...prev, año: validateAnioVehiculo(value) }))
    } else if (name === 'capacidadTanque') {
      setFieldErrors((prev) => ({ ...prev, capacidadTanque: validateCapacidadTanqueVehiculo(value) }))
    } else if (name === 'departamentoId') {
      setFieldErrors((prev) => ({ ...prev, departamentoId: value ? null : 'Debe seleccionar un departamento.' }))
    }
  }

  function openCreate() {
    setEditingId(null)
    setForm(EMPTY_FORM)
    setFieldErrors({})
    setFormError(null)
    setShowForm(true)
  }

  function openEdit(veh) {
    setEditingId(veh.id)
    setForm({
      placa: veh.placa,
      ficha: veh.ficha,
      marca: veh.marca,
      modelo: veh.modelo,
      año: String(veh.año),
      tipo: veh.tipo,
      departamentoId: veh.departamentoId,
      capacidadTanque: String(veh.capacidadTanque),
      odometro: String(veh.odometro),
    })
    setFieldErrors({})
    setFormError(null)
    setShowForm(true)
  }

  const requiredFieldsFilled =
    form.placa.trim() &&
    form.ficha.trim() &&
    form.marca.trim() &&
    form.modelo.trim() &&
    form.año &&
    form.tipo.trim() &&
    form.departamentoId &&
    form.capacidadTanque &&
    Object.values(fieldErrors).every((err) => !err)

  async function handleSubmit(e) {
    e.preventDefault()
    if (!validateAllFields(form)) {
      return
    }

    setSubmitting(true)
    setFormError(null)

    const payload = {
      placa: form.placa.trim().toUpperCase(),
      ficha: form.ficha.trim().toUpperCase(),
      marca: form.marca.trim(),
      modelo: form.modelo.trim(),
      año: Number(form.año),
      tipo: form.tipo.trim(),
      departamentoId: Number(form.departamentoId),
      capacidadTanque: Number(form.capacidadTanque),
      odometro: form.odometro ? Number(form.odometro) : 0,
    }

    try {
      if (editingId) {
        await apiRequest(`/vehiculos/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        })
      } else {
        await apiRequest('/vehiculos', {
          method: 'POST',
          body: JSON.stringify(payload),
        })
      }
      setShowForm(false)
      await cargarVehiculos()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeactivate(veh) {
    if (!window.confirm(`¿Desactivar el vehículo ${veh.placa}?`)) return
    setActionError(null)
    setDeactivatingId(veh.id)
    try {
      await apiRequest(`/vehiculos/${veh.id}`, { method: 'DELETE' })
      await cargarVehiculos()
    } catch (e) {
      setActionError(e.message)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Vehículos">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          + Nuevo vehículo
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-sm border border-acero/20">
          <table className="min-w-full divide-y divide-acero/20 text-sm">
            <thead className="bg-fondo">
              <tr>
                {[
                  'Placa',
                  'Ficha',
                  'Marca',
                  'Modelo',
                  'Año',
                  'Tipo',
                  'Departamento',
                  'Capacidad tanque',
                  'Odómetro',
                  'Estado',
                  'Acciones',
                ].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {vehiculos.length === 0 && (
                <tr>
                  <td colSpan={11} className="px-4 py-6 text-center text-acero/70">
                    Sin vehículos registrados.
                  </td>
                </tr>
              )}
              {vehiculos.map((veh) => (
                <tr key={veh.id} className="hover:bg-fondo">
                  <td className="px-4 py-3 font-medium font-mono text-tinta">{veh.placa}</td>
                  <td className="px-4 py-3 font-mono text-acero">{veh.ficha}</td>
                  <td className="px-4 py-3 text-acero">{veh.marca}</td>
                  <td className="px-4 py-3 text-acero">{veh.modelo}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{veh.año}</td>
                  <td className="px-4 py-3 text-acero">{veh.tipo}</td>
                  <td className="px-4 py-3 text-acero">{veh.departamentoNombre}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{veh.capacidadTanque}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{veh.odometro}</td>
                  <td className="px-4 py-3">
                    <StatusBadge active={veh.activo} />
                  </td>
                  <td className="px-4 py-3.5">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(veh)}
                        className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-acero/30 bg-white px-3 py-1.5 text-sm font-medium text-tinta transition-colors hover:border-tanque hover:bg-fondo"
                      >
                        <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z" />
                          <path d="m15 5 4 4" />
                        </svg>
                        Editar
                      </button>
                      {veh.activo && (
                        <button
                          type="button"
                          onClick={() => handleDeactivate(veh)}
                          disabled={deactivatingId === veh.id}
                          className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-peligro/30 bg-white px-3 py-1.5 text-sm font-medium text-peligro transition-colors hover:border-peligro hover:bg-peligro/10 disabled:opacity-50"
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <circle cx="12" cy="12" r="10" />
                            <line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
                          </svg>
                          {deactivatingId === veh.id ? 'Desactivando...' : 'Desactivar'}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showForm && (
        <Modal title={editingId ? 'Editar vehículo' : 'Nuevo vehículo'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field
                label="Placa de matrícula"
                required
                error={fieldErrors.placa}
                hint="Ej. A123456, G987654"
              >
                <input
                  type="text"
                  name="placa"
                  value={form.placa}
                  onChange={handleFormChange}
                  required
                  maxLength={10}
                  placeholder="A123456"
                  className={`${fieldErrors.placa ? inputClsError : inputCls} font-mono`}
                />
              </Field>

              <Field
                label="Ficha interna"
                required
                error={fieldErrors.ficha}
                hint="Código de inventario o flota"
              >
                <input
                  type="text"
                  name="ficha"
                  value={form.ficha}
                  onChange={handleFormChange}
                  required
                  maxLength={20}
                  placeholder="VEH-018"
                  className={`${fieldErrors.ficha ? inputClsError : inputCls} font-mono`}
                />
              </Field>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field
                label="Marca"
                required
                error={fieldErrors.marca}
              >
                <input
                  type="text"
                  name="marca"
                  value={form.marca}
                  onChange={handleFormChange}
                  required
                  maxLength={50}
                  placeholder="Toyota, Ford, etc."
                  className={fieldErrors.marca ? inputClsError : inputCls}
                />
              </Field>

              <Field
                label="Modelo"
                required
                error={fieldErrors.modelo}
              >
                <input
                  type="text"
                  name="modelo"
                  value={form.modelo}
                  onChange={handleFormChange}
                  required
                  maxLength={50}
                  placeholder="Hilux, Ranger, etc."
                  className={fieldErrors.modelo ? inputClsError : inputCls}
                />
              </Field>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field
                label="Año de fabricación"
                required
                error={fieldErrors.año}
                hint="Rango: 1980 en adelante"
              >
                <input
                  type="number"
                  name="año"
                  value={form.año}
                  onChange={handleFormChange}
                  required
                  min={1980}
                  max={2100}
                  placeholder="2022"
                  className={`${fieldErrors.año ? inputClsError : inputCls} font-mono`}
                />
              </Field>

              <Field
                label="Tipo de carrocería"
                required
                error={fieldErrors.tipo}
              >
                <input
                  type="text"
                  name="tipo"
                  value={form.tipo}
                  onChange={handleFormChange}
                  required
                  maxLength={50}
                  placeholder="Ej. Camioneta, Sedán, Camión"
                  className={fieldErrors.tipo ? inputClsError : inputCls}
                />
              </Field>
            </div>

            <Field
              label="Departamento asignado"
              required
              error={fieldErrors.departamentoId}
            >
              <select
                name="departamentoId"
                value={form.departamentoId}
                onChange={handleFormChange}
                required
                className={fieldErrors.departamentoId ? inputClsError : inputCls}
              >
                <option value="">Seleccionar departamento...</option>
                {departamentos.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.nombre}
                  </option>
                ))}
              </select>
            </Field>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field
                label="Capacidad de tanque (galones)"
                required
                error={fieldErrors.capacidadTanque}
                hint="Rango típico: 5 a 500 galones"
              >
                <input
                  type="number"
                  name="capacidadTanque"
                  value={form.capacidadTanque}
                  onChange={handleFormChange}
                  required
                  min="1"
                  max="500"
                  step="0.01"
                  placeholder="Ej. 20"
                  className={`${fieldErrors.capacidadTanque ? inputClsError : inputCls} font-mono`}
                />
              </Field>

              <Field
                label="Odómetro actual (km)"
                error={fieldErrors.odometro}
              >
                <input
                  type="number"
                  name="odometro"
                  value={form.odometro}
                  onChange={handleFormChange}
                  min="0"
                  step="1"
                  placeholder="0"
                  className={`${fieldErrors.odometro ? inputClsError : inputCls} font-mono`}
                />
              </Field>
            </div>

            {formError && <p className="text-sm text-peligro">{formError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setShowForm(false)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={submitting || !requiredFieldsFilled}
                className="rounded-md bg-tanque px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50 font-medium"
              >
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear vehículo'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
