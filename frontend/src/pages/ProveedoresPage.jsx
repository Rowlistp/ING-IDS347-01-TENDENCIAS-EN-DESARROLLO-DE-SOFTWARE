import { useEffect, useState } from 'react'
import { useAuth } from '../hooks/useAuth'
import { ROLES } from '../utils/rbac'
import ConfirmModal from '../components/ConfirmModal'
import { apiRequest } from '../services/api'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import Field, { inputCls, inputClsError } from '../components/Field'
import { validateRnc, formatRnc, validateTextoMinimo } from '../utils/validators'

const EMPTY_FORM = {
  rnc: '',
  nombre: '',
  activo: true,
}

/* ────────────────────────────────────────────────────────────────────────────
 * Skeleton Loader — replica las 4 columnas de la tabla con animación pulse.
 * Mejora: Visibilidad del Estado del Sistema (Nielsen #1).
 * ──────────────────────────────────────────────────────────────────────────── */
function TableSkeleton({ rows = 4 }) {
  return (
    <div className="overflow-x-auto rounded-lg border border-acero/20">
      <table className="min-w-full divide-y divide-acero/20 text-sm">
        <thead className="bg-fondo">
          <tr>
            {['RNC', 'Nombre', 'Estado', 'Acciones'].map((h) => (
              <th key={h} className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-acero">
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-acero/10 bg-white">
          {Array.from({ length: rows }, (_, i) => (
            <tr key={i} className="animate-pulse">
              <td className="px-4 py-4"><div className="h-4 w-28 rounded bg-acero/15" /></td>
              <td className="px-4 py-4"><div className="h-4 w-44 rounded bg-acero/15" /></td>
              <td className="px-4 py-4"><div className="h-5 w-16 rounded-full bg-acero/15" /></td>
              <td className="px-4 py-4">
                <div className="flex gap-2.5">
                  <div className="h-9 w-20 rounded-md bg-acero/15" />
                  <div className="h-9 w-24 rounded-md bg-acero/15" />
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/* ────────────────────────────────────────────────────────────────────────────
 * Empty State — Tarjeta ilustrativa con CTA. Elimina el dead-end.
 * ──────────────────────────────────────────────────────────────────────────── */
function EmptyState({ onCreateClick }) {
  return (
    <div className="flex flex-col items-center justify-center rounded-lg border-2 border-dashed border-acero/20 bg-white px-6 py-16 text-center">
      {/* Inline SVG: building/supplier illustration */}
      <div className="mb-5 rounded-full bg-fondo p-4 text-acero/50">
        <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
          <path d="M16 20V4a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" />
          <rect x="1" y="10" width="22" height="12" rx="2" />
          <path d="M12 14v4" />
          <path d="M10 16h4" />
        </svg>
      </div>
      <h3 className="text-lg font-semibold text-tinta">Sin proveedores registrados</h3>
      <p className="mt-2 max-w-sm text-sm text-acero">
        Los proveedores son necesarios para registrar recepciones de combustible.
        Agrega el primero para comenzar a operar.
      </p>
      <button
        type="button"
        onClick={onCreateClick}
        className="mt-6 flex min-h-[44px] items-center gap-2 rounded-lg bg-tanque px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:opacity-90 active:scale-[0.98]"
      >
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
          <line x1="12" y1="5" x2="12" y2="19" />
          <line x1="5" y1="12" x2="19" y2="12" />
        </svg>
        Registrar primer proveedor
      </button>
    </div>
  )
}

export default function ProveedoresPage() {
  const { user } = useAuth()
  const esAdministrador = user?.roles?.includes(ROLES.ADMINISTRADOR)
  const [proveedores, setProveedores] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [fieldErrors, setFieldErrors] = useState({})
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [deactivatingId, setDeactivatingId] = useState(null)
  const [confirmProv, setConfirmProv] = useState(null)
  const [actionError, setActionError] = useState(null)

  // Búsqueda local
  const [search, setSearch] = useState('')

  async function cargarProveedores() {
    try {
      const data = await apiRequest('/proveedores')
      setProveedores(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/proveedores')
      .then((data) => { if (!cancelado) setProveedores(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  // Filtrado local por nombre o RNC
  const filtered = proveedores.filter((p) => {
    if (!search.trim()) return true
    const q = search.toLowerCase()
    return p.nombre.toLowerCase().includes(q) || p.rnc.toLowerCase().includes(q)
  })

  function validateAllFields(currentForm) {
    const errors = {}
    const rncErr = validateRnc(currentForm.rnc)
    if (rncErr) errors.rnc = rncErr

    const nomErr = validateTextoMinimo(currentForm.nombre, 'El nombre', 3)
    if (nomErr) errors.nombre = nomErr

    setFieldErrors(errors)
    return Object.keys(errors).length === 0
  }

  function handleFormChange(e) {
    const { name, value, type, checked } = e.target
    const newVal = type === 'checkbox' ? checked : value
    setForm((f) => ({ ...f, [name]: newVal }))

    // Validación inline al escribir
    if (name === 'rnc') {
      const err = validateRnc(newVal)
      setFieldErrors((prev) => ({ ...prev, rnc: err }))
    } else if (name === 'nombre') {
      const err = validateTextoMinimo(newVal, 'El nombre', 3)
      setFieldErrors((prev) => ({ ...prev, nombre: err }))
    }
  }

  function openCreate() {
    setEditingId(null)
    setForm(EMPTY_FORM)
    setFieldErrors({})
    setFormError(null)
    setShowForm(true)
  }

  function openEdit(p) {
    setEditingId(p.id)
    setForm({
      rnc: p.rnc,
      nombre: p.nombre,
      activo: p.activo,
    })
    setFieldErrors({})
    setFormError(null)
    setShowForm(true)
  }

  const requiredFieldsFilled = form.rnc.trim() && form.nombre.trim() && !fieldErrors.rnc && !fieldErrors.nombre

  async function handleSubmit(e) {
    e.preventDefault()
    if (!validateAllFields(form)) {
      return
    }

    setSubmitting(true)
    setFormError(null)

    const payload = {
      rnc: form.rnc.replace(/[\s-]/g, ''),
      nombre: form.nombre.trim(),
      activo: form.activo,
    }

    try {
      if (editingId) {
        await apiRequest(`/proveedores/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        })
      } else {
        await apiRequest('/proveedores', {
          method: 'POST',
          body: JSON.stringify(payload),
        })
      }
      setShowForm(false)
      await cargarProveedores()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  function handleDeactivate(p) {
    setActionError(null)
    setConfirmProv(p)
  }

  async function handleConfirmDeactivate() {
    if (!confirmProv) return
    const p = confirmProv
    setActionError(null)
    setDeactivatingId(p.id)
    try {
      await apiRequest(`/proveedores/${p.id}`, { method: 'DELETE' })
      await cargarProveedores()
      setConfirmProv(null)
    } catch (e) {
      // El backend responde 409 PROVEEDOR_CON_RECEPCIONES si tiene recepciones
      // históricas — el mensaje ya viene claro desde el backend, se muestra tal cual.
      setActionError(e.message)
      setConfirmProv(null)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Proveedores">
      {/* Toolbar: contador + búsqueda + botón primario */}
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          {!loading && !error && (
            <span className="text-sm text-acero">
              Total: <span className="font-semibold text-tinta">{proveedores.length}</span> {proveedores.length === 1 ? 'proveedor' : 'proveedores'}
            </span>
          )}
          {/* Búsqueda local */}
          {!loading && !error && proveedores.length > 0 && (
            <div className="relative">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="absolute left-2.5 top-1/2 -translate-y-1/2 text-acero/50">
                <circle cx="11" cy="11" r="8" />
                <path d="m21 21-4.3-4.3" />
              </svg>
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Buscar por nombre o RNC…"
                className="h-10 rounded-lg border border-acero/20 bg-white pl-9 pr-3 text-sm text-tinta placeholder:text-acero/50 focus:border-tanque focus:outline-none focus:ring-1 focus:ring-tanque/30"
              />
            </div>
          )}
        </div>

        <button
          type="button"
          onClick={openCreate}
          className="flex min-h-[44px] w-full sm:w-auto items-center justify-center gap-2 rounded-lg bg-tanque px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:opacity-90 active:scale-[0.98]"
        >
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Nuevo proveedor
        </button>
      </div>

      {/* Error banners */}
      {error && (
        <div className="mb-4 rounded-lg border border-peligro/20 bg-peligro/5 px-4 py-3 text-sm text-peligro">
          {error}
        </div>
      )}
      {actionError && (
        <div className="mb-4 rounded-lg border border-peligro/20 bg-peligro/5 px-4 py-3 text-sm text-peligro">
          {actionError}
        </div>
      )}

      {/* Skeleton loading state */}
      {loading && <TableSkeleton />}

      {/* Data table */}
      {!loading && !error && proveedores.length > 0 && (
        <ResponsiveTable
          data={filtered}
          keyField="id"
          emptyMessage={`No se encontraron resultados para "${search}"`}
          columns={[
            {
              key: 'rnc',
              label: 'RNC',
              priority: 'high',
              render: (p) => <span className="font-mono text-tinta">{formatRnc(p.rnc)}</span>,
            },
            {
              key: 'nombre',
              label: 'Nombre',
              primary: true,
              priority: 'high',
              render: (p) => <span className="font-semibold text-tinta">{p.nombre}</span>,
            },
            {
              key: 'activo',
              label: 'Estado',
              priority: 'high',
              render: (p) => <StatusBadge active={p.activo} />,
            },
          ]}
          actions={(p) => (
            <div className="flex flex-wrap gap-2.5">
              <button
                type="button"
                onClick={() => openEdit(p)}
                className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-acero/30 bg-white px-3.5 py-2 text-sm font-medium text-tinta shadow-xs transition-colors hover:border-tanque hover:bg-fondo active:scale-[0.98]"
              >
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z" />
                  <path d="m15 5 4 4" />
                </svg>
                Editar
              </button>
              {p.activo && esAdministrador && (
                <button
                  type="button"
                  onClick={() => handleDeactivate(p)}
                  disabled={deactivatingId === p.id}
                  className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-peligro/30 bg-white px-3.5 py-2 text-sm font-medium text-peligro shadow-xs transition-colors hover:border-peligro hover:bg-peligro/10 disabled:opacity-50 active:scale-[0.98]"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <circle cx="12" cy="12" r="10" />
                    <line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
                  </svg>
                  {deactivatingId === p.id ? 'Desactivando…' : 'Desactivar'}
                </button>
              )}
            </div>
          )}
        />
      )}

      {/* Empty state */}
      {!loading && !error && proveedores.length === 0 && (
        <EmptyState onCreateClick={openCreate} />
      )}

      {showForm && (
        <Modal title={editingId ? 'Editar proveedor' : 'Nuevo proveedor'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field
              label="RNC"
              required
              error={fieldErrors.rnc}
              hint="9 dígitos (empresa) u 11 dígitos (persona física/cédula)"
            >
              <input
                type="text"
                name="rnc"
                value={form.rnc}
                onChange={handleFormChange}
                required
                maxLength={13}
                placeholder="101000001 o 00100000001"
                className={`${fieldErrors.rnc ? inputClsError : inputCls} font-mono`}
              />
            </Field>

            <Field
              label="Nombre del Proveedor"
              required
              error={fieldErrors.nombre}
              hint="Mínimo 3 caracteres"
            >
              <input
                type="text"
                name="nombre"
                value={form.nombre}
                onChange={handleFormChange}
                required
                maxLength={150}
                placeholder="Ej. Distribuidora Nacional de Combustibles"
                className={fieldErrors.nombre ? inputClsError : inputCls}
              />
            </Field>

            {editingId && (
              <label className="flex items-center gap-2 text-sm text-tinta cursor-pointer">
                <input type="checkbox" name="activo" disabled={!esAdministrador && Boolean(proveedores.find((item) => item.id === editingId)?.activo)} checked={form.activo} onChange={handleFormChange} className="rounded text-tanque focus:ring-tanque" />
                <span>Proveedor activo en el sistema</span>
              </label>
            )}

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
                className="rounded-md bg-tanque px-4 py-2 text-sm text-white hover:opacity-90 disabled:opacity-50 font-medium"
              >
                {submitting ? 'Guardando…' : editingId ? 'Guardar cambios' : 'Crear proveedor'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      <ConfirmModal
        isOpen={Boolean(confirmProv)}
        title="¿Desactivar proveedor suplidor?"
        message={
          <>
            Está a punto de desactivar al suplidor{' '}
            <strong className="font-semibold text-tinta">{confirmProv?.nombre}</strong>
            {confirmProv?.rnc && <span className="font-mono text-acero"> (RNC: {formatRnc(confirmProv.rnc)})</span>}.
          </>
        }
        consequences={[
          'El suplidor no aparecerá como opción para registrar nuevas recepciones de combustible.',
          'Las recepciones y descargas históricas se conservarán intactas para auditoría.',
        ]}
        type="danger"
        confirmText="Desactivar proveedor"
        cancelText="Cancelar"
        isLoading={Boolean(deactivatingId)}
        onConfirm={handleConfirmDeactivate}
        onClose={() => setConfirmProv(null)}
      />
    </PageContainer>
  )
}
