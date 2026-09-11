import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'

const EMPTY_FORM = {
  nombreUsuario: '',
  contrasena: '',
  rolIds: [],
}

export default function UsuariosPage() {
  const [usuarios, setUsuarios] = useState([])
  const [roles, setRoles] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [statusChangingId, setStatusChangingId] = useState(null)
  const [actionError, setActionError] = useState(null)

  const [resetModalUser, setResetModalUser] = useState(null)
  const [nuevaContrasena, setNuevaContrasena] = useState('')
  const [resetSubmitting, setResetSubmitting] = useState(false)
  const [resetError, setResetError] = useState(null)

  async function cargarUsuarios() {
    try {
      const data = await apiRequest('/usuarios')
      setUsuarios(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    cargarUsuarios()
    apiRequest('/roles')
      .then(setRoles)
      .catch(() => {})
  }, [])

  function handleNombreChange(e) {
    setForm((f) => ({ ...f, nombreUsuario: e.target.value }))
  }

  function handleContrasenaChange(e) {
    setForm((f) => ({ ...f, contrasena: e.target.value }))
  }

  function toggleRol(rolId) {
    setForm((f) => ({
      ...f,
      rolIds: f.rolIds.includes(rolId) ? f.rolIds.filter((id) => id !== rolId) : [...f.rolIds, rolId],
    }))
  }

  function openCreate() {
    setEditingId(null)
    setForm(EMPTY_FORM)
    setFormError(null)
    setShowForm(true)
  }

  function openEdit(usuario) {
    const rolIds = roles.filter((r) => usuario.roles.includes(r.nombre)).map((r) => r.id)
    setEditingId(usuario.id)
    setForm({ nombreUsuario: usuario.nombreUsuario, contrasena: '', rolIds })
    setFormError(null)
    setShowForm(true)
  }

  const requiredFieldsFilled =
    form.nombreUsuario.trim() && form.rolIds.length > 0 && (editingId || form.contrasena.length >= 12)

  async function handleSubmit(e) {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)

    try {
      if (editingId) {
        await apiRequest(`/usuarios/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify({
            nombreUsuario: form.nombreUsuario.trim(),
            rolIds: form.rolIds,
          }),
        })
      } else {
        await apiRequest('/usuarios', {
          method: 'POST',
          body: JSON.stringify({
            nombreUsuario: form.nombreUsuario.trim(),
            contrasena: form.contrasena,
            rolIds: form.rolIds,
          }),
        })
      }
      setShowForm(false)
      await cargarUsuarios()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleToggleStatus(usuario) {
    const accion = usuario.activo ? 'desactivar' : 'reactivar'
    if (!window.confirm(`¿Seguro que quieres ${accion} a "${usuario.nombreUsuario}"?`)) return
    setActionError(null)
    setStatusChangingId(usuario.id)
    try {
      await apiRequest(`/usuarios/${usuario.id}/estado`, {
        method: 'PATCH',
        body: JSON.stringify({ activo: !usuario.activo }),
      })
      await cargarUsuarios()
    } catch (e) {
      setActionError(e.message)
    } finally {
      setStatusChangingId(null)
    }
  }

  function openResetModal(usuario) {
    setResetModalUser(usuario)
    setNuevaContrasena('')
    setResetError(null)
  }

  async function handleResetPassword(e) {
    e.preventDefault()
    setResetSubmitting(true)
    setResetError(null)
    try {
      await apiRequest('/auth/password/reset', {
        method: 'POST',
        body: JSON.stringify({ usuarioId: resetModalUser.id, nuevaContrasena }),
      })
      setResetModalUser(null)
    } catch (e) {
      setResetError(e.message)
    } finally {
      setResetSubmitting(false)
    }
  }

  return (
    <PageContainer title="Usuarios">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          + Nuevo usuario
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
                {['Usuario', 'Roles', 'Estado', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {usuarios.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-center text-gray-400">
                    Sin usuarios registrados.
                  </td>
                </tr>
              )}
              {usuarios.map((u) => (
                <tr key={u.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-800">{u.nombreUsuario}</td>
                  <td className="px-4 py-3 text-gray-600">{u.roles.join(', ')}</td>
                  <td className="px-4 py-3">
                    <StatusBadge active={u.activo} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(u)}
                        className="rounded bg-blue-600 px-2 py-1 text-xs text-white hover:bg-blue-700"
                      >
                        Editar
                      </button>
                      <button
                        type="button"
                        onClick={() => openResetModal(u)}
                        className="rounded bg-amber-600 px-2 py-1 text-xs text-white hover:bg-amber-700"
                      >
                        Restablecer contraseña
                      </button>
                      <button
                        type="button"
                        onClick={() => handleToggleStatus(u)}
                        disabled={statusChangingId === u.id}
                        className={`rounded px-2 py-1 text-xs text-white disabled:opacity-50 ${
                          u.activo ? 'bg-red-600 hover:bg-red-700' : 'bg-green-600 hover:bg-green-700'
                        }`}
                      >
                        {statusChangingId === u.id
                          ? 'Procesando...'
                          : u.activo
                            ? 'Desactivar'
                            : 'Reactivar'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showForm && (
        <Modal title={editingId ? 'Editar usuario' : 'Nuevo usuario'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field label="Nombre de usuario">
              <input
                type="text"
                value={form.nombreUsuario}
                onChange={handleNombreChange}
                required
                maxLength={100}
                className={inputCls}
              />
            </Field>

            {!editingId && (
              <Field label="Contraseña inicial">
                <input
                  type="password"
                  value={form.contrasena}
                  onChange={handleContrasenaChange}
                  required
                  minLength={12}
                  maxLength={128}
                  className={inputCls}
                />
                <p className="mt-1 text-xs text-gray-400">
                  Entre 12 y 128 caracteres, con mayúscula, minúscula, número y carácter especial, sin espacios.
                </p>
              </Field>
            )}

            <Field label="Roles">
              <div className="space-y-1">
                {roles.map((r) => (
                  <label key={r.id} className="flex items-center gap-2 text-sm text-gray-700">
                    <input
                      type="checkbox"
                      checked={form.rolIds.includes(r.id)}
                      onChange={() => toggleRol(r.id)}
                    />
                    {r.nombre}
                  </label>
                ))}
              </div>
            </Field>

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
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear usuario'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {resetModalUser && (
        <Modal title={`Restablecer contraseña — ${resetModalUser.nombreUsuario}`} onClose={() => setResetModalUser(null)}>
          <form onSubmit={handleResetPassword} className="space-y-4">
            <Field label="Nueva contraseña">
              <input
                type="password"
                value={nuevaContrasena}
                onChange={(e) => setNuevaContrasena(e.target.value)}
                required
                minLength={12}
                maxLength={128}
                autoFocus
                className={inputCls}
              />
              <p className="mt-1 text-xs text-gray-400">
                Entre 12 y 128 caracteres, con mayúscula, minúscula, número y carácter especial, sin espacios.
              </p>
            </Field>

            {resetError && <p className="text-sm text-red-600">{resetError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setResetModalUser(null)}
                className="rounded-md border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={resetSubmitting || nuevaContrasena.length < 12}
                className="rounded-md bg-amber-600 px-4 py-2 text-sm text-white hover:bg-amber-700 disabled:opacity-50"
              >
                {resetSubmitting ? 'Guardando...' : 'Restablecer'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
