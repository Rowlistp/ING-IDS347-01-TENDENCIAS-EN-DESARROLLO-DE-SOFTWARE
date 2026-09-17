import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'

const EMPTY_FORM = {
  rnc: '',
  nombre: '',
  activo: true,
}

export default function ProveedoresPage() {
  const [proveedores, setProveedores] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [deactivatingId, setDeactivatingId] = useState(null)
  const [actionError, setActionError] = useState(null)

  async function cargarProveedores() {
    try {
      const data = await apiRequest('/proveedores')
      setProveedores(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/proveedores')
      .then((data) => { if (!cancelado) setProveedores(data) })
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

  function openEdit(p) {
    setEditingId(p.id)
    setForm({
      rnc: p.rnc,
      nombre: p.nombre,
      activo: p.activo,
    })
    setFormError(null)
    setShowForm(true)
  }

  const requiredFieldsFilled = form.rnc.trim() && form.nombre.trim()

  async function handleSubmit(e) {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)

    const payload = {
      rnc: form.rnc.trim(),
      nombre: form.nombre.trim(),
      activo: form.activo,
    }

    try {
      if (editingId) {
        await apiRequest(`/proveedores/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        })
      } else {
        await apiRequest('/proveedores', {
          method: 'POST',
          body: JSON.stringify(payload),
        })
      }
      setShowForm(false)
      await cargarProveedores()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeactivate(p) {
    if (!window.confirm(`¿Desactivar el proveedor ${p.nombre}?`)) return
    setActionError(null)
    setDeactivatingId(p.id)
    try {
      await apiRequest(`/proveedores/${p.id}`, { method: 'DELETE' })
      await cargarProveedores()
    } catch (e) {
      // El backend responde 409 PROVEEDOR_CON_RECEPCIONES si tiene recepciones
      // históricas — el mensaje ya viene claro desde el backend, se muestra tal cual.
      setActionError(e.message)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Proveedores">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          + Nuevo proveedor
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
                {['RNC', 'Nombre', 'Estado', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {proveedores.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-center text-acero/70">
                    Sin proveedores registrados.
                  </td>
                </tr>
              )}
              {proveedores.map((p) => (
                <tr key={p.id} className="hover:bg-fondo">
                  <td className="px-4 py-3 font-mono text-tinta">{p.rnc}</td>
                  <td className="px-4 py-3 font-medium text-tinta">{p.nombre}</td>
                  <td className="px-4 py-3">
                    <StatusBadge active={p.activo} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(p)}
                        className="rounded bg-tanque px-2 py-1 text-xs text-white hover:opacity-90"
                      >
                        Editar
                      </button>
                      {p.activo && (
                        <button
                          type="button"
                          onClick={() => handleDeactivate(p)}
                          disabled={deactivatingId === p.id}
                          className="rounded bg-peligro px-2 py-1 text-xs text-white hover:opacity-90 disabled:opacity-50"
                        >
                          {deactivatingId === p.id ? 'Desactivando...' : 'Desactivar'}
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
        <Modal title={editingId ? 'Editar proveedor' : 'Nuevo proveedor'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field label="RNC">
              <input
                type="text"
                name="rnc"
                value={form.rnc}
                onChange={handleFormChange}
                required
                maxLength={20}
                className={`${inputCls} font-mono`}
              />
            </Field>

            <Field label="Nombre">
              <input
                type="text"
                name="nombre"
                value={form.nombre}
                onChange={handleFormChange}
                required
                maxLength={150}
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
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear proveedor'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
