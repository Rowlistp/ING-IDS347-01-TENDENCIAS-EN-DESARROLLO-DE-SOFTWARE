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

export default function DepartamentosPage() {
  const [departamentos, setDepartamentos] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [deactivatingId, setDeactivatingId] = useState(null)
  const [actionError, setActionError] = useState(null)

  async function cargarDepartamentos() {
    try {
      const data = await apiRequest('/departamentos')
      setDepartamentos(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    cargarDepartamentos()
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

  function openEdit(dep) {
    setEditingId(dep.id)
    setForm({
      nombre: dep.nombre,
      activo: dep.activo,
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
        await apiRequest(`/departamentos/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        })
      } else {
        await apiRequest('/departamentos', {
          method: 'POST',
          body: JSON.stringify(payload),
        })
      }
      setShowForm(false)
      await cargarDepartamentos()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeactivate(dep) {
    if (!window.confirm(`¿Desactivar el departamento ${dep.nombre}?`)) return
    setActionError(null)
    setDeactivatingId(dep.id)
    try {
      await apiRequest(`/departamentos/${dep.id}`, { method: 'DELETE' })
      await cargarDepartamentos()
    } catch (e) {
      setActionError(e.message)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Departamentos">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          + Nuevo departamento
        </button>
      </div>

      {loading && <p className="text-sm text-gray-500">Cargando...</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}
      {actionError && <p className="text-sm text-red-600">{actionError}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-lg border border-gray-200">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                {['Nombre', 'Estado', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {departamentos.length === 0 && (
                <tr>
                  <td colSpan={3} className="px-4 py-6 text-center text-gray-400">
                    Sin departamentos registrados.
                  </td>
                </tr>
              )}
              {departamentos.map((dep) => (
                <tr key={dep.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-800">{dep.nombre}</td>
                  <td className="px-4 py-3">
                    <StatusBadge active={dep.activo} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(dep)}
                        className="rounded bg-blue-600 px-2 py-1 text-xs text-white hover:bg-blue-700"
                      >
                        Editar
                      </button>
                      {dep.activo && (
                        <button
                          type="button"
                          onClick={() => handleDeactivate(dep)}
                          disabled={deactivatingId === dep.id}
                          className="rounded bg-red-600 px-2 py-1 text-xs text-white hover:bg-red-700 disabled:opacity-50"
                        >
                          {deactivatingId === dep.id ? 'Desactivando...' : 'Desactivar'}
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
        <Modal title={editingId ? 'Editar departamento' : 'Nuevo departamento'} onClose={() => setShowForm(false)}>
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
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input type="checkbox" name="activo" checked={form.activo} onChange={handleFormChange} />
                Activo
              </label>
            )}

            {formError && <p className="text-sm text-red-600">{formError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setShowForm(false)}
                className="rounded-md border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={submitting || !requiredFieldsFilled}
                className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
              >
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear departamento'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
