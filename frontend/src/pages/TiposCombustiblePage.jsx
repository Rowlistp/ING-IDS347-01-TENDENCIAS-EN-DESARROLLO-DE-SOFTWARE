import { useEffect, useState } from 'react'
import { useAuth } from '../hooks/useAuth'
import { ROLES } from '../utils/rbac'
import ConfirmModal from '../components/ConfirmModal'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'

const EMPTY_FORM = {
  nombre: '',
  activo: true,
}

export default function TiposCombustiblePage() {
  const { user } = useAuth()
  const esAdministrador = user?.roles?.includes(ROLES.ADMINISTRADOR)
  const [tipos, setTipos] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [deactivatingId, setDeactivatingId] = useState(null)
  const [confirmTarget, setConfirmTarget] = useState(null)
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

  function handleDeactivate(t) {
    setActionError(null)
    setConfirmTarget(t)
  }

  async function handleConfirmDeactivate() {
    if (!confirmTarget) return
    const t = confirmTarget
    setActionError(null)
    setDeactivatingId(t.id)
    try {
      await apiRequest(`/tipos-combustible/${t.id}`, { method: 'DELETE' })
      await cargarTipos()
      setConfirmTarget(null)
    } catch (e) {
      // El backend valida 3 dependencias por separado, cada una con su propio
      // mensaje claro: TIPO_COMBUSTIBLE_CON_TANQUES_ACTIVOS,
      // TIPO_COMBUSTIBLE_CON_SOLICITUDES_ACTIVAS, TIPO_COMBUSTIBLE_CON_TICKETS_ACTIVOS.
      // Se muestra el mensaje tal cual, ya viene distinto por cada caso.
      setActionError(e.message)
      setConfirmTarget(null)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Tipos de Combustible">
      <div className="mb-4 flex flex-col sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="inline-flex min-h-[44px] w-full sm:w-auto items-center justify-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-semibold text-white hover:bg-tanque/90 shadow-sm transition-colors"
        >
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
          </svg>
          Nuevo tipo de combustible
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <ResponsiveTable
          data={tipos}
          keyField="id"
          columns={[
            {
              key: 'nombre',
              label: 'Nombre del Combustible',
              primary: true,
              priority: 'high',
              render: (t) => <span className="font-semibold text-tinta">{t.nombre}</span>,
            },
            {
              key: 'activo',
              label: 'Estado',
              priority: 'high',
              render: (t) => <StatusBadge active={t.activo} />,
            },
          ]}
          actions={(t) => (
            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                onClick={() => openEdit(t)}
                className="inline-flex min-h-[38px] items-center gap-1.5 rounded-md border border-acero/30 bg-white px-2.5 py-1.5 text-xs font-semibold text-tinta hover:bg-fondo hover:border-tanque transition-colors shadow-xs"
              >
                <svg className="h-3.5 w-3.5 text-acero" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
                Editar
              </button>
              {t.activo && esAdministrador && (
                <button
                  type="button"
                  onClick={() => handleDeactivate(t)}
                  disabled={deactivatingId === t.id}
                  className="inline-flex min-h-[38px] items-center gap-1.5 rounded-md border border-peligro/30 bg-peligro/10 px-2.5 py-1.5 text-xs font-semibold text-peligro hover:bg-peligro hover:text-white transition-colors shadow-xs disabled:opacity-50"
                >
                  <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                  </svg>
                  {deactivatingId === t.id ? 'Desactivando...' : 'Desactivar'}
                </button>
              )}
            </div>
          )}
          emptyMessage="Sin tipos de combustible registrados."
        />
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
                <input type="checkbox" name="activo" disabled={!esAdministrador && Boolean(tipos.find((item) => item.id === editingId)?.activo)} checked={form.activo} onChange={handleFormChange} />
                Activo
              </label>
            )}

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
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear tipo de combustible'}
              </button>
            </div>

          </form>
        </Modal>
      )}

      <ConfirmModal
        isOpen={Boolean(confirmTarget)}
        title="¿Desactivar tipo de combustible?"
        message={
          <>
            Está a punto de desactivar{' '}
            <strong className="font-semibold text-tinta">{confirmTarget?.nombre}</strong>.
          </>
        }
        consequences={[
          'No se podrán emitir nuevas solicitudes con este tipo de combustible.',
          'Los vehículos que tengan este combustible requerirán cambio de asignación.',
        ]}
        type="danger"
        confirmText="Desactivar combustible"
        cancelText="Cancelar"
        isLoading={Boolean(deactivatingId)}
        onConfirm={handleConfirmDeactivate}
        onClose={() => setConfirmTarget(null)}
      />
    </PageContainer>
  )
}
