import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import apiRequest, { apiDownload } from '../services/api'
import { getUser } from '../services/auth'

function todayInputValue() {
  return new Date().toISOString().slice(0, 10)
}

function formatFechaHora(value) {
  return value ? new Date(value).toLocaleString() : '—'
}

export default function CierreDiarioPage() {
  // POST /cierres-diarios solo permite Administrador/Supervisor/Despachador
  // (CierresDiariosController.cs) — Auditor tiene acceso de solo lectura a
  // esta pantalla, así que se oculta el botón de generar cierre.
  const rolesConGenerar = ['Administrador', 'Supervisor', 'Despachador']
  const puedeGenerar = getUser()?.roles?.some((r) => rolesConGenerar.includes(r)) ?? false
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
      {puedeGenerar && (
        <div className="mb-4 flex justify-end">
          <button
            type="button"
            onClick={openGenerar}
            className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
          >
            + Generar cierre
          </button>
        </div>
      )}

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {downloadError && <p className="text-sm text-peligro">{downloadError}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-sm border border-acero/20">
          <table className="min-w-full divide-y divide-acero/20 text-sm">
            <thead className="bg-fondo">
              <tr>
                {['Fecha', 'Despachos', 'Volumen despachado', 'Inventario final', 'Diferencias', 'Creado por', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {cierres.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-6 text-center text-acero/70">
                    Sin cierres generados todavía.
                  </td>
                </tr>
              )}
              {cierres.map((c) => (
                <tr key={c.id} onClick={() => setDetalle(c)} className="cursor-pointer hover:bg-fondo">
                  <td className="px-4 py-3 font-medium font-mono text-tinta">{c.fecha}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{c.totalDespachos}</td>
                  <td className="px-4 py-3 font-mono num text-tinta">{c.totalVolumenDespachado.toFixed(2)} gal</td>
                  <td className="px-4 py-3 font-mono num text-acero">{c.totalInventarioFinal.toFixed(2)} gal</td>
                  <td className={`px-4 py-3 font-mono num font-medium ${c.totalDiferencias !== 0 ? 'text-peligro' : 'text-acero'}`}>
                    {c.totalDiferencias.toFixed(2)} gal
                  </td>
                  <td className="px-4 py-3 text-acero">{c.creadoPorNombre}</td>
                  <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                    <button
                      type="button"
                      onClick={() => handleDescargarPdf(c)}
                      disabled={!c.pdfDisponible || downloadingId === c.id}
                      className="rounded bg-acero px-2 py-1 text-xs text-white hover:opacity-90 disabled:opacity-50"
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
              <p className="rounded-md bg-peligro/10 p-2 text-sm text-peligro">{generarError}</p>
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
                className="rounded-md bg-tanque px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
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
                <p className="text-xs text-acero/70">Total despachos</p>
                <p className="font-mono num text-tinta">{detalle.totalDespachos}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Volumen total despachado</p>
                <p className="font-mono num text-tinta">{detalle.totalVolumenDespachado.toFixed(2)} gal</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Inventario final total</p>
                <p className="font-mono num text-tinta">{detalle.totalInventarioFinal.toFixed(2)} gal</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Diferencias totales</p>
                <p className={`font-mono num ${detalle.totalDiferencias !== 0 ? 'font-medium text-peligro' : 'text-tinta'}`}>
                  {detalle.totalDiferencias.toFixed(2)} gal
                </p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Creado por</p>
                <p className="text-tinta">{detalle.creadoPorNombre}</p>
              </div>
              <div>
                <p className="text-xs text-acero/70">Creado en</p>
                <p className="text-tinta">{formatFechaHora(detalle.creadoEn)}</p>
              </div>
            </div>

            <div className="border-t pt-3">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-acero">Detalle por tanque</p>
              <div className="overflow-x-auto rounded-md border border-acero/20">
                <table className="min-w-full divide-y divide-acero/20 text-xs">
                  <thead className="bg-fondo">
                    <tr>
                      {['Tanque', 'Combustible', 'Despachos', 'Vol. desp.', 'Vol. recib.', 'Inv. inicial', 'Inv. final', 'Dif.'].map((h) => (
                        <th key={h} className="px-2 py-2 text-left font-medium text-acero uppercase">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-acero/10 bg-white">
                    {detalle.detalles.map((d) => (
                      <tr key={d.tanqueId}>
                        <td className="px-2 py-2 font-medium font-mono text-tinta">{d.tanqueIdentificacion}</td>
                        <td className="px-2 py-2 text-acero">{d.tipoCombustible}</td>
                        <td className="px-2 py-2 font-mono num text-acero">{d.numeroDespachos}</td>
                        <td className="px-2 py-2 font-mono num text-acero">{d.volumenDespachado.toFixed(2)}</td>
                        <td className="px-2 py-2 font-mono num text-acero">{d.volumenRecibido.toFixed(2)}</td>
                        <td className="px-2 py-2 font-mono num text-acero">{d.inventarioInicial.toFixed(2)}</td>
                        <td className="px-2 py-2 font-mono num text-acero">{d.inventarioFinal.toFixed(2)}</td>
                        <td className={`px-2 py-2 font-mono num font-medium ${d.diferencias !== 0 ? 'text-peligro' : 'text-acero'}`}>
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
                disabled={!detalle.pdfDisponible || downloadingId === detalle.id}
                className="rounded-md bg-acero px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
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
