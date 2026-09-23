import { useEffect, useState } from 'react'
import ConfirmModal from '../components/ConfirmModal'
import Field, { inputCls, inputClsError } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
import StatusBadge from '../components/StatusBadge'
import { useAuth } from '../hooks/useAuth'
import { useDepartamentos } from '../hooks/useDepartamentos'
import { useTiposCombustible } from '../hooks/useTiposCombustible'
import apiRequest from '../services/api'
import { canManageCatalogs } from '../utils/rbac'
import {
  validatePlacaVehiculo,
  validateAnioVehiculo,
  validateCapacidadTanqueVehiculo,
  validateTextoMinimo,
} from '../utils/validators'

const EMPTY_FORM = {
  placa: '',
  ficha: '',
  marca: '',
  modelo: '',
  año: '',
  tipo: '',
  departamentoId: '',
  tipoCombustibleId: '',
  capacidadTanque: '',
  odometro: '',
}

export default function VehiculosPage() {
  const { user } = useAuth()
  const canManage = canManageCatalogs(user)
  const esAdministrador = user?.roles?.includes('Administrador') ?? false
  const [vehiculos, setVehiculos] = useState([])
  const departamentos = useDepartamentos()
  const tiposCombustible = useTiposCombustible()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [fieldErrors, setFieldErrors] = useState({})
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [deactivatingId, setDeactivatingId] = useState(null)
  const [confirmVeh, setConfirmVeh] = useState(null)
  const [actionError, setActionError] = useState(null)

  async function cargarVehiculos() {
    try {
      const data = await apiRequest('/vehiculos')
      setVehiculos(data)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelado = false
    apiRequest('/vehiculos')
      .then((data) => { if (!cancelado) setVehiculos(data) })
      .catch((e) => { if (!cancelado) setError(e.message) })
      .finally(() => { if (!cancelado) setLoading(false) })
    return () => { cancelado = true }
  }, [])

  function validateAllFields(f) {
    const errs = {}
    const placaErr = validatePlacaVehiculo(f.placa)
    if (placaErr) errs.placa = placaErr

    const fichaErr = validateTextoMinimo(f.ficha, 'La ficha o número interno', 2)
    if (fichaErr) errs.ficha = fichaErr

    const marcaErr = validateTextoMinimo(f.marca, 'La marca', 2)
    if (marcaErr) errs.marca = marcaErr

    const modeloErr = validateTextoMinimo(f.modelo, 'El modelo', 2)
    if (modeloErr) errs.modelo = modeloErr

    const anioErr = validateAnioVehiculo(f.año)
    if (anioErr) errs.año = anioErr

    const tipoErr = validateTextoMinimo(f.tipo, 'El tipo de vehículo', 2)
    if (tipoErr) errs.tipo = tipoErr

    if (!f.departamentoId) {
      errs.departamentoId = 'Debe seleccionar un departamento.'
    }

    if (!f.tipoCombustibleId) {
      errs.tipoCombustibleId = 'Debe seleccionar el tipo de combustible del vehículo.'
    }

    const capErr = validateCapacidadTanqueVehiculo(f.capacidadTanque)
    if (capErr) errs.capacidadTanque = capErr

    if (f.odometro !== '' && f.odometro !== undefined && Number(f.odometro) < 0) {
      errs.odometro = 'El odómetro no puede ser negativo.'
    }

    setFieldErrors(errs)
    return Object.keys(errs).length === 0
  }

  function handleFormChange(e) {
    const { name, value } = e.target
    setForm((f) => ({ ...f, [name]: value }))

    // Inline validation
    if (name === 'placa') {
      setFieldErrors((prev) => ({ ...prev, placa: validatePlacaVehiculo(value) }))
    } else if (name === 'ficha') {
      setFieldErrors((prev) => ({ ...prev, ficha: validateTextoMinimo(value, 'La ficha', 2) }))
    } else if (name === 'año') {
      setFieldErrors((prev) => ({ ...prev, año: validateAnioVehiculo(value) }))
    } else if (name === 'capacidadTanque') {
      setFieldErrors((prev) => ({ ...prev, capacidadTanque: validateCapacidadTanqueVehiculo(value) }))
    } else if (name === 'departamentoId') {
      setFieldErrors((prev) => ({ ...prev, departamentoId: value ? null : 'Debe seleccionar un departamento.' }))
    } else if (name === 'tipoCombustibleId') {
      setFieldErrors((prev) => ({ ...prev, tipoCombustibleId: value ? null : 'Debe seleccionar el tipo de combustible del vehículo.' }))
    }
  }

  function openCreate() {
    setEditingId(null)
    setForm(EMPTY_FORM)
    setFieldErrors({})
    setFormError(null)
    setShowForm(true)
  }

  function openEdit(veh) {
    setEditingId(veh.id)
    setForm({
      placa: veh.placa,
      ficha: veh.ficha,
      marca: veh.marca,
      modelo: veh.modelo,
      año: String(veh.año),
      tipo: veh.tipo,
      departamentoId: veh.departamentoId,
      tipoCombustibleId: veh.tipoCombustibleId ?? '',
      capacidadTanque: String(veh.capacidadTanque),
      odometro: String(veh.odometro),
      activo: veh.activo,
    })
    setFieldErrors({})
    setFormError(null)
    setShowForm(true)
  }

  const requiredFieldsFilled =
    form.placa.trim() &&
    form.ficha.trim() &&
    form.marca.trim() &&
    form.modelo.trim() &&
    form.año &&
    form.tipo.trim() &&
    form.departamentoId &&
    form.tipoCombustibleId &&
    form.capacidadTanque &&
    Object.values(fieldErrors).every((err) => !err)

  async function handleSubmit(e) {
    e.preventDefault()
    if (!validateAllFields(form)) {
      return
    }

    setSubmitting(true)
    setFormError(null)

    const payload = {
      placa: form.placa.trim().toUpperCase(),
      ficha: form.ficha.trim().toUpperCase(),
      marca: form.marca.trim(),
      modelo: form.modelo.trim(),
      año: Number(form.año),
      tipo: form.tipo.trim(),
      departamentoId: Number(form.departamentoId),
      tipoCombustibleId: Number(form.tipoCombustibleId),
      capacidadTanque: Number(form.capacidadTanque),
      odometro: form.odometro ? Number(form.odometro) : 0,
      ...(editingId ? { activo: Boolean(form.activo) } : {}),
    }

    try {
      if (editingId) {
        await apiRequest(`/vehiculos/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        })
      } else {
        await apiRequest('/vehiculos', {
          method: 'POST',
          body: JSON.stringify(payload),
        })
      }
      setShowForm(false)
      await cargarVehiculos()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  function handleDeactivate(veh) {
    setActionError(null)
    setConfirmVeh(veh)
  }

  async function handleConfirmDeactivate() {
    if (!confirmVeh) return
    const veh = confirmVeh
    setActionError(null)
    setDeactivatingId(veh.id)
    try {
      await apiRequest(`/vehiculos/${veh.id}`, { method: 'DELETE' })
      await cargarVehiculos()
      setConfirmVeh(null)
    } catch (e) {
      setActionError(e.message)
      setConfirmVeh(null)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Vehículos">
      {canManage && (
        <div className="mb-4 flex justify-end">
          <button
            type="button"
            onClick={openCreate}
            className="flex min-h-[44px] w-full sm:w-auto items-center justify-center gap-2 rounded-md bg-tanque px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-tanque/90 active:scale-[0.98]"
          >
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
            </svg>
            Nuevo vehículo
          </button>
        </div>
      )}

      {loading && <p className="text-sm text-acero">Cargando...</p>}
      {error && <p className="text-sm text-peligro">{error}</p>}
      {actionError && <p className="text-sm text-peligro">{actionError}</p>}

      {!loading && !error && (
        <ResponsiveTable
          data={vehiculos}
          keyField="id"
          emptyMessage="Sin vehículos registrados."
          columns={[
            {
              key: 'placa',
              label: 'Placa',
              primary: true,
              priority: 'high',
              render: (veh) => (
                <div>
                  <span className="font-semibold font-mono text-tinta">{veh.placa}</span>
                  <span className="ml-2 font-mono text-xs text-acero sm:hidden">Ficha: {veh.ficha}</span>
                </div>
              ),
            },
            {
              key: 'ficha',
              label: 'Ficha',
              priority: 'med',
              render: (veh) => <span className="font-mono text-acero">{veh.ficha}</span>,
            },
            {
              key: 'marcaModelo',
              label: 'Marca / Modelo',
              priority: 'high',
              render: (veh) => <span className="text-tinta">{veh.marca} {veh.modelo}</span>,
            },
            {
              key: 'año',
              label: 'Año',
              priority: 'low',
              render: (veh) => <span className="font-mono num text-acero">{veh.año}</span>,
            },
            {
              key: 'tipo',
              label: 'Tipo',
              priority: 'low',
              render: (veh) => <span className="text-acero">{veh.tipo}</span>,
            },
            {
              key: 'departamentoNombre',
              label: 'Departamento',
              priority: 'high',
              render: (veh) => <span className="text-acero">{veh.departamentoNombre}</span>,
            },
            {
              key: 'tipoCombustibleNombre',
              label: 'Combustible',
              priority: 'med',
              render: (veh) => veh.tipoCombustibleNombre
                ? <span className="text-tinta">{veh.tipoCombustibleNombre}</span>
                : <span className="text-xs italic text-advertencia">Sin definir</span>,
            },
            {
              key: 'capacidadTanque',
              label: 'Capacidad tanque',
              priority: 'low',
              render: (veh) => <span className="font-mono num text-acero">{veh.capacidadTanque} gal</span>,
            },
            {
              key: 'odometro',
              label: 'Odómetro',
              priority: 'low',
              render: (veh) => <span className="font-mono num text-acero">{veh.odometro} km</span>,
            },
            {
              key: 'activo',
              label: 'Estado',
              priority: 'high',
              render: (veh) => <StatusBadge active={veh.activo} />,
            },
          ]}
          actions={canManage ? (veh) => (
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => openEdit(veh)}
                className="flex min-h-[38px] items-center gap-1.5 rounded-sm border border-acero/30 bg-white px-3 py-1.5 text-xs font-medium text-tinta shadow-xs transition-colors hover:border-tanque hover:bg-fondo active:scale-[0.98]"
              >
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z" />
                  <path d="m15 5 4 4" />
                </svg>
                Editar
              </button>
              {veh.activo && esAdministrador && (
                <button
                  type="button"
                  onClick={() => handleDeactivate(veh)}
                  disabled={deactivatingId === veh.id}
                  className="flex min-h-[38px] items-center gap-1.5 rounded-sm bg-peligro/10 border border-peligro/30 px-3 py-1.5 text-xs font-medium text-peligro shadow-xs transition-colors hover:bg-peligro hover:text-white disabled:opacity-50 active:scale-[0.98]"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <circle cx="12" cy="12" r="10" />
                    <line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
                  </svg>
                  {deactivatingId === veh.id ? 'Desactivando...' : 'Desactivar'}
                </button>
              )}
            </div>
          ) : undefined}
        />
      )}

      {showForm && (
        <Modal title={editingId ? 'Editar vehículo' : 'Nuevo vehículo'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field
                label="Placa de matrícula"
                required
                error={fieldErrors.placa}
                hint="Ej. A123456, G987654"
              >
                <input
                  type="text"
                  name="placa"
                  value={form.placa}
                  onChange={handleFormChange}
                  required
                  maxLength={10}
                  placeholder="A123456"
                  className={`${fieldErrors.placa ? inputClsError : inputCls} font-mono`}
                />
              </Field>

              <Field
                label="Ficha interna"
                required
                error={fieldErrors.ficha}
                hint="Código de inventario o flota"
              >
                <input
                  type="text"
                  name="ficha"
                  value={form.ficha}
                  onChange={handleFormChange}
                  required
                  maxLength={20}
                  placeholder="VEH-018"
                  className={`${fieldErrors.ficha ? inputClsError : inputCls} font-mono`}
                />
              </Field>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field
                label="Marca"
                required
                error={fieldErrors.marca}
              >
                <input
                  type="text"
                  name="marca"
                  value={form.marca}
                  onChange={handleFormChange}
                  required
                  maxLength={50}
                  placeholder="Toyota, Ford, etc."
                  className={fieldErrors.marca ? inputClsError : inputCls}
                />
              </Field>

              <Field
                label="Modelo"
                required
                error={fieldErrors.modelo}
              >
                <input
                  type="text"
                  name="modelo"
                  value={form.modelo}
                  onChange={handleFormChange}
                  required
                  maxLength={50}
                  placeholder="Hilux, Ranger, etc."
                  className={fieldErrors.modelo ? inputClsError : inputCls}
                />
              </Field>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field
                label="Año de fabricación"
                required
                error={fieldErrors.año}
                hint="Rango: 1980 en adelante"
              >
                <input
                  type="number"
                  name="año"
                  value={form.año}
                  onChange={handleFormChange}
                  required
                  min={1980}
                  max={2100}
                  placeholder="2022"
                  className={`${fieldErrors.año ? inputClsError : inputCls} font-mono`}
                />
              </Field>

              <Field
                label="Tipo de carrocería"
                required
                error={fieldErrors.tipo}
              >
                <input
                  type="text"
                  name="tipo"
                  value={form.tipo}
                  onChange={handleFormChange}
                  required
                  maxLength={50}
                  placeholder="Ej. Camioneta, Sedán, Camión"
                  className={fieldErrors.tipo ? inputClsError : inputCls}
                />
              </Field>
            </div>

            <Field
              label="Departamento asignado"
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
              label="Tipo de combustible"
              required
              error={fieldErrors.tipoCombustibleId}
              hint="Combustible que usa este vehículo; se propone automáticamente al crear solicitudes."
            >
              <select
                name="tipoCombustibleId"
                value={form.tipoCombustibleId}
                onChange={handleFormChange}
                required
                className={fieldErrors.tipoCombustibleId ? inputClsError : inputCls}
              >
                <option value="">Seleccionar combustible...</option>
                {tiposCombustible
                  .filter((t) => t.activo !== false || t.id === Number(form.tipoCombustibleId))
                  .map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.nombre}{t.activo === false ? ' (inactivo)' : ''}
                    </option>
                  ))}
              </select>
            </Field>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field
                label="Capacidad de tanque (galones)"
                required
                error={fieldErrors.capacidadTanque}
                hint="Rango típico: 5 a 500 galones"
              >
                <input
                  type="number"
                  name="capacidadTanque"
                  value={form.capacidadTanque}
                  onChange={handleFormChange}
                  required
                  min="1"
                  max="500"
                  step="0.01"
                  placeholder="Ej. 20"
                  className={`${fieldErrors.capacidadTanque ? inputClsError : inputCls} font-mono`}
                />
              </Field>

              <Field
                label="Odómetro actual (km)"
                error={fieldErrors.odometro}
              >
                <input
                  type="number"
                  name="odometro"
                  value={form.odometro}
                  onChange={handleFormChange}
                  min="0"
                  step="1"
                  placeholder="0"
                  className={`${fieldErrors.odometro ? inputClsError : inputCls} font-mono`}
                />
              </Field>
            </div>

            {editingId && <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" disabled={!esAdministrador && Boolean(vehiculos.find((item) => item.id === editingId)?.activo)} checked={Boolean(form.activo)} onChange={(e) => setForm((f) => ({ ...f, activo: e.target.checked }))} />
              Vehículo activo
            </label>}

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
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear vehículo'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      <ConfirmModal
        isOpen={Boolean(confirmVeh)}
        title="¿Desactivar vehículo?"
        message={
          <>
            Está a punto de desactivar el vehículo con placa{' '}
            <strong className="font-semibold text-tinta font-mono">{confirmVeh?.placa}</strong>
            {confirmVeh?.marca && ` (${confirmVeh.marca} ${confirmVeh.modelo || ''})`}.
          </>
        }
        consequences={[
          'El vehículo no estará disponible para nuevas solicitudes de combustible.',
          'Los despachos y consumos históricos se mantendrán para fines de auditoría.',
        ]}
        type="danger"
        confirmText="Desactivar vehículo"
        cancelText="Cancelar"
        isLoading={Boolean(deactivatingId)}
        onConfirm={handleConfirmDeactivate}
        onClose={() => setConfirmVeh(null)}
      />
    </PageContainer>
  )
}
