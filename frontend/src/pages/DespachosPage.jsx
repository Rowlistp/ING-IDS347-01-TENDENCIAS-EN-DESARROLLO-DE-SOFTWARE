import { useEffect, useState } from 'react'
import { Html5Qrcode } from 'html5-qrcode'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
import StatusBadge from '../components/StatusBadge'
import apiRequest from '../services/api'
import { getUser } from '../services/auth'
import {
  descargarComprobanteDespachoHtml,
  imprimirComprobanteDespacho,
} from '../utils/download'

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

  // Roles y permisos
  const user = getUser()
  const roles = user?.roles || []
  const canCreate = roles.some((r) =>
    ['Despachador', 'Administrador', 'Supervisor'].includes(r)
  )

  // Estado del modal de despacho con lector QR
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [modoEscaneo, setModoEscaneo] = useState('camera') // 'camera' | 'manual' | 'file'
  const [manualPayload, setManualPayload] = useState('')
  const [validating, setValidating] = useState(false)
  const [validationError, setValidationError] = useState(null)
  const [cameraError, setCameraError] = useState(null)
  const [fileError, setFileError] = useState(null)

  // Ticket validado y datos operativos
  const [qrPayload, setQrPayload] = useState('')
  const [ticketValidado, setTicketValidado] = useState(null)
  const [estaciones, setEstaciones] = useState([])
  const [tanques, setTanques] = useState([])
  const [selectedEstacionId, setSelectedEstacionId] = useState('')
  const [selectedTanqueId, setSelectedTanqueId] = useState('')
  const [galonesServidos, setGalonesServidos] = useState('')
  const [observaciones, setObservaciones] = useState('')
  const [submittingDispatch, setSubmittingDispatch] = useState(false)
  const [dispatchError, setDispatchError] = useState(null)

  // Modal de confirmación de despacho exitoso
  const [despachoExitoso, setDespachoExitoso] = useState(null)

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

  async function openCreateModal() {
    setShowCreateModal(true)
    setModoEscaneo('camera')
    setManualPayload('')
    setValidationError(null)
    setCameraError(null)
    setFileError(null)
    setQrPayload('')
    setTicketValidado(null)
    setGalonesServidos('')
    setObservaciones('')
    setDispatchError(null)

    try {
      const [estacionesData, tanquesData] = await Promise.all([
        apiRequest('/estaciones?soloActivas=true'),
        apiRequest('/tanques'),
      ])
      const estacionesActivas = (estacionesData || []).filter((e) => e.activo)
      const tanquesActivos = (tanquesData || []).filter((t) => t.activo)
      setEstaciones(estacionesActivas)
      setTanques(tanquesActivos)
      if (estacionesActivas.length > 0) {
        setSelectedEstacionId(String(estacionesActivas[0].id))
      } else {
        setSelectedEstacionId('')
      }
    } catch {
      // Ignorar si falla precarga de catálogos
    }
  }

  // Garantiza que selectedEstacionId siempre sea el Id de una estación activa disponible
  useEffect(() => {
    if (estaciones.length > 0) {
      if (!estaciones.some((e) => String(e.id) === String(selectedEstacionId))) {
        setSelectedEstacionId(String(estaciones[0].id))
      }
    } else {
      setSelectedEstacionId('')
    }
  }, [estaciones, selectedEstacionId])

  // Garantiza que selectedTanqueId apunte a un tanque compatible activo
  useEffect(() => {
    if (!ticketValidado) return
    const compatibles = tanques.filter(
      (t) => t.activo && t.tipoCombustibleId === ticketValidado.tipoCombustibleId
    )
    if (compatibles.length > 0) {
      if (!compatibles.some((t) => String(t.id) === String(selectedTanqueId))) {
        setSelectedTanqueId(String(compatibles[0].id))
      }
    } else {
      setSelectedTanqueId('')
    }
  }, [ticketValidado, tanques, selectedTanqueId])

  function closeCreateModal() {
    setShowCreateModal(false)
    setTicketValidado(null)
    setQrPayload('')
    setManualPayload('')
  }

  // Hook del escáner con cámara activa
  useEffect(() => {
    if (!showCreateModal || modoEscaneo !== 'camera' || ticketValidado) return

    let html5QrCode = null
    let isMounted = true

    const timer = setTimeout(async () => {
      try {
        const element = document.getElementById('html5-qr-reader-div')
        if (!element || !isMounted) return

        html5QrCode = new Html5Qrcode('html5-qr-reader-div')
        await html5QrCode.start(
          { facingMode: 'environment' },
          {
            fps: 10,
            qrbox: { width: 230, height: 230 },
          },
          async (decodedText) => {
            if (!isMounted) return
            try {
              if (html5QrCode.isScanning) {
                await html5QrCode.stop()
              }
            } catch {
              // No-op
            }
            await validarCodigoQr(decodedText)
          },
          () => {
            // Ignorar frames no decodificados
          }
        )
        if (isMounted) setCameraError(null)
      } catch {
        if (isMounted) {
          setCameraError(
            'No se pudo inicializar la cámara. Verifique los permisos en el navegador o use la pestaña "Pistola USB / Manual".'
          )
        }
      }
    }, 150)

    return () => {
      isMounted = false
      clearTimeout(timer)
      if (html5QrCode) {
        try {
          if (html5QrCode.isScanning) {
            html5QrCode
              .stop()
              .then(() => {
                try {
                  html5QrCode.clear()
                } catch {
                  // No-op
                }
              })
              .catch(() => {})
          } else {
            try {
              html5QrCode.clear()
            } catch {
              // No-op
            }
          }
        } catch {
          // No-op
        }
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [showCreateModal, modoEscaneo, ticketValidado])

  async function validarCodigoQr(rawText) {
    const payload = rawText.trim()
    if (!payload) return
    setValidating(true)
    setValidationError(null)
    try {
      const res = await apiRequest('/tickets/validar', {
        method: 'POST',
        body: JSON.stringify({ qrPayload: payload }),
      })

      if (!res.valido) {
        setValidationError(res.mensaje || `Ticket inválido (${res.codigo})`)
        setTicketValidado(null)
        setQrPayload('')
        return
      }

      setQrPayload(payload)
      setTicketValidado(res.ticket)
      setGalonesServidos(String(res.ticket.cantidadAutorizada || ''))

      const compatibles = tanques.filter(
        (t) => t.activo && t.tipoCombustibleId === res.ticket.tipoCombustibleId
      )
      if (compatibles.length > 0) {
        setSelectedTanqueId(String(compatibles[0].id))
      } else {
        setSelectedTanqueId('')
      }
    } catch (err) {
      setValidationError(err.message || 'Error al validar el ticket con el servidor.')
    } finally {
      setValidating(false)
    }
  }

  async function handleFileUpload(e) {
    const file = e.target.files?.[0]
    if (!file) return
    setFileError(null)
    setValidating(true)
    try {
      const html5QrCode = new Html5Qrcode('html5-qr-file-dummy')
      const decodedText = await html5QrCode.scanFile(file, true)
      await validarCodigoQr(decodedText)
    } catch {
      setFileError('No se encontró ningún código QR legible en la imagen seleccionada.')
    } finally {
      setValidating(false)
    }
  }

  function handleManualSubmit(e) {
    e.preventDefault()
    if (!manualPayload.trim()) return
    validarCodigoQr(manualPayload)
  }

  async function handleConfirmarDespacho(e) {
    e.preventDefault()
    if (!ticketValidado || !qrPayload) return
    if (!selectedTanqueId) {
      setDispatchError('Debe seleccionar un tanque de suministro.')
      return
    }
    if (!selectedEstacionId || !estaciones.some((e) => String(e.id) === String(selectedEstacionId))) {
      setDispatchError('Debe seleccionar una estación de servicio activa.')
      return
    }
    const galones = parseFloat(galonesServidos)
    if (isNaN(galones) || galones <= 0) {
      setDispatchError('Indique una cantidad válida y positiva de galones.')
      return
    }
    if (galones > ticketValidado.cantidadAutorizada) {
      setDispatchError(
        `Los galones a despachar (${galones}) no pueden superar la cantidad autorizada (${ticketValidado.cantidadAutorizada} gal).`
      )
      return
    }

    setSubmittingDispatch(true)
    setDispatchError(null)

    try {
      const result = await apiRequest('/despachos', {
        method: 'POST',
        body: JSON.stringify({
          ticketId: ticketValidado.id,
          qrPayload,
          tanqueId: parseInt(selectedTanqueId, 10),
          estacionId: parseInt(selectedEstacionId, 10),
          galonesServidos: galones,
          observaciones: observaciones.trim() || null,
        }),
      })

      setShowCreateModal(false)
      setDespachoExitoso(result)
      await cargarDespachos()
    } catch (err) {
      setDispatchError(err.message || 'Error al procesar el despacho de combustible.')
    } finally {
      setSubmittingDispatch(false)
    }
  }

  const tanqueSeleccionadoObj = tanques.find(
    (t) => String(t.id) === String(selectedTanqueId)
  )
  const tanquesCompatibles = ticketValidado
    ? tanques.filter(
        (t) => t.activo && t.tipoCombustibleId === ticketValidado.tipoCombustibleId
      )
    : []

  return (
    <PageContainer title="Despachos">
      {/* Contenedor invisible para decodificación de archivos QR */}
      <div id="html5-qr-file-dummy" style={{ display: 'none' }} />

      <div className="mb-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <p className="text-sm text-acero">
          Registro y fiscalización de despachos de combustible. Valide tickets oficiales escaneando su código QR en la estación.
        </p>
        {canCreate && (
          <button
            type="button"
            onClick={openCreateModal}
            className="flex min-h-[44px] w-full sm:w-auto items-center justify-center gap-2 rounded-md bg-tanque px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-tanque/90 active:scale-[0.98]"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect width="5" height="5" x="3" y="3" rx="1"/>
              <rect width="5" height="5" x="16" y="3" rx="1"/>
              <rect width="5" height="5" x="3" y="16" rx="1"/>
              <path d="M21 16h-3a2 2 0 0 0-2 2v3"/>
              <path d="M21 21v.01"/>
              <path d="M12 7v3a2 2 0 0 1-2 2H7"/>
              <path d="M3 12h.01"/>
              <path d="M12 3h.01"/>
              <path d="M12 16v.01"/>
              <path d="M16 12h1"/>
              <path d="M21 12v.01"/>
              <path d="M12 21v-1"/>
            </svg>
            Nuevo despacho (Lector QR)
          </button>
        )}
      </div>

      <form onSubmit={handleAplicarFiltro} className="mb-4 flex flex-col sm:flex-row items-stretch sm:items-end gap-3">
        <div className="flex-1 sm:max-w-xs">
          <label className="mb-1 block text-sm font-medium text-tinta">Filtrar por ID de ticket</label>
          <input
            type="text"
            value={filtroTicketId}
            onChange={(e) => setFiltroTicketId(e.target.value)}
            placeholder="UUID del ticket"
            className="w-full min-h-[42px] rounded-md border border-acero/40 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-tanque/50"
          />
        </div>
        <div className="flex gap-2">
          <button
            type="submit"
            className="flex-1 sm:flex-initial min-h-[42px] rounded-md bg-tanque px-5 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-tanque/90 active:scale-[0.98]"
          >
            Filtrar
          </button>
          {ticketIdAplicado && (
            <button
              type="button"
              onClick={handleLimpiarFiltro}
              className="min-h-[42px] rounded-md border border-acero/30 bg-white px-4 py-2 text-sm font-medium text-tinta hover:bg-fondo active:scale-[0.98]"
            >
              Limpiar
            </button>
          )}
        </div>
      </form>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}

      {!loading && !error && (
        <>
          <ResponsiveTable
            data={despachos}
            keyField="despachoId"
            onRowClick={(d) => openDetalle(d)}
            emptyMessage={
              ticketIdAplicado
                ? 'Sin despachos para ese ticket.'
                : 'Sin despachos registrados todavía. Pulse "Nuevo despacho (Lector QR)" para registrar un despacho.'
            }
            columns={[
              {
                key: 'codigoTicket',
                label: 'Ticket',
                primary: true,
                priority: 'high',
                render: (d) => <span className="font-semibold font-mono text-tanque">{d.codigoTicket}</span>,
              },
              {
                key: 'fecha',
                label: 'Fecha / Hora',
                priority: 'med',
                render: (d) => <span className="text-acero text-xs sm:text-sm">{formatFechaHora(d.fecha, d.hora)}</span>,
              },
              {
                key: 'galonesServidos',
                label: 'Galones servidos',
                priority: 'high',
                render: (d) => <span className="font-mono num font-semibold text-tinta">{d.galonesServidos.toFixed(2)} gal</span>,
              },
              {
                key: 'operador',
                label: 'Operador',
                priority: 'med',
                render: (d) => <span className="text-acero">{d.operador}</span>,
              },
              {
                key: 'tanqueIdentificacion',
                label: 'Tanque',
                priority: 'low',
                render: (d) => <span className="font-mono text-acero">{d.tanqueIdentificacion}</span>,
              },
              {
                key: 'estacionNombre',
                label: 'Estación',
                priority: 'high',
                render: (d) => <span className="text-acero">{d.estacionNombre}</span>,
              },
              {
                key: 'observaciones',
                label: 'Observaciones',
                priority: 'low',
                render: (d) => <span className="text-acero">{d.observaciones || '—'}</span>,
              },
            ]}
            actions={(d) => (
              <div className="flex items-center gap-1.5">
                <button
                  type="button"
                  onClick={() => openDetalle(d)}
                  className="inline-flex min-h-[38px] items-center gap-1.5 rounded-sm border border-acero/30 bg-white px-2.5 py-1.5 text-xs font-medium text-tinta shadow-xs transition-colors hover:border-tanque hover:bg-fondo active:scale-[0.98]"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                    <circle cx="12" cy="12" r="3" />
                  </svg>
                  Ver
                </button>
                <button
                  type="button"
                  onClick={async () => {
                    let t = null
                    try {
                      t = await apiRequest(`/tickets/${d.ticketId}`)
                    } catch {
                      // No-op
                    }
                    imprimirComprobanteDespacho(d, t)
                  }}
                  title="Imprimir comprobante oficial"
                  className="inline-flex min-h-[38px] items-center gap-1.5 rounded-sm border border-acero/30 bg-white px-2.5 py-1.5 text-xs font-medium text-acero shadow-xs transition-colors hover:border-tanque hover:text-tanque active:scale-[0.98]"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="6 9 6 2 18 2 18 9" />
                    <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" />
                    <rect width="12" height="8" x="6" y="14" />
                  </svg>
                  Imprimir
                </button>
              </div>
            )}
          />

          <div className="mt-4 flex flex-col sm:flex-row items-center justify-between gap-3 text-sm">
            <span className="text-acero order-2 sm:order-1">Página {pagina}</span>
            <div className="flex w-full sm:w-auto gap-2 order-1 sm:order-2">
              <button
                type="button"
                onClick={() => setPagina((p) => Math.max(1, p - 1))}
                disabled={pagina === 1}
                className="flex-1 sm:flex-initial min-h-[42px] rounded-md border border-acero/30 bg-white px-4 py-2 text-tinta hover:bg-fondo disabled:opacity-50"
              >
                Anterior
              </button>
              <button
                type="button"
                onClick={() => setPagina((p) => p + 1)}
                disabled={despachos.length < TAMANO_PAGINA}
                className="flex-1 sm:flex-initial min-h-[42px] rounded-md border border-acero/30 bg-white px-4 py-2 text-tinta hover:bg-fondo disabled:opacity-50"
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
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-2">
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

            <div className="border-t border-acero/20 pt-3">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-acero">Ticket asociado</p>
              {ticketDetalleError && <p className="text-sm text-peligro">{ticketDetalleError}</p>}
              {!ticketDetalleError && !ticketDetalle && <p className="text-sm text-acero/70">Cargando ticket...</p>}
              {ticketDetalle && (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-2">
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

            <div className="flex flex-col sm:flex-row justify-between items-stretch sm:items-center gap-2 pt-3 border-t border-acero/20">
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => imprimirComprobanteDespacho(detalle, ticketDetalle)}
                  className="inline-flex min-h-[40px] items-center justify-center gap-1.5 rounded-md border border-acero/30 bg-white px-3 py-2 text-xs font-semibold text-tinta hover:bg-fondo active:scale-[0.98]"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="6 9 6 2 18 2 18 9" />
                    <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" />
                    <rect width="12" height="8" x="6" y="14" />
                  </svg>
                  Imprimir comprobante
                </button>
                <button
                  type="button"
                  onClick={() => descargarComprobanteDespachoHtml(detalle, ticketDetalle)}
                  className="inline-flex min-h-[40px] items-center justify-center gap-1.5 rounded-md border border-acero/30 bg-white px-3 py-2 text-xs font-semibold text-tinta hover:bg-fondo active:scale-[0.98]"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                    <polyline points="7 10 12 15 17 10" />
                    <line x1="12" x2="12" y1="15" y2="3" />
                  </svg>
                  Descargar HTML
                </button>
              </div>
              <button
                type="button"
                onClick={() => setDetalle(null)}
                className="min-h-[40px] rounded-md border border-acero/30 px-5 py-2 text-sm font-medium text-tinta hover:bg-fondo active:scale-[0.98]"
              >
                Cerrar
              </button>
            </div>
          </div>
        </Modal>
      )}

      {/* Modal Principal de Creación de Despacho (Lector QR) */}
      {showCreateModal && (
        <Modal
          title={ticketValidado ? `Despacho de combustible — ${ticketValidado.codigo}` : 'Escanear ticket de combustible'}
          onClose={closeCreateModal}
        >
          <div className="space-y-4">
            {!ticketValidado ? (
              <div>
                {/* Selector de modo de escaneo */}
                <div className="flex border-b border-acero/20 mb-4">
                  <button
                    type="button"
                    onClick={() => {
                      setModoEscaneo('camera')
                      setValidationError(null)
                    }}
                    className={`flex items-center gap-2 px-4 py-2 text-xs font-semibold border-b-2 transition-colors ${
                      modoEscaneo === 'camera'
                        ? 'border-tanque text-tanque'
                        : 'border-transparent text-acero hover:text-tinta'
                    }`}
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"/>
                      <circle cx="12" cy="13" r="4"/>
                    </svg>
                    Cámara en vivo
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setModoEscaneo('manual')
                      setValidationError(null)
                    }}
                    className={`flex items-center gap-2 px-4 py-2 text-xs font-semibold border-b-2 transition-colors ${
                      modoEscaneo === 'manual'
                        ? 'border-tanque text-tanque'
                        : 'border-transparent text-acero hover:text-tinta'
                    }`}
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <rect width="20" height="16" x="2" y="4" rx="2"/>
                      <path d="M6 8h.01"/>
                      <path d="M10 8h.01"/>
                      <path d="M14 8h.01"/>
                      <path d="M18 8h.01"/>
                      <path d="M8 12h8"/>
                      <path d="M6 16h.01"/>
                      <path d="M10 16h.01"/>
                      <path d="M14 16h.01"/>
                      <path d="M18 16h.01"/>
                    </svg>
                    Pistola USB / Manual
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setModoEscaneo('file')
                      setValidationError(null)
                    }}
                    className={`flex items-center gap-2 px-4 py-2 text-xs font-semibold border-b-2 transition-colors ${
                      modoEscaneo === 'file'
                        ? 'border-tanque text-tanque'
                        : 'border-transparent text-acero hover:text-tinta'
                    }`}
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                      <polyline points="17 8 12 3 7 8"/>
                      <line x1="12" x2="12" y1="3" y2="15"/>
                    </svg>
                    Subir imagen QR
                  </button>
                </div>

                {/* Contenido del modo Cámara */}
                {modoEscaneo === 'camera' && (
                  <div className="space-y-3">
                    <p className="text-xs text-acero">
                      Apunte la cámara hacia el código QR impreso o en la pantalla del conductor para validarlo automáticamente.
                    </p>
                    <div className="relative overflow-hidden rounded-md border border-acero/30 bg-black min-h-[250px] flex items-center justify-center">
                      <div id="html5-qr-reader-div" className="w-full h-full" />
                      {validating && (
                        <div className="absolute inset-0 bg-black/70 flex flex-col items-center justify-center text-white gap-2 z-10">
                          <div className="h-7 w-7 animate-spin rounded-full border-2 border-white border-t-transparent" />
                          <span className="text-xs font-medium">Validando firma criptográfica del ticket...</span>
                        </div>
                      )}
                    </div>
                    {cameraError && (
                      <div className="rounded bg-peligro/10 border border-peligro/20 p-2.5 text-xs text-peligro">
                        {cameraError}
                      </div>
                    )}
                  </div>
                )}

                {/* Contenido del modo Pistola USB / Entrada manual */}
                {modoEscaneo === 'manual' && (
                  <form onSubmit={handleManualSubmit} className="space-y-3">
                    <p className="text-xs text-acero">
                      Coloque el cursor en el campo y dispare su lector de código de barras USB, o pegue el contenido del código QR.
                    </p>
                    <Field label="Payload del código QR" required hint="Formato con firma ECDSA generado por FuelTrack">
                      <textarea
                        rows={5}
                        value={manualPayload}
                        onChange={(e) => setManualPayload(e.target.value)}
                        placeholder="Pegue o escanee aquí el texto codificado del QR del ticket..."
                        className="w-full rounded-md border border-acero/30 p-2.5 font-mono text-xs text-tinta focus:border-tanque focus:outline-none focus:ring-1 focus:ring-tanque"
                        autoFocus
                      />
                    </Field>
                    <button
                      type="submit"
                      disabled={validating || !manualPayload.trim()}
                      className="flex min-h-[44px] w-full items-center justify-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-tanque/90 disabled:opacity-50"
                    >
                      {validating ? (
                        <>
                          <div className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                          <span>Validando ticket...</span>
                        </>
                      ) : (
                        'Validar código QR'
                      )}
                    </button>
                  </form>
                )}

                {/* Contenido del modo Subir archivo */}
                {modoEscaneo === 'file' && (
                  <div className="space-y-3">
                    <p className="text-xs text-acero">
                      Seleccione una foto, captura de pantalla o comprobante digital que contenga el código QR del ticket.
                    </p>
                    <label className="flex flex-col items-center justify-center border-2 border-dashed border-acero/40 rounded-lg p-6 cursor-pointer hover:border-tanque transition-colors bg-fondo/30">
                      <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" className="text-acero mb-2">
                        <rect width="18" height="18" x="3" y="3" rx="2" ry="2"/>
                        <circle cx="9" cy="9" r="2"/>
                        <path d="m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21"/>
                      </svg>
                      <span className="text-xs font-semibold text-tinta">Haga clic para buscar imagen</span>
                      <span className="text-[11px] text-acero mt-1">Soporta PNG, JPG, WEBP</span>
                      <input
                        type="file"
                        accept="image/*"
                        onChange={handleFileUpload}
                        className="hidden"
                      />
                    </label>
                    {fileError && (
                      <p className="text-xs text-peligro">{fileError}</p>
                    )}
                  </div>
                )}

                {validationError && (
                  <div className="mt-3 rounded-md bg-peligro/10 border border-peligro/20 p-3 text-xs text-peligro flex items-start gap-2">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="shrink-0 mt-0.5">
                      <circle cx="12" cy="12" r="10"/>
                      <line x1="12" x2="12" y1="8" y2="12"/>
                      <line x1="12" x2="12.01" y1="16" y2="16"/>
                    </svg>
                    <span>{validationError}</span>
                  </div>
                )}
              </div>
            ) : (
              /* Paso 2: Ticket validado correctamente -> Registrar despacho */
              <form onSubmit={handleConfirmarDespacho} className="space-y-4 text-sm">
                {/* Tarjeta de verificación de ticket */}
                <div className="rounded-md border border-exito/30 bg-exito/5 p-3.5 space-y-2">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="inline-flex items-center justify-center rounded-full bg-exito text-white h-5 w-5 text-xs font-bold">
                        ✓
                      </span>
                      <span className="font-semibold text-tinta text-sm">Ticket verificado y auténtico</span>
                    </div>
                    <button
                      type="button"
                      onClick={() => {
                        setTicketValidado(null)
                        setQrPayload('')
                      }}
                      className="text-xs font-medium text-acero hover:text-peligro underline"
                    >
                      Escanear otro ticket
                    </button>
                  </div>

                  <div className="grid grid-cols-2 gap-2 text-xs pt-1 border-t border-exito/20">
                    <div>
                      <span className="text-acero block">Código oficial:</span>
                      <span className="font-mono font-bold text-tanque">{ticketValidado.codigo}</span>
                    </div>
                    <div>
                      <span className="text-acero block">Beneficiario / Chofer:</span>
                      <span className="font-semibold text-tinta">{ticketValidado.empleadoNombre}</span>
                    </div>
                    <div>
                      <span className="text-acero block">Vehículo:</span>
                      <span className="text-tinta">{ticketValidado.vehiculoPlaca} {ticketValidado.vehiculoModelo ? `(${ticketValidado.vehiculoModelo})` : ''}</span>
                    </div>
                    <div>
                      <span className="text-acero block">Combustible autorizado:</span>
                      <span className="font-semibold text-tinta">{ticketValidado.tipoCombustibleNombre}</span>
                    </div>
                    <div className="col-span-2 bg-white rounded p-2 border border-exito/20 flex justify-between items-center">
                      <span className="text-xs text-acero font-medium">Volumen máximo autorizado:</span>
                      <span className="font-mono font-bold text-sm text-exito">{ticketValidado.cantidadAutorizada.toFixed(2)} galones</span>
                    </div>
                  </div>
                </div>

                {/* Selección de Estación */}
                <Field
                  label="Estación de servicio / Bomba"
                  required
                  hint={
                    estaciones.length === 0
                      ? 'No hay estaciones de servicio activas disponibles.'
                      : undefined
                  }
                >
                  <select
                    value={selectedEstacionId}
                    onChange={(e) => setSelectedEstacionId(e.target.value)}
                    required
                    disabled={estaciones.length === 0}
                    className={inputCls}
                  >
                    {estaciones.length === 0 ? (
                      <option value="">Sin estaciones activas disponibles</option>
                    ) : (
                      estaciones.map((est) => (
                        <option key={est.id} value={est.id}>
                          {est.nombre}
                        </option>
                      ))
                    )}
                  </select>
                </Field>

                {/* Selección de Tanque (filtrado por combustible autorizado) */}
                <Field
                  label="Tanque de suministro"
                  required
                  hint={
                    tanquesCompatibles.length === 0
                      ? 'No hay tanques activos con el tipo de combustible requerido.'
                      : tanqueSeleccionadoObj
                      ? `Nivel actual en tanque: ${tanqueSeleccionadoObj.nivelActual?.toFixed(2) || '0.00'} gal`
                      : 'Seleccione un tanque'
                  }
                >
                  <select
                    value={selectedTanqueId}
                    onChange={(e) => setSelectedTanqueId(e.target.value)}
                    required
                    disabled={tanquesCompatibles.length === 0}
                    className={inputCls}
                  >
                    {tanquesCompatibles.length === 0 && (
                      <option value="">Sin tanques para {ticketValidado.tipoCombustibleNombre}</option>
                    )}
                    {tanquesCompatibles.map((t) => (
                      <option key={t.id} value={t.id}>
                        {t.identificacion} — {t.tipoCombustibleNombre} (Disp: {t.nivelActual?.toFixed(1) || '0.0'} gal)
                      </option>
                    ))}
                  </select>
                </Field>

                {/* Cantidad Servida */}
                <Field
                  label="Galones efectivamente servidos"
                  required
                  hint={`Hasta un máximo de ${ticketValidado.cantidadAutorizada.toFixed(2)} galones`}
                >
                  <div className="flex gap-2">
                    <input
                      type="number"
                      step="any"
                      min="0.01"
                      max={ticketValidado.cantidadAutorizada}
                      value={galonesServidos}
                      onChange={(e) => setGalonesServidos(e.target.value)}
                      required
                      placeholder="0.00"
                      className={`${inputCls} font-mono num`}
                    />
                    <button
                      type="button"
                      onClick={() => setGalonesServidos(String(ticketValidado.cantidadAutorizada))}
                      className="whitespace-nowrap px-3 py-2 text-xs font-semibold rounded border border-acero/30 bg-white hover:bg-fondo text-tanque active:scale-[0.98]"
                    >
                      Total ({ticketValidado.cantidadAutorizada} gal)
                    </button>
                  </div>
                </Field>

                {/* Observaciones opcionales */}
                <Field label="Observaciones operacionales" hint="Opcional. Contador de bomba, precinto, etc.">
                  <textarea
                    rows={2}
                    value={observaciones}
                    onChange={(e) => setObservaciones(e.target.value)}
                    maxLength={500}
                    placeholder="Ej. Suministrado en bomba principal 01, medidor verificado."
                    className={inputCls}
                  />
                </Field>

                {dispatchError && (
                  <div className="rounded-md bg-peligro/10 border border-peligro/20 p-3 text-xs text-peligro">
                    {dispatchError}
                  </div>
                )}

                <div className="flex flex-col-reverse sm:flex-row justify-end gap-2 pt-2 border-t border-acero/20">
                  <button
                    type="button"
                    onClick={closeCreateModal}
                    className="min-h-[42px] rounded-md border border-acero/30 px-4 py-2 text-sm font-medium text-tinta hover:bg-fondo active:scale-[0.98]"
                  >
                    Cancelar
                  </button>
                  <button
                    type="submit"
                    disabled={
                      submittingDispatch ||
                      !selectedTanqueId ||
                      !selectedEstacionId ||
                      estaciones.length === 0 ||
                      tanquesCompatibles.length === 0 ||
                      !galonesServidos ||
                      parseFloat(galonesServidos) <= 0
                    }
                    className="flex min-h-[42px] items-center justify-center gap-2 rounded-md bg-tanque px-5 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-tanque/90 disabled:opacity-50 active:scale-[0.98]"
                  >
                    {submittingDispatch ? (
                      <>
                        <div className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                        <span>Registrando despacho...</span>
                      </>
                    ) : (
                      'Confirmar despacho de combustible'
                    )}
                  </button>
                </div>
              </form>
            )}
          </div>
        </Modal>
      )}

      {/* Modal de Éxito y Emisión de Comprobante */}
      {despachoExitoso && (
        <Modal
          title="Despacho registrado con éxito"
          onClose={() => setDespachoExitoso(null)}
        >
          <div className="space-y-4 text-center py-2">
            <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-exito/10 text-exito border border-exito/30">
              <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="20 6 9 17 4 12"/>
              </svg>
            </div>

            <div>
              <h3 className="text-lg font-bold text-tinta">¡Suministro Registrado!</h3>
              <p className="text-xs font-mono text-acero mt-1">
                Comprobante oficial: <strong className="text-tanque">#DSP-{String(despachoExitoso.despachoId).padStart(6, '0')}</strong>
              </p>
            </div>

            <div className="rounded-md border border-acero/20 bg-fondo/40 p-4 text-left space-y-2 text-xs">
              <div className="flex justify-between">
                <span className="text-acero">Ticket consumido:</span>
                <span className="font-mono font-bold text-tinta">{despachoExitoso.codigoTicket}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-acero">Galones suministrados:</span>
                <span className="font-mono font-bold text-exito text-sm">{despachoExitoso.galonesServidos.toFixed(2)} gal</span>
              </div>
              <div className="flex justify-between">
                <span className="text-acero">Estación de servicio:</span>
                <span className="font-medium text-tinta">{despachoExitoso.estacionNombre}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-acero">Tanque surtidor:</span>
                <span className="font-mono text-tinta">{despachoExitoso.tanqueIdentificacion}</span>
              </div>
              <div className="flex justify-between border-t border-acero/20 pt-2">
                <span className="text-acero">Saldo restante en tanque:</span>
                <span className="font-mono text-tinta font-semibold">{despachoExitoso.inventarioRestante.toFixed(2)} gal</span>
              </div>
            </div>

            <div className="flex flex-col sm:flex-row gap-2 pt-2">
              <button
                type="button"
                onClick={() => imprimirComprobanteDespacho(despachoExitoso, ticketValidado)}
                className="flex-1 flex min-h-[44px] items-center justify-center gap-2 rounded-md bg-tanque px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-tanque/90 active:scale-[0.98]"
              >
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <polyline points="6 9 6 2 18 2 18 9" />
                  <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" />
                  <rect width="12" height="8" x="6" y="14" />
                </svg>
                Imprimir comprobante
              </button>
              <button
                type="button"
                onClick={() => descargarComprobanteDespachoHtml(despachoExitoso, ticketValidado)}
                className="flex-1 flex min-h-[44px] items-center justify-center gap-2 rounded-md border border-acero/30 bg-white px-4 py-2.5 text-sm font-medium text-tinta hover:bg-fondo active:scale-[0.98]"
              >
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                  <polyline points="7 10 12 15 17 10" />
                  <line x1="12" x2="12" y1="15" y2="3" />
                </svg>
                Descargar HTML
              </button>
            </div>

            <div className="pt-1">
              <button
                type="button"
                onClick={() => setDespachoExitoso(null)}
                className="text-xs text-acero hover:text-tinta underline"
              >
                Finalizar y volver a la lista
              </button>
            </div>
          </div>
        </Modal>
      )}
    </PageContainer>
  )
}
