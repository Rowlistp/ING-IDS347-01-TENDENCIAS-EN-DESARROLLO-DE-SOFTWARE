import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import { formatDate } from '../utils/dates'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
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
  return formatDate(value)
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
    const { name, value } = e.target
    setForm((f) => {
      if (name === 'empleadoId') {
        const empleado = empleados.find((item) => item.id === Number(value))
        return { ...f, empleadoId: value, departamentoId: String(empleado?.departamentoId || ''), vehiculoId: '' }
      }
      return { ...f, [name]: value }
    })
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

      <div className="mb-4 flex flex-col sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="inline-flex min-h-[44px] w-full sm:w-auto items-center justify-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-semibold text-white hover:bg-tanque/90 shadow-sm transition-colors"
        >
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
          </svg>
          Nueva plantilla
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <ResponsiveTable
          data={plantillas}
          keyField="id"
          columns={[
            {
              key: 'empleadoNombre',
              label: 'Empleado',
              primary: true,
              priority: 'high',
              render: (p) => <span className="font-semibold text-tinta">{p.empleadoNombre}</span>,
            },
            {
              key: 'vehiculoPlaca',
              label: 'Vehículo',
              priority: 'high',
              render: (p) => <span className="font-mono text-acero">{p.vehiculoPlaca}</span>,
            },
            {
              key: 'departamentoNombre',
              label: 'Departamento',
              priority: 'med',
            },
            {
              key: 'tipoCombustibleNombre',
              label: 'Tipo',
              priority: 'med',
            },
            {
              key: 'cantidadSolicitada',
              label: 'Cantidad',
              priority: 'high',
              render: (p) => <span className="font-mono num font-bold text-tinta">{p.cantidadSolicitada} gal</span>,
            },
            {
              key: 'periodicidad',
              label: 'Periodicidad',
              priority: 'high',
              render: (p) => PERIODICIDAD_LABEL[p.periodicidad] ?? p.periodicidad,
            },
            {
              key: 'fechaInicio',
              label: 'Inicio',
              priority: 'med',
              render: (p) => <span className="font-mono num">{formatFecha(p.fechaInicio)}</span>,
            },
            {
              key: 'fechaFin',
              label: 'Fin',
              priority: 'low',
              render: (p) => <span className="font-mono num">{formatFecha(p.fechaFin)}</span>,
            },
            {
              key: 'ultimaEjecucion',
              label: 'Última ejecución',
              priority: 'low',
              render: (p) => <span className="font-mono num">{formatFecha(p.ultimaEjecucion)}</span>,
            },
            {
              key: 'activo',
              label: 'Estado',
              priority: 'high',
              render: (p) => <StatusBadge active={p.activa} activeText="Activa" inactiveText="Inactiva" />,
            },
          ]}
          actions={(p) => (
            <button
              type="button"
              onClick={() => handleCambiarEstado(p)}
              disabled={cambiandoEstadoId === p.id}
              className={`inline-flex min-h-[38px] items-center gap-1.5 rounded-md border px-3 py-1.5 text-xs font-semibold transition-colors disabled:opacity-50 shadow-xs ${
                p.activa
                  ? 'border-peligro/30 bg-peligro/10 text-peligro hover:bg-peligro hover:text-white'
                  : 'border-exito/30 bg-exito/10 text-exito hover:bg-exito hover:text-white'
              }`}
            >
              {p.activa ? (
                <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                </svg>
              ) : (
                <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
                </svg>
              )}
              {cambiandoEstadoId === p.id ? 'Guardando...' : p.activa ? 'Desactivar' : 'Activar'}
            </button>
          )}
          emptyMessage="Sin plantillas de solicitud recurrente registradas."
        />
      )}


      {showCreate && (
        <Modal title="Nueva plantilla recurrente" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate} className="space-y-4">
            <Field label="Empleado">
              <select name="empleadoId" value={form.empleadoId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {empleados.filter((e) => e.activo).map((e) => (
                  <option key={e.id} value={e.id}>{e.nombreCompleto}</option>
                ))}
              </select>
            </Field>
            <Field label="Vehículo">
              <select name="vehiculoId" value={form.vehiculoId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {vehiculos.filter((v) => v.activo && (!form.departamentoId || v.departamentoId === Number(form.departamentoId))).map((v) => (
                  <option key={v.id} value={v.id}>{v.placa} — {v.marca} {v.modelo}</option>
                ))}
              </select>
            </Field>
            <Field label="Departamento" hint="Se asigna automáticamente según el empleado">
              <select name="departamentoId" value={form.departamentoId} disabled required className={inputCls}>
                <option value="">Seleccionar...</option>
                {departamentos.filter((d) => d.activo).map((d) => (
                  <option key={d.id} value={d.id}>{d.nombre}</option>
                ))}
              </select>
            </Field>
            <Field label="Tipo de combustible">
              <select name="tipoCombustibleId" value={form.tipoCombustibleId} onChange={handleFormChange} required className={inputCls}>
                <option value="">Seleccionar...</option>
                {tiposCombustible.filter((t) => t.activo).map((t) => (
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
            <div className="flex flex-col-reverse sm:flex-row justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setShowCreate(false)}
                className="flex min-h-[44px] items-center justify-center rounded-md border border-acero/30 px-4 py-2 text-sm font-medium text-tinta hover:bg-fondo transition-colors"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={submitting || !requiredFieldsFilled}
                className="flex min-h-[44px] items-center justify-center rounded-md bg-tanque px-4 py-2 text-sm font-semibold text-white hover:bg-tanque/90 disabled:opacity-50 transition-colors shadow-sm"
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
