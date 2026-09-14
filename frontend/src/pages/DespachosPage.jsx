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
      <p className="mb-4 text-sm text-gray-500">
        Vista de consulta. El registro de un despacho ocurre desde la aplicación móvil al escanear el ticket en la estación —
        esta pantalla no permite crear despachos nuevos.
      </p>

      <form onSubmit={handleAplicarFiltro} className="mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Filtrar por ID de ticket</label>
          <input
            type="text"
            value={filtroTicketId}
            onChange={(e) => setFiltroTicketId(e.target.value)}
            placeholder="UUID del ticket"
            className="w-72 rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <button
          type="submit"
          className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          Filtrar
        </button>
        {ticketIdAplicado && (
          <button
            type="button"
            onClick={handleLimpiarFiltro}
            className="rounded-md border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
          >
            Limpiar
          </button>
        )}
      </form>

      {loading && <p className="text-sm text-gray-500">Cargando...</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}

      {!loading && !error && (
        <>
          <div className="overflow-x-auto rounded-lg border border-gray-200">
            <table className="min-w-full divide-y divide-gray-200 text-sm">
              <thead className="bg-gray-50">
                <tr>
                  {['Ticket', 'Fecha / Hora', 'Galones servidos', 'Operador', 'Tanque', 'Estación', 'Observaciones'].map((h) => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 bg-white">
                {despachos.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-4 py-6 text-center text-gray-400">
                      {ticketIdAplicado
                        ? 'Sin despachos para ese ticket.'
                        : 'Sin despachos registrados todavía. Los despachos se registran desde la aplicación móvil al escanear un ticket.'}
                    </td>
                  </tr>
                )}
                {despachos.map((d) => (
                  <tr key={d.despachoId} onClick={() => openDetalle(d)} className="cursor-pointer hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium text-gray-800">{d.codigoTicket}</td>
                    <td className="px-4 py-3 text-gray-600">{formatFechaHora(d.fecha, d.hora)}</td>
                    <td className="px-4 py-3 text-gray-800">{d.galonesServidos.toFixed(2)} gal</td>
                    <td className="px-4 py-3 text-gray-600">{d.operador}</td>
                    <td className="px-4 py-3 text-gray-600">{d.tanqueIdentificacion}</td>
                    <td className="px-4 py-3 text-gray-600">{d.estacionNombre}</td>
                    <td className="px-4 py-3 text-gray-500">{d.observaciones || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-4 flex items-center justify-between text-sm">
            <span className="text-gray-500">Página {pagina}</span>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setPagina((p) => Math.max(1, p - 1))}
                disabled={pagina === 1}
                className="rounded-md border px-3 py-1.5 text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                Anterior
              </button>
              <button
                type="button"
                onClick={() => setPagina((p) => p + 1)}
                disabled={despachos.length < TAMANO_PAGINA}
                className="rounded-md border px-3 py-1.5 text-gray-700 hover:bg-gray-50 disabled:opacity-50"
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
                <p className="text-xs text-gray-400">Fecha / Hora</p>
                <p className="text-gray-700">{formatFechaHora(detalle.fecha, detalle.hora)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Galones servidos</p>
                <p className="text-gray-700">{detalle.galonesServidos.toFixed(2)} gal</p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Operador</p>
                <p className="text-gray-700">{detalle.operador}</p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Estado del ticket al despachar</p>
                <StatusBadge label={ESTADO_LABEL[detalle.estadoTicket] ?? detalle.estadoTicket} variant={ESTADO_VARIANT[detalle.estadoTicket]} />
              </div>
              <div>
                <p className="text-xs text-gray-400">Tanque</p>
                <p className="text-gray-700">{detalle.tanqueIdentificacion}</p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Estación</p>
                <p className="text-gray-700">{detalle.estacionNombre}</p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Inventario restante</p>
                <p className="text-gray-700">{detalle.inventarioRestante.toFixed(2)} gal</p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Disponibilidad restante</p>
                <p className="text-gray-700">{detalle.disponibilidadRestante.toFixed(2)} gal</p>
              </div>
            </div>

            {detalle.observaciones && (
              <p className="rounded-md bg-gray-50 p-2 text-xs text-gray-600">
                Observaciones: {detalle.observaciones}
              </p>
            )}

            <div className="border-t pt-3">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-gray-500">Ticket asociado</p>
              {ticketDetalleError && <p className="text-sm text-red-600">{ticketDetalleError}</p>}
              {!ticketDetalleError && !ticketDetalle && <p className="text-sm text-gray-400">Cargando ticket...</p>}
              {ticketDetalle && (
                <div className="grid grid-cols-2 gap-x-4 gap-y-2">
                  <div>
                    <p className="text-xs text-gray-400">Código</p>
                    <p className="text-gray-700">{ticketDetalle.codigo}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-400">Empleado</p>
                    <p className="text-gray-700">{ticketDetalle.empleadoNombre}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-400">Vehículo</p>
                    <p className="text-gray-700">{ticketDetalle.vehiculoPlaca}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-400">Departamento</p>
                    <p className="text-gray-700">{ticketDetalle.departamentoNombre}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-400">Cantidad autorizada</p>
                    <p className="text-gray-700">{ticketDetalle.cantidadAutorizada} {ticketDetalle.tipoCombustibleNombre}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-400">Estado actual del ticket</p>
                    <StatusBadge label={ESTADO_LABEL[ticketDetalle.estado] ?? ticketDetalle.estado} variant={ESTADO_VARIANT[ticketDetalle.estado]} />
                  </div>
                </div>
              )}
            </div>

            <div className="flex justify-end pt-2">
              <button
                type="button"
                onClick={() => setDetalle(null)}
                className="rounded-md border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
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
