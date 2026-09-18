import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'
import { getUser } from '../services/auth'
import { useTanques } from '../hooks/useTanques'

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
  // POST /inventario/ajustes y /inventario/transferencias solo permiten
  // Administrador/Supervisor (InventarioController.cs) — el resto de roles
  // con acceso a esta pantalla (Despachador, Auditor, Consulta, Solicitante)
  // solo consultan existencia e historial.
  const puedeAjustar = getUser()?.roles?.some((r) => ['Administrador', 'Supervisor'].includes(r)) ?? false
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

  async function cargarInventarios() {
    try {
      const data = await apiRequest('/inventario')
      setInventarios(data)
      setError(null)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

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
  }, [])

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
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div className="flex gap-1 rounded-md bg-acero/10 p-1">
          <button
            type="button"
            onClick={() => setTab('existencia')}
            className={`rounded px-3 py-1.5 text-sm font-medium ${tab === 'existencia' ? 'bg-white shadow text-tinta' : 'text-acero'}`}
          >
            Existencia actual
          </button>
          <button
            type="button"
            onClick={() => setTab('historial')}
            className={`rounded px-3 py-1.5 text-sm font-medium ${tab === 'historial' ? 'bg-white shadow text-tinta' : 'text-acero'}`}
          >
            Historial de movimientos
          </button>
        </div>

        {puedeAjustar && (
          <div className="flex gap-2">
            <button
              type="button"
              onClick={openAjuste}
              className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
            >
              + Registrar ajuste
            </button>
            <button
              type="button"
              onClick={openTransferencia}
              className="rounded-md bg-info px-4 py-2 text-sm font-medium text-white hover:opacity-90"
            >
              + Transferir entre tanques
            </button>
          </div>
        )}
      </div>

      {tab === 'existencia' && (
        <>
          {loading && <p className="text-sm text-acero">Cargando...</p>}
          {error && <p className="text-sm text-peligro">{error}</p>}

          {!loading && !error && (
            <div className="overflow-x-auto rounded-sm border border-acero/20">
              <table className="min-w-full divide-y divide-acero/20 text-sm">
                <thead className="bg-fondo">
                  <tr>
                    {['Tanque', 'Tipo combustible', 'Existencia actual', 'Disponibilidad', 'Nivel crítico', 'Estado'].map((h) => (
                      <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-acero/10 bg-white">
                  {filas.length === 0 && (
                    <tr>
                      <td colSpan={6} className="px-4 py-6 text-center text-acero/70">
                        Sin tanques registrados.
                      </td>
                    </tr>
                  )}
                  {filas.map((f) => {
                    const critico = f.tanque && f.existenciaActual <= f.tanque.nivelCritico
                    const inactivo = f.tanque && !f.tanque.activo
                    return (
                      <tr key={f.id} className="hover:bg-fondo">
                        <td className="px-4 py-3 font-medium font-mono text-tinta">{f.tanqueIdentificacion}</td>
                        <td className="px-4 py-3 text-acero">{f.tanque?.tipoCombustibleNombre ?? '—'}</td>
                        <td className="px-4 py-3 font-mono num text-tinta">{f.existenciaActual.toFixed(2)} gal</td>
                        <td className="px-4 py-3 font-mono num text-acero">{f.disponibilidad.toFixed(2)} gal</td>
                        <td className="px-4 py-3 font-mono num text-acero">{f.tanque ? `${f.tanque.nivelCritico.toFixed(2)} gal` : '—'}</td>
                        <td className="px-4 py-3">
                          {inactivo
                            ? <StatusBadge label="Inactivo" variant="gray" />
                            : critico
                              ? <StatusBadge label="Nivel bajo" variant="red" />
                              : <StatusBadge label="Normal" variant="green" />}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}

      {tab === 'historial' && (
        <>
          <div className="mb-4">
            <select
              value={filtroTanque}
              onChange={(e) => setFiltroTanque(e.target.value)}
              className={`${inputCls} max-w-xs`}
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
            <div className="overflow-x-auto rounded-sm border border-acero/20">
              <table className="min-w-full divide-y divide-acero/20 text-sm">
                <thead className="bg-fondo">
                  <tr>
                    {['Fecha', 'Tipo', 'Volumen', 'Tanque', 'Usuario', 'Referencia / Observaciones'].map((h) => (
                      <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-acero/10 bg-white">
                  {movimientos.length === 0 && (
                    <tr>
                      <td colSpan={6} className="px-4 py-6 text-center text-acero/70">
                        Sin movimientos registrados.
                      </td>
                    </tr>
                  )}
                  {movimientos.map((m) => (
                    <tr key={m.id} className="hover:bg-fondo">
                      <td className="px-4 py-3 text-acero">{formatFecha(m.fechaHora)}</td>
                      <td className="px-4 py-3">
                        <StatusBadge label={TIPO_LABEL[m.tipo] ?? m.tipo} variant={TIPO_VARIANT[m.tipo]} />
                      </td>
                      <td className={`px-4 py-3 font-medium font-mono num ${m.volumen < 0 ? 'text-peligro' : 'text-tinta'}`}>
                        {m.volumen > 0 ? '+' : ''}{m.volumen.toFixed(2)} gal
                      </td>
                      <td className="px-4 py-3 font-mono text-acero">{m.tanqueIdentificacion}</td>
                      <td className="px-4 py-3 text-acero">{m.usuarioNombreUsuario}</td>
                      <td className="px-4 py-3 text-acero">{m.referenciaOperacion || m.observaciones || '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
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
                {tanques.map((t) => (
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
                min="0.0001"
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
                {tanques.map((t) => (
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
                {tanques.map((t) => (
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
