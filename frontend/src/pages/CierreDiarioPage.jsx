import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import apiRequest, { apiDownload } from '../services/api'

function todayInputValue() {
  return new Date().toISOString().slice(0, 10)
}

function formatFechaHora(value) {
  return value ? new Date(value).toLocaleString() : '—'
}

export default function CierreDiarioPage() {
  const [cierres, setCierres] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showGenerar, setShowGenerar] = useState(false)
  const [fecha, setFecha] = useState(todayInputValue())
  const [generando, setGenerando] = useState(false)
  const [generarError, setGenerarError] = useState(null)

  const [detalle, setDetalle] = useState(null)
  const [downloadingId, setDownloadingId] = useState(null)
  const [downloadError, setDownloadError] = useState(null)
  const [toastMessage, setToastMessage] = useState(null)

  async function cargarCierres() {
    try {
      const data = await apiRequest('/cierres-diarios')
      setCierres(data)
      setError(null)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/cierres-diarios')
      .then((data) => { if (!cancelado) { setCierres(data); setError(null) } })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  function openGenerar() {
    setFecha(todayInputValue())
    setGenerarError(null)
    setShowGenerar(true)
  }

  async function handleGenerar(e) {
    e.preventDefault()
    if (!fecha) {
      setGenerarError('Debe seleccionar una fecha para el cierre.')
      return
    }
    if (fecha > todayInputValue()) {
      setGenerarError('No se pueden generar cierres diarios para fechas futuras.')
      return
    }
    setGenerando(true)
    setGenerarError(null)
    try {
      await apiRequest('/cierres-diarios', {
        method: 'POST',
        body: JSON.stringify({ fecha }),
      })
      setShowGenerar(false)
      setToastMessage(`Cierre diario del ${fecha} generado exitosamente.`)
      setTimeout(() => setToastMessage(null), 5000)
      await cargarCierres()
    } catch (e) {
      setGenerarError(e.message)
    } finally {
      setGenerando(false)
    }
  }

  async function handleDescargarPdf(cierre) {
    setDownloadError(null)
    setDownloadingId(cierre.id)
    try {
      const { blob } = await apiDownload(`/cierres-diarios/${cierre.id}/pdf`)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `cierre-diario-${cierre.fecha}.pdf`
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (e) {
      setDownloadError(e.message)
    } finally {
      setDownloadingId(null)
    }
  }

  // Resumen de métricas
  const totalDespachosHist = cierres.reduce((acc, c) => acc + (c.totalDespachos || 0), 0)
  const totalVolumenHist = cierres.reduce((acc, c) => acc + (c.totalVolumenDespachado || 0), 0)
  const totalDiferenciasHist = cierres.reduce((acc, c) => acc + (c.totalDiferencias || 0), 0)

  return (
    <PageContainer title="Cierre Diario">
      {/* Toast de confirmación */}
      {toastMessage && (
        <div className="mb-4 flex items-center gap-2 rounded-md bg-exito/10 border border-exito/30 p-3 text-sm text-exito shadow-sm">
          <svg className="h-5 w-5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <span>{toastMessage}</span>
        </div>
      )}

      {/* Tarjetas KPI de Auditoría */}
      <div className="mb-6 grid grid-cols-1 gap-3 sm:grid-cols-3">
        <div className="rounded-lg border border-acero/20 bg-white p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold uppercase tracking-wider text-acero">Cierres Auditados</span>
            <span className="rounded-full bg-tanque/10 p-1.5 text-tanque">
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </span>
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-tinta">{cierres.length}</div>
          <p className="mt-1 text-xs text-acero/70">Actas oficiales registradas</p>
        </div>

        <div className="rounded-lg border border-acero/20 bg-white p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold uppercase tracking-wider text-acero">Volumen Despachado</span>
            <span className="rounded-full bg-medidor/15 p-1.5 text-medidor">
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </span>
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-tinta">
            {totalVolumenHist.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} <span className="text-xs font-normal text-acero">gal</span>
          </div>
          <p className="mt-1 text-xs text-acero/70">{totalDespachosHist} despachos en total</p>
        </div>

        <div className="rounded-lg border border-acero/20 bg-white p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold uppercase tracking-wider text-acero">Balance de Diferencias</span>
            <span className={`rounded-full p-1.5 ${totalDiferenciasHist === 0 ? 'bg-exito/15 text-exito' : 'bg-peligro/15 text-peligro'}`}>
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3" />
              </svg>
            </span>
          </div>
          <div className={`mt-2 text-2xl font-bold font-mono ${totalDiferenciasHist === 0 ? 'text-exito' : 'text-peligro'}`}>
            {totalDiferenciasHist.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} <span className="text-xs font-normal text-acero">gal</span>
          </div>
          <p className="mt-1 text-xs text-acero/70">
            {totalDiferenciasHist === 0 ? 'Inventarios perfectamente conciliados' : 'Desviación neta acumulada'}
          </p>
        </div>
      </div>

      <div className="mb-4 flex items-center justify-between">
        <div>
          <h2 className="text-sm font-semibold text-tinta">Historial de Cierres Diarios</h2>
          <p className="text-xs text-acero">Consolidación de despachos, inventarios y actas emitidas</p>
        </div>
        <button
          type="button"
          onClick={openGenerar}
          className="inline-flex items-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 shadow-sm"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
          </svg>
          Generar nuevo cierre
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {downloadError && <p className="text-sm text-peligro">{downloadError}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-sm border border-acero/20 shadow-sm">
          <table className="min-w-full divide-y divide-acero/20 text-sm">
            <thead className="bg-fondo">
              <tr>
                {['Fecha', 'Despachos', 'Volumen despachado', 'Inventario final', 'Diferencias', 'Responsable', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {cierres.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-acero/70">
                    <svg className="mx-auto h-8 w-8 text-acero/40 mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                    Sin cierres generados todavía. Haz clic en <strong>"+ Generar nuevo cierre"</strong> para conciliar una fecha.
                  </td>
                </tr>
              )}
              {cierres.map((c) => (
                <tr key={c.id} onClick={() => setDetalle(c)} className="cursor-pointer hover:bg-fondo/70 transition-colors">
                  <td className="px-4 py-3 font-medium font-mono text-tinta">{c.fecha}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{c.totalDespachos}</td>
                  <td className="px-4 py-3 font-mono num font-semibold text-tinta">{c.totalVolumenDespachado.toFixed(2)} gal</td>
                  <td className="px-4 py-3 font-mono num text-acero">{c.totalInventarioFinal.toFixed(2)} gal</td>
                  <td className="px-4 py-3">
                    {c.totalDiferencias === 0 ? (
                      <span className="inline-flex items-center gap-1 rounded-full bg-exito/10 border border-exito/20 px-2 py-0.5 text-xs font-mono font-medium text-exito">
                        0.00 gal
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 rounded-full bg-peligro/10 border border-peligro/20 px-2 py-0.5 text-xs font-mono font-semibold text-peligro">
                        {c.totalDiferencias.toFixed(2)} gal
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-acero">{c.creadoPorNombre}</td>
                  <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={() => setDetalle(c)}
                        className="inline-flex items-center gap-1.5 rounded border border-acero/30 bg-white px-2.5 py-1 text-xs font-medium text-tinta hover:bg-fondo transition-colors shadow-sm"
                        title="Ver balance y desglose por tanque"
                      >
                        <svg className="h-3.5 w-3.5 text-acero" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                        </svg>
                        Detalle
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDescargarPdf(c)}
                        disabled={downloadingId === c.id}
                        className="inline-flex items-center gap-1.5 rounded bg-tanque px-2.5 py-1 text-xs font-medium text-white hover:opacity-90 disabled:opacity-50 transition-colors shadow-sm"
                        title="Descargar Acta Oficial en PDF"
                      >
                        <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                        </svg>
                        {downloadingId === c.id ? 'Descargando...' : 'Acta PDF'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showGenerar && (
        <Modal title="Generar Cierre Diario de Operaciones" onClose={() => setShowGenerar(false)}>
          <form onSubmit={handleGenerar} className="space-y-4">
            <div className="rounded-md border border-medidor/30 bg-medidor/10 p-3 text-xs text-tinta">
              <div className="flex items-center gap-2 font-semibold text-tinta mb-1">
                <svg className="h-4 w-4 text-medidor flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                Auditoría y Conciliación de Combustible
              </div>
              <p className="text-acero leading-relaxed">
                El cierre consolida el inventario inicial, recepciones registradas, despachos efectuados e inventario final calculado por tanque para la fecha seleccionada. Genera automáticamente el <strong>Acta Oficial de Cierre Diario</strong> en PDF con validez operativa.
              </p>
            </div>

            <Field label="Fecha a conciliar" required hint="Seleccione la fecha del día que desea auditar y cerrar">
              <input
                type="date"
                value={fecha}
                onChange={(e) => setFecha(e.target.value)}
                max={todayInputValue()}
                required
                className={inputCls}
              />
            </Field>

            {generarError && (
              <div className="rounded-md border border-peligro/20 bg-peligro/10 p-3 text-sm text-peligro">
                <div className="font-semibold mb-0.5">Error al procesar el cierre</div>
                {generarError}
              </div>
            )}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setShowGenerar(false)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={generando || !fecha}
                className="inline-flex items-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50 shadow-sm"
              >
                {generando ? (
                  <>
                    <svg className="h-4 w-4 animate-spin text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
                    </svg>
                    Procesando cierre...
                  </>
                ) : (
                  'Ejecutar Cierre Diario'
                )}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {detalle && (
        <Modal title={`Acta de Cierre Diario — ${detalle.fecha}`} onClose={() => setDetalle(null)}>
          <div className="space-y-4 text-sm">
            <div className="grid grid-cols-2 gap-x-4 gap-y-3 rounded-lg border border-acero/15 bg-fondo/50 p-4">
              <div>
                <p className="text-xs text-acero font-medium">Total Despachos</p>
                <p className="font-mono num font-semibold text-tinta text-base">{detalle.totalDespachos}</p>
              </div>
              <div>
                <p className="text-xs text-acero font-medium">Volumen Total Despachado</p>
                <p className="font-mono num font-bold text-tinta text-base">{detalle.totalVolumenDespachado.toFixed(2)} gal</p>
              </div>
              <div>
                <p className="text-xs text-acero font-medium">Inventario Final Total</p>
                <p className="font-mono num font-semibold text-tinta text-base">{detalle.totalInventarioFinal.toFixed(2)} gal</p>
              </div>
              <div>
                <p className="text-xs text-acero font-medium">Diferencia Total Conciliada</p>
                <p className={`font-mono num font-bold text-base ${detalle.totalDiferencias !== 0 ? 'text-peligro' : 'text-exito'}`}>
                  {detalle.totalDiferencias.toFixed(2)} gal
                </p>
              </div>
              <div>
                <p className="text-xs text-acero font-medium">Generado por</p>
                <p className="text-tinta font-medium">{detalle.creadoPorNombre}</p>
              </div>
              <div>
                <p className="text-xs text-acero font-medium">Fecha y Hora de Emisión</p>
                <p className="text-tinta font-medium">{formatFechaHora(detalle.creadoEn)}</p>
              </div>
            </div>

            <div className="border-t border-acero/20 pt-3">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-acero">Balance Desglosado por Tanque</p>
              <div className="overflow-x-auto rounded-md border border-acero/20">
                <table className="min-w-full divide-y divide-acero/20 text-xs">
                  <thead className="bg-fondo">
                    <tr>
                      {['Tanque', 'Combustible', 'Despachos', 'Vol. Desp.', 'Vol. Recib.', 'Inv. Inicial', 'Inv. Final', 'Diferencia'].map((h) => (
                        <th key={h} className="px-2.5 py-2 text-left font-semibold text-acero uppercase">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-acero/10 bg-white">
                    {detalle.detalles.map((d) => (
                      <tr key={d.tanqueId}>
                        <td className="px-2.5 py-2 font-medium font-mono text-tinta">{d.tanqueIdentificacion}</td>
                        <td className="px-2.5 py-2 text-acero">{d.tipoCombustible}</td>
                        <td className="px-2.5 py-2 font-mono num text-acero">{d.numeroDespachos}</td>
                        <td className="px-2.5 py-2 font-mono num text-acero">{d.volumenDespachado.toFixed(2)}</td>
                        <td className="px-2.5 py-2 font-mono num text-acero">{d.volumenRecibido.toFixed(2)}</td>
                        <td className="px-2.5 py-2 font-mono num text-acero">{d.inventarioInicial.toFixed(2)}</td>
                        <td className="px-2.5 py-2 font-mono num text-acero">{d.inventarioFinal.toFixed(2)}</td>
                        <td className={`px-2.5 py-2 font-mono num font-semibold ${d.diferencias !== 0 ? 'text-peligro' : 'text-exito'}`}>
                          {d.diferencias.toFixed(2)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {downloadError && <p className="text-sm text-peligro">{downloadError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setDetalle(null)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cerrar
              </button>
              <button
                type="button"
                onClick={() => handleDescargarPdf(detalle)}
                disabled={downloadingId === detalle.id}
                className="inline-flex items-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50 shadow-sm"
              >
                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                {downloadingId === detalle.id ? 'Descargando Acta...' : 'Descargar Acta Oficial PDF'}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </PageContainer>
  )
}
