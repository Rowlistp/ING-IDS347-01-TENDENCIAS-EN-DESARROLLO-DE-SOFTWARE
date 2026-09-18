import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'
import { validateTextoMinimo } from '../utils/validators'

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
  const [errors, setErrors] = useState({})
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
    let cancelado = false
    apiRequest('/departamentos')
      .then((data) => { if (!cancelado) setDepartamentos(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  function handleFormChange(e) {
    const { name, value, type, checked } = e.target
    setForm((f) => ({ ...f, [name]: type === 'checkbox' ? checked : value }))
    if (errors[name]) {
      setErrors((errs) => ({ ...errs, [name]: null }))
    }
  }

  function openCreate() {
    setEditingId(null)
    setForm(EMPTY_FORM)
    setErrors({})
    setFormError(null)
    setShowForm(true)
  }

  function openEdit(dep) {
    setEditingId(dep.id)
    setForm({
      nombre: dep.nombre,
      activo: dep.activo,
    })
    setErrors({})
    setFormError(null)
    setShowForm(true)
  }

  async function handleSubmit(e) {
    e.preventDefault()
    const nombreErr = validateTextoMinimo(form.nombre, 3, 'El nombre del departamento')
    if (nombreErr) {
      setErrors({ nombre: nombreErr })
      return
    }

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
          className="inline-flex items-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 shadow-sm"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
          </svg>
          Nuevo departamento
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-sm border border-acero/20 shadow-sm">
          <table className="min-w-full divide-y divide-acero/20 text-sm">
            <thead className="bg-fondo">
              <tr>
                {['Nombre', 'Estado', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {departamentos.length === 0 && (
                <tr>
                  <td colSpan={3} className="px-4 py-6 text-center text-acero/70">
                    Sin departamentos registrados.
                  </td>
                </tr>
              )}
              {departamentos.map((dep) => (
                <tr key={dep.id} className="hover:bg-fondo/70 transition-colors">
                  <td className="px-4 py-3 font-medium text-tinta">{dep.nombre}</td>
                  <td className="px-4 py-3">
                    <StatusBadge active={dep.activo} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(dep)}
                        className="inline-flex items-center gap-1.5 rounded border border-acero/30 bg-white px-2.5 py-1 text-xs font-medium text-tinta hover:bg-fondo transition-colors shadow-sm"
                      >
                        <svg className="h-3.5 w-3.5 text-acero" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                        Editar
                      </button>
                      {dep.activo && (
                        <button
                          type="button"
                          onClick={() => handleDeactivate(dep)}
                          disabled={deactivatingId === dep.id}
                          className="inline-flex items-center gap-1.5 rounded bg-peligro/10 border border-peligro/30 px-2.5 py-1 text-xs font-medium text-peligro hover:bg-peligro hover:text-white transition-colors disabled:opacity-50 shadow-sm"
                        >
                          <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                          </svg>
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
            <Field label="Nombre del Departamento" required error={errors.nombre} hint="Ej. Operaciones, Logística, Mantenimiento">
              <input
                type="text"
                name="nombre"
                value={form.nombre}
                onChange={handleFormChange}
                required
                maxLength={100}
                placeholder="Ej. Logística y Distribución"
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
                disabled={submitting}
                className="inline-flex items-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50 shadow-sm"
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
