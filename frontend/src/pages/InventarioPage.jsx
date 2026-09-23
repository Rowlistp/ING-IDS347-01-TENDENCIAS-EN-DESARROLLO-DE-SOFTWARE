import { useCallback, useEffect, useRef, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
import StatusBadge from '../components/StatusBadge'
import { useAuth } from '../hooks/useAuth'
import apiRequest from '../services/api'
import { useTanques } from '../hooks/useTanques'
import { canAdjustInventory } from '../utils/rbac'

const TIPO_LABEL = {
  Entrada: 'Entrada',
  Salida: 'Salida',
  Ajuste: 'Ajuste',
  Transferencia: 'Transferencia',
  Merma: 'Merma',
}

const TIPO_VARIANT = {
  Entrada: 'green',
  Salida: 'gray',
  Ajuste: 'blue',
  Transferencia: 'purple',
  Merma: 'red',
}

const EMPTY_AJUSTE = { tanqueId: '', tipoAjuste: 'positivo', magnitud: '', observaciones: '' }
const EMPTY_TRANSFERENCIA = { tanqueOrigenId: '', tanqueDestinoId: '', volumen: '', observaciones: '' }

function formatFecha(value) {
  return value ? new Date(value).toLocaleString() : '—'
}

export default function InventarioPage() {
  const { user } = useAuth()
  const canAdjust = canAdjustInventory(user)
  const tanques = useTanques()
  const [inventarios, setInventarios] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [tab, setTab] = useState('existencia')

  const [movimientos, setMovimientos] = useState([])
  const [movLoading, setMovLoading] = useState(false)
  const [movError, setMovError] = useState(null)
  const [filtroTanque, setFiltroTanque] = useState('')

  const [showAjuste, setShowAjuste] = useState(false)
  const [ajusteForm, setAjusteForm] = useState(EMPTY_AJUSTE)
  const [ajusteSubmitting, setAjusteSubmitting] = useState(false)
  const [ajusteError, setAjusteError] = useState(null)

  const [showTransferencia, setShowTransferencia] = useState(false)
  const [transferenciaForm, setTransferenciaForm] = useState(EMPTY_TRANSFERENCIA)
  const [transferenciaSubmitting, setTransferenciaSubmitting] = useState(false)
  const [transferenciaError, setTransferenciaError] = useState(null)

  const inventoryRequest = useRef(0)
  const cargarInventarios = useCallback(async () => {
    const requestId = ++inventoryRequest.current
    try {
      const data = await apiRequest('/inventario')
      if (requestId !== inventoryRequest.current) return
      setInventarios(data)
      setError(null)
    } catch (e) {
      if (requestId === inventoryRequest.current) setError(e.message)
    } finally {
      if (requestId === inventoryRequest.current) setLoading(false)
    }
  }, [])

  async function cargarMovimientos() {
    setMovLoading(true)
    setMovError(null)
    try {
      const qs = filtroTanque ? `?tanqueId=${filtroTanque}` : ''
      const data = await apiRequest(`/inventario/movimientos${qs}`)
      setMovimientos(data)
    } catch (e) {
      setMovError(e.message)
    } finally {
      setMovLoading(false)
    }
  }

  useEffect(() => {
    cargarInventarios()
    const refresh = () => { if (document.visibilityState === 'visible') cargarInventarios() }
    const requests = inventoryRequest
    const timer = window.setInterval(refresh, 5000)
    window.addEventListener('focus', refresh)
    document.addEventListener('visibilitychange', refresh)
    return () => {
      ++requests.current
      window.clearInterval(timer)
      window.removeEventListener('focus', refresh)
      document.removeEventListener('visibilitychange', refresh)
    }
  }, [cargarInventarios])

  useEffect(() => {
    if (tab === 'historial') cargarMovimientos()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, filtroTanque])

  const tanqueById = Object.fromEntries(tanques.map((t) => [t.id, t]))

  const filas = inventarios.map((inv) => ({
    ...inv,
    tanque: tanqueById[inv.tanqueId],
  }))

  function openAjuste() {
    setAjusteForm(EMPTY_AJUSTE)
    setAjusteError(null)
    setShowAjuste(true)
  }

  function handleAjusteChange(e) {
    const { name, value } = e.target
    setAjusteForm((f) => ({ ...f, [name]: value }))
  }

  async function handleAjusteSubmit(e) {
    e.preventDefault()
    setAjusteSubmitting(true)
    setAjusteError(null)
    const magnitud = Number(ajusteForm.magnitud)
    const volumen = ajusteForm.tipoAjuste === 'negativo' ? -magnitud : magnitud
    try {
      await apiRequest('/inventario/ajustes', {
        method: 'POST',
        body: JSON.stringify({
          tanqueId: Number(ajusteForm.tanqueId),
          volumen,
          observaciones: ajusteForm.observaciones.trim(),
        }),
      })
      setShowAjuste(false)
      await cargarInventarios()
      if (tab === 'historial') await cargarMovimientos()
    } catch (e) {
      setAjusteError(e.message)
    } finally {
      setAjusteSubmitting(false)
    }
  }

  function openTransferencia() {
    setTransferenciaForm(EMPTY_TRANSFERENCIA)
    setTransferenciaError(null)
    setShowTransferencia(true)
  }

  function handleTransferenciaChange(e) {
    const { name, value } = e.target
    setTransferenciaForm((f) => ({ ...f, [name]: value }))
  }

  async function handleTransferenciaSubmit(e) {
    e.preventDefault()
    setTransferenciaSubmitting(true)
    setTransferenciaError(null)
    try {
      await apiRequest('/inventario/transferencias', {
        method: 'POST',
        body: JSON.stringify({
          tanqueOrigenId: Number(transferenciaForm.tanqueOrigenId),
          tanqueDestinoId: Number(transferenciaForm.tanqueDestinoId),
          volumen: Number(transferenciaForm.volumen),
          observaciones: transferenciaForm.observaciones.trim() || undefined,
        }),
      })
      setShowTransferencia(false)
      await cargarInventarios()
      if (tab === 'historial') await cargarMovimientos()
    } catch (e) {
      setTransferenciaError(e.message)
    } finally {
      setTransferenciaSubmitting(false)
    }
  }

  return (
    <PageContainer title="Inventario">
      <div className="mb-4 flex flex-col md:flex-row md:items-center justify-between gap-3">
        <div className="flex gap-1 rounded-md bg-acero/10 p-1 self-start">
          <button
            type="button"
            onClick={() => setTab('existencia')}
            className={`min-h-[38px] rounded px-3 py-1.5 text-sm font-medium transition-colors ${tab === 'existencia' ? 'bg-white shadow-xs text-tinta' : 'text-acero hover:text-tinta'}`}
          >
            Existencia actual
          </button>
          <button
            type="button"
            onClick={() => setTab('historial')}
            className={`min-h-[38px] rounded px-3 py-1.5 text-sm font-medium transition-colors ${tab === 'historial' ? 'bg-white shadow-xs text-tinta' : 'text-acero hover:text-tinta'}`}
          >
            Historial de movimientos
          </button>
          <button type="button" onClick={cargarInventarios} className="min-h-[38px] rounded px-3 py-1.5 text-sm text-tanque hover:bg-white">Actualizar</button>
        </div>

        {canAdjust && (
          <div className="flex flex-col sm:flex-row gap-2 w-full sm:w-auto">
            <button
              type="button"
              onClick={openAjuste}
              className="flex min-h-[44px] items-center justify-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-tanque/90 active:scale-[0.98]"
            >
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                <line x1="12" y1="5" x2="12" y2="19" />
                <line x1="5" y1="12" x2="19" y2="12" />
              </svg>
              Registrar ajuste
            </button>
            <button
              type="button"
              onClick={openTransferencia}
              className="flex min-h-[44px] items-center justify-center gap-2 rounded-md bg-info px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-info/90 active:scale-[0.98]"
            >
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                <path d="m16 3 4 4-4 4" />
                <path d="M20 7H4" />
                <path d="m8 21-4-4 4-4" />
                <path d="M4 17h16" />
              </svg>
              Transferir tanques
            </button>
          </div>
        )}
      </div>

      {tab === 'existencia' && (
        <>
          {loading && <p className="text-sm text-acero">Cargando...</p>}
          {error && <p className="text-sm text-peligro">{error}</p>}

          {!loading && !error && (
            <ResponsiveTable
              data={filas}
              keyField="id"
              emptyMessage="Sin tanques registrados."
              columns={[
                {
                  key: 'tanqueIdentificacion',
                  label: 'Tanque',
                  primary: true,
                  priority: 'high',
                  render: (f) => <span className="font-semibold font-mono text-tinta">{f.tanqueIdentificacion}</span>,
                },
                {
                  key: 'tipoCombustibleNombre',
                  label: 'Tipo combustible',
                  priority: 'high',
                  render: (f) => <span className="text-acero">{f.tanque?.tipoCombustibleNombre ?? '—'}</span>,
                },
                {
                  key: 'existenciaActual',
                  label: 'Existencia actual',
                  priority: 'high',
                  render: (f) => <span className="font-mono num font-semibold text-tinta">{f.existenciaActual.toFixed(2)} gal</span>,
                },
                {
                  key: 'disponibilidad',
                  label: 'Disponibilidad',
                  priority: 'med',
                  render: (f) => <span className="font-mono num text-acero">{f.disponibilidad.toFixed(2)} gal</span>,
                },
                {
                  key: 'nivelCritico',
                  label: 'Nivel crítico',
                  priority: 'low',
                  render: (f) => <span className="font-mono num text-acero">{f.tanque ? `${f.tanque.nivelCritico.toFixed(2)} gal` : '—'}</span>,
                },
                {
                  key: 'estado',
                  label: 'Estado',
                  priority: 'high',
                  render: (f) => {
                    const critico = f.tanque && f.existenciaActual <= f.tanque.nivelCritico
                    const inactivo = f.tanque && !f.tanque.activo
                    return inactivo
                      ? <StatusBadge label="Inactivo" variant="gray" />
                      : critico
                        ? <StatusBadge label="Nivel bajo" variant="red" />
                        : <StatusBadge label="Normal" variant="green" />
                  },
                },
              ]}
            />
          )}
        </>
      )}

      {tab === 'historial' && (
        <>
          <div className="mb-4">
            <select
              value={filtroTanque}
              onChange={(e) => setFiltroTanque(e.target.value)}
              className={`${inputCls} w-full sm:max-w-xs min-h-[42px]`}
            >
              <option value="">Todos los tanques</option>
              {tanques.map((t) => (
                <option key={t.id} value={t.id}>{t.identificacion}</option>
              ))}
            </select>
          </div>

          {movLoading && <p className="text-sm text-acero">Cargando...</p>}
          {movError && <p className="text-sm text-peligro">{movError}</p>}

          {!movLoading && !movError && (
            <ResponsiveTable
              data={movimientos}
              keyField="id"
              emptyMessage="Sin movimientos registrados."
              columns={[
                {
                  key: 'fechaHora',
                  label: 'Fecha / Hora',
                  priority: 'high',
                  render: (m) => <span className="text-acero text-xs sm:text-sm">{formatFecha(m.fechaHora)}</span>,
                },
                {
                  key: 'tipo',
                  label: 'Tipo',
                  priority: 'high',
                  render: (m) => <StatusBadge label={TIPO_LABEL[m.tipo] ?? m.tipo} variant={TIPO_VARIANT[m.tipo]} />,
                },
                {
                  key: 'volumen',
                  label: 'Volumen',
                  primary: true,
                  priority: 'high',
                  render: (m) => (
                    <span className={`font-semibold font-mono num ${m.volumen < 0 ? 'text-peligro' : 'text-tinta'}`}>
                      {m.volumen > 0 ? '+' : ''}{m.volumen.toFixed(2)} gal
                    </span>
                  ),
                },
                {
                  key: 'tanqueIdentificacion',
                  label: 'Tanque',
                  priority: 'med',
                  render: (m) => <span className="font-mono text-acero">{m.tanqueIdentificacion}</span>,
                },
                {
                  key: 'usuarioNombreUsuario',
                  label: 'Usuario',
                  priority: 'low',
                  render: (m) => <span className="text-acero">{m.usuarioNombreUsuario}</span>,
                },
                {
                  key: 'referencia',
                  label: 'Referencia / Obs.',
                  priority: 'low',
                  render: (m) => <span className="text-acero">{m.referenciaOperacion || m.observaciones || '—'}</span>,
                },
              ]}
            />
          )}
        </>
      )}

      {showAjuste && (
        <Modal title="Registrar ajuste de inventario" onClose={() => setShowAjuste(false)}>
          <form onSubmit={handleAjusteSubmit} className="space-y-4">
            <Field label="Tanque">
              <select
                name="tanqueId"
                value={ajusteForm.tanqueId}
                onChange={handleAjusteChange}
                required
                className={inputCls}
              >
                <option value="">Seleccione un tanque</option>
                {tanques.filter((t) => t.activo && t.tipoCombustibleActivo !== false).map((t) => (
                  <option key={t.id} value={t.id}>{t.identificacion} ({t.tipoCombustibleNombre})</option>
                ))}
              </select>
            </Field>

            <Field label="Tipo de ajuste">
              <select
                name="tipoAjuste"
                value={ajusteForm.tipoAjuste}
                onChange={handleAjusteChange}
                className={inputCls}
              >
                <option value="positivo">Positivo (agregar combustible)</option>
                <option value="negativo">Negativo (restar combustible)</option>
              </select>
            </Field>

            <Field label="Cantidad (galones)">
              <input
                type="number"
                name="magnitud"
                value={ajusteForm.magnitud}
                onChange={handleAjusteChange}
                required
                min="0"
                step="0.0001"
                className={inputCls}
              />
            </Field>

            <Field label="Motivo / justificación">
              <textarea
                name="observaciones"
                value={ajusteForm.observaciones}
                onChange={handleAjusteChange}
                required
                maxLength={500}
                rows={3}
                className={inputCls}
              />
            </Field>

            {ajusteError && <p className="text-sm text-peligro">{ajusteError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setShowAjuste(false)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={ajusteSubmitting || !ajusteForm.tanqueId || !ajusteForm.magnitud || !ajusteForm.observaciones.trim()}
                className="rounded-md bg-tanque px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
              >
                {ajusteSubmitting ? 'Guardando...' : 'Registrar ajuste'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {showTransferencia && (
        <Modal title="Transferir entre tanques" onClose={() => setShowTransferencia(false)}>
          <form onSubmit={handleTransferenciaSubmit} className="space-y-4">
            <Field label="Tanque origen">
              <select
                name="tanqueOrigenId"
                value={transferenciaForm.tanqueOrigenId}
                onChange={handleTransferenciaChange}
                required
                className={inputCls}
              >
                <option value="">Seleccione un tanque</option>
                {tanques.filter((t) => t.activo && t.tipoCombustibleActivo !== false).map((t) => (
                  <option key={t.id} value={t.id}>{t.identificacion} ({t.tipoCombustibleNombre})</option>
                ))}
              </select>
            </Field>

            <Field label="Tanque destino">
              <select
                name="tanqueDestinoId"
                value={transferenciaForm.tanqueDestinoId}
                onChange={handleTransferenciaChange}
                required
                className={inputCls}
              >
                <option value="">Seleccione un tanque</option>
                {tanques.filter((t) => t.activo && t.tipoCombustibleActivo !== false).map((t) => (
                  <option key={t.id} value={t.id}>{t.identificacion} ({t.tipoCombustibleNombre})</option>
                ))}
              </select>
            </Field>

            <Field label="Volumen a transferir (galones)">
              <input
                type="number"
                name="volumen"
                value={transferenciaForm.volumen}
                onChange={handleTransferenciaChange}
                required
                min="0.0001"
                step="0.0001"
                className={inputCls}
              />
            </Field>

            <Field label="Observaciones (opcional)">
              <textarea
                name="observaciones"
                value={transferenciaForm.observaciones}
                onChange={handleTransferenciaChange}
                maxLength={500}
                rows={3}
                className={inputCls}
              />
            </Field>

            {transferenciaError && <p className="text-sm text-peligro">{transferenciaError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setShowTransferencia(false)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={
                  transferenciaSubmitting ||
                  !transferenciaForm.tanqueOrigenId ||
                  !transferenciaForm.tanqueDestinoId ||
                  !transferenciaForm.volumen
                }
                className="rounded-md bg-info px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
              >
                {transferenciaSubmitting ? 'Transfiriendo...' : 'Transferir'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
