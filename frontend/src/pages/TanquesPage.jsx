import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'
import { useTiposCombustible } from '../hooks/useTiposCombustible'

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

  function handleFormChange(e) {
    const { name, value, type, checked } = e.target
    setForm((f) => ({ ...f, [name]: type === 'checkbox' ? checked : value }))
  }

  function openCreate() {
    setEditingId(null)
    setForm(EMPTY_FORM)
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
    setFormError(null)
    setShowForm(true)
  }

  const requiredFieldsFilled =
    form.identificacion.trim() && form.capacidad && form.nivelCritico !== '' && form.tipoCombustibleId

  async function handleSubmit(e) {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)

    const payload = {
      identificacion: form.identificacion.trim(),
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
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(t)}
                        className="rounded bg-tanque px-2 py-1 text-xs text-white hover:opacity-90"
                      >
                        Editar
                      </button>
                      {t.activo && (
                        <button
                          type="button"
                          onClick={() => handleDeactivate(t)}
                          disabled={deactivatingId === t.id}
                          className="rounded bg-peligro px-2 py-1 text-xs text-white hover:opacity-90 disabled:opacity-50"
                        >
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
            <Field label="Identificación">
              <input
                type="text"
                name="identificacion"
                value={form.identificacion}
                onChange={handleFormChange}
                required
                maxLength={50}
                className={`${inputCls} font-mono`}
              />
            </Field>

            <Field label="Tipo de combustible">
              <select
                name="tipoCombustibleId"
                value={form.tipoCombustibleId}
                onChange={handleFormChange}
                required
                className={inputCls}
              >
                <option value="">Seleccione un tipo de combustible</option>
                {tiposCombustible.map((t) => (
                  <option key={t.id} value={t.id}>{t.nombre}</option>
                ))}
              </select>
            </Field>

            <Field label="Capacidad (galones)">
              <input
                type="number"
                name="capacidad"
                value={form.capacidad}
                onChange={handleFormChange}
                required
                min="0.0001"
                step="0.0001"
                className={inputCls}
              />
            </Field>

            <Field label="Nivel crítico (galones)">
              <input
                type="number"
                name="nivelCritico"
                value={form.nivelCritico}
                onChange={handleFormChange}
                required
                min="0"
                step="0.0001"
                className={inputCls}
              />
            </Field>

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
                className="rounded-md bg-tanque px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
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
