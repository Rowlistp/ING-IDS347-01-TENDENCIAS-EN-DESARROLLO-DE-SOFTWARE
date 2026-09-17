import { useEffect, useState } from 'react'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'

const TAMANO_PAGINA = 20

const ESTADO_LABEL = {
  PENDIENTE: 'Pendiente',
  PROCESANDO: 'Procesando',
  ENVIADA: 'Enviada',
  FALLIDA: 'Fallida',
}

const ESTADO_VARIANT = {
  PENDIENTE: 'yellow',
  PROCESANDO: 'blue',
  ENVIADA: 'green',
  FALLIDA: 'red',
}

// Tipos observados en el código real (Notifications/*.cs); el backend hace
// comparación exacta por texto, no valida contra un enum cerrado, así que esta
// lista es orientativa — si aparece un tipo nuevo no listado aquí, seguirá
// visible en la tabla, solo no aparecerá como opción del filtro por tipo.
const TIPOS_CONOCIDOS = [
  'TICKET_EMITIDO',
  'TICKET_PROXIMO_VENCER',
  'TICKET_VENCIDO',
  'INVENTARIO_BAJO',
  'AJUSTE_INVENTARIO',
]

const FILTROS_VACIOS = { estado: '', canal: '', tipo: '', fechaDesde: '', fechaHasta: '' }

function formatFecha(value) {
  return value ? new Date(value).toLocaleString() : '—'
}

export default function NotificacionesPage() {
  const [pagina, setPagina] = useState(1)
  const [filtros, setFiltros] = useState(FILTROS_VACIOS)
  const [filtrosPendientes, setFiltrosPendientes] = useState(FILTROS_VACIOS)

  const [respuesta, setRespuesta] = useState(null)
  const [claveCargada, setClaveCargada] = useState(null)
  const [error, setError] = useState(null)

  const [reintentandoId, setReintentandoId] = useState(null)
  const [reintentoError, setReintentoError] = useState(null)
  // Se incrementa tras un reintento exitoso para forzar la recarga de la misma
  // página/filtros (la clave por sí sola no cambiaría, así que el efecto no se
  // volvería a ejecutar sin esto).
  const [version, setVersion] = useState(0)

  const claveActual = JSON.stringify({ pagina, ...filtros, version })
  const loading = claveCargada !== claveActual

  useEffect(() => {
    let cancelado = false
    const params = new URLSearchParams({ pagina: String(pagina), tamanoPagina: String(TAMANO_PAGINA) })
    if (filtros.estado) params.set('estado', filtros.estado)
    if (filtros.canal) params.set('canal', filtros.canal)
    if (filtros.tipo) params.set('tipo', filtros.tipo)
    if (filtros.fechaDesde) params.set('fechaDesde', new Date(filtros.fechaDesde).toISOString())
    if (filtros.fechaHasta) params.set('fechaHasta', new Date(filtros.fechaHasta).toISOString())

    apiRequest(`/notificaciones?${params.toString()}`)
      .then((data) => {
        if (cancelado) return
        setRespuesta(data)
        setClaveCargada(claveActual)
        setError(null)
      })
      .catch((e) => { if (!cancelado) setError(e.message) })
    return () => { cancelado = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [claveActual])

  function handleFiltroChange(e) {
    const { name, value } = e.target
    setFiltrosPendientes((f) => ({ ...f, [name]: value }))
  }

  function handleAplicarFiltros(e) {
    e.preventDefault()
    setPagina(1)
    setFiltros(filtrosPendientes)
  }

  function handleLimpiarFiltros() {
    setFiltrosPendientes(FILTROS_VACIOS)
    setFiltros(FILTROS_VACIOS)
    setPagina(1)
  }

  async function handleReintentar(n) {
    setReintentoError(null)
    setReintentandoId(n.id)
    try {
      await apiRequest(`/notificaciones/${n.id}/reintentar`, { method: 'POST' })
      setVersion((v) => v + 1)
    } catch (e) {
      // El backend responde 409 REINTENTO_NO_PERMITIDO si la notificación no está
      // FALLIDA o si es de canal INTERNO — el mensaje ya viene claro.
      setReintentoError(e.message)
    } finally {
      setReintentandoId(null)
    }
  }

  const totalPaginas = respuesta ? Math.max(1, Math.ceil(respuesta.total / respuesta.tamanoPagina)) : 1

  return (
    <PageContainer title="Notificaciones">
      <p className="mb-4 text-sm text-acero">
        El envío real de correo/SMS está deshabilitado en este entorno de desarrollo — las notificaciones quedan
        en estado "Pendiente" indefinidamente en vez de completarse o fallar. Esto es esperado, no un error.
      </p>

      <form onSubmit={handleAplicarFiltros} className="mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label className="mb-1 block text-sm font-medium text-acero">Estado</label>
          <select
            name="estado"
            value={filtrosPendientes.estado}
            onChange={handleFiltroChange}
            className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
          >
            <option value="">Todos</option>
            {Object.keys(ESTADO_LABEL).map((e) => (
              <option key={e} value={e}>{ESTADO_LABEL[e]}</option>
            ))}
          </select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-acero">Canal</label>
          <select
            name="canal"
            value={filtrosPendientes.canal}
            onChange={handleFiltroChange}
            className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
          >
            <option value="">Todos</option>
            <option value="EMAIL">Email</option>
            <option value="SMS">SMS</option>
            <option value="INTERNO">Interno</option>
          </select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-acero">Tipo</label>
          <select
            name="tipo"
            value={filtrosPendientes.tipo}
            onChange={handleFiltroChange}
            className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
          >
            <option value="">Todos</option>
            {TIPOS_CONOCIDOS.map((t) => (
              <option key={t} value={t}>{t}</option>
            ))}
          </select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-acero">Desde</label>
          <input
            type="date"
            name="fechaDesde"
            value={filtrosPendientes.fechaDesde}
            onChange={handleFiltroChange}
            className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-acero">Hasta</label>
          <input
            type="date"
            name="fechaHasta"
            value={filtrosPendientes.fechaHasta}
            onChange={handleFiltroChange}
            className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
          />
        </div>

        <button
          type="submit"
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          Filtrar
        </button>
        <button
          type="button"
          onClick={handleLimpiarFiltros}
          className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
        >
          Limpiar
        </button>
      </form>

      {error && <p className="text-sm text-peligro">{error}</p>}
      {reintentoError && <p className="text-sm text-peligro">{reintentoError}</p>}
      {!error && loading && <p className="text-sm text-acero">Cargando...</p>}

      {!error && !loading && respuesta && (
        <>
          <div className="overflow-x-auto rounded-sm border border-acero/20">
            <table className="min-w-full divide-y divide-acero/20 text-sm">
              <thead className="bg-fondo">
                <tr>
                  {['Tipo', 'Canal', 'Destinatario', 'Estado', 'Fecha', 'Intentos', 'Acciones'].map((h) => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-acero/10 bg-white">
                {respuesta.elementos.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-4 py-6 text-center text-acero/70">
                      Sin notificaciones para los filtros aplicados.
                    </td>
                  </tr>
                )}
                {respuesta.elementos.map((n) => (
                  <tr key={n.id} className="hover:bg-fondo">
                    <td className="px-4 py-3 font-mono text-tinta">{n.tipo}</td>
                    <td className="px-4 py-3 font-mono text-acero">{n.canal}</td>
                    <td className="px-4 py-3 font-mono text-acero">{n.destinatario}</td>
                    <td className="px-4 py-3">
                      <StatusBadge label={ESTADO_LABEL[n.estado] ?? n.estado} variant={ESTADO_VARIANT[n.estado]} />
                    </td>
                    <td className="px-4 py-3 font-mono num text-acero">{formatFecha(n.fechaHora)}</td>
                    <td className="px-4 py-3 font-mono num text-acero">{n.intentos} / {n.intentosTotales}</td>
                    <td className="px-4 py-3">
                      {n.estado === 'FALLIDA' && (
                        <button
                          type="button"
                          onClick={() => handleReintentar(n)}
                          disabled={reintentandoId === n.id}
                          className="rounded bg-tanque px-2 py-1 text-xs text-white hover:opacity-90 disabled:opacity-50"
                        >
                          {reintentandoId === n.id ? 'Reintentando...' : 'Reintentar'}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-4 flex items-center justify-between text-sm">
            <span className="font-mono num text-acero">
              Página {respuesta.pagina} de {totalPaginas} — {respuesta.total} registros
            </span>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setPagina((p) => Math.max(1, p - 1))}
                disabled={pagina === 1}
                className="rounded-md border border-acero/40 px-3 py-1.5 text-tinta hover:bg-fondo disabled:opacity-50"
              >
                Anterior
              </button>
              <button
                type="button"
                onClick={() => setPagina((p) => p + 1)}
                disabled={pagina >= totalPaginas}
                className="rounded-md border border-acero/40 px-3 py-1.5 text-tinta hover:bg-fondo disabled:opacity-50"
              >
                Siguiente
              </button>
            </div>
          </div>
        </>
      )}
    </PageContainer>
  )
}
