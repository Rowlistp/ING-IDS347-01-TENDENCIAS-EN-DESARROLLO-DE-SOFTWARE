import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import apiRequest from '../services/api'
import { useTanques } from '../hooks/useTanques'
import { useProveedores } from '../hooks/useProveedores'
import { validateCapacidadCombustible, validateTextoMinimo, formatRNC } from '../utils/validators'

const EMPTY_FORM = { proveedorId: '', tanqueId: '', numeroFactura: '', volumenRecibido: '', fecha: '' }

function formatFecha(value) {
  return value ? new Date(value).toLocaleString() : '—'
}

function nowLocalInputValue() {
  const now = new Date()
  now.setSeconds(0, 0)
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset())
  return now.toISOString().slice(0, 16)
}

export default function RecepcionesPage() {
  const tanques = useTanques()
  const proveedores = useProveedores()

  const [recepciones, setRecepciones] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)
  const [errors, setErrors] = useState({})
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)
  const [toastMessage, setToastMessage] = useState(null)

  const [detalle, setDetalle] = useState(null)
  const [detalleLoading, setDetalleLoading] = useState(false)
  const [detalleError, setDetalleError] = useState(null)

  const proveedorById = Object.fromEntries(proveedores.map((p) => [p.id, p]))
  const tanqueSeleccionado = tanques.find((t) => t.id === Number(form.tanqueId))

  async function cargarRecepciones() {
    try {
      const data = await apiRequest('/recepciones')
      setRecepciones(data)
      setError(null)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/recepciones')
      .then((data) => { if (!cancelado) { setRecepciones(data); setError(null) } })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  function openCreate() {
    setForm({ ...EMPTY_FORM, fecha: nowLocalInputValue() })
    setErrors({})
    setFormError(null)
    setShowForm(true)
  }

  function handleFormChange(e) {
    const { name, value } = e.target
    setForm((f) => ({ ...f, [name]: value }))

    // Limpiar error del campo modificado
    if (errors[name]) {
      setErrors((errs) => ({ ...errs, [name]: null }))
    }
  }

  function validarFormulario() {
    const errs = {}

    if (!form.proveedorId) {
      errs.proveedorId = 'Debe seleccionar un proveedor registrado.'
    }

    if (!form.tanqueId) {
      errs.tanqueId = 'Debe seleccionar el tanque receptor.'
    }

    const facturaErr = validateTextoMinimo(form.numeroFactura, 2, 'El número de factura o conduce')
    if (facturaErr) errs.numeroFactura = facturaErr

    const volErr = validateCapacidadCombustible(form.volumenRecibido, 'El volumen recibido')
    if (volErr) {
      errs.volumenRecibido = volErr
    } else if (tanqueSeleccionado) {
      const espacioDisponible = Math.max(0, tanqueSeleccionado.capacidad - tanqueSeleccionado.nivelActual)
      if (Number(form.volumenRecibido) > espacioDisponible) {
        errs.volumenRecibido = `Atención: El volumen (${form.volumenRecibido} gal) excede el espacio libre del tanque (${espacioDisponible.toFixed(2)} gal).`
      }
    }

    if (!form.fecha) {
      errs.fecha = 'Debe ingresar la fecha y hora de la recepción.'
    }

    setErrors(errs)
    return Object.keys(errs).length === 0
  }

  async function handleSubmit(e) {
    e.preventDefault()
    if (!validarFormulario()) return

    setSubmitting(true)
    setFormError(null)
    try {
      await apiRequest('/recepciones', {
        method: 'POST',
        body: JSON.stringify({
          proveedorId: Number(form.proveedorId),
          tanqueId: Number(form.tanqueId),
          numeroFactura: form.numeroFactura.trim(),
          volumenRecibido: Number(form.volumenRecibido),
          fecha: new Date(form.fecha).toISOString(),
        }),
      })
      setShowForm(false)
      setToastMessage(`Recepción de ${Number(form.volumenRecibido).toFixed(2)} gal registrada con éxito.`)
      setTimeout(() => setToastMessage(null), 5000)
      await cargarRecepciones()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function openDetalle(id) {
    setDetalle({})
    setDetalleLoading(true)
    setDetalleError(null)
    try {
      const data = await apiRequest(`/recepciones/${id}`)
      setDetalle(data)
    } catch (e) {
      setDetalleError(e.message)
    } finally {
      setDetalleLoading(false)
    }
  }

  // Métricas acumuladas
  const totalRecepciones = recepciones.length
  const totalVolumenRecibido = recepciones.reduce((acc, r) => acc + (r.volumenRecibido || 0), 0)

  return (
    <PageContainer title="Recepciones de Combustible">
      {toastMessage && (
        <div className="mb-4 flex items-center gap-2 rounded-md bg-exito/10 border border-exito/30 p-3 text-sm text-exito shadow-sm">
          <svg className="h-5 w-5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <span>{toastMessage}</span>
        </div>
      )}

      {/* KPI Cards */}
      <div className="mb-6 grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div className="rounded-lg border border-acero/20 bg-white p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold uppercase tracking-wider text-acero">Recepciones Registradas</span>
            <span className="rounded-full bg-tanque/10 p-1.5 text-tanque">
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
              </svg>
            </span>
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-tinta">{totalRecepciones}</div>
          <p className="mt-1 text-xs text-acero/70">Cargas recibidas de proveedores</p>
        </div>

        <div className="rounded-lg border border-acero/20 bg-white p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold uppercase tracking-wider text-acero">Volumen Total Abastecido</span>
            <span className="rounded-full bg-exito/15 p-1.5 text-exito">
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M9 19l3 3m0 0l3-3m-3 3V10" />
              </svg>
            </span>
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-tinta">
            {totalVolumenRecibido.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} <span className="text-xs font-normal text-acero">gal</span>
          </div>
          <p className="mt-1 text-xs text-acero/70">Combustible ingresado al inventario</p>
        </div>
      </div>

      <div className="mb-4 flex items-center justify-between">
        <div>
          <h2 className="text-sm font-semibold text-tinta">Historial de Descargas y Recepciones</h2>
          <p className="text-xs text-acero">Registro de conduces y facturas de combustible ingresado a tanques</p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="inline-flex items-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 shadow-sm"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
          </svg>
          Registrar recepción
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-sm border border-acero/20 shadow-sm">
          <table className="min-w-full divide-y divide-acero/20 text-sm">
            <thead className="bg-fondo">
              <tr>
                {['Proveedor', 'RNC', 'Factura / Conduce', 'Volumen Recibido', 'Fecha / Hora', 'Tanque', 'Acciones'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {recepciones.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-acero/70">
                    <svg className="mx-auto h-8 w-8 text-acero/40 mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
                    </svg>
                    Sin recepciones registradas. Pulsa en <strong>"+ Registrar recepción"</strong> para ingresar combustible.
                  </td>
                </tr>
              )}
              {recepciones.map((r) => (
                <tr
                  key={r.id}
                  onClick={() => openDetalle(r.id)}
                  className="cursor-pointer hover:bg-fondo/70 transition-colors"
                >
                  <td className="px-4 py-3 font-medium text-tinta">{r.proveedorNombre}</td>
                  <td className="px-4 py-3 font-mono text-acero text-xs">
                    {formatRNC(proveedorById[r.proveedorId]?.rnc) || '—'}
                  </td>
                  <td className="px-4 py-3 font-mono text-xs font-medium text-tinta">{r.numeroFactura}</td>
                  <td className="px-4 py-3 font-mono num font-semibold text-tinta">{r.volumenRecibido.toFixed(2)} gal</td>
                  <td className="px-4 py-3 text-acero text-xs">{formatFecha(r.fecha)}</td>
                  <td className="px-4 py-3 font-mono text-xs text-acero">{r.tanqueIdentificacion}</td>
                  <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                    <button
                      type="button"
                      onClick={() => openDetalle(r.id)}
                      className="inline-flex items-center gap-1.5 rounded border border-acero/30 bg-white px-2.5 py-1 text-xs font-medium text-tinta hover:bg-fondo transition-colors shadow-sm"
                      title="Ver comprobante digital de recepción"
                    >
                      <svg className="h-3.5 w-3.5 text-acero" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                      </svg>
                      Comprobante
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showForm && (
        <Modal title="Registrar Recepción de Combustible" onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field label="Proveedor de Combustible" required error={errors.proveedorId} hint="Empresa suplidora autorizada">
              <select
                name="proveedorId"
                value={form.proveedorId}
                onChange={handleFormChange}
                required
                className={inputCls}
              >
                <option value="">Seleccione un proveedor</option>
                {proveedores.map((p) => (
                  <option key={p.id} value={p.id}>{p.nombre} — RNC {formatRNC(p.rnc)}</option>
                ))}
              </select>
            </Field>

            <Field
              label="Tanque Destino"
              required
              error={errors.tanqueId}
              hint={tanqueSeleccionado ? `Capacidad: ${tanqueSeleccionado.capacidad.toLocaleString()} gal | Nivel actual: ${tanqueSeleccionado.nivelActual.toLocaleString()} gal | Espacio libre: ${(tanqueSeleccionado.capacidad - tanqueSeleccionado.nivelActual).toLocaleString()} gal` : 'Tanque donde se descargará el combustible'}
            >
              <select
                name="tanqueId"
                value={form.tanqueId}
                onChange={handleFormChange}
                required
                className={inputCls}
              >
                <option value="">Seleccione un tanque</option>
                {tanques.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.identificacion} — {t.tipoCombustibleNombre} (Disp: {(t.capacidad - t.nivelActual).toFixed(0)} gal)
                  </option>
                ))}
              </select>
            </Field>

            <Field
              label="Número de Factura o Conduce"
              required
              error={errors.numeroFactura}
              hint="Documento fiscal o de entrega emitido por el suplidor (ej: B0100004523, CND-8831)"
            >
              <input
                type="text"
                name="numeroFactura"
                value={form.numeroFactura}
                onChange={handleFormChange}
                required
                maxLength={50}
                placeholder="Ej. B0100004523 o CND-8831"
                className={inputCls}
              />
            </Field>

            <Field
              label="Volumen Recibido (Galones)"
              required
              error={errors.volumenRecibido}
              hint="Cantidad neta en galones descargada al tanque"
            >
              <input
                type="number"
                name="volumenRecibido"
                value={form.volumenRecibido}
                onChange={handleFormChange}
                required
                min="0.0001"
                step="0.01"
                placeholder="0.00"
                className={inputCls}
              />
            </Field>

            <Field label="Fecha y Hora de Recepción" required error={errors.fecha} hint="Momento en que finalizó la descarga">
              <input
                type="datetime-local"
                name="fecha"
                value={form.fecha}
                onChange={handleFormChange}
                required
                className={inputCls}
              />
            </Field>

            {formError && (
              <div className="rounded-md border border-peligro/20 bg-peligro/10 p-3 text-sm text-peligro">
                <div className="font-semibold mb-0.5">Error al registrar</div>
                {formError}
              </div>
            )}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setShowForm(false)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="inline-flex items-center gap-2 rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50 shadow-sm"
              >
                {submitting ? 'Registrando...' : 'Registrar recepción'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {detalle && (
        <Modal title={`Comprobante de Recepción ${detalle.id ? `#REC-${detalle.id.toString().padStart(5, '0')}` : ''}`} onClose={() => setDetalle(null)}>
          {detalleLoading && <p className="text-sm text-acero">Cargando comprobante...</p>}
          {detalleError && <p className="text-sm text-peligro">{detalleError}</p>}
          {!detalleLoading && !detalleError && detalle.id && (
            <div className="space-y-4 text-sm">
              {/* Encabezado Comprobante */}
              <div className="flex items-center justify-between rounded-lg border border-exito/30 bg-exito/10 p-3">
                <div className="flex items-center gap-2">
                  <svg className="h-5 w-5 text-exito" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <span className="font-semibold text-exito">Carga Ingresada a Inventario</span>
                </div>
                <span className="font-mono text-xs text-acero">ID: {detalle.id}</span>
              </div>

              {/* Tarjeta de Volumen destacado */}
              <div className="rounded-lg border border-acero/20 bg-fondo p-4 text-center">
                <span className="text-xs uppercase font-semibold text-acero tracking-wider">Volumen Total Descargado</span>
                <div className="mt-1 text-3xl font-extrabold font-mono text-tinta">
                  {detalle.volumenRecibido.toFixed(2)} <span className="text-sm font-normal text-acero">galones</span>
                </div>
              </div>

              {/* Desglose de Datos */}
              <div className="grid grid-cols-2 gap-3 rounded-lg border border-acero/15 bg-white p-3.5">
                <div>
                  <span className="text-xs text-acero block">Proveedor</span>
                  <span className="font-semibold text-tinta text-sm">{detalle.proveedorNombre}</span>
                </div>
                <div>
                  <span className="text-xs text-acero block">RNC Suplidor</span>
                  <span className="font-mono text-sm text-tinta">{formatRNC(proveedorById[detalle.proveedorId]?.rnc) || '—'}</span>
                </div>
                <div>
                  <span className="text-xs text-acero block">Factura / Conduce</span>
                  <span className="font-mono font-bold text-sm text-tinta">{detalle.numeroFactura}</span>
                </div>
                <div>
                  <span className="text-xs text-acero block">Tanque Destino</span>
                  <span className="font-mono font-medium text-sm text-tinta">{detalle.tanqueIdentificacion}</span>
                </div>
                <div className="col-span-2 border-t border-acero/10 pt-2">
                  <span className="text-xs text-acero block">Fecha y Hora de Descarga</span>
                  <span className="font-medium text-tinta text-sm">{formatFecha(detalle.fecha)}</span>
                </div>
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => window.print()}
                  className="inline-flex items-center gap-1.5 rounded-md border border-acero/30 px-3.5 py-2 text-xs font-medium text-tinta hover:bg-fondo transition-colors"
                >
                  <svg className="h-4 w-4 text-acero" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
                  </svg>
                  Imprimir comprobante
                </button>
                <button
                  type="button"
                  onClick={() => setDetalle(null)}
                  className="rounded-md bg-tanque px-4 py-2 text-xs font-medium text-white hover:opacity-90 transition-colors"
                >
                  Cerrar
                </button>
              </div>
            </div>
          )}
        </Modal>
      )}
    </PageContainer>
  )
}
