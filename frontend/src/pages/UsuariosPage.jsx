import { useEffect, useState } from 'react'
import ConfirmModal from '../components/ConfirmModal'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
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
  const [confirmUser, setConfirmUser] = useState(null)
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
    let cancelado = false
    apiRequest('/usuarios')
      .then((data) => { if (!cancelado) setUsuarios(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    apiRequest('/roles')
      .then((data) => { if (!cancelado) setRoles(data) })
      .catch(() => {})
    return () => { cancelado = true }
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

  function handleToggleStatus(usuario) {
    setActionError(null)
    setConfirmUser(usuario)
  }

  async function handleConfirmToggleStatus() {
    if (!confirmUser) return
    const usuario = confirmUser
    setActionError(null)
    setStatusChangingId(usuario.id)
    try {
      await apiRequest(`/usuarios/${usuario.id}/estado`, {
        method: 'PATCH',
        body: JSON.stringify({ activo: !usuario.activo }),
      })
      await cargarUsuarios()
      setConfirmUser(null)
    } catch (e) {
      setActionError(e.message)
      setConfirmUser(null)
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
      <div className="mb-4 flex flex-col sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="inline-flex min-h-[44px] w-full sm:w-auto items-center justify-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-semibold text-white hover:bg-tanque/90 shadow-sm transition-colors"
        >
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
          </svg>
          + Nuevo usuario
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <ResponsiveTable
          data={usuarios}
          keyField="id"
          columns={[
            {
              key: 'nombreUsuario',
              label: 'Usuario',
              primary: true,
              priority: 'high',
              render: (u) => <span className="font-semibold text-tinta">{u.nombreUsuario}</span>,
            },
            {
              key: 'empleadoNombre',
              label: 'Empleado vinculado',
              priority: 'high',
              render: (u) =>
                u.empleadoNombre ? (
                  <span className="inline-flex items-center gap-1 font-medium text-xs text-tanque bg-tanque/10 px-2 py-0.5 rounded border border-tanque/20">
                    🪪 {u.empleadoNombre}
                  </span>
                ) : (
                  <span className="text-xs text-acero/60 italic">Sin empleado</span>
                ),
            },
            {
              key: 'roles',
              label: 'Roles',
              priority: 'med',
              render: (u) => <span className="text-acero text-xs">{u.roles.join(', ')}</span>,
            },
            {
              key: 'activo',
              label: 'Estado',
              priority: 'high',
              render: (u) => <StatusBadge active={u.activo} />,
            },
          ]}
          actions={(u) => (
            <div className="flex flex-wrap items-center gap-1.5">
              <button
                type="button"
                onClick={() => openEdit(u)}
                className="inline-flex min-h-[38px] items-center gap-1.5 rounded-md border border-acero/30 bg-white px-2.5 py-1.5 text-xs font-semibold text-tinta hover:bg-fondo hover:border-tanque transition-colors shadow-xs"
              >
                <svg className="h-3.5 w-3.5 text-acero" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
                Editar
              </button>
              <button
                type="button"
                onClick={() => openResetModal(u)}
                className="inline-flex min-h-[38px] items-center gap-1.5 rounded-md border border-info/30 bg-info/10 px-2.5 py-1.5 text-xs font-semibold text-info hover:bg-info hover:text-white transition-colors shadow-xs"
              >
                <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
                </svg>
                Restablecer
              </button>
              <button
                type="button"
                onClick={() => handleToggleStatus(u)}
                disabled={statusChangingId === u.id}
                className={`inline-flex min-h-[38px] items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-xs font-semibold transition-colors disabled:opacity-50 shadow-xs ${
                  u.activo
                    ? 'border-peligro/30 bg-peligro/10 text-peligro hover:bg-peligro hover:text-white'
                    : 'border-exito/30 bg-exito/10 text-exito hover:bg-exito hover:text-white'
                }`}
              >
                {u.activo ? (
                  <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                  </svg>
                ) : (
                  <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                )}
                {statusChangingId === u.id
                  ? 'Procesando...'
                  : u.activo
                    ? 'Desactivar'
                    : 'Reactivar'}
              </button>
            </div>
          )}
          emptyMessage="Sin usuarios registrados."
        />
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
                <p className="mt-1 text-xs text-acero/70">
                  Entre 12 y 128 caracteres, con mayúscula, minúscula, número y carácter especial, sin espacios.
                </p>
              </Field>
            )}

            <Field label="Roles">
              <div className="space-y-1">
                {roles.map((r) => (
                  <label key={r.id} className="flex items-center gap-2 text-sm text-tinta">
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

            {formError && <p className="text-sm text-peligro">{formError}</p>}

            <div className="flex flex-col-reverse sm:flex-row justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setShowForm(false)}
                className="flex min-h-[44px] items-center justify-center rounded-md border border-acero/30 px-4 py-2 text-sm font-medium text-tinta hover:bg-fondo transition-colors"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={submitting || !requiredFieldsFilled}
                className="flex min-h-[44px] items-center justify-center rounded-md bg-tanque px-4 py-2 text-sm font-semibold text-white hover:bg-tanque/90 disabled:opacity-50 transition-colors shadow-sm"
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
              <p className="mt-1 text-xs text-acero/70">
                Entre 12 y 128 caracteres, con mayúscula, minúscula, número y carácter especial, sin espacios.
              </p>
            </Field>

            {resetError && <p className="text-sm text-peligro">{resetError}</p>}

            <div className="flex flex-col-reverse sm:flex-row justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setResetModalUser(null)}
                className="flex min-h-[44px] items-center justify-center rounded-md border border-acero/30 px-4 py-2 text-sm font-medium text-tinta hover:bg-fondo transition-colors"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={resetSubmitting || nuevaContrasena.length < 12}
                className="flex min-h-[44px] items-center justify-center rounded-md bg-tanque px-4 py-2 text-sm font-semibold text-white hover:bg-tanque/90 disabled:opacity-50 transition-colors shadow-sm"
              >
                {resetSubmitting ? 'Guardando...' : 'Restablecer'}
              </button>
            </div>

          </form>
        </Modal>
      )}

      <ConfirmModal
        isOpen={Boolean(confirmUser)}
        title={confirmUser?.activo ? '¿Desactivar usuario?' : '¿Reactivar usuario?'}
        message={
          <>
            Está a punto de {confirmUser?.activo ? 'desactivar' : 'reactivar'} al usuario{' '}
            <strong className="font-semibold text-tinta">{confirmUser?.nombreUsuario}</strong>.
          </>
        }
        consequences={
          confirmUser?.activo
            ? [
                'El usuario no podrá iniciar sesión en la plataforma mientras esté inactivo.',
                'Las sesiones activas serán invalidadas por seguridad.',
              ]
            : [
                'El usuario recuperará el acceso al sistema con sus roles previamente asignados.',
              ]
        }
        type={confirmUser?.activo ? 'danger' : 'success'}
        confirmText={confirmUser?.activo ? 'Desactivar usuario' : 'Reactivar usuario'}
        cancelText="Cancelar"
        isLoading={Boolean(statusChangingId)}
        onConfirm={handleConfirmToggleStatus}
        onClose={() => setConfirmUser(null)}
      />
    </PageContainer>
  )
}
