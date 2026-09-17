import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'

const EMPTY_FORM = {
  nombre: '',
  activo: true,
}

export default function TiposCombustiblePage() {
  const [tipos, setTipos] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [deactivatingId, setDeactivatingId] = useState(null)
  const [actionError, setActionError] = useState(null)

  async function cargarTipos() {
    try {
      const data = await apiRequest('/tipos-combustible')
      setTipos(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/tipos-combustible')
      .then((data) => { if (!cancelado) setTipos(data) })
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
      nombre: t.nombre,
      activo: t.activo,
    })
    setFormError(null)
    setShowForm(true)
  }

  const requiredFieldsFilled = form.nombre.trim()

  async function handleSubmit(e) {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)

    const payload = {
      nombre: form.nombre.trim(),
      activo: form.activo,
    }

    try {
      if (editingId) {
        await apiRequest(`/tipos-combustible/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        })
      } else {
        await apiRequest('/tipos-combustible', {
          method: 'POST',
          body: JSON.stringify(payload),
        })
      }
      setShowForm(false)
      await cargarTipos()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeactivate(t) {
    if (!window.confirm(`¿Desactivar el tipo de combustible ${t.nombre}?`)) return
    setActionError(null)
    setDeactivatingId(t.id)
    try {
      await apiRequest(`/tipos-combustible/${t.id}`, { method: 'DELETE' })
      await cargarTipos()
    } catch (e) {
      // El backend valida 3 dependencias por separado, cada una con su propio
      // mensaje claro: TIPO_COMBUSTIBLE_CON_TANQUES_ACTIVOS,
      // TIPO_COMBUSTIBLE_CON_SOLICITUDES_ACTIVAS, TIPO_COMBUSTIBLE_CON_TICKETS_ACTIVOS.
      // Se muestra el mensaje tal cual, ya viene distinto por cada caso.
      setActionError(e.message)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Tipos de Combustible">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          + Nuevo tipo de combustible
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
                {['Nombre', 'Estado', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {tipos.length === 0 && (
                <tr>
                  <td colSpan={3} className="px-4 py-6 text-center text-acero/70">
                    Sin tipos de combustible registrados.
                  </td>
                </tr>
              )}
              {tipos.map((t) => (
                <tr key={t.id} className="hover:bg-fondo">
                  <td className="px-4 py-3 font-medium text-tinta">{t.nombre}</td>
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
        <Modal title={editingId ? 'Editar tipo de combustible' : 'Nuevo tipo de combustible'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field label="Nombre">
              <input
                type="text"
                name="nombre"
                value={form.nombre}
                onChange={handleFormChange}
                required
                maxLength={100}
                className={inputCls}
              />
            </Field>

            {editingId && (
              <label className="flex items-center gap-2 text-sm text-tinta">
                <input type="checkbox" name="activo" checked={form.activo} onChange={handleFormChange} />
                Activo
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
                className="rounded-md bg-tanque px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
              >
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear tipo de combustible'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
