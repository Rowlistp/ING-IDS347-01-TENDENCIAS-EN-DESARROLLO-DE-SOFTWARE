import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
import StatusBadge from '../components/StatusBadge'
import { useAuth } from '../hooks/useAuth'
import { useDepartamentos } from '../hooks/useDepartamentos'
import { useEmpleados } from '../hooks/useEmpleados'
import { useVehiculos } from '../hooks/useVehiculos'
import apiRequest from '../services/api'
import { canApproveRequests, isReadOnlyRole, ROLES } from '../utils/rbac'

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
  diasVigencia: '7',
}

export default function SolicitudesPage() {
  const { user } = useAuth()
  const canApprove = canApproveRequests(user)
  const isReadOnly = isReadOnlyRole(user)
  const [solicitudes, setSolicitudes] = useState([])
  const empleados = useEmpleados()
  const vehiculos = useVehiculos()
  const departamentos = useDepartamentos()
  const [tiposCombustible, setTipos] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const isSolicitanteOnly =
    user?.roles?.includes(ROLES.SOLICITANTE) &&
    !user?.roles?.includes(ROLES.ADMINISTRADOR) &&
    !user?.roles?.includes(ROLES.SUPERVISOR)
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

  // Vehículos que se pueden elegir: los activos del departamento del empleado,
  // más su vehículo habitual si el administrador le asignó uno de otro departamento.
  function vehiculosElegibles(empleado, departamentoId) {
    return vehiculos.filter(
      (v) => v.activo && (v.departamentoId === Number(departamentoId) || (empleado && v.id === empleado.vehiculoHabitualId)),
    )
  }

  function combustibleDe(vehiculo) {
    const usable = vehiculo?.tipoCombustibleId && tiposCombustible.some((t) => t.id === vehiculo.tipoCombustibleId && t.activo)
    return usable ? String(vehiculo.tipoCombustibleId) : ''
  }

  // Valores por defecto al elegir empleado: su vehículo habitual; si no tiene,
  // el único vehículo activo del departamento. El combustible sale del vehículo.
  function propuestaPara(empleado) {
    if (!empleado) return { departamentoId: '', vehiculoId: '', tipoCombustibleId: '' }
    const elegibles = vehiculosElegibles(empleado, empleado.departamentoId)
    const vehiculo = elegibles.find((v) => v.id === empleado.vehiculoHabitualId) ?? (elegibles.length === 1 ? elegibles[0] : null)
    return {
      departamentoId: String(empleado.departamentoId),
      vehiculoId: vehiculo ? String(vehiculo.id) : '',
      tipoCombustibleId: combustibleDe(vehiculo),
    }
  }

  function handleFormChange(e) {
    const { name, value } = e.target
    if (name === 'empleadoId') {
      const empleado = empleados.find((item) => item.id === Number(value))
      setForm((f) => ({ ...f, empleadoId: value, ...propuestaPara(empleado) }))
    } else if (name === 'vehiculoId') {
      const vehiculo = vehiculos.find((v) => v.id === Number(value))
      setForm((f) => ({ ...f, vehiculoId: value, tipoCombustibleId: combustibleDe(vehiculo) || f.tipoCombustibleId }))
    } else {
      setForm((f) => ({ ...f, [name]: value }))
    }
  }

  function openCreate() {
    setForm({
      ...EMPTY_FORM,
      empleadoId: miEmpleado ? String(miEmpleado.id) : '',
      ...(miEmpleado ? propuestaPara(miEmpleado) : {}),
    })
    setFormError(null)
    setShowCreate(true)
  }

  const empleadoDelForm = isSolicitanteOnly ? miEmpleado : empleados.find((e) => String(e.id) === form.empleadoId)
  const vehiculoSeleccionado = vehiculos.find((v) => String(v.id) === form.vehiculoId)
  const avisoCombustible = !vehiculoSeleccionado
    ? undefined
    : !vehiculoSeleccionado.tipoCombustibleId
      ? 'Este vehículo aún no tiene combustible definido; pide que se lo asignen en Vehículos.'
      : String(vehiculoSeleccionado.tipoCombustibleId) === form.tipoCombustibleId
        ? `Combustible del vehículo: ${vehiculoSeleccionado.tipoCombustibleNombre}.`
        : `⚠️ Este vehículo usa ${vehiculoSeleccionado.tipoCombustibleNombre}; verifica el combustible antes de enviar.`

  const requiredFieldsFilled =
    (isSolicitanteOnly ? Boolean(miEmpleado) : Boolean(form.empleadoId)) &&
    form.vehiculoId &&
    (isSolicitanteOnly ? Boolean(miEmpleado) : Boolean(form.departamentoId)) &&
    form.tipoCombustibleId &&
    form.cantidadSolicitada &&
    form.diasVigencia


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
          fechaVencimiento: new Date(
  Date.now() + Number(form.diasVigencia || 7) * 24 * 60 * 60 * 1000
).toISOString(),
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
      {!isReadOnly && (
        <div className="mb-4 flex flex-col sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={openCreate}
            className="inline-flex min-h-[44px] w-full sm:w-auto items-center justify-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-semibold text-white hover:bg-tanque/90 shadow-sm transition-colors"
          >
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
            </svg>
            Nueva solicitud
          </button>
        </div>
      )}

      {isSolicitanteOnly && !miEmpleado && (
        <div className="mb-4 rounded-md border border-peligro/30 bg-peligro/10 p-3 text-sm text-peligro">
          ⚠️ <strong>Cuenta no vinculada:</strong> Tu cuenta de usuario aún no está asociada a ningún empleado de la institución. Por favor solicita a un Administrador que complete tu vinculación.
        </div>
      )}

      {isSolicitanteOnly && miEmpleado && (
        <div className="mb-4 rounded-md border border-tanque/20 bg-tanque/5 p-3 text-sm text-tanque flex flex-col sm:flex-row sm:items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <span>👤</span>
            <span>
              Solicitante: <strong>{miEmpleado.nombreCompleto}</strong> — {miEmpleado.departamentoNombre}
            </span>
          </div>
          <span className="text-xs bg-tanque/10 text-tanque border border-tanque/20 px-2.5 py-1 rounded font-medium self-start sm:self-auto">
            Mis solicitudes
          </span>
        </div>
      )}

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <ResponsiveTable
          data={solicitudes}
          keyField="id"
          columns={[
            {
              key: 'id',
              label: '#',
              priority: 'low',
              render: (s) => <span className="font-mono num text-acero">{s.id}</span>,
            },
            {
              key: 'empleadoNombre',
              label: 'Empleado',
              primary: true,
              priority: 'high',
              render: (s) => <span className="font-semibold text-tinta">{s.empleadoNombre}</span>,
            },
            {
              key: 'vehiculoPlaca',
              label: 'Vehículo',
              priority: 'high',
              render: (s) => <span className="font-mono text-acero">{s.vehiculoPlaca}</span>,
            },
            {
              key: 'departamentoNombre',
              label: 'Departamento',
              priority: 'low',
            },
            {
              key: 'tipoCombustibleNombre',
              label: 'Tipo',
              priority: 'med',
            },
            {
              key: 'cantidadSolicitada',
              label: 'Solicitado',
              priority: 'high',
              render: (s) => <span className="font-mono num font-bold text-tinta">{s.cantidadSolicitada} gal</span>,
            },
            {
              key: 'cantidadAutorizada',
              label: 'Autorizado',
              priority: 'med',
              render: (s) => (
                <span className="font-mono num text-acero">
                  {s.cantidadAutorizada ? `${s.cantidadAutorizada} gal` : '—'}
                </span>
              ),
            },
            {
              key: 'estado',
              label: 'Estado',
              priority: 'high',
              render: (s) => <StatusBadge label={s.estado} variant={ESTADO_VARIANT[s.estado]} />,
            },
            {
              key: 'fechaSolicitud',
              label: 'Fecha',
              priority: 'med',
              render: (s) => <span className="font-mono num text-xs">{new Date(s.fechaSolicitud).toLocaleDateString()}</span>,
            },
            {
              key: 'fechaVencimiento',
              label: 'Vence',
              priority: 'low',
              render: (s) => (
                <span className="font-mono num text-xs">
                  {s.fechaVencimiento ? new Date(s.fechaVencimiento).toLocaleDateString() : '—'}
                </span>
              ),
            },
          ]}
          actions={(s) => (
            <>
              {s.estado === 'Pendiente' && canApprove && (
                <div className="flex flex-wrap items-center gap-1.5">
                  <button
                    type="button"
                    onClick={() => {
                      setAprobarModal(s)
                      setCantidadAutorizada(String(s.cantidadSolicitada))
                      setActionError(null)
                    }}
                    className="inline-flex min-h-[38px] items-center gap-1.5 rounded-md border border-exito/30 bg-exito/10 px-3 py-1.5 text-xs font-semibold text-exito transition-colors hover:bg-exito hover:text-white shadow-xs"
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
                    className="inline-flex min-h-[38px] items-center gap-1.5 rounded-md border border-peligro/30 bg-white px-3 py-1.5 text-xs font-semibold text-peligro transition-colors hover:border-peligro hover:bg-peligro/10 shadow-xs"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                      <line x1="18" y1="6" x2="6" y2="18" />
                      <line x1="6" y1="6" x2="18" y2="18" />
                    </svg>
                    Rechazar
                  </button>
                </div>
              )}
              {s.estado === 'Pendiente' && !canApprove && (
                <span className="text-xs text-acero italic">En espera de autorización</span>
              )}
              {s.estado === 'Rechazada' && s.motivoRechazo && (
                <span className="text-xs text-acero/80" title={s.motivoRechazo}>
                  Motivo: {s.motivoRechazo}
                </span>
              )}
            </>
          )}
          emptyMessage="Sin solicitudes registradas."
        />
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
                    {empleados.filter((e) => e.activo).map((e) => (
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
                {vehiculosElegibles(empleadoDelForm, form.departamentoId).map((v) => (
                  <option key={v.id} value={v.id}>
                    {v.placa} — {v.marca} {v.modelo} ({v.ficha})
                    {v.tipoCombustibleNombre ? ` · ${v.tipoCombustibleNombre}` : ''}
                    {empleadoDelForm && v.id === empleadoDelForm.vehiculoHabitualId ? ' · habitual' : ''}
                  </option>
                ))}
              </select>
            </Field>

            <Field
              label="Departamento"
              required
              hint={form.vehiculoId ? "Empleado, vehículo y departamento deben coincidir" : undefined}
            >
              <select
                name="departamentoId"
                disabled
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

            <Field label="Tipo de combustible" required hint={avisoCombustible}>
              <select
                name="tipoCombustibleId"
                value={form.tipoCombustibleId}
                onChange={handleFormChange}
                required
                className={inputCls}
              >
                <option value="">Seleccionar...</option>
                {tiposCombustible.filter((t) => t.activo).map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.nombre}
                  </option>
                ))}
              </select>
            </Field>

            <Field label="Cantidad solicitada (galones)" required hint="Indica el volumen requerido en galones">
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

            <Field label="Días de vigencia" required>
              <input
                type="number"
                name="diasVigencia"
                value={form.diasVigencia}
                onChange={handleFormChange}
                min="1"
                max="365"
                required
                placeholder="7"
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
