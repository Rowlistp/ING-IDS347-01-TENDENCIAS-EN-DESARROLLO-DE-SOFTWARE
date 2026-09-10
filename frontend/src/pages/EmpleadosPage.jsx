import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'

const EMPTY_FORM = {
  codigo: '',
  nombreCompleto: '',
  cedula: '',
  cargo: '',
  correo: '',
  telefono: '',
  departamentoId: '',
  activo: true,
}

export default function EmpleadosPage() {
  const [empleados, setEmpleados] = useState([])
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

  async function cargarEmpleados() {
    try {
      const data = await apiRequest('/empleados')
      setEmpleados(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    cargarEmpleados()
    apiRequest('/departamentos')
      .then(setDepartamentos)
      .catch(() => {})
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

  function openEdit(emp) {
    setEditingId(emp.id)
    setForm({
      codigo: emp.codigo,
      nombreCompleto: emp.nombreCompleto,
      cedula: emp.cedula,
      cargo: emp.cargo,
      correo: emp.correo,
      telefono: emp.telefono,
      departamentoId: emp.departamentoId,
      activo: emp.activo,
    })
    setFormError(null)
    setShowForm(true)
  }

  const requiredFieldsFilled =
    form.codigo.trim() &&
    form.nombreCompleto.trim() &&
    form.cedula.trim() &&
    form.cargo.trim() &&
    form.correo.trim() &&
    form.telefono.trim() &&
    form.departamentoId

  async function handleSubmit(e) {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)

    const payload = {
      codigo: form.codigo.trim(),
      nombreCompleto: form.nombreCompleto.trim(),
      cedula: form.cedula.trim(),
      cargo: form.cargo.trim(),
      correo: form.correo.trim(),
      telefono: form.telefono.trim(),
      departamentoId: Number(form.departamentoId),
      activo: form.activo,
    }

    try {
      if (editingId) {
        await apiRequest(`/empleados/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        })
      } else {
        await apiRequest('/empleados', {
          method: 'POST',
          body: JSON.stringify(payload),
        })
      }
      setShowForm(false)
      await cargarEmpleados()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeactivate(emp) {
    if (!window.confirm(`¿Desactivar a ${emp.nombreCompleto}?`)) return
    setActionError(null)
    setDeactivatingId(emp.id)
    try {
      await apiRequest(`/empleados/${emp.id}`, { method: 'DELETE' })
      await cargarEmpleados()
    } catch (e) {
      setActionError(e.message)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Empleados">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          + Nuevo empleado
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
                {['Código', 'Nombre completo', 'Cédula', 'Departamento', 'Cargo', 'Correo', 'Teléfono', 'Estado', 'Acciones'].map(
                  (h) => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      {h}
                    </th>
                  )
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {empleados.length === 0 && (
                <tr>
                  <td colSpan={9} className="px-4 py-6 text-center text-gray-400">
                    Sin empleados registrados.
                  </td>
                </tr>
              )}
              {empleados.map((emp) => (
                <tr key={emp.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-500">{emp.codigo}</td>
                  <td className="px-4 py-3 font-medium text-gray-800">{emp.nombreCompleto}</td>
                  <td className="px-4 py-3 text-gray-600">{emp.cedula}</td>
                  <td className="px-4 py-3 text-gray-600">{emp.departamentoNombre}</td>
                  <td className="px-4 py-3 text-gray-600">{emp.cargo}</td>
                  <td className="px-4 py-3 text-gray-600">{emp.correo}</td>
                  <td className="px-4 py-3 text-gray-600">{emp.telefono}</td>
                  <td className="px-4 py-3">
                    <StatusBadge active={emp.activo} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(emp)}
                        className="rounded bg-blue-600 px-2 py-1 text-xs text-white hover:bg-blue-700"
                      >
                        Editar
                      </button>
                      {emp.activo && (
                        <button
                          type="button"
                          onClick={() => handleDeactivate(emp)}
                          disabled={deactivatingId === emp.id}
                          className="rounded bg-red-600 px-2 py-1 text-xs text-white hover:bg-red-700 disabled:opacity-50"
                        >
                          {deactivatingId === emp.id ? 'Desactivando...' : 'Desactivar'}
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
        <Modal title={editingId ? 'Editar empleado' : 'Nuevo empleado'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field label="Código">
              <input
                type="text"
                name="codigo"
                value={form.codigo}
                onChange={handleFormChange}
                required
                maxLength={20}
                className={inputCls}
              />
            </Field>
            <Field label="Nombre completo">
              <input
                type="text"
                name="nombreCompleto"
                value={form.nombreCompleto}
                onChange={handleFormChange}
                required
                maxLength={150}
                className={inputCls}
              />
            </Field>
            <Field label="Cédula">
              <input
                type="text"
                name="cedula"
                value={form.cedula}
                onChange={handleFormChange}
                required
                maxLength={20}
                className={inputCls}
              />
            </Field>
            <Field label="Cargo">
              <input
                type="text"
                name="cargo"
                value={form.cargo}
                onChange={handleFormChange}
                required
                maxLength={100}
                className={inputCls}
              />
            </Field>
            <Field label="Correo">
              <input
                type="email"
                name="correo"
                value={form.correo}
                onChange={handleFormChange}
                required
                maxLength={150}
                className={inputCls}
              />
            </Field>
            <Field label="Teléfono">
              <input
                type="text"
                name="telefono"
                value={form.telefono}
                onChange={handleFormChange}
                required
                maxLength={20}
                className={inputCls}
              />
            </Field>
            <Field label="Departamento">
              <select
                name="departamentoId"
                value={form.departamentoId}
                onChange={handleFormChange}
                required
                className={inputCls}
              >
                <option value="">Seleccionar...</option>
                {departamentos.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.nombre}
                  </option>
                ))}
              </select>
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
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear empleado'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
