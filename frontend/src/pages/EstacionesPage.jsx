import { useEffect, useState } from 'react'
import ConfirmModal from '../components/ConfirmModal'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
import StatusBadge from '../components/StatusBadge'
import { useAuth } from '../hooks/useAuth'
import apiRequest from '../services/api'
import { canManageCatalogs } from '../utils/rbac'
import { validateTextoMinimo } from '../utils/validators'

const EMPTY_FORM = { nombre: '' }

// GET /api/v1/estaciones solo devuelve estaciones activas (filtro real del
// backend, distinto de Tanques/Proveedores que devuelven todas). Por eso, tras
// desactivar una estación, NO se vuelve a pedir la lista completa (desaparecería
// sin forma de reactivarla desde aquí) — se marca inactiva localmente y se ofrece
// "Reactivar" (PUT con activo:true), que si dispara un refetch seguro al final.
export default function EstacionesPage() {
  const [estaciones, setEstaciones] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [errors, setErrors] = useState({})
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [cambiandoEstadoId, setCambiandoEstadoId] = useState(null)
  const [confirmEstacion, setConfirmEstacion] = useState(null)
  const [actionError, setActionError] = useState(null)

  const { user } = useAuth()
  const canManage = canManageCatalogs(user)
  // El backend distingue permisos: crear/editar → Administrador o Supervisor;
  // desactivar → solo Administrador (verificado en EstacionesController.cs).
  // El backend ya rechaza esto con 403, pero ocultar el botón evita que un
  // Supervisor vea una acción que sabemos de antemano que va a fallar.
  const esAdministrador = user?.roles?.includes('Administrador') ?? false

  async function cargarEstaciones() {
    try {
      const data = await apiRequest('/estaciones')
      setEstaciones(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/estaciones')
      .then((data) => { if (!cancelado) setEstaciones(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  function handleFormChange(e) {
    const { name, value } = e.target
    setForm((f) => ({ ...f, [name]: value }))
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

  function openEdit(e) {
    setEditingId(e.id)
    setForm({ nombre: e.nombre })
    setErrors({})
    setFormError(null)
    setShowForm(true)
  }

  async function handleSubmit(e) {
    e.preventDefault()
    const nombreErr = validateTextoMinimo(form.nombre, 3, 'El nombre de la estación')
    if (nombreErr) {
      setErrors({ nombre: nombreErr })
      return
    }

    setSubmitting(true)
    setFormError(null)
    try {
      if (editingId) {
        // El PUT real acepta también "activo", pero este formulario nunca lo
        // envía como false: la única vía para desactivar es el botón dedicado,
        // que sí pasa por la validación de dependencias (DELETE la tiene, PUT no).
        await apiRequest(`/estaciones/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify({ nombre: form.nombre.trim(), activo: true }),
        })
      } else {
        await apiRequest('/estaciones', {
          method: 'POST',
          body: JSON.stringify({ nombre: form.nombre.trim(), activo: true }),
        })
      }
      setShowForm(false)
      await cargarEstaciones()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  function handleDesactivar(estacion) {
    setActionError(null)
    setConfirmEstacion(estacion)
  }

  async function handleConfirmDesactivar() {
    if (!confirmEstacion) return
    const estacion = confirmEstacion
    setActionError(null)
    setCambiandoEstadoId(estacion.id)
    try {
      await apiRequest(`/estaciones/${estacion.id}`, { method: 'DELETE' })
      // No refetch: GET /estaciones ya no incluiría esta fila. Se marca inactiva
      // localmente para poder seguir ofreciendo "Reactivar".
      setEstaciones((prev) => prev.map((e) => (e.id === estacion.id ? { ...e, activo: false } : e)))
      setConfirmEstacion(null)
    } catch (e) {
      // El backend responde 409 ESTACION_CON_DESPACHOS si tiene despachos
      // asociados — el mensaje ya viene claro desde el backend.
      setActionError(e.message)
      setConfirmEstacion(null)
    } finally {
      setCambiandoEstadoId(null)
    }
  }

  async function handleReactivar(estacion) {
    setActionError(null)
    setCambiandoEstadoId(estacion.id)
    try {
      await apiRequest(`/estaciones/${estacion.id}`, {
        method: 'PUT',
        body: JSON.stringify({ nombre: estacion.nombre, activo: true }),
      })
      // Ahora sí es seguro refrescar: la estación reactivada volverá a aparecer
      // en GET /estaciones.
      await cargarEstaciones()
    } catch (e) {
      setActionError(e.message)
    } finally {
      setCambiandoEstadoId(null)
    }
  }

  return (
    <PageContainer title="Estaciones">
      {canManage && (
        <div className="mb-4 flex justify-end">
          <button
            type="button"
            onClick={openCreate}
            className="flex min-h-[44px] w-full sm:w-auto items-center justify-center gap-2 rounded-md bg-tanque px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-tanque/90 active:scale-[0.98]"
          >
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
            </svg>
            Nueva estación
          </button>
        </div>
      )}

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <ResponsiveTable
          data={estaciones}
          keyField="id"
          emptyMessage="Sin estaciones registradas."
          columns={[
            {
              key: 'nombre',
              label: 'Nombre',
              primary: true,
              priority: 'high',
              render: (e) => <span className="font-semibold text-tinta">{e.nombre}</span>,
            },
            {
              key: 'activo',
              label: 'Estado',
              priority: 'high',
              render: (e) => <StatusBadge active={e.activo} />,
            },
          ]}
          actions={canManage ? (e) => (
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => openEdit(e)}
                className="inline-flex min-h-[38px] items-center gap-1.5 rounded-sm border border-acero/30 bg-white px-3 py-1.5 text-xs font-medium text-tinta shadow-xs transition-colors hover:border-tanque hover:bg-fondo active:scale-[0.98]"
              >
                <svg className="h-3.5 w-3.5 text-acero" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
                Editar
              </button>
              {e.activo && esAdministrador && (
                <button
                  type="button"
                  onClick={() => handleDesactivar(e)}
                  disabled={cambiandoEstadoId === e.id}
                  className="inline-flex min-h-[38px] items-center gap-1.5 rounded-sm bg-peligro/10 border border-peligro/30 px-3 py-1.5 text-xs font-medium text-peligro shadow-xs transition-colors hover:bg-peligro hover:text-white disabled:opacity-50 active:scale-[0.98]"
                >
                  <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                  </svg>
                  {cambiandoEstadoId === e.id ? 'Desactivando...' : 'Desactivar'}
                </button>
              )}
              {!e.activo && (
                <button
                  type="button"
                  onClick={() => handleReactivar(e)}
                  disabled={cambiandoEstadoId === e.id}
                  className="inline-flex min-h-[38px] items-center gap-1.5 rounded-sm bg-exito/10 border border-exito/30 px-3 py-1.5 text-xs font-medium text-exito shadow-xs transition-colors hover:bg-exito hover:text-white disabled:opacity-50 active:scale-[0.98]"
                >
                  <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                  {cambiandoEstadoId === e.id ? 'Guardando...' : 'Reactivar'}
                </button>
              )}
            </div>
          ) : undefined}
        />
      )}

      {showForm && (
        <Modal title={editingId ? 'Editar estación' : 'Nueva estación'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field label="Nombre de la Estación" required error={errors.nombre} hint="Ej. Estación Central, Bomba Principal, Dispensador Norte">
              <input
                type="text"
                name="nombre"
                value={form.nombre}
                onChange={handleFormChange}
                required
                maxLength={100}
                placeholder="Ej. Estación Central de Despacho"
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
                disabled={submitting}
                className="inline-flex items-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50 shadow-sm"
              >
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear estación'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      <ConfirmModal
        isOpen={Boolean(confirmEstacion)}
        title="¿Desactivar estación de despacho?"
        message={
          <>
            Está a punto de desactivar la estación{' '}
            <strong className="font-semibold text-tinta">{confirmEstacion?.nombre}</strong>.
          </>
        }
        consequences={[
          'La estación no estará disponible para registrar nuevos despachos ni recepciones.',
          'El historial operacional y auditorías de la estación permanecerán intactos.',
        ]}
        type="danger"
        confirmText="Desactivar estación"
        cancelText="Cancelar"
        isLoading={Boolean(cambiandoEstadoId)}
        onConfirm={handleConfirmDesactivar}
        onClose={() => setConfirmEstacion(null)}
      />
    </PageContainer>
  )
}
