import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import { useDepartamentos } from '../hooks/useDepartamentos'
import { useEmpleados } from '../hooks/useEmpleados'
import { useTiposCombustible } from '../hooks/useTiposCombustible'
import { useVehiculos } from '../hooks/useVehiculos'
import apiRequest from '../services/api'

const PERIODICIDAD_LABEL = {
  Diaria: 'Diaria',
  Semanal: 'Semanal',
  Mensual: 'Mensual',
}

const EMPTY_FORM = {
  empleadoId: '',
  vehiculoId: '',
  departamentoId: '',
  tipoCombustibleId: '',
  cantidadSolicitada: '',
  periodicidad: 'Mensual',
  fechaInicio: '',
  fechaFin: '',
}

function todayInputValue() {
  return new Date().toISOString().slice(0, 10)
}

function formatFecha(value) {
  return value ? new Date(value).toLocaleDateString() : '—'
}

export default function SolicitudesRecurrentesPage() {
  const empleados = useEmpleados()
  const vehiculos = useVehiculos()
  const departamentos = useDepartamentos()
  const tiposCombustible = useTiposCombustible()

  const [plantillas, setPlantillas] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [cambiandoEstadoId, setCambiandoEstadoId] = useState(null)
  const [actionError, setActionError] = useState(null)

  async function cargarPlantillas() {
    try {
      const data = await apiRequest('/solicitudes-recurrentes')
      setPlantillas(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/solicitudes-recurrentes')
      .then((data) => { if (!cancelado) setPlantillas(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  function handleFormChange(e) {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))
  }

  function openCreate() {
    setForm({ ...EMPTY_FORM, fechaInicio: todayInputValue() })
    setFormError(null)
    setShowCreate(true)
  }

  const requiredFieldsFilled =
    form.empleadoId &&
    form.vehiculoId &&
    form.departamentoId &&
    form.tipoCombustibleId &&
    form.cantidadSolicitada &&
    form.periodicidad &&
    form.fechaInicio

  async function handleCreate(e) {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)
    try {
      await apiRequest('/solicitudes-recurrentes', {
        method: 'POST',
        body: JSON.stringify({
          empleadoId: Number(form.empleadoId),
          vehiculoId: Number(form.vehiculoId),
          departamentoId: Number(form.departamentoId),
          tipoCombustibleId: Number(form.tipoCombustibleId),
          cantidadSolicitada: Number(form.cantidadSolicitada),
          periodicidad: form.periodicidad,
          fechaInicio: form.fechaInicio,
          fechaFin: form.fechaFin || null,
        }),
      })
      setShowCreate(false)
      await cargarPlantillas()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleCambiarEstado(p) {
    const accion = p.activa ? 'desactivar' : 'activar'
    setActionError(null)
    setCambiandoEstadoId(p.id)
    try {
      await apiRequest(`/solicitudes-recurrentes/${p.id}/${accion}`, { method: 'POST' })
      await cargarPlantillas()
    } catch (e) {
      // El backend responde 409 YA_ACTIVA / YA_DESACTIVADA si el estado ya
      // coincidía (ej. dos pestañas cambiándolo a la vez) — mensaje tal cual.
      setActionError(e.message)
    } finally {
      setCambiandoEstadoId(null)
    }
  }

  return (
    <PageContainer title="Solicitudes Recurrentes">
      <p className="mb-4 text-sm text-acero">
        Plantillas que generan solicitudes de combustible automáticamente según una periodicidad (RF-11). Un proceso
        en segundo plano las procesa a medianoche UTC — crear una plantilla aquí no genera una solicitud de inmediato.
      </p>

      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          + Nueva plantilla
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-sm border border-acero/20">
          <table className="min-w-full divide-y divide-acero/20 text-sm">
            <thead className="bg-fondo">
              <tr>
                {['Empleado', 'Vehículo', 'Departamento', 'Tipo', 'Cantidad', 'Periodicidad', 'Inicio', 'Fin', 'Última ejecución', 'Estado', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {plantillas.length === 0 && (
                <tr>
                  <td colSpan={11} className="px-4 py-6 text-center text-acero/70">
                    Sin plantillas de solicitud recurrente registradas.
                  </td>
                </tr>
              )}
              {plantillas.map((p) => (
                <tr key={p.id} className="hover:bg-fondo">
                  <td className="px-4 py-3 font-medium text-tinta">{p.empleadoNombre}</td>
                  <td className="px-4 py-3 font-mono text-acero">{p.vehiculoPlaca}</td>
                  <td className="px-4 py-3 text-acero">{p.departamentoNombre}</td>
                  <td className="px-4 py-3 text-acero">{p.tipoCombustibleNombre}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{p.cantidadSolicitada}</td>
                  <td className="px-4 py-3 text-acero">{PERIODICIDAD_LABEL[p.periodicidad] ?? p.periodicidad}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{formatFecha(p.fechaInicio)}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{formatFecha(p.fechaFin)}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{formatFecha(p.ultimaEjecucion)}</td>
                  <td className="px-4 py-3">
                    <StatusBadge active={p.activa} activeText="Activa" inactiveText="Inactiva" />
                  </td>
                  <td className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => handleCambiarEstado(p)}
                      disabled={cambiandoEstadoId === p.id}
                      className={`rounded px-2 py-1 text-xs text-white hover:opacity-90 disabled:opacity-50 ${p.activa ? 'bg-peligro' : 'bg-exito'}`}
                    >
                      {cambiandoEstadoId === p.id ? 'Guardando...' : p.activa ? 'Desactivar' : 'Activar'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showCreate && (
        <Modal title="Nueva plantilla recurrente" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate} className="space-y-4">
            <Field label="Empleado">
              <select name="empleadoId" value={form.empleadoId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {empleados.map((e) => (
                  <option key={e.id} value={e.id}>{e.nombreCompleto}</option>
                ))}
              </select>
            </Field>
            <Field label="Vehículo">
              <select name="vehiculoId" value={form.vehiculoId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {vehiculos.map((v) => (
                  <option key={v.id} value={v.id}>{v.placa} — {v.marca} {v.modelo}</option>
                ))}
              </select>
            </Field>
            <Field label="Departamento">
              <select name="departamentoId" value={form.departamentoId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {departamentos.map((d) => (
                  <option key={d.id} value={d.id}>{d.nombre}</option>
                ))}
              </select>
            </Field>
            <Field label="Tipo de combustible">
              <select name="tipoCombustibleId" value={form.tipoCombustibleId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {tiposCombustible.map((t) => (
                  <option key={t.id} value={t.id}>{t.nombre}</option>
                ))}
              </select>
            </Field>
            <Field label="Cantidad solicitada (por ejecución)">
              <input
                type="number"
                name="cantidadSolicitada"
                value={form.cantidadSolicitada}
                onChange={handleFormChange}
                min="0.0001"
                step="0.0001"
                required
                className={inputCls}
              />
            </Field>
            <Field label="Periodicidad">
              <select name="periodicidad" value={form.periodicidad} onChange={handleFormChange} required className={inputCls}>
                {Object.keys(PERIODICIDAD_LABEL).map((p) => (
                  <option key={p} value={p}>{PERIODICIDAD_LABEL[p]}</option>
                ))}
              </select>
            </Field>
            <Field label="Fecha de inicio">
              <input
                type="date"
                name="fechaInicio"
                value={form.fechaInicio}
                onChange={handleFormChange}
                required
                className={inputCls}
              />
            </Field>
            <Field label="Fecha de fin (opcional)">
              <input
                type="date"
                name="fechaFin"
                value={form.fechaFin}
                onChange={handleFormChange}
                min={form.fechaInicio || undefined}
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
                className="rounded-md bg-tanque px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
              >
                {submitting ? 'Guardando...' : 'Crear plantilla'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
