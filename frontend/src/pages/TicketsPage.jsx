import { useEffect, useMemo, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
import StatusBadge from '../components/StatusBadge'
import { useAuth } from '../hooks/useAuth'
import apiRequest, { apiDownload } from '../services/api'
import { getSequentialFilename, downloadBlob } from '../utils/download'
import { canEmitTickets } from '../utils/rbac'

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
  const { user } = useAuth()
  const canEmit = canEmitTickets(user)
  const [tickets, setTickets] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [actionError, setActionError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  const isSolicitanteOnly =
    user?.roles?.includes('Solicitante') &&
    !user?.roles?.includes('Administrador') &&
    !user?.roles?.includes('Supervisor') &&
    !user?.roles?.includes('Despachador') &&
    !user?.roles?.includes('Auditor')

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

  const nextSequenceNumber = useMemo(() => {
    if (!tickets || tickets.length === 0) return 1
    const max = Math.max(...tickets.map((t) => Number(t.numeroSecuencial) || 0), 0)
    return max + 1
  }, [tickets])
  const formattedNextSeq = String(nextSequenceNumber).padStart(6, '0')

  function openEmitModal() {
    setEmitForm({ solicitudId: '', prefijo: 'TCK' })
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
      const res = await apiRequest('/tickets', {
        method: 'POST',
        body: JSON.stringify({
          solicitudId: Number(emitForm.solicitudId),
          prefijo: emitForm.prefijo.trim().toUpperCase() || 'TCK',
        }),
      })
      setShowEmitModal(false)
      setSuccessMessage(`Ticket ${res.codigo} emitido exitosamente.`)
      setTimeout(() => setSuccessMessage(null), 5000)
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
      const { blob } = await apiDownload(`/tickets/${ticket.id}/pdf`)
      const basePrefix = `ticket-${ticket.codigo}`
      const filename = getSequentialFilename(basePrefix, 'pdf')
      downloadBlob(blob, filename)
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
      setSuccessMessage(`Ticket ${ticket.codigo} enviado con éxito por los canales de notificación configurados.`)
      setTimeout(() => setSuccessMessage(null), 6000)
      await cargarTickets()
      if (detailTicket && detailTicket.id === ticket.id) {
        setDetailTicket((prev) => prev ? { ...prev, estado: 'Enviado' } : null)
      }
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
      setSuccessMessage('El ticket fue anulado correctamente.')
      setTimeout(() => setSuccessMessage(null), 5000)
      await cargarTickets()
    } catch (e) {
      setAnularError(e.message)
    } finally {
      setAnulando(false)
    }
  }

  return (
    <PageContainer title={isSolicitanteOnly ? 'Mis Tickets' : 'Tickets de Combustible'}>
      {isSolicitanteOnly && (
        <div className="mb-4 rounded-md border border-tanque/20 bg-tanque/5 p-3 text-sm text-tanque flex flex-col sm:flex-row sm:items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <span>👤</span>
            <span>Mostrando únicamente los tickets autorizados a tu nombre para consumo de combustible.</span>
          </div>
          <span className="text-xs bg-tanque/10 text-tanque border border-tanque/20 px-2.5 py-1 rounded font-medium self-start sm:self-auto">
            Mis tickets
          </span>
        </div>
      )}

      <div className="mb-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          {!loading && !error && (
            <span className="text-sm text-acero">
              Total emitidos: <strong className="text-tinta font-mono">{tickets.length}</strong>
            </span>
          )}
        </div>
        {canEmit && (
          <button
            type="button"
            onClick={openEmitModal}
            className="flex min-h-[44px] w-full sm:w-auto justify-center items-center gap-2 rounded-md bg-tanque px-5 py-2 text-sm font-semibold text-white shadow-sm transition-all hover:bg-tanque/90 active:scale-[0.98]"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
            Emitir ticket
          </button>
        )}
      </div>

      {successMessage && (
        <div className="mb-4 flex items-center justify-between rounded-md border border-exito/40 bg-exito/10 p-3.5 text-sm font-medium text-exito">
          <div className="flex items-center gap-2">
            <svg className="w-5 h-5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
            </svg>
            <span>{successMessage}</span>
          </div>
          <button type="button" onClick={() => setSuccessMessage(null)} className="text-exito hover:opacity-70">✕</button>
        </div>
      )}

      {loading && <p className="text-sm text-acero">Cargando tickets...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <ResponsiveTable
          data={tickets}
          keyField="id"
          onRowClick={(t) => setDetailTicket(t)}
          columns={[
            {
              key: 'codigo',
              label: 'Código',
              primary: true,
              priority: 'high',
              render: (t) => <span className="font-semibold font-mono text-tanque">{t.codigo}</span>,
            },
            {
              key: 'empleadoNombre',
              label: 'Empleado',
              priority: 'high',
              render: (t) => <span className="text-tinta font-medium">{t.empleadoNombre}</span>,
            },
            {
              key: 'vehiculoPlaca',
              label: 'Vehículo',
              priority: 'high',
              render: (t) => <span className="font-mono text-acero">{t.vehiculoPlaca}</span>,
            },
            {
              key: 'departamentoNombre',
              label: 'Departamento',
              priority: 'low',
            },
            {
              key: 'cantidadAutorizada',
              label: 'Autorizado',
              priority: 'high',
              render: (t) => <span className="font-mono num font-bold text-tinta">{t.cantidadAutorizada} gal</span>,
            },
            {
              key: 'tipoCombustibleNombre',
              label: 'Tipo',
              priority: 'med',
            },
            {
              key: 'fechaCreacion',
              label: 'Creación',
              priority: 'low',
              render: (t) => <span className="font-mono num text-xs">{formatFecha(t.fechaCreacion)}</span>,
            },
            {
              key: 'fechaVencimiento',
              label: 'Vencimiento',
              priority: 'med',
              render: (t) => <span className="font-mono num text-xs">{formatFecha(t.fechaVencimiento)}</span>,
            },
            {
              key: 'estado',
              label: 'Estado',
              priority: 'high',
              render: (t) => (
                <StatusBadge label={ESTADO_LABEL[t.estado] ?? t.estado} variant={ESTADO_VARIANT[t.estado]} />
              ),
            },
          ]}
          actions={(t) => (
            <div className="flex items-center gap-1.5" onClick={(e) => e.stopPropagation()}>
              <button
                type="button"
                onClick={() => setDetailTicket(t)}
                className="inline-flex min-h-[38px] items-center gap-1 rounded-md border border-acero/30 bg-white px-2.5 py-1.5 text-xs font-semibold text-tinta hover:border-tanque hover:bg-fondo transition-colors shadow-xs"
                title="Ver detalle completo"
              >
                <svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z" />
                  <circle cx="12" cy="12" r="3" />
                </svg>
                Ver
              </button>
              <button
                type="button"
                onClick={() => handleDescargarPdf(t)}
                disabled={downloadingId === t.id}
                className="inline-flex min-h-[38px] items-center gap-1 rounded-md border border-tanque/30 bg-tanque/5 px-2.5 py-1.5 text-xs font-semibold text-tanque hover:bg-tanque hover:text-white transition-colors disabled:opacity-50 shadow-xs"
                title="Descargar PDF"
              >
                <svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                  <polyline points="7 10 12 15 17 10" />
                  <line x1="12" y1="15" x2="12" y2="3" />
                </svg>
                {downloadingId === t.id ? 'Descargando…' : 'PDF'}
              </button>
            </div>
          )}
          emptyMessage="Sin tickets emitidos."
        />
      )}


      {showEmitModal && (
        <Modal title="Emitir Ticket de Combustible" onClose={() => setShowEmitModal(false)}>
          <form onSubmit={handleEmitir} className="space-y-4">
            <Field label="Solicitud aprobada" required hint="Seleccione la solicitud que autoriza la emisión">
              <select
                value={emitForm.solicitudId}
                onChange={(e) => setEmitForm((f) => ({ ...f, solicitudId: e.target.value }))}
                required
                className={inputCls}
              >
                <option value="">Seleccionar solicitud aprobada...</option>
                {solicitudesAprobadas.map((s) => (
                  <option key={s.id} value={s.id}>
                    #{s.id} — {s.empleadoNombre} — {s.vehiculoPlaca} — {s.cantidadAutorizada} gal ({s.tipoCombustibleNombre})
                  </option>
                ))}
              </select>
              {solicitudesAprobadas.length === 0 && (
                <p className="mt-1 text-xs text-advertencia font-medium">No hay solicitudes aprobadas pendientes de emisión.</p>
              )}
            </Field>

            <div className="space-y-2">
              <Field
                label="Prefijo de numeración"
                hint="Identificador alfabético (2 a 5 letras, ej. TCK, COM, DSL). El correlativo numérico es automático."
              >
                <div className="space-y-2">
                  <div className="flex flex-wrap gap-1.5">
                    {['TCK', 'COM', 'DSL', 'GAS', 'EMG'].map((pfx) => (
                      <button
                        key={pfx}
                        type="button"
                        onClick={() => setEmitForm((f) => ({ ...f, prefijo: pfx }))}
                        className={`px-3 py-1 text-xs font-semibold rounded border transition-all ${
                          (emitForm.prefijo || 'TCK') === pfx
                            ? 'bg-tanque text-white border-tanque shadow-xs'
                            : 'bg-white text-acero border-acero/30 hover:bg-fondo'
                        }`}
                      >
                        {pfx}
                      </button>
                    ))}
                  </div>
                  <input
                    type="text"
                    value={emitForm.prefijo}
                    onChange={(e) =>
                      setEmitForm((f) => ({
                        ...f,
                        prefijo: e.target.value.replace(/[^A-Za-z]/g, '').toUpperCase().slice(0, 5),
                      }))
                    }
                    maxLength={5}
                    placeholder="TCK"
                    className={`${inputCls} font-mono`}
                  />
                </div>
              </Field>

              {/* Live Preview of Code */}
              <div className="rounded-lg border border-tanque/20 bg-tanque/5 p-3.5 text-xs space-y-1">
                <div className="flex items-center justify-between">
                  <span className="text-acero font-medium">Previsualización del código que se generará:</span>
                  <span className="rounded bg-tanque/10 px-2 py-0.5 font-mono text-[10px] font-bold text-tanque">
                    Secuencia correlativa: #{formattedNextSeq}
                  </span>
                </div>
                <div className="font-mono text-base font-bold text-tanque">
                  {(emitForm.prefijo.trim() || 'TCK').toUpperCase()}-{new Date().getFullYear()}-{formattedNextSeq}
                </div>
                <p className="text-[11px] text-acero/80">
                  El sistema asigna e incrementa el número secuencial correlativo de 6 dígitos automáticamente al emitir el ticket.
                </p>
              </div>
            </div>

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
                className="rounded-md bg-tanque px-5 py-2 text-sm font-semibold text-white hover:opacity-90 disabled:opacity-50"
              >
                {emitting ? 'Emitiendo ticket…' : 'Emitir ticket'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {detailTicket && (
        <Modal title={`Detalle de Ticket — ${detailTicket.codigo}`} onClose={() => setDetailTicket(null)}>
          <div className="space-y-4 text-sm">
            {/* Header info band */}
            <div className="flex items-center justify-between border-b border-acero/20 pb-3">
              <div>
                <p className="text-xs text-acero">Código de ticket</p>
                <p className="font-mono text-lg font-bold text-tanque">{detailTicket.codigo}</p>
              </div>
              <div>
                <StatusBadge label={ESTADO_LABEL[detailTicket.estado] ?? detailTicket.estado} variant={ESTADO_VARIANT[detailTicket.estado]} />
              </div>
            </div>

            {/* Grid of metadata */}
            <div className="grid grid-cols-2 gap-x-4 gap-y-3 bg-fondo p-3.5 rounded-md border border-acero/15 text-xs">
              <div>
                <p className="text-acero font-medium">Empleado solicitante</p>
                <p className="font-semibold text-tinta mt-0.5">{detailTicket.empleadoNombre}</p>
              </div>
              <div>
                <p className="text-acero font-medium">Vehículo asignado</p>
                <p className="font-mono font-semibold text-tinta mt-0.5">{detailTicket.vehiculoPlaca}</p>
              </div>
              <div>
                <p className="text-acero font-medium">Departamento</p>
                <p className="text-tinta mt-0.5">{detailTicket.departamentoNombre}</p>
              </div>
              <div>
                <p className="text-acero font-medium">Cantidad autorizada</p>
                <p className="font-bold text-sm text-tanque mt-0.5">
                  <span className="font-mono">{detailTicket.cantidadAutorizada} gal</span> ({detailTicket.tipoCombustibleNombre})
                </p>
              </div>
              <div>
                <p className="text-acero font-medium">Fecha de creación</p>
                <p className="text-tinta mt-0.5">{formatFecha(detailTicket.fechaCreacion)}</p>
              </div>
              <div>
                <p className="text-acero font-medium">Fecha de vencimiento</p>
                <p className="text-peligro font-bold mt-0.5">{formatFecha(detailTicket.fechaVencimiento)}</p>
              </div>
              <div>
                <p className="text-acero font-medium">Número secuencial</p>
                <p className="font-mono text-tinta mt-0.5">{detailTicket.numeroSecuencial}</p>
              </div>
              <div>
                <p className="text-acero font-medium">Solicitud ref.</p>
                <p className="text-tinta mt-0.5">#{detailTicket.solicitudId}</p>
              </div>
            </div>

            {detailTicket.motivoAnulacion && (
              <div className="rounded-md bg-peligro/10 border border-peligro/30 p-2.5 text-xs text-peligro">
                <span className="font-bold block">Motivo de anulación:</span>
                {detailTicket.motivoAnulacion}
              </div>
            )}

            <div className="rounded-md border border-exito/30 bg-exito/5 p-3 text-xs text-acero flex items-center gap-2">
              <svg className="w-5 h-5 text-exito flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span>Ticket verificado con código QR oficial listo para validación y despacho de combustible.</span>
            </div>

            {/* Modal Actions */}
            <div className="border-t border-acero/20 pt-3 flex flex-wrap items-center justify-between gap-2">
              <div>
                {ESTADOS_NO_TERMINALES.includes(detailTicket.estado) && canEmit && (
                  <button
                    type="button"
                    onClick={() => {
                      setAnularModal({ id: detailTicket.id })
                      setMotivoAnulacion('')
                      setAnularError(null)
                    }}
                    className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-peligro/40 bg-white px-3 py-2 text-xs font-semibold text-peligro hover:bg-peligro/10 transition-colors"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <line x1="18" y1="6" x2="6" y2="18" />
                      <line x1="6" y1="6" x2="18" y2="18" />
                    </svg>
                    Anular ticket
                  </button>
                )}
              </div>

              <div className="flex gap-2">
                {ESTADOS_NO_TERMINALES.includes(detailTicket.estado) && canEmit && (
                  <button
                    type="button"
                    onClick={() => handleEnviar(detailTicket)}
                    disabled={sendingId === detailTicket.id}
                    className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-acero/30 bg-white px-3.5 py-2 text-xs font-semibold text-tinta hover:border-tanque hover:bg-fondo transition-colors disabled:opacity-50"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <line x1="22" y1="2" x2="11" y2="13" />
                      <polygon points="22 2 15 22 11 13 2 9 22 2" />
                    </svg>
                    {sendingId === detailTicket.id ? 'Enviando…' : 'Enviar por correo / SMS'}
                  </button>
                )}

                <button
                  type="button"
                  onClick={() => handleDescargarPdf(detailTicket)}
                  disabled={downloadingId === detailTicket.id}
                  className="flex min-h-[38px] items-center gap-1.5 rounded-md bg-tanque px-4 py-2 text-xs font-semibold text-white hover:opacity-90 transition-opacity disabled:opacity-50"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                    <polyline points="7 10 12 15 17 10" />
                    <line x1="12" y1="15" x2="12" y2="3" />
                  </svg>
                  {downloadingId === detailTicket.id ? 'Descargando…' : 'Descargar PDF oficial'}
                </button>
              </div>
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
