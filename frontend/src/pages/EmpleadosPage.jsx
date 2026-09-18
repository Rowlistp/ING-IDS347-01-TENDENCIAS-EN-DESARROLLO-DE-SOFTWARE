import { useEffect, useState } from 'react'
import Field, { inputCls, inputClsError } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import { useDepartamentos } from '../hooks/useDepartamentos'
import { useUsuarios } from '../hooks/useUsuarios'
import apiRequest from '../services/api'
import {
  validateCedula,
  formatCedula,
  validateTelefonoRD,
  formatTelefonoRD,
  validateEmail,
  validateCodigoEmpleado,
  validateTextoMinimo,
} from '../utils/validators'

const EMPTY_FORM = {
  codigo: '',
  nombreCompleto: '',
  cedula: '',
  cargo: '',
  correo: '',
  telefono: '',
  departamentoId: '',
  activo: true,
  usuarioId: '',
}

export default function EmpleadosPage() {
  const [empleados, setEmpleados] = useState([])
  const departamentos = useDepartamentos()
  const usuarios = useUsuarios()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [fieldErrors, setFieldErrors] = useState({})
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [deactivatingId, setDeactivatingId] = useState(null)
  const [actionError, setActionError] = useState(null)

  const [vinculoModalEmp, setVinculoModalEmp] = useState(null)
  const [selectedUsuarioId, setSelectedUsuarioId] = useState('')
  const [vinculando, setVinculando] = useState(false)
  const [vinculoError, setVinculoError] = useState(null)

  async function cargarEmpleados() {
    try {
      const data = await apiRequest('/empleados')
      setEmpleados(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/empleados')
      .then((data) => { if (!cancelado) setEmpleados(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  function validateAllFields(f) {
    const errs = {}
    const codErr = validateCodigoEmpleado(f.codigo)
    if (codErr) errs.codigo = codErr

    const nomErr = validateTextoMinimo(f.nombreCompleto, 'El nombre completo', 3)
    if (nomErr) errs.nombreCompleto = nomErr

    const cedErr = validateCedula(f.cedula)
    if (cedErr) errs.cedula = cedErr

    const carErr = validateTextoMinimo(f.cargo, 'El cargo', 2)
    if (carErr) errs.cargo = carErr

    const corErr = validateEmail(f.correo)
    if (corErr) errs.correo = corErr

    const telErr = validateTelefonoRD(f.telefono)
    if (telErr) errs.telefono = telErr

    if (!f.departamentoId) {
      errs.departamentoId = 'Debe seleccionar un departamento.'
    }

    setFieldErrors(errs)
    return Object.keys(errs).length === 0
  }

  function handleFormChange(e) {
    const { name, value, type, checked } = e.target
    const newVal = type === 'checkbox' ? checked : value
    setForm((f) => ({ ...f, [name]: newVal }))

    // Inline validation
    if (name === 'codigo') {
      setFieldErrors((prev) => ({ ...prev, codigo: validateCodigoEmpleado(newVal) }))
    } else if (name === 'nombreCompleto') {
      setFieldErrors((prev) => ({ ...prev, nombreCompleto: validateTextoMinimo(newVal, 'El nombre completo', 3) }))
    } else if (name === 'cedula') {
      setFieldErrors((prev) => ({ ...prev, cedula: validateCedula(newVal) }))
    } else if (name === 'correo') {
      setFieldErrors((prev) => ({ ...prev, correo: validateEmail(newVal) }))
    } else if (name === 'telefono') {
      setFieldErrors((prev) => ({ ...prev, telefono: validateTelefonoRD(newVal) }))
    } else if (name === 'cargo') {
      setFieldErrors((prev) => ({ ...prev, cargo: validateTextoMinimo(newVal, 'El cargo', 2) }))
    } else if (name === 'departamentoId') {
      setFieldErrors((prev) => ({ ...prev, departamentoId: newVal ? null : 'Debe seleccionar un departamento.' }))
    }
  }

  function openCreate() {
    setEditingId(null)
    setForm(EMPTY_FORM)
    setFieldErrors({})
    setFormError(null)
    setShowForm(true)
  }

  function openEdit(emp) {
    setEditingId(emp.id)
    setForm({
      codigo: emp.codigo,
      nombreCompleto: emp.nombreCompleto,
      cedula: emp.cedula,
      cargo: emp.cargo,
      correo: emp.correo,
      telefono: emp.telefono,
      departamentoId: emp.departamentoId,
      activo: emp.activo,
      usuarioId: emp.usuarioId ? String(emp.usuarioId) : '',
    })
    setFieldErrors({})
    setFormError(null)
    setShowForm(true)
  }

  function openVinculo(emp) {
    setVinculoModalEmp(emp)
    setSelectedUsuarioId(emp.usuarioId ? String(emp.usuarioId) : '')
    setVinculoError(null)
  }

  async function handleVinculoSubmit(e) {
    e.preventDefault()
    if (!vinculoModalEmp) return
    setVinculando(true)
    setVinculoError(null)
    try {
      await apiRequest(`/empleados/${vinculoModalEmp.id}/vincular-usuario`, {
        method: 'PUT',
        body: JSON.stringify({
          usuarioId: selectedUsuarioId ? Number(selectedUsuarioId) : null,
        }),
      })
      setVinculoModalEmp(null)
      await cargarEmpleados()
    } catch (err) {
      setVinculoError(err.message)
    } finally {
      setVinculando(false)
    }
  }

  const requiredFieldsFilled =
    form.codigo.trim() &&
    form.nombreCompleto.trim() &&
    form.cedula.trim() &&
    form.cargo.trim() &&
    form.correo.trim() &&
    form.telefono.trim() &&
    form.departamentoId &&
    Object.values(fieldErrors).every((err) => !err)

  async function handleSubmit(e) {
    e.preventDefault()
    if (!validateAllFields(form)) {
      return
    }

    setSubmitting(true)
    setFormError(null)

    const payload = {
      codigo: form.codigo.trim().toUpperCase(),
      nombreCompleto: form.nombreCompleto.trim(),
      cedula: form.cedula.replace(/[\s-]/g, ''),
      cargo: form.cargo.trim(),
      correo: form.correo.trim().toLowerCase(),
      telefono: form.telefono.replace(/[\s()+-]/g, ''),
      departamentoId: Number(form.departamentoId),
      activo: form.activo,
      usuarioId: form.usuarioId ? Number(form.usuarioId) : null,
    }

    try {
      if (editingId) {
        await apiRequest(`/empleados/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        })
      } else {
        await apiRequest('/empleados', {
          method: 'POST',
          body: JSON.stringify(payload),
        })
      }
      setShowForm(false)
      await cargarEmpleados()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeactivate(emp) {
    if (!window.confirm(`¿Desactivar a ${emp.nombreCompleto}?`)) return
    setActionError(null)
    setDeactivatingId(emp.id)
    try {
      await apiRequest(`/empleados/${emp.id}`, { method: 'DELETE' })
      await cargarEmpleados()
    } catch (e) {
      setActionError(e.message)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Empleados">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          + Nuevo empleado
        </button>
      </div>

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <div className="overflow-x-auto rounded-sm border border-acero/20">
          <table className="min-w-full divide-y divide-acero/20 text-sm">
            <thead className="bg-fondo">
              <tr>
                {['Código', 'Nombre completo', 'Cédula', 'Departamento', 'Cargo', 'Correo', 'Teléfono', 'Usuario', 'Estado', 'Acciones'].map(
                  (h) => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                      {h}
                    </th>
                  )
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {empleados.length === 0 && (
                <tr>
                  <td colSpan={10} className="px-4 py-6 text-center text-acero/70">
                    Sin empleados registrados.
                  </td>
                </tr>
              )}
              {empleados.map((emp) => (
                <tr key={emp.id} className="hover:bg-fondo transition-colors">
                  <td className="px-4 py-3.5 font-mono text-acero">{emp.codigo}</td>
                  <td className="px-4 py-3.5 font-medium text-tinta">{emp.nombreCompleto}</td>
                  <td className="px-4 py-3.5 font-mono text-acero">{formatCedula(emp.cedula)}</td>
                  <td className="px-4 py-3.5 text-acero">{emp.departamentoNombre}</td>
                  <td className="px-4 py-3.5 text-acero">{emp.cargo}</td>
                  <td className="px-4 py-3.5 text-acero">{emp.correo}</td>
                  <td className="px-4 py-3.5 font-mono text-acero">{formatTelefonoRD(emp.telefono)}</td>
                  <td className="px-4 py-3.5">
                    {emp.usuarioNombre ? (
                      <span className="inline-flex items-center gap-1 font-mono text-xs font-medium text-tanque bg-tanque/10 border border-tanque/20 px-2 py-0.5 rounded">
                        👤 {emp.usuarioNombre}
                      </span>
                    ) : (
                      <span className="text-xs text-acero/60 italic">Sin vincular</span>
                    )}
                  </td>
                  <td className="px-4 py-3.5">
                    <StatusBadge active={emp.activo} />
                  </td>
                  <td className="px-4 py-3.5">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(emp)}
                        className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-acero/30 bg-white px-3 py-1.5 text-sm font-medium text-tinta transition-colors hover:border-tanque hover:bg-fondo"
                      >
                        <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z" />
                          <path d="m15 5 4 4" />
                        </svg>
                        Editar
                      </button>
                      <button
                        type="button"
                        onClick={() => openVinculo(emp)}
                        title="Vincular o cambiar cuenta de usuario"
                        className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-acero/30 bg-white px-3 py-1.5 text-sm font-medium text-tinta transition-colors hover:border-tanque hover:bg-fondo"
                      >
                        <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
                          <circle cx="9" cy="7" r="4" />
                          <path d="M19 8v6m3-3h-6" />
                        </svg>
                        {emp.usuarioId ? 'Usuario' : 'Vincular'}
                      </button>
                      {emp.activo && (
                        <button
                          type="button"
                          onClick={() => handleDeactivate(emp)}
                          disabled={deactivatingId === emp.id}
                          className="flex min-h-[38px] items-center gap-1.5 rounded-md border border-peligro/30 bg-white px-3 py-1.5 text-sm font-medium text-peligro transition-colors hover:border-peligro hover:bg-peligro/10 disabled:opacity-50"
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <circle cx="12" cy="12" r="10" />
                            <line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
                          </svg>
                          {deactivatingId === emp.id ? 'Desactivando...' : 'Desactivar'}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showForm && (
        <Modal title={editingId ? 'Editar empleado' : 'Nuevo empleado'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field
              label="Código de empleado"
              required
              error={fieldErrors.codigo}
              hint="Formato oficial: EMP-0042 o similar"
            >
              <input
                type="text"
                name="codigo"
                value={form.codigo}
                onChange={handleFormChange}
                required
                maxLength={20}
                placeholder="EMP-0042"
                className={`${fieldErrors.codigo ? inputClsError : inputCls} font-mono`}
              />
            </Field>

            <Field
              label="Nombre completo"
              required
              error={fieldErrors.nombreCompleto}
              hint="Nombres y apellidos del empleado"
            >
              <input
                type="text"
                name="nombreCompleto"
                value={form.nombreCompleto}
                onChange={handleFormChange}
                required
                maxLength={150}
                placeholder="Juan Carlos Pérez"
                className={fieldErrors.nombreCompleto ? inputClsError : inputCls}
              />
            </Field>

            <Field
              label="Cédula de Identidad"
              required
              error={fieldErrors.cedula}
              hint="11 dígitos numéricos (ej: 001-1234567-8)"
            >
              <input
                type="text"
                name="cedula"
                value={form.cedula}
                onChange={handleFormChange}
                required
                maxLength={15}
                placeholder="00112345678"
                className={`${fieldErrors.cedula ? inputClsError : inputCls} font-mono`}
              />
            </Field>

            <Field
              label="Cargo / Puesto"
              required
              error={fieldErrors.cargo}
            >
              <input
                type="text"
                name="cargo"
                value={form.cargo}
                onChange={handleFormChange}
                required
                maxLength={100}
                placeholder="Ej. Chofer de Operaciones"
                className={fieldErrors.cargo ? inputClsError : inputCls}
              />
            </Field>

            <Field
              label="Correo electrónico corporativo"
              required
              error={fieldErrors.correo}
              hint="Se utilizará para notificaciones de tickets de combustible"
            >
              <input
                type="email"
                name="correo"
                value={form.correo}
                onChange={handleFormChange}
                required
                maxLength={150}
                placeholder="jperez@empresa.com"
                className={fieldErrors.correo ? inputClsError : inputCls}
              />
            </Field>

            <Field
              label="Teléfono móvil"
              required
              error={fieldErrors.telefono}
              hint="Código de área dominicano: 809, 829 u 849"
            >
              <input
                type="text"
                name="telefono"
                value={form.telefono}
                onChange={handleFormChange}
                required
                maxLength={20}
                placeholder="8095551234"
                className={`${fieldErrors.telefono ? inputClsError : inputCls} font-mono`}
              />
            </Field>

            <Field
              label="Departamento"
              required
              error={fieldErrors.departamentoId}
            >
              <select
                name="departamentoId"
                value={form.departamentoId}
                onChange={handleFormChange}
                required
                className={fieldErrors.departamentoId ? inputClsError : inputCls}
              >
                <option value="">Seleccionar departamento...</option>
                {departamentos.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.nombre}
                  </option>
                ))}
              </select>
            </Field>

            <Field
              label="Usuario del sistema (acceso y autoservicio)"
              hint="Opcional. Vincula la cuenta de usuario para que pueda emitir solicitudes y ver sus tickets"
            >
              <select
                name="usuarioId"
                value={form.usuarioId}
                onChange={handleFormChange}
                className={inputCls}
              >
                <option value="">-- Sin usuario vinculado --</option>
                {usuarios.filter((u) => u.activo).map((u) => {
                  const vinculadoAOtro = empleados.some(
                    (e) => e.usuarioId === u.id && e.id !== editingId
                  )
                  return (
                    <option key={u.id} value={u.id} disabled={vinculadoAOtro}>
                      {u.nombreUsuario} ({u.roles?.join(', ') || 'Sin roles'}) {vinculadoAOtro ? '— (Asignado a otro empleado)' : ''}
                    </option>
                  )
                })}
              </select>
            </Field>

            {editingId && (
              <label className="flex items-center gap-2 text-sm text-tinta cursor-pointer">
                <input type="checkbox" name="activo" checked={form.activo} onChange={handleFormChange} className="rounded text-tanque focus:ring-tanque" />
                <span>Empleado activo en la organización</span>
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
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear empleado'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {vinculoModalEmp && (
        <Modal
          title={`Vincular usuario a ${vinculoModalEmp.nombreCompleto}`}
          onClose={() => setVinculoModalEmp(null)}
        >
          <form onSubmit={handleVinculoSubmit} className="space-y-4">
            <p className="text-sm text-acero">
              Al vincular a <strong className="text-tinta">{vinculoModalEmp.nombreCompleto}</strong> con un usuario del sistema, el filtrado de seguridad <code className="rounded bg-fondo px-1 py-0.5 text-xs font-mono font-semibold text-tanque">OwnerFilter</code> permitirá que un Solicitante acceda y opere de forma segura exclusivamente sobre sus propios tickets y solicitudes.
            </p>

            <Field
              label="Cuenta de usuario a vincular"
              hint="Seleccione una cuenta activa sin empleado asignado"
            >
              <select
                value={selectedUsuarioId}
                onChange={(e) => setSelectedUsuarioId(e.target.value)}
                className={inputCls}
              >
                <option value="">-- Ninguno (Desvincular cuenta) --</option>
                {usuarios.filter((u) => u.activo).map((u) => {
                  const vinculadoAOtro = empleados.some(
                    (e) => e.usuarioId === u.id && e.id !== vinculoModalEmp.id
                  )
                  return (
                    <option key={u.id} value={u.id} disabled={vinculadoAOtro}>
                      {u.nombreUsuario} ({u.roles?.join(', ') || 'Sin roles'}) {vinculadoAOtro ? '— (Asignado a otro empleado)' : ''}
                    </option>
                  )
                })}
              </select>
            </Field>

            {vinculoError && <p className="text-sm text-peligro">{vinculoError}</p>}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setVinculoModalEmp(null)}
                className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={vinculando}
                className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
              >
                {vinculando ? 'Guardando...' : 'Guardar vínculo'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
