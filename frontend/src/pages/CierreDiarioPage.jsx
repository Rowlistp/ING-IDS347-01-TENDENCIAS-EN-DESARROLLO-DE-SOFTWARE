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
    cargarCierres()
  }, [])

  function openGenerar() {
    setFecha(todayInputValue())
    setGenerarError(null)
    setShowGenerar(true)
  }

  async function handleGenerar(e) {
    e.preventDefault()
    setGenerando(true)
    setGenerarError(null)
    try {
      await apiRequest('/cierres-diarios', {
        method: 'POST',
        body: JSON.stringify({ fecha }),
      })
      setShowGenerar(false)
      await cargarCierres()
    } catch (e) {
      // El backend ya devuelve un mensaje claro y distinto por cada código
      // (FECHA_FUTURA, SIN_DESPACHOS, CIERRE_YA_EXISTE) — se muestra tal cual.
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

  return (
    <PageContainer title="Cierre Diario">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openGenerar}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          + Generar cierre
        </button>
      </div>

      {loading && <p className="text-sm text-gray-500">Cargando...</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}
      {downloadError && <p className="text-sm text-red-600">{downloadError}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-lg border border-gray-200">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                {['Fecha', 'Despachos', 'Volumen despachado', 'Inventario final', 'Diferencias', 'Creado por', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {cierres.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-6 text-center text-gray-400">
                    Sin cierres generados todavía.
                  </td>
                </tr>
              )}
              {cierres.map((c) => (
                <tr key={c.id} onClick={() => setDetalle(c)} className="cursor-pointer hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-800">{c.fecha}</td>
                  <td className="px-4 py-3 text-gray-600">{c.totalDespachos}</td>
                  <td className="px-4 py-3 text-gray-800">{c.totalVolumenDespachado.toFixed(2)} gal</td>
                  <td className="px-4 py-3 text-gray-600">{c.totalInventarioFinal.toFixed(2)} gal</td>
                  <td className={`px-4 py-3 font-medium ${c.totalDiferencias !== 0 ? 'text-red-600' : 'text-gray-600'}`}>
                    {c.totalDiferencias.toFixed(2)} gal
                  </td>
                  <td className="px-4 py-3 text-gray-600">{c.creadoPorNombre}</td>
                  <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                    <button
                      type="button"
                      onClick={() => handleDescargarPdf(c)}
                      disabled={!c.pdfDisponible || downloadingId === c.id}
                      className="rounded bg-gray-600 px-2 py-1 text-xs text-white hover:bg-gray-700 disabled:opacity-50"
                    >
                      {downloadingId === c.id ? 'Descargando...' : 'PDF'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showGenerar && (
        <Modal title="Generar cierre diario" onClose={() => setShowGenerar(false)}>
          <form onSubmit={handleGenerar} className="space-y-4">
            <Field label="Fecha del cierre">
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
              <p className="rounded-md bg-red-50 p-2 text-sm text-red-700">{generarError}</p>
            )}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setShowGenerar(false)}
                className="rounded-md border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={generando || !fecha}
                className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
              >
                {generando ? 'Generando...' : 'Generar cierre'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {detalle && (
        <Modal title={`Cierre diario — ${detalle.fecha}`} onClose={() => setDetalle(null)}>
          <div className="space-y-4 text-sm">
            <div className="grid grid-cols-2 gap-x-4 gap-y-2">
              <div>
                <p className="text-xs text-gray-400">Total despachos</p>
                <p className="text-gray-700">{detalle.totalDespachos}</p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Volumen total despachado</p>
                <p className="text-gray-700">{detalle.totalVolumenDespachado.toFixed(2)} gal</p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Inventario final total</p>
                <p className="text-gray-700">{detalle.totalInventarioFinal.toFixed(2)} gal</p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Diferencias totales</p>
                <p className={detalle.totalDiferencias !== 0 ? 'font-medium text-red-600' : 'text-gray-700'}>
                  {detalle.totalDiferencias.toFixed(2)} gal
                </p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Creado por</p>
                <p className="text-gray-700">{detalle.creadoPorNombre}</p>
              </div>
              <div>
                <p className="text-xs text-gray-400">Creado en</p>
                <p className="text-gray-700">{formatFechaHora(detalle.creadoEn)}</p>
              </div>
            </div>

            <div className="border-t pt-3">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-gray-500">Detalle por tanque</p>
              <div className="overflow-x-auto rounded-md border border-gray-200">
                <table className="min-w-full divide-y divide-gray-200 text-xs">
                  <thead className="bg-gray-50">
                    <tr>
                      {['Tanque', 'Combustible', 'Despachos', 'Vol. desp.', 'Vol. recib.', 'Inv. inicial', 'Inv. final', 'Dif.'].map((h) => (
                        <th key={h} className="px-2 py-2 text-left font-medium text-gray-500 uppercase">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 bg-white">
                    {detalle.detalles.map((d) => (
                      <tr key={d.tanqueId}>
                        <td className="px-2 py-2 font-medium text-gray-800">{d.tanqueIdentificacion}</td>
                        <td className="px-2 py-2 text-gray-600">{d.tipoCombustible}</td>
                        <td className="px-2 py-2 text-gray-600">{d.numeroDespachos}</td>
                        <td className="px-2 py-2 text-gray-600">{d.volumenDespachado.toFixed(2)}</td>
                        <td className="px-2 py-2 text-gray-600">{d.volumenRecibido.toFixed(2)}</td>
                        <td className="px-2 py-2 text-gray-600">{d.inventarioInicial.toFixed(2)}</td>
                        <td className="px-2 py-2 text-gray-600">{d.inventarioFinal.toFixed(2)}</td>
                        <td className={`px-2 py-2 font-medium ${d.diferencias !== 0 ? 'text-red-600' : 'text-gray-600'}`}>
                          {d.diferencias.toFixed(2)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {downloadError && <p className="text-sm text-red-600">{downloadError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setDetalle(null)}
                className="rounded-md border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                Cerrar
              </button>
              <button
                type="button"
                onClick={() => handleDescargarPdf(detalle)}
                disabled={!detalle.pdfDisponible || downloadingId === detalle.id}
                className="rounded-md bg-gray-600 px-4 py-2 text-sm text-white hover:bg-gray-700 disabled:opacity-50"
              >
                {downloadingId === detalle.id ? 'Descargando...' : 'Descargar PDF'}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </PageContainer>
  )
}
