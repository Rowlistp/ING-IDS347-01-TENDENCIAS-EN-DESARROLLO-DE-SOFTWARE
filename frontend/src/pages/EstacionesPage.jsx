import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'
import { getUser } from '../services/auth'
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
  const [actionError, setActionError] = useState(null)

  // El backend distingue permisos: crear/editar → Administrador o Supervisor;
  // desactivar → solo Administrador (verificado en EstacionesController.cs).
  // El backend ya rechaza esto con 403, pero ocultar el botón evita que un
  // Supervisor vea una acción que sabemos de antemano que va a fallar.
  const esAdministrador = getUser()?.roles?.includes('Administrador') ?? false

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

  async function handleDesactivar(estacion) {
    if (!window.confirm(`¿Desactivar la estación ${estacion.nombre}?`)) return
    setActionError(null)
    setCambiandoEstadoId(estacion.id)
    try {
      await apiRequest(`/estaciones/${estacion.id}`, { method: 'DELETE' })
      // No refetch: GET /estaciones ya no incluiría esta fila. Se marca inactiva
      // localmente para poder seguir ofreciendo "Reactivar".
      setEstaciones((prev) => prev.map((e) => (e.id === estacion.id ? { ...e, activo: false } : e)))
    } catch (e) {
      // El backend responde 409 ESTACION_CON_DESPACHOS si tiene despachos
      // asociados — el mensaje ya viene claro desde el backend.
      setActionError(e.message)
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
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="inline-flex items-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 shadow-sm"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
          </svg>
          Nueva estación
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
              {estaciones.length === 0 && (
                <tr>
                  <td colSpan={3} className="px-4 py-6 text-center text-acero/70">
                    Sin estaciones registradas.
                  </td>
                </tr>
              )}
              {estaciones.map((e) => (
                <tr key={e.id} className="hover:bg-fondo/70 transition-colors">
                  <td className="px-4 py-3 font-medium text-tinta">{e.nombre}</td>
                  <td className="px-4 py-3">
                    <StatusBadge active={e.activo} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(e)}
                        className="inline-flex items-center gap-1.5 rounded border border-acero/30 bg-white px-2.5 py-1 text-xs font-medium text-tinta hover:bg-fondo transition-colors shadow-sm"
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
                          className="inline-flex items-center gap-1.5 rounded bg-peligro/10 border border-peligro/30 px-2.5 py-1 text-xs font-medium text-peligro hover:bg-peligro hover:text-white transition-colors disabled:opacity-50 shadow-sm"
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
                          className="inline-flex items-center gap-1.5 rounded bg-exito/10 border border-exito/30 px-2.5 py-1 text-xs font-medium text-exito hover:bg-exito hover:text-white transition-colors disabled:opacity-50 shadow-sm"
                        >
                          <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                          </svg>
                          {cambiandoEstadoId === e.id ? 'Guardando...' : 'Reactivar'}
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
    </PageContainer>
  )
}
