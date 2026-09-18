import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import apiRequest, { apiDownload } from '../services/api'
import { getUser } from '../services/auth'

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

const ESTADOS_NO_TERMINALES = ['Creado', 'Enviado', 'Pendiente', 'ProximoAVencer']

const EMPTY_EMIT_FORM = { solicitudId: '', prefijo: '' }

function formatFecha(value) {
  return value ? new Date(value).toLocaleString() : '—'
}

export default function TicketsPage() {
  // Emitir/enviar/anular ticket son ManagementRoles en TicketsController.cs:
  // solo Administrador/Supervisor. El resto de roles con acceso a esta
  // pantalla (Despachador, Auditor, Consulta, Solicitante) solo consultan
  // (Solicitante ve únicamente sus propios tickets vía OwnerFilter).
  const puedeGestionar = getUser()?.roles?.some((r) => ['Administrador', 'Supervisor'].includes(r)) ?? false
  const [tickets, setTickets] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [actionError, setActionError] = useState(null)

  const [solicitudesAprobadas, setSolicitudesAprobadas] = useState([])
  const [showEmitModal, setShowEmitModal] = useState(false)
  const [emitForm, setEmitForm] = useState(EMPTY_EMIT_FORM)
  const [emitting, setEmitting] = useState(false)
  const [emitError, setEmitError] = useState(null)

  const [detailTicket, setDetailTicket] = useState(null)
  const [downloadingId, setDownloadingId] = useState(null)
  const [sendingId, setSendingId] = useState(null)

  const [anularModal, setAnularModal] = useState(null)
  const [motivoAnulacion, setMotivoAnulacion] = useState('')
  const [anulando, setAnulando] = useState(false)
  const [anularError, setAnularError] = useState(null)

  async function cargarTickets() {
    try {
      const data = await apiRequest('/tickets')
      setTickets(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/tickets')
      .then((data) => { if (!cancelado) setTickets(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  function openEmitModal() {
    setEmitForm(EMPTY_EMIT_FORM)
    setEmitError(null)
    apiRequest('/solicitudes')
      .then((data) => setSolicitudesAprobadas(data.filter((s) => s.estado === 'Aprobada')))
      .catch(() => setSolicitudesAprobadas([]))
    setShowEmitModal(true)
  }

  async function handleEmitir(e) {
    e.preventDefault()
    setEmitting(true)
    setEmitError(null)
    try {
      await apiRequest('/tickets', {
        method: 'POST',
        body: JSON.stringify({
          solicitudId: Number(emitForm.solicitudId),
          prefijo: emitForm.prefijo.trim() || undefined,
        }),
      })
      setShowEmitModal(false)
      await cargarTickets()
    } catch (e) {
      setEmitError(e.message)
    } finally {
      setEmitting(false)
    }
  }

  async function handleDescargarPdf(ticket) {
    setActionError(null)
    setDownloadingId(ticket.id)
    try {
      // El backend no expone el header Content-Disposition vía CORS, así que
      // armamos un nombre legible con el código del ticket que ya tenemos.
      const { blob } = await apiDownload(`/tickets/${ticket.id}/pdf`)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `ticket-${ticket.codigo}.pdf`
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (e) {
      setActionError(e.message)
    } finally {
      setDownloadingId(null)
    }
  }

  async function handleEnviar(ticket) {
    setActionError(null)
    setSendingId(ticket.id)
    try {
      await apiRequest(`/tickets/${ticket.id}/enviar`, { method: 'POST' })
      await cargarTickets()
    } catch (e) {
      setActionError(e.message)
    } finally {
      setSendingId(null)
    }
  }

  async function handleAnular(e) {
    e.preventDefault()
    setAnulando(true)
    setAnularError(null)
    try {
      await apiRequest(`/tickets/${anularModal.id}/anular`, {
        method: 'POST',
        body: JSON.stringify({ motivo: motivoAnulacion }),
      })
      setAnularModal(null)
      setDetailTicket(null)
      await cargarTickets()
    } catch (e) {
      setAnularError(e.message)
    } finally {
      setAnulando(false)
    }
  }

  return (
    <PageContainer title="Tickets">
      {puedeGestionar && (
        <div className="mb-4 flex justify-end">
          <button
            type="button"
            onClick={openEmitModal}
            className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
          >
            + Emitir ticket
          </button>
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
                {[
                  'Código',
                  'Empleado',
                  'Vehículo',
                  'Departamento',
                  'Autorizado',
                  'Tipo',
                  'Creación',
                  'Vencimiento',
                  'Estado',
                  'Acciones',
                ].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {tickets.length === 0 && (
                <tr>
                  <td colSpan={10} className="px-4 py-6 text-center text-acero/70">
                    Sin tickets emitidos.
                  </td>
                </tr>
              )}
              {tickets.map((t) => (
                <tr key={t.id} className="cursor-pointer hover:bg-fondo" onClick={() => setDetailTicket(t)}>
                  <td className="px-4 py-3 font-medium font-mono text-tinta">{t.codigo}</td>
                  <td className="px-4 py-3 text-acero">{t.empleadoNombre}</td>
                  <td className="px-4 py-3 font-mono text-acero">{t.vehiculoPlaca}</td>
                  <td className="px-4 py-3 text-acero">{t.departamentoNombre}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{t.cantidadAutorizada}</td>
                  <td className="px-4 py-3 text-acero">{t.tipoCombustibleNombre}</td>
                  <td className="px-4 py-3 text-acero">{formatFecha(t.fechaCreacion)}</td>
                  <td className="px-4 py-3 text-acero">{formatFecha(t.fechaVencimiento)}</td>
                  <td className="px-4 py-3">
                    <StatusBadge label={ESTADO_LABEL[t.estado] ?? t.estado} variant={ESTADO_VARIANT[t.estado]} />
                  </td>
                  <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        onClick={() => handleDescargarPdf(t)}
                        disabled={downloadingId === t.id}
                        className="rounded bg-acero px-2 py-1 text-xs text-white hover:opacity-90 disabled:opacity-50"
                      >
                        {downloadingId === t.id ? 'Descargando...' : 'PDF'}
                      </button>
                      {ESTADOS_NO_TERMINALES.includes(t.estado) && puedeGestionar && (
                        <>
                          <button
                            type="button"
                            onClick={() => handleEnviar(t)}
                            disabled={sendingId === t.id}
                            className="rounded bg-acero px-2 py-1 text-xs text-white hover:opacity-90 disabled:opacity-50"
                          >
                            {sendingId === t.id ? 'Enviando...' : 'Enviar'}
                          </button>
                          <button
                            type="button"
                            onClick={() => {
                              setAnularModal({ id: t.id })
                              setMotivoAnulacion('')
                              setAnularError(null)
                            }}
                            className="rounded bg-peligro px-2 py-1 text-xs text-white hover:opacity-90"
                          >
                            Anular
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showEmitModal && (
        <Modal title="Emitir ticket" onClose={() => setShowEmitModal(false)}>
          <form onSubmit={handleEmitir} className="space-y-4">
            <Field label="Solicitud aprobada">
              <select
                value={emitForm.solicitudId}
                onChange={(e) => setEmitForm((f) => ({ ...f, solicitudId: e.target.value }))}
                required
                className={inputCls}
              >
                <option value="">Seleccionar...</option>
                {solicitudesAprobadas.map((s) => (
                  <option key={s.id} value={s.id}>
                    #{s.id} — {s.empleadoNombre} — {s.vehiculoPlaca} — {s.cantidadAutorizada} {s.tipoCombustibleNombre}
                  </option>
                ))}
              </select>
              {solicitudesAprobadas.length === 0 && (
                <p className="mt-1 text-xs text-acero/70">No hay solicitudes aprobadas disponibles.</p>
              )}
            </Field>
            <Field label="Prefijo (opcional)">
              <input
                type="text"
                value={emitForm.prefijo}
                onChange={(e) => setEmitForm((f) => ({ ...f, prefijo: e.target.value }))}
                maxLength={10}
                placeholder="Por defecto: COM"
                className={inputCls}
              />
            </Field>
            {emitError && <p className="text-sm text-peligro">{emitError}</p>}
            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setShowEmitModal(false)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={emitting || !emitForm.solicitudId}
                className="rounded-md bg-tanque px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
              >
                {emitting ? 'Emitiendo...' : 'Emitir ticket'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {detailTicket && (
        <Modal title={`Ticket ${detailTicket.codigo}`} onClose={() => setDetailTicket(null)}>
          <div className="space-y-3 text-sm">
            <div className="grid grid-cols-2 gap-x-4 gap-y-2">
              <div>
                <p className="text-xs text-acero/70">UUID</p>
                <p className="break-all font-mono text-tinta">{detailTicket.id}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Número secuencial</p>
                <p className="font-mono num text-tinta">{detailTicket.numeroSecuencial}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Empleado</p>
                <p className="text-tinta">{detailTicket.empleadoNombre}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Vehículo</p>
                <p className="font-mono text-tinta">{detailTicket.vehiculoPlaca}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Departamento</p>
                <p className="text-tinta">{detailTicket.departamentoNombre}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Cantidad autorizada</p>
                <p className="text-tinta">
                  <span className="font-mono num">{detailTicket.cantidadAutorizada}</span> {detailTicket.tipoCombustibleNombre}
                </p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Fecha de creación</p>
                <p className="text-tinta">{formatFecha(detailTicket.fechaCreacion)}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Fecha de vencimiento</p>
                <p className="text-tinta">{formatFecha(detailTicket.fechaVencimiento)}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Estado</p>
                <StatusBadge label={ESTADO_LABEL[detailTicket.estado] ?? detailTicket.estado} variant={ESTADO_VARIANT[detailTicket.estado]} />
              </div>
              <div>
                <p className="text-xs text-acero/70">Solicitud de origen</p>
                <p className="text-tinta">#{detailTicket.solicitudId}</p>
              </div>
            </div>

            {detailTicket.motivoAnulacion && (
              <p className="rounded-md bg-peligro/10 p-2 text-xs text-peligro">
                Motivo de anulación: {detailTicket.motivoAnulacion}
              </p>
            )}

            <div className="rounded-md bg-fondo p-3 text-xs text-acero">
              {detailTicket.qrDisponible
                ? 'El QR seguro está disponible, pero la API no expone la imagen por separado — solo viene embebido en el PDF del ticket. Descárgalo para verlo.'
                : 'Este ticket no tiene un QR disponible.'}
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setDetailTicket(null)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cerrar
              </button>
              <button
                type="button"
                onClick={() => handleDescargarPdf(detailTicket)}
                disabled={downloadingId === detailTicket.id}
                className="rounded-md bg-acero px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
              >
                {downloadingId === detailTicket.id ? 'Descargando...' : 'Descargar PDF'}
              </button>
            </div>
          </div>
        </Modal>
      )}

      {anularModal && (
        <Modal title="Anular ticket" onClose={() => setAnularModal(null)}>
          <form onSubmit={handleAnular} className="space-y-4">
            <Field label="Motivo de anulación">
              <textarea
                value={motivoAnulacion}
                onChange={(e) => setMotivoAnulacion(e.target.value)}
                required
                minLength={5}
                maxLength={500}
                rows={3}
                className={inputCls}
                placeholder="Indique el motivo..."
              />
            </Field>
            {anularError && <p className="text-sm text-peligro">{anularError}</p>}
            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setAnularModal(null)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={anulando || motivoAnulacion.trim().length < 5}
                className="rounded-md bg-peligro px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
              >
                {anulando ? 'Anulando...' : 'Anular'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
