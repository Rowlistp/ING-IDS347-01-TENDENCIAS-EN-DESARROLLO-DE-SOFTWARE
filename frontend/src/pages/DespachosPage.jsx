import { useEffect, useState } from 'react'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'

const ESTADO_LABEL = {
  Creado: 'Creado',
  Enviado: 'Enviado',
  Pendiente: 'Pendiente',
  ProximoAVencer: 'Próximo a vencer',
  Vencido: 'Vencido',
  Consumido: 'Consumido',
  Anulado: 'Anulado',
}

const ESTADO_VARIANT = {
  Creado: 'blue',
  Enviado: 'green',
  Pendiente: 'yellow',
  ProximoAVencer: 'orange',
  Vencido: 'red',
  Consumido: 'gray',
  Anulado: 'purple',
}

const TAMANO_PAGINA = 20

function formatFechaHora(fecha, hora) {
  if (!fecha) return '—'
  return hora ? `${fecha} ${hora}` : fecha
}

export default function DespachosPage() {
  const [despachos, setDespachos] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [pagina, setPagina] = useState(1)
  const [filtroTicketId, setFiltroTicketId] = useState('')
  const [ticketIdAplicado, setTicketIdAplicado] = useState('')

  const [detalle, setDetalle] = useState(null)
  const [ticketDetalle, setTicketDetalle] = useState(null)
  const [ticketDetalleError, setTicketDetalleError] = useState(null)

  async function cargarDespachos() {
    setLoading(true)
    setError(null)
    try {
      const params = new URLSearchParams({ pagina: String(pagina), tamanoPagina: String(TAMANO_PAGINA) })
      if (ticketIdAplicado.trim()) params.set('ticketId', ticketIdAplicado.trim())
      const data = await apiRequest(`/despachos?${params.toString()}`)
      setDespachos(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    cargarDespachos()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pagina, ticketIdAplicado])

  function handleAplicarFiltro(e) {
    e.preventDefault()
    setPagina(1)
    setTicketIdAplicado(filtroTicketId)
  }

  function handleLimpiarFiltro() {
    setFiltroTicketId('')
    setTicketIdAplicado('')
    setPagina(1)
  }

  async function openDetalle(despacho) {
    setDetalle(despacho)
    setTicketDetalle(null)
    setTicketDetalleError(null)
    try {
      const ticket = await apiRequest(`/tickets/${despacho.ticketId}`)
      setTicketDetalle(ticket)
    } catch (e) {
      setTicketDetalleError(e.message)
    }
  }

  return (
    <PageContainer title="Despachos">
      <p className="mb-4 text-sm text-acero">
        Vista de consulta. El registro de un despacho ocurre desde la aplicación móvil al escanear el ticket en la estación —
        esta pantalla no permite crear despachos nuevos.
      </p>

      <form onSubmit={handleAplicarFiltro} className="mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label className="mb-1 block text-sm font-medium text-tinta">Filtrar por ID de ticket</label>
          <input
            type="text"
            value={filtroTicketId}
            onChange={(e) => setFiltroTicketId(e.target.value)}
            placeholder="UUID del ticket"
            className="w-72 rounded-md border border-acero/40 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-tanque/50"
          />
        </div>
        <button
          type="submit"
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          Filtrar
        </button>
        {ticketIdAplicado && (
          <button
            type="button"
            onClick={handleLimpiarFiltro}
            className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
          >
            Limpiar
          </button>
        )}
      </form>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}

      {!loading && !error && (
        <>
          <div className="overflow-x-auto rounded-sm border border-acero/20">
            <table className="min-w-full divide-y divide-acero/20 text-sm">
              <thead className="bg-fondo">
                <tr>
                  {['Ticket', 'Fecha / Hora', 'Galones servidos', 'Operador', 'Tanque', 'Estación', 'Observaciones'].map((h) => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-acero/10 bg-white">
                {despachos.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-4 py-6 text-center text-acero/70">
                      {ticketIdAplicado
                        ? 'Sin despachos para ese ticket.'
                        : 'Sin despachos registrados todavía. Los despachos se registran desde la aplicación móvil al escanear un ticket.'}
                    </td>
                  </tr>
                )}
                {despachos.map((d) => (
                  <tr key={d.despachoId} onClick={() => openDetalle(d)} className="cursor-pointer hover:bg-fondo">
                    <td className="px-4 py-3 font-medium font-mono text-tinta">{d.codigoTicket}</td>
                    <td className="px-4 py-3 text-acero">{formatFechaHora(d.fecha, d.hora)}</td>
                    <td className="px-4 py-3 font-mono num text-tinta">{d.galonesServidos.toFixed(2)} gal</td>
                    <td className="px-4 py-3 text-acero">{d.operador}</td>
                    <td className="px-4 py-3 font-mono text-acero">{d.tanqueIdentificacion}</td>
                    <td className="px-4 py-3 text-acero">{d.estacionNombre}</td>
                    <td className="px-4 py-3 text-acero">{d.observaciones || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-4 flex items-center justify-between text-sm">
            <span className="text-acero">Página {pagina}</span>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setPagina((p) => Math.max(1, p - 1))}
                disabled={pagina === 1}
                className="rounded-md border px-3 py-1.5 text-tinta hover:bg-fondo disabled:opacity-50"
              >
                Anterior
              </button>
              <button
                type="button"
                onClick={() => setPagina((p) => p + 1)}
                disabled={despachos.length < TAMANO_PAGINA}
                className="rounded-md border px-3 py-1.5 text-tinta hover:bg-fondo disabled:opacity-50"
              >
                Siguiente
              </button>
            </div>
          </div>
        </>
      )}

      {detalle && (
        <Modal title={`Despacho — ${detalle.codigoTicket}`} onClose={() => setDetalle(null)}>
          <div className="space-y-4 text-sm">
            <div className="grid grid-cols-2 gap-x-4 gap-y-2">
              <div>
                <p className="text-xs text-acero/70">Fecha / Hora</p>
                <p className="text-tinta">{formatFechaHora(detalle.fecha, detalle.hora)}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Galones servidos</p>
                <p className="font-mono num text-tinta">{detalle.galonesServidos.toFixed(2)} gal</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Operador</p>
                <p className="text-tinta">{detalle.operador}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Estado del ticket al despachar</p>
                <StatusBadge label={ESTADO_LABEL[detalle.estadoTicket] ?? detalle.estadoTicket} variant={ESTADO_VARIANT[detalle.estadoTicket]} />
              </div>
              <div>
                <p className="text-xs text-acero/70">Tanque</p>
                <p className="font-mono text-tinta">{detalle.tanqueIdentificacion}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Estación</p>
                <p className="text-tinta">{detalle.estacionNombre}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Inventario restante</p>
                <p className="font-mono num text-tinta">{detalle.inventarioRestante.toFixed(2)} gal</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Disponibilidad restante</p>
                <p className="font-mono num text-tinta">{detalle.disponibilidadRestante.toFixed(2)} gal</p>
              </div>
            </div>

            {detalle.observaciones && (
              <p className="rounded-md bg-fondo p-2 text-xs text-acero">
                Observaciones: {detalle.observaciones}
              </p>
            )}

            <div className="border-t pt-3">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-acero">Ticket asociado</p>
              {ticketDetalleError && <p className="text-sm text-peligro">{ticketDetalleError}</p>}
              {!ticketDetalleError && !ticketDetalle && <p className="text-sm text-acero/70">Cargando ticket...</p>}
              {ticketDetalle && (
                <div className="grid grid-cols-2 gap-x-4 gap-y-2">
                  <div>
                    <p className="text-xs text-acero/70">Código</p>
                    <p className="font-mono text-tinta">{ticketDetalle.codigo}</p>
                  </div>
                  <div>
                    <p className="text-xs text-acero/70">Empleado</p>
                    <p className="text-tinta">{ticketDetalle.empleadoNombre}</p>
                  </div>
                  <div>
                    <p className="text-xs text-acero/70">Vehículo</p>
                    <p className="text-tinta">{ticketDetalle.vehiculoPlaca}</p>
                  </div>
                  <div>
                    <p className="text-xs text-acero/70">Departamento</p>
                    <p className="text-tinta">{ticketDetalle.departamentoNombre}</p>
                  </div>
                  <div>
                    <p className="text-xs text-acero/70">Cantidad autorizada</p>
                    <p className="text-tinta">{ticketDetalle.cantidadAutorizada} {ticketDetalle.tipoCombustibleNombre}</p>
                  </div>
                  <div>
                    <p className="text-xs text-acero/70">Estado actual del ticket</p>
                    <StatusBadge label={ESTADO_LABEL[ticketDetalle.estado] ?? ticketDetalle.estado} variant={ESTADO_VARIANT[ticketDetalle.estado]} />
                  </div>
                </div>
              )}
            </div>

            <div className="flex justify-end pt-2">
              <button
                type="button"
                onClick={() => setDetalle(null)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cerrar
              </button>
            </div>
          </div>
        </Modal>
      )}
    </PageContainer>
  )
}
