import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import { useDepartamentos } from '../hooks/useDepartamentos'
import { useEmpleados } from '../hooks/useEmpleados'
import { useVehiculos } from '../hooks/useVehiculos'
import apiRequest from '../services/api'

const ESTADO_VARIANT = {
  Pendiente: 'yellow',
  Aprobada: 'green',
  Rechazada: 'red',
}

const EMPTY_FORM = {
  empleadoId: '',
  vehiculoId: '',
  departamentoId: '',
  tipoCombustibleId: '',
  cantidadSolicitada: '',
  fechaVencimiento: '',
}

function nowLocalInputValue() {
  const now = new Date()
  const pad = (n) => String(n).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`
}

export default function SolicitudesPage() {
  const [solicitudes, setSolicitudes] = useState([])
  const empleados = useEmpleados()
  const vehiculos = useVehiculos()
  const departamentos = useDepartamentos()
  const [tipos, setTipos] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [aprobarModal, setAprobarModal] = useState(null)
  const [rechazarModal, setRechazarModal] = useState(null)
  const [cantidadAutorizada, setCantidadAutorizada] = useState('')
  const [motivoRechazo, setMotivoRechazo] = useState('')
  const [actionError, setActionError] = useState(null)

  async function cargarSolicitudes() {
    try {
      const data = await apiRequest('/solicitudes')
      setSolicitudes(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    cargarSolicitudes()
    apiRequest('/tipos-combustible')
      .then(setTipos)
      .catch(() => {})
  }, [])

  function handleFormChange(e) {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))
  }

  const requiredFieldsFilled =
    form.empleadoId &&
    form.vehiculoId &&
    form.departamentoId &&
    form.tipoCombustibleId &&
    form.cantidadSolicitada &&
    form.fechaVencimiento

  async function handleCreate(e) {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)
    try {
      await apiRequest('/solicitudes', {
        method: 'POST',
        body: JSON.stringify({
          empleadoId: Number(form.empleadoId),
          vehiculoId: Number(form.vehiculoId),
          departamentoId: Number(form.departamentoId),
          tipoCombustibleId: Number(form.tipoCombustibleId),
          cantidadSolicitada: Number(form.cantidadSolicitada),
          fechaVencimiento: new Date(form.fechaVencimiento).toISOString(),
        }),
      })
      setShowCreate(false)
      setForm(EMPTY_FORM)
      await cargarSolicitudes()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleAprobar(e) {
    e.preventDefault()
    setActionError(null)
    try {
      await apiRequest(`/solicitudes/${aprobarModal.id}/aprobar`, {
        method: 'POST',
        body: JSON.stringify({ cantidadAutorizada: Number(cantidadAutorizada) }),
      })
      setAprobarModal(null)
      await cargarSolicitudes()
    } catch (e) {
      setActionError(e.message)
    }
  }

  async function handleRechazar(e) {
    e.preventDefault()
    setActionError(null)
    try {
      await apiRequest(`/solicitudes/${rechazarModal.id}/rechazar`, {
        method: 'POST',
        body: JSON.stringify({ motivoRechazo }),
      })
      setRechazarModal(null)
      await cargarSolicitudes()
    } catch (e) {
      setActionError(e.message)
    }
  }

  return (
    <PageContainer title="Solicitudes de Combustible">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={() => {
            setForm(EMPTY_FORM)
            setFormError(null)
            setShowCreate(true)
          }}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          + Nueva solicitud
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
                {['#', 'Empleado', 'Vehículo', 'Departamento', 'Tipo', 'Solicitado', 'Autorizado', 'Estado', 'Fecha solicitud', 'Vencimiento', 'Acciones'].map(
                  (h) => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      {h}
                    </th>
                  )
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {solicitudes.length === 0 && (
                <tr>
                  <td colSpan={11} className="px-4 py-6 text-center text-gray-400">
                    Sin solicitudes registradas.
                  </td>
                </tr>
              )}
              {solicitudes.map((s) => (
                <tr key={s.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-500">{s.id}</td>
                  <td className="px-4 py-3 font-medium text-gray-800">{s.empleadoNombre}</td>
                  <td className="px-4 py-3 text-gray-600">{s.vehiculoPlaca}</td>
                  <td className="px-4 py-3 text-gray-600">{s.departamentoNombre}</td>
                  <td className="px-4 py-3 text-gray-600">{s.tipoCombustibleNombre}</td>
                  <td className="px-4 py-3 text-gray-600">{s.cantidadSolicitada}</td>
                  <td className="px-4 py-3 text-gray-600">{s.cantidadAutorizada ?? '—'}</td>
                  <td className="px-4 py-3">
                    <StatusBadge label={s.estado} variant={ESTADO_VARIANT[s.estado]} />
                  </td>
                  <td className="px-4 py-3 text-gray-500">{new Date(s.fechaSolicitud).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-gray-500">
                    {s.fechaVencimiento ? new Date(s.fechaVencimiento).toLocaleDateString() : '—'}
                  </td>
                  <td className="px-4 py-3">
                    {s.estado === 'Pendiente' && (
                      <div className="flex gap-2">
                        <button
                          type="button"
                          onClick={() => {
                            setAprobarModal({ id: s.id })
                            setCantidadAutorizada(String(s.cantidadSolicitada))
                            setActionError(null)
                          }}
                          className="rounded bg-green-600 px-2 py-1 text-xs text-white hover:bg-green-700"
                        >
                          Aprobar
                        </button>
                        <button
                          type="button"
                          onClick={() => {
                            setRechazarModal({ id: s.id })
                            setMotivoRechazo('')
                            setActionError(null)
                          }}
                          className="rounded bg-red-600 px-2 py-1 text-xs text-white hover:bg-red-700"
                        >
                          Rechazar
                        </button>
                      </div>
                    )}
                    {s.estado === 'Rechazada' && s.motivoRechazo && (
                      <span className="text-xs text-gray-400" title={s.motivoRechazo}>
                        · {s.motivoRechazo.slice(0, 30)}
                        {s.motivoRechazo.length > 30 ? '…' : ''}
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showCreate && (
        <Modal title="Nueva solicitud manual" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate} className="space-y-4">
            <Field label="Empleado">
              <select name="empleadoId" value={form.empleadoId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {empleados.map((e) => (
                  <option key={e.id} value={e.id}>
                    {e.nombreCompleto}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Vehículo">
              <select name="vehiculoId" value={form.vehiculoId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {vehiculos.map((v) => (
                  <option key={v.id} value={v.id}>
                    {v.placa} — {v.marca} {v.modelo}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Departamento">
              <select name="departamentoId" value={form.departamentoId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {departamentos.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.nombre}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Tipo de combustible">
              <select name="tipoCombustibleId" value={form.tipoCombustibleId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {tipos.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.nombre}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Cantidad solicitada">
              <input
                type="number"
                name="cantidadSolicitada"
                value={form.cantidadSolicitada}
                onChange={handleFormChange}
                min="0.0001"
                step="0.0001"
                required
                className={inputCls}
                placeholder="0.00"
              />
            </Field>
            <Field label="Fecha de vencimiento">
              <input
                type="datetime-local"
                name="fechaVencimiento"
                value={form.fechaVencimiento}
                onChange={handleFormChange}
                required
                min={nowLocalInputValue()}
                className={inputCls}
              />
            </Field>
            {formError && <p className="text-sm text-red-600">{formError}</p>}
            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setShowCreate(false)}
                className="rounded-md border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={submitting || !requiredFieldsFilled}
                className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
              >
                {submitting ? 'Guardando...' : 'Crear solicitud'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {aprobarModal && (
        <Modal title="Aprobar solicitud" onClose={() => setAprobarModal(null)}>
          <form onSubmit={handleAprobar} className="space-y-4">
            <Field label="Cantidad autorizada">
              <input
                type="number"
                value={cantidadAutorizada}
                onChange={(e) => setCantidadAutorizada(e.target.value)}
                min="0.0001"
                step="0.0001"
                required
                className={inputCls}
              />
            </Field>
            {actionError && <p className="text-sm text-red-600">{actionError}</p>}
            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setAprobarModal(null)}
                className="rounded-md border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                Cancelar
              </button>
              <button type="submit" className="rounded-md bg-green-600 px-4 py-2 text-sm text-white hover:bg-green-700">
                Aprobar
              </button>
            </div>
          </form>
        </Modal>
      )}

      {rechazarModal && (
        <Modal title="Rechazar solicitud" onClose={() => setRechazarModal(null)}>
          <form onSubmit={handleRechazar} className="space-y-4">
            <Field label="Motivo de rechazo">
              <textarea
                value={motivoRechazo}
                onChange={(e) => setMotivoRechazo(e.target.value)}
                required
                maxLength={500}
                rows={3}
                className={inputCls}
                placeholder="Indique el motivo..."
              />
            </Field>
            {actionError && <p className="text-sm text-red-600">{actionError}</p>}
            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setRechazarModal(null)}
                className="rounded-md border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                Cancelar
              </button>
              <button type="submit" className="rounded-md bg-red-600 px-4 py-2 text-sm text-white hover:bg-red-700">
                Rechazar
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
