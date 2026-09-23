import { useEffect, useState } from 'react'
import { useAuth } from '../hooks/useAuth'
import { ROLES } from '../utils/rbac'
import ConfirmModal from '../components/ConfirmModal'
import Field, { inputCls, inputClsError } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
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
  activo: true,
}

export default function TanquesPage() {
  const { user } = useAuth()
  const esAdministrador = user?.roles?.includes(ROLES.ADMINISTRADOR)
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

  const [statusChangingId, setStatusChangingId] = useState(null)
  const [confirmTanque, setConfirmTanque] = useState(null)
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
      activo: t.activo ?? true,
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
      activo: Boolean(form.activo),
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

  function handleToggleStatus(t) {
    setActionError(null)
    setConfirmTanque(t)
  }

  async function handleConfirmToggleStatus() {
    if (!confirmTanque) return
    const t = confirmTanque
    setActionError(null)
    setStatusChangingId(t.id)
    try {
      if (t.activo) {
        await apiRequest(`/tanques/${t.id}`, { method: 'DELETE' })
      } else {
        await apiRequest(`/tanques/${t.id}/activar`, { method: 'PUT' })
      }
      await cargarTanques()
      setConfirmTanque(null)
    } catch (e) {
      // El backend responde 409 TANQUE_CON_INVENTARIO si el tanque todavía tiene
      // existencia registrada, o TIPO_COMBUSTIBLE_INACTIVO si su tipo está inactivo.
      setActionError(e.message)
      setConfirmTanque(null)
    } finally {
      setStatusChangingId(null)
    }
  }

  return (
    <PageContainer title="Tanques">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="flex min-h-[44px] w-full sm:w-auto items-center justify-center gap-2 rounded-md bg-tanque px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-tanque/90 active:scale-[0.98]"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
          </svg>
          Nuevo tanque
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <ResponsiveTable
          data={tanques}
          keyField="id"
          emptyMessage="Sin tanques registrados."
          columns={[
            {
              key: 'identificacion',
              label: 'Identificación',
              primary: true,
              priority: 'high',
              render: (t) => <span className="font-semibold font-mono text-tinta">{t.identificacion}</span>,
            },
            {
              key: 'tipoCombustibleNombre',
              label: 'Tipo combustible',
              priority: 'high',
              render: (t) => <span className="text-acero">{t.tipoCombustibleNombre}</span>,
            },
            {
              key: 'capacidad',
              label: 'Capacidad',
              priority: 'med',
              render: (t) => <span className="font-mono num text-acero">{t.capacidad.toFixed(2)} gal</span>,
            },
            {
              key: 'nivelCritico',
              label: 'Nivel crítico',
              priority: 'low',
              render: (t) => <span className="font-mono num text-acero">{t.nivelCritico.toFixed(2)} gal</span>,
            },
            {
              key: 'nivelActual',
              label: 'Existencia actual',
              priority: 'high',
              render: (t) => <span className="font-mono num font-semibold text-tinta">{t.nivelActual.toFixed(2)} gal</span>,
            },
            {
              key: 'activo',
              label: 'Estado',
              priority: 'high',
              render: (t) => <StatusBadge active={t.activo} />,
            },
          ]}
          actions={(t) => (
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => openEdit(t)}
                className="flex min-h-[38px] items-center gap-1.5 rounded-sm border border-acero/30 bg-white px-3 py-1.5 text-xs font-medium text-tinta shadow-xs transition-colors hover:border-tanque hover:bg-fondo active:scale-[0.98]"
              >
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z" />
                  <path d="m15 5 4 4" />
                </svg>
                Editar
              </button>
              {(!t.activo || esAdministrador) && <button
                type="button"
                onClick={() => handleToggleStatus(t)}
                disabled={statusChangingId === t.id}
                className={`flex min-h-[38px] items-center gap-1.5 rounded-sm border px-3 py-1.5 text-xs font-medium shadow-xs transition-colors active:scale-[0.98] disabled:opacity-50 ${
                  t.activo
                    ? 'border-peligro/30 bg-peligro/10 text-peligro hover:bg-peligro hover:text-white'
                    : 'border-exito/30 bg-exito/10 text-exito hover:bg-exito hover:text-white'
                }`}
              >
                {t.activo ? (
                  <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <circle cx="12" cy="12" r="10" />
                    <line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
                  </svg>
                ) : (
                  <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                )}
                {statusChangingId === t.id
                  ? (t.activo ? 'Desactivando...' : 'Reactivando...')
                  : (t.activo ? 'Desactivar' : 'Reactivar')}
              </button>}
            </div>
          )}
        />
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
                {tiposCombustible.filter((t) => t.activo || (editingId && t.id === Number(form.tipoCombustibleId))).map((t) => (
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

            {editingId && (
              <label className="flex items-center gap-2 cursor-pointer pt-1 text-sm text-tinta">
                <input
                  type="checkbox"
                  name="activo" disabled={!esAdministrador && Boolean(tanques.find((item) => item.id === editingId)?.activo)}
                  checked={Boolean(form.activo)}
                  onChange={handleFormChange}
                  className="h-4 w-4 rounded border-acero/30 text-tanque focus:ring-tanque"
                />
                <span>Tanque activo para operaciones de combustible</span>
              </label>
            )}

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

      <ConfirmModal
        isOpen={Boolean(confirmTanque)}
        title={confirmTanque?.activo ? '¿Desactivar tanque de combustible?' : '¿Reactivar tanque de combustible?'}
        message={
          <>
            Está a punto de {confirmTanque?.activo ? 'desactivar' : 'reactivar'} el tanque{' '}
            <strong className="font-semibold text-tinta font-mono">{confirmTanque?.identificacion}</strong>
            {confirmTanque?.tipoCombustibleNombre && ` (${confirmTanque.tipoCombustibleNombre})`}.
          </>
        }
        consequences={
          confirmTanque?.activo
            ? [
                'No se podrán recibir nuevas cargas ni despachar desde este tanque.',
                'Si el tanque posee combustible remanente, el sistema requerirá transferirlo antes de desactivar.',
              ]
            : [
                'El tanque volverá a estar disponible para recepciones de combustible y despachos.',
                'Asegúrese de que el tipo de combustible asociado esté actualmente activo.',
              ]
        }
        type={confirmTanque?.activo ? 'danger' : 'success'}
        confirmText={confirmTanque?.activo ? 'Desactivar tanque' : 'Reactivar tanque'}
        cancelText="Cancelar"
        isLoading={Boolean(statusChangingId)}
        onConfirm={handleConfirmToggleStatus}
        onClose={() => setConfirmTanque(null)}
      />
    </PageContainer>
  )
}
