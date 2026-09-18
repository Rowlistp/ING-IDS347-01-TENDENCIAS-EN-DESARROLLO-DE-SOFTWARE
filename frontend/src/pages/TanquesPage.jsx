import { useEffect, useState } from 'react'
import Field, { inputCls, inputClsError } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'
import { useTiposCombustible } from '../hooks/useTiposCombustible'
import {
  validateCapacidadTanque,
  validateNivelCriticoTanque,
  validateIdentificacionTanque,
  suggestTanqueIdentificacion,
} from '../utils/validators'

const EMPTY_FORM = {
  identificacion: '',
  capacidad: '',
  nivelCritico: '',
  tipoCombustibleId: '',
}

export default function TanquesPage() {
  const tiposCombustible = useTiposCombustible()

  const [tanques, setTanques] = useState([])
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

  async function cargarTanques() {
    try {
      const data = await apiRequest('/tanques')
      setTanques(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/tanques')
      .then((data) => { if (!cancelado) setTanques(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  function validateAllFields(f) {
    const errs = {}
    const idErr = validateIdentificacionTanque(f.identificacion)
    if (idErr) errs.identificacion = idErr

    if (!f.tipoCombustibleId) {
      errs.tipoCombustibleId = 'Debe seleccionar un tipo de combustible.'
    }

    const capErr = validateCapacidadTanque(f.capacidad)
    if (capErr) errs.capacidad = capErr

    const critErr = validateNivelCriticoTanque(f.nivelCritico, f.capacidad)
    if (critErr) errs.nivelCritico = critErr

    setFieldErrors(errs)
    return Object.keys(errs).length === 0
  }

  function handleFormChange(e) {
    const { name, value, type, checked } = e.target
    const newVal = type === 'checkbox' ? checked : value
    setForm((f) => ({ ...f, [name]: newVal }))

    // Inline validation
    if (name === 'identificacion') {
      setFieldErrors((prev) => ({ ...prev, identificacion: validateIdentificacionTanque(newVal) }))
    } else if (name === 'capacidad') {
      setFieldErrors((prev) => ({
        ...prev,
        capacidad: validateCapacidadTanque(newVal),
        nivelCritico: validateNivelCriticoTanque(form.nivelCritico, newVal),
      }))
    } else if (name === 'nivelCritico') {
      setFieldErrors((prev) => ({
        ...prev,
        nivelCritico: validateNivelCriticoTanque(newVal, form.capacidad),
      }))
    } else if (name === 'tipoCombustibleId') {
      setFieldErrors((prev) => ({ ...prev, tipoCombustibleId: newVal ? null : 'Debe seleccionar un combustible.' }))
    }
  }

  function handleSuggestNomenclature() {
    const tipo = tiposCombustible.find((t) => String(t.id) === String(form.tipoCombustibleId))
    const count = tanques.filter((t) => String(t.tipoCombustibleId) === String(form.tipoCombustibleId)).length + 1
    const suggested = suggestTanqueIdentificacion(tipo ? tipo.nombre : '', count)
    setForm((f) => ({ ...f, identificacion: suggested }))
    setFieldErrors((prev) => ({ ...prev, identificacion: null }))
  }

  function openCreate() {
    setEditingId(null)
    setForm(EMPTY_FORM)
    setFieldErrors({})
    setFormError(null)
    setShowForm(true)
  }

  function openEdit(t) {
    setEditingId(t.id)
    setForm({
      identificacion: t.identificacion,
      capacidad: String(t.capacidad),
      nivelCritico: String(t.nivelCritico),
      tipoCombustibleId: String(t.tipoCombustibleId),
    })
    setFieldErrors({})
    setFormError(null)
    setShowForm(true)
  }

  const requiredFieldsFilled =
    form.identificacion.trim() &&
    form.capacidad &&
    form.nivelCritico !== '' &&
    form.tipoCombustibleId &&
    Object.values(fieldErrors).every((err) => !err)

  async function handleSubmit(e) {
    e.preventDefault()
    if (!validateAllFields(form)) {
      return
    }

    setSubmitting(true)
    setFormError(null)

    const payload = {
      identificacion: form.identificacion.trim().toUpperCase(),
      capacidad: Number(form.capacidad),
      nivelCritico: Number(form.nivelCritico),
      tipoCombustibleId: Number(form.tipoCombustibleId),
    }

    try {
      if (editingId) {
        await apiRequest(`/tanques/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        })
      } else {
        await apiRequest('/tanques', {
          method: 'POST',
          body: JSON.stringify(payload),
        })
      }
      setShowForm(false)
      await cargarTanques()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeactivate(t) {
    if (!window.confirm(`¿Desactivar el tanque ${t.identificacion}?`)) return
    setActionError(null)
    setDeactivatingId(t.id)
    try {
      await apiRequest(`/tanques/${t.id}`, { method: 'DELETE' })
      await cargarTanques()
    } catch (e) {
      // El backend responde 409 TANQUE_CON_INVENTARIO si el tanque todavía tiene
      // existencia registrada — el mensaje ya viene claro desde el backend.
      setActionError(e.message)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Tanques">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          + Nuevo tanque
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
                {['Identificación', 'Tipo combustible', 'Capacidad', 'Nivel crítico', 'Existencia actual', 'Estado', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {tanques.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-6 text-center text-acero/70">
                    Sin tanques registrados.
                  </td>
                </tr>
              )}
              {tanques.map((t) => (
                <tr key={t.id} className="hover:bg-fondo">
                  <td className="px-4 py-3 font-medium font-mono text-tinta">{t.identificacion}</td>
                  <td className="px-4 py-3 text-acero">{t.tipoCombustibleNombre}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{t.capacidad.toFixed(2)} gal</td>
                  <td className="px-4 py-3 font-mono num text-acero">{t.nivelCritico.toFixed(2)} gal</td>
                  <td className="px-4 py-3 font-mono num text-tinta">{t.nivelActual.toFixed(2)} gal</td>
                  <td className="px-4 py-3">
                    <StatusBadge active={t.activo} />
                  </td>
                  <td className="px-4 py-3.5">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(t)}
                        className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-acero/30 bg-white px-3 py-1.5 text-sm font-medium text-tinta transition-colors hover:border-tanque hover:bg-fondo"
                      >
                        <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z" />
                          <path d="m15 5 4 4" />
                        </svg>
                        Editar
                      </button>
                      {t.activo && (
                        <button
                          type="button"
                          onClick={() => handleDeactivate(t)}
                          disabled={deactivatingId === t.id}
                          className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-peligro/30 bg-white px-3 py-1.5 text-sm font-medium text-peligro transition-colors hover:border-peligro hover:bg-peligro/10 disabled:opacity-50"
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <circle cx="12" cy="12" r="10" />
                            <line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
                          </svg>
                          {deactivatingId === t.id ? 'Desactivando...' : 'Desactivar'}
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
        <Modal title={editingId ? 'Editar tanque' : 'Nuevo tanque'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field
              label="Tipo de combustible"
              required
              error={fieldErrors.tipoCombustibleId}
            >
              <select
                name="tipoCombustibleId"
                value={form.tipoCombustibleId}
                onChange={handleFormChange}
                required
                className={fieldErrors.tipoCombustibleId ? inputClsError : inputCls}
              >
                <option value="">Seleccione un tipo de combustible</option>
                {tiposCombustible.map((t) => (
                  <option key={t.id} value={t.id}>{t.nombre}</option>
                ))}
              </select>
            </Field>

            <Field
              label="Identificación del tanque"
              required
              error={fieldErrors.identificacion}
              hint="Código alfanumérico único (ej: TNQ-DSL-01, TNQ-GPR-01)"
            >
              <div className="space-y-1.5">
                <div className="flex gap-2">
                  <input
                    type="text"
                    name="identificacion"
                    value={form.identificacion}
                    onChange={handleFormChange}
                    required
                    maxLength={25}
                    placeholder="TNQ-DSL-01"
                    className={`${fieldErrors.identificacion ? inputClsError : inputCls} font-mono flex-1`}
                  />
                  {form.tipoCombustibleId && (
                    <button
                      type="button"
                      onClick={handleSuggestNomenclature}
                      className="px-3 py-2 text-xs font-semibold rounded-md border border-acero/30 bg-fondo hover:bg-acero/10 text-acero transition-colors"
                      title="Generar código estándar según el combustible seleccionado"
                    >
                      Autogenerar
                    </button>
                  )}
                </div>
              </div>
            </Field>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field
                label="Capacidad total (galones)"
                required
                error={fieldErrors.capacidad}
                hint="Rango: 50 a 100,000 gal"
              >
                <input
                  type="number"
                  name="capacidad"
                  value={form.capacidad}
                  onChange={handleFormChange}
                  required
                  min="50"
                  max="100000"
                  step="0.01"
                  placeholder="Ej. 10000"
                  className={`${fieldErrors.capacidad ? inputClsError : inputCls} font-mono`}
                />
              </Field>

              <Field
                label="Nivel crítico (galones)"
                required
                error={fieldErrors.nivelCritico}
                hint="Debe ser menor a la capacidad"
              >
                <input
                  type="number"
                  name="nivelCritico"
                  value={form.nivelCritico}
                  onChange={handleFormChange}
                  required
                  min="1"
                  max="100000"
                  step="0.01"
                  placeholder="Ej. 1000"
                  className={`${fieldErrors.nivelCritico ? inputClsError : inputCls} font-mono`}
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
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear tanque'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
