import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import apiRequest from '../services/api'
import { useTanques } from '../hooks/useTanques'
import { useProveedores } from '../hooks/useProveedores'

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
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [detalle, setDetalle] = useState(null)
  const [detalleLoading, setDetalleLoading] = useState(false)
  const [detalleError, setDetalleError] = useState(null)

  const proveedorById = Object.fromEntries(proveedores.map((p) => [p.id, p]))

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
    cargarRecepciones()
  }, [])

  function openCreate() {
    setForm({ ...EMPTY_FORM, fecha: nowLocalInputValue() })
    setFormError(null)
    setShowForm(true)
  }

  function handleFormChange(e) {
    const { name, value } = e.target
    setForm((f) => ({ ...f, [name]: value }))
  }

  const requiredFieldsFilled =
    form.proveedorId && form.tanqueId && form.numeroFactura.trim() && form.volumenRecibido && form.fecha

  async function handleSubmit(e) {
    e.preventDefault()
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

  return (
    <PageContainer title="Recepciones de Combustible">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          + Registrar recepción
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-sm border border-acero/20">
          <table className="min-w-full divide-y divide-acero/20 text-sm">
            <thead className="bg-fondo">
              <tr>
                {['Proveedor', 'RNC', 'Factura', 'Volumen recibido', 'Fecha', 'Tanque'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {recepciones.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-acero/70">
                    Sin recepciones registradas.
                  </td>
                </tr>
              )}
              {recepciones.map((r) => (
                <tr
                  key={r.id}
                  onClick={() => openDetalle(r.id)}
                  className="cursor-pointer hover:bg-fondo"
                >
                  <td className="px-4 py-3 font-medium text-tinta">{r.proveedorNombre}</td>
                  <td className="px-4 py-3 font-mono text-acero">{proveedorById[r.proveedorId]?.rnc ?? '—'}</td>
                  <td className="px-4 py-3 font-mono text-acero">{r.numeroFactura}</td>
                  <td className="px-4 py-3 font-mono num text-tinta">{r.volumenRecibido.toFixed(2)} gal</td>
                  <td className="px-4 py-3 text-acero">{formatFecha(r.fecha)}</td>
                  <td className="px-4 py-3 font-mono text-acero">{r.tanqueIdentificacion}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showForm && (
        <Modal title="Registrar recepción de combustible" onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field label="Proveedor">
              <select
                name="proveedorId"
                value={form.proveedorId}
                onChange={handleFormChange}
                required
                className={inputCls}
              >
                <option value="">Seleccione un proveedor</option>
                {proveedores.map((p) => (
                  <option key={p.id} value={p.id}>{p.nombre} — RNC {p.rnc}</option>
                ))}
              </select>
            </Field>

            <Field label="Tanque destino">
              <select
                name="tanqueId"
                value={form.tanqueId}
                onChange={handleFormChange}
                required
                className={inputCls}
              >
                <option value="">Seleccione un tanque</option>
                {tanques.map((t) => (
                  <option key={t.id} value={t.id}>{t.identificacion} ({t.tipoCombustibleNombre})</option>
                ))}
              </select>
            </Field>

            <Field label="Número de factura">
              <input
                type="text"
                name="numeroFactura"
                value={form.numeroFactura}
                onChange={handleFormChange}
                required
                maxLength={50}
                className={inputCls}
              />
            </Field>

            <Field label="Volumen recibido (galones)">
              <input
                type="number"
                name="volumenRecibido"
                value={form.volumenRecibido}
                onChange={handleFormChange}
                required
                min="0.0001"
                step="0.0001"
                className={inputCls}
              />
            </Field>

            <Field label="Fecha">
              <input
                type="datetime-local"
                name="fecha"
                value={form.fecha}
                onChange={handleFormChange}
                required
                className={inputCls}
              />
            </Field>

            {formError && <p className="text-sm text-peligro">{formError}</p>}

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
                disabled={submitting || !requiredFieldsFilled}
                className="rounded-md bg-tanque px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50"
              >
                {submitting ? 'Registrando...' : 'Registrar recepción'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {detalle && (
        <Modal title="Detalle de recepción" onClose={() => setDetalle(null)}>
          {detalleLoading && <p className="text-sm text-acero">Cargando...</p>}
          {detalleError && <p className="text-sm text-peligro">{detalleError}</p>}
          {!detalleLoading && !detalleError && detalle.id && (
            <div className="space-y-3 text-sm">
              <div className="flex justify-between"><span className="text-acero">Proveedor</span><span className="font-medium text-tinta">{detalle.proveedorNombre}</span></div>
              <div className="flex justify-between"><span className="text-acero">RNC</span><span className="font-mono font-medium text-tinta">{proveedorById[detalle.proveedorId]?.rnc ?? '—'}</span></div>
              <div className="flex justify-between"><span className="text-acero">Factura</span><span className="font-mono font-medium text-tinta">{detalle.numeroFactura}</span></div>
              <div className="flex justify-between"><span className="text-acero">Volumen recibido</span><span className="font-mono num font-medium text-tinta">{detalle.volumenRecibido.toFixed(2)} gal</span></div>
              <div className="flex justify-between"><span className="text-acero">Fecha</span><span className="font-medium text-tinta">{formatFecha(detalle.fecha)}</span></div>
              <div className="flex justify-between"><span className="text-acero">Tanque</span><span className="font-mono font-medium text-tinta">{detalle.tanqueIdentificacion}</span></div>
            </div>
          )}
        </Modal>
      )}
    </PageContainer>
  )
}
