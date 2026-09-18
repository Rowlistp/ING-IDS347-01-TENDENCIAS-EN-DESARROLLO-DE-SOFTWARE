import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import { useAuth } from '../hooks/useAuth'
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

export default function SolicitudesPage() {
  const { user } = useAuth()
  const [solicitudes, setSolicitudes] = useState([])
  const empleados = useEmpleados()
  const vehiculos = useVehiculos()
  const departamentos = useDepartamentos()
  const [tiposCombustible, setTipos] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const isSolicitanteOnly =
    user?.roles?.includes('Solicitante') &&
    !user?.roles?.includes('Administrador') &&
    !user?.roles?.includes('Supervisor')
  const miEmpleado = empleados.find((e) => e.usuarioId === user?.usuarioId)

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
    let cancelado = false
    apiRequest('/solicitudes')
      .then((data) => { if (!cancelado) setSolicitudes(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    apiRequest('/tipos-combustible')
      .then((data) => { if (!cancelado) setTipos(data) })
      .catch(() => {})
    return () => { cancelado = true }
  }, [])

  function handleFormChange(e) {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))
  }

  function openCreate() {
    setForm({
      ...EMPTY_FORM,
      empleadoId: miEmpleado ? String(miEmpleado.id) : '',
      departamentoId: miEmpleado ? String(miEmpleado.departamentoId) : '',
    })
    setFormError(null)
    setShowCreate(true)
  }

  const requiredFieldsFilled =
    (isSolicitanteOnly ? Boolean(miEmpleado) : Boolean(form.empleadoId)) &&
    form.vehiculoId &&
    (isSolicitanteOnly ? Boolean(miEmpleado) : Boolean(form.departamentoId)) &&
    form.tipoCombustibleId &&
    form.cantidadSolicitada &&
    form.fechaVencimiento

  async function handleCreate(e) {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)

    const empId = isSolicitanteOnly && miEmpleado ? miEmpleado.id : Number(form.empleadoId)
    const deptoId = isSolicitanteOnly && miEmpleado ? miEmpleado.departamentoId : Number(form.departamentoId)

    try {
      await apiRequest('/solicitudes', {
        method: 'POST',
        body: JSON.stringify({
          empleadoId: empId,
          vehiculoId: Number(form.vehiculoId),
          departamentoId: deptoId,
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
          onClick={openCreate}
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          + Nueva solicitud
        </button>
      </div>

      {isSolicitanteOnly && !miEmpleado && (
        <div className="mb-4 rounded-md border border-peligro/30 bg-peligro/10 p-3 text-sm text-peligro">
          ⚠️ <strong>Cuenta no vinculada:</strong> Su usuario actual (<code>{user?.nombreUsuario}</code>) no está vinculado a ningún empleado. Comuníquese con un Administrador para asociar su cuenta desde el catálogo de Empleados.
        </div>
      )}

      {isSolicitanteOnly && miEmpleado && (
        <div className="mb-4 rounded-md border border-tanque/30 bg-tanque/10 p-3 text-sm text-tanque flex items-center justify-between">
          <span>
            👤 Sesión activa como Solicitante: <strong>{miEmpleado.nombreCompleto}</strong> ({miEmpleado.codigo} — {miEmpleado.departamentoNombre})
          </span>
          <span className="text-xs bg-tanque text-white px-2.5 py-0.5 rounded font-mono font-medium">
            OwnerFilter Activo
          </span>
        </div>
      )}

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-sm border border-acero/20">
          <table className="min-w-full divide-y divide-acero/20 text-sm">
            <thead className="bg-fondo">
              <tr>
                {['#', 'Empleado', 'Vehículo', 'Departamento', 'Tipo', 'Solicitado', 'Autorizado', 'Estado', 'Fecha solicitud', 'Vencimiento', 'Acciones'].map(
                  (h) => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                      {h}
                    </th>
                  )
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {solicitudes.length === 0 && (
                <tr>
                  <td colSpan={11} className="px-4 py-6 text-center text-acero/70">
                    Sin solicitudes registradas.
                  </td>
                </tr>
              )}
              {solicitudes.map((s) => (
                <tr key={s.id} className="hover:bg-fondo">
                  <td className="px-4 py-3 font-mono num text-acero">{s.id}</td>
                  <td className="px-4 py-3 font-medium text-tinta">{s.empleadoNombre}</td>
                  <td className="px-4 py-3 font-mono text-acero">{s.vehiculoPlaca}</td>
                  <td className="px-4 py-3 text-acero">{s.departamentoNombre}</td>
                  <td className="px-4 py-3 text-acero">{s.tipoCombustibleNombre}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{s.cantidadSolicitada}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{s.cantidadAutorizada ?? '—'}</td>
                  <td className="px-4 py-3">
                    <StatusBadge label={s.estado} variant={ESTADO_VARIANT[s.estado]} />
                  </td>
                  <td className="px-4 py-3 text-acero">{new Date(s.fechaSolicitud).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-acero">
                    {s.fechaVencimiento ? new Date(s.fechaVencimiento).toLocaleDateString() : '—'}
                  </td>
                  <td className="px-4 py-3.5">
                    {s.estado === 'Pendiente' && (
                      <div className="flex gap-2">
                        <button
                          type="button"
                          onClick={() => {
                            setAprobarModal(s)
                            setCantidadAutorizada(String(s.cantidadSolicitada))
                            setActionError(null)
                          }}
                          className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-exito/30 bg-exito/10 px-3 py-1.5 text-xs font-semibold text-exito transition-colors hover:bg-exito hover:text-white"
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                            <polyline points="20 6 9 17 4 12" />
                          </svg>
                          Aprobar
                        </button>
                        <button
                          type="button"
                          onClick={() => {
                            setRechazarModal(s)
                            setMotivoRechazo('')
                            setActionError(null)
                          }}
                          className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-peligro/30 bg-white px-3 py-1.5 text-xs font-semibold text-peligro transition-colors hover:border-peligro hover:bg-peligro/10"
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <line x1="18" y1="6" x2="6" y2="18" />
                            <line x1="6" y1="6" x2="18" y2="18" />
                          </svg>
                          Rechazar
                        </button>
                      </div>
                    )}
                    {s.estado === 'Rechazada' && s.motivoRechazo && (
                      <span className="text-xs text-acero/80" title={s.motivoRechazo}>
                        Motivo: {s.motivoRechazo.slice(0, 25)}
                        {s.motivoRechazo.length > 25 ? '…' : ''}
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
        <Modal title="Nueva solicitud de combustible" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate} className="space-y-4">
            {isSolicitanteOnly && !miEmpleado && (
              <div className="rounded-md border border-peligro/30 bg-peligro/10 p-3 text-sm text-peligro">
                ⚠️ Su cuenta de usuario no está vinculada a ningún empleado. No puede emitir solicitudes hasta que un Administrador asocie su cuenta.
              </div>
            )}

            <Field
              label="Empleado solicitante"
              required
              hint={isSolicitanteOnly && miEmpleado ? `Registrando como: ${miEmpleado.nombreCompleto} (${miEmpleado.codigo})` : undefined}
            >
              <select
                name="empleadoId"
                value={isSolicitanteOnly && miEmpleado ? miEmpleado.id : form.empleadoId}
                onChange={handleFormChange}
                required
                disabled={isSolicitanteOnly}
                className={inputCls}
              >
                {isSolicitanteOnly ? (
                  miEmpleado ? (
                    <option value={miEmpleado.id}>
                      {miEmpleado.nombreCompleto} ({miEmpleado.codigo})
                    </option>
                  ) : (
                    <option value="">-- Sin empleado vinculado --</option>
                  )
                ) : (
                  <>
                    <option value="">Seleccionar...</option>
                    {empleados.map((e) => (
                      <option key={e.id} value={e.id}>
                        {e.nombreCompleto} ({e.codigo}) {e.usuarioNombre ? `[Usuario: ${e.usuarioNombre}]` : ''}
                      </option>
                    ))}
                  </>
                )}
              </select>
            </Field>

            <Field label="Vehículo" required>
              <select
                name="vehiculoId"
                value={form.vehiculoId}
                onChange={handleFormChange}
                required
                className={inputCls}
              >
                <option value="">Seleccionar...</option>
                {vehiculos.map((v) => (
                  <option key={v.id} value={v.id}>
                    {v.placa} — {v.marca} {v.modelo} ({v.ficha})
                  </option>
                ))}
              </select>
            </Field>

            <Field label="Departamento" required>
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

            <Field label="Tipo de combustible" required>
              <select
                name="tipoCombustibleId"
                value={form.tipoCombustibleId}
                onChange={handleFormChange}
                required
                className={inputCls}
              >
                <option value="">Seleccionar...</option>
                {tiposCombustible.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.nombre}
                  </option>
                ))}
              </select>
            </Field>

            <Field label="Cantidad solicitada (galones)" required hint="Rango permitido: 1 a 500 galones">
              <input
                type="number"
                name="cantidadSolicitada"
                value={form.cantidadSolicitada}
                onChange={handleFormChange}
                min="0.0001"
                step="0.0001"
                required
                className={`${inputCls} font-mono`}
              />
            </Field>

            <Field label="Días de vigencia">
              <input
                type="number"
                name="diasVigencia"
                value={form.diasVigencia}
                onChange={handleFormChange}
                min="1"
                max="365"
                placeholder="7 (por defecto)"
                className={inputCls}
              />
            </Field>

            {formError && <p className="text-sm text-peligro">{formError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setShowCreate(false)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={submitting || !requiredFieldsFilled}
                className="rounded-md bg-tanque px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50 font-medium"
              >
                {submitting ? 'Guardando...' : 'Crear solicitud'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {aprobarModal && (
        <Modal title="Aprobación de Solicitud de Combustible" onClose={() => setAprobarModal(null)}>
          <form onSubmit={handleAprobar} className="space-y-4">
            {/* Context Card */}
            <div className="rounded-md border border-tanque/20 bg-tanque/5 p-3.5 text-sm space-y-2">
              <div className="flex justify-between items-center border-b border-tanque/10 pb-2">
                <span className="font-semibold text-tanque">Solicitud #{aprobarModal.id}</span>
                <span className="text-xs bg-tanque/10 text-tanque px-2 py-0.5 rounded font-mono">
                  {aprobarModal.departamentoNombre}
                </span>
              </div>
              <div className="grid grid-cols-2 gap-2 text-xs">
                <div>
                  <span className="text-acero block">Empleado:</span>
                  <span className="font-medium text-tinta">{aprobarModal.empleadoNombre}</span>
                </div>
                <div>
                  <span className="text-acero block">Vehículo:</span>
                  <span className="font-medium font-mono text-tinta">{aprobarModal.vehiculoPlaca}</span>
                </div>
                <div>
                  <span className="text-acero block">Combustible:</span>
                  <span className="font-medium text-tinta">{aprobarModal.tipoCombustibleNombre}</span>
                </div>
                <div>
                  <span className="text-acero block">Cantidad Solicitada:</span>
                  <span className="font-bold font-mono text-tanque">{aprobarModal.cantidadSolicitada} galones</span>
                </div>
              </div>
            </div>

            {/* Quick Percentage Presets */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold uppercase tracking-wider text-acero">
                Opciones rápidas de autorización
              </label>
              <div className="grid grid-cols-3 gap-2">
                <button
                  type="button"
                  onClick={() => setCantidadAutorizada(String(aprobarModal.cantidadSolicitada))}
                  className={`py-1.5 px-3 rounded text-xs font-semibold border transition-all ${
                    Number(cantidadAutorizada) === Number(aprobarModal.cantidadSolicitada)
                      ? 'bg-tanque text-white border-tanque'
                      : 'bg-white text-tanque border-tanque/30 hover:bg-tanque/5'
                  }`}
                >
                  100% ({aprobarModal.cantidadSolicitada} gal)
                </button>
                <button
                  type="button"
                  onClick={() => setCantidadAutorizada(String(Number((aprobarModal.cantidadSolicitada * 0.75).toFixed(2))))}
                  className={`py-1.5 px-3 rounded text-xs font-semibold border transition-all ${
                    Number(cantidadAutorizada) === Number((aprobarModal.cantidadSolicitada * 0.75).toFixed(2))
                      ? 'bg-tanque text-white border-tanque'
                      : 'bg-white text-tanque border-tanque/30 hover:bg-tanque/5'
                  }`}
                >
                  75% ({(aprobarModal.cantidadSolicitada * 0.75).toFixed(2)} gal)
                </button>
                <button
                  type="button"
                  onClick={() => setCantidadAutorizada(String(Number((aprobarModal.cantidadSolicitada * 0.5).toFixed(2))))}
                  className={`py-1.5 px-3 rounded text-xs font-semibold border transition-all ${
                    Number(cantidadAutorizada) === Number((aprobarModal.cantidadSolicitada * 0.5).toFixed(2))
                      ? 'bg-tanque text-white border-tanque'
                      : 'bg-white text-tanque border-tanque/30 hover:bg-tanque/5'
                  }`}
                >
                  50% ({(aprobarModal.cantidadSolicitada * 0.5).toFixed(2)} gal)
                </button>
              </div>
            </div>

            <Field
              label="Cantidad autorizada definitiva (galones)"
              required
              hint={`No puede ser mayor a los ${aprobarModal.cantidadSolicitada} galones solicitados`}
            >
              <input
                type="number"
                value={cantidadAutorizada}
                onChange={(e) => setCantidadAutorizada(e.target.value)}
                min="0.01"
                max={aprobarModal.cantidadSolicitada}
                step="0.01"
                required
                className={`${inputCls} font-mono text-base font-bold`}
              />
            </Field>

            <div className="rounded-md border border-advertencia/40 bg-advertencia/10 p-3 text-xs text-tinta">
              <span className="font-semibold block mb-0.5">Aviso de emisión:</span>
              Al aprobar esta solicitud, quedará disponible para emisión de ticket. Asegúrese de que el volumen asignado sea el correcto.
            </div>

            {actionError && <p className="text-sm text-peligro">{actionError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setAprobarModal(null)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={!cantidadAutorizada || Number(cantidadAutorizada) <= 0 || Number(cantidadAutorizada) > aprobarModal.cantidadSolicitada}
                className="rounded-md bg-exito px-5 py-2 text-sm font-semibold text-white hover:opacity-90 disabled:opacity-50"
              >
                Confirmar y aprobar
              </button>
            </div>
          </form>
        </Modal>
      )}

      {rechazarModal && (
        <Modal title="Rechazar solicitud" onClose={() => setRechazarModal(null)}>
          <form onSubmit={handleRechazar} className="space-y-4">
            <div className="rounded-md border border-peligro/20 bg-peligro/5 p-3 text-xs text-tinta space-y-1">
              <p><strong className="text-peligro">Solicitud #{rechazarModal.id}</strong></p>
              <p>Empleado: <span className="font-medium">{rechazarModal.empleadoNombre}</span> · Vehículo: <span className="font-mono">{rechazarModal.vehiculoPlaca}</span></p>
              <p>Volumen: <span className="font-medium">{rechazarModal.cantidadSolicitada} galones</span> de {rechazarModal.tipoCombustibleNombre}</p>
            </div>

            <Field label="Motivo de rechazo" required hint="Explique al solicitante por qué se deniega la autorización">
              <textarea
                value={motivoRechazo}
                onChange={(e) => setMotivoRechazo(e.target.value)}
                required
                maxLength={500}
                rows={3}
                className={inputCls}
                placeholder="Ej. Cuota mensual del departamento agotada, vehículo en mantenimiento..."
              />
            </Field>

            {actionError && <p className="text-sm text-peligro">{actionError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setRechazarModal(null)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={!motivoRechazo.trim()}
                className="rounded-md bg-peligro px-4 py-2 text-sm font-semibold text-white hover:opacity-90 disabled:opacity-50"
              >
                Confirmar rechazo
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
