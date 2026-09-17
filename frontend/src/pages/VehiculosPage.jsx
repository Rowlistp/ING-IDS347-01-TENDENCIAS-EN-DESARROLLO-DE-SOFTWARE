import { useEffect, useState } from 'react'
import Field, { inputCls } from '../components/Field'
import Modal from '../components/Modal'
import PageContainer from '../components/PageContainer'
import StatusBadge from '../components/StatusBadge'
import { useDepartamentos } from '../hooks/useDepartamentos'
import apiRequest from '../services/api'

const EMPTY_FORM = {
  placa: '',
  ficha: '',
  marca: '',
  modelo: '',
  año: '',
  tipo: '',
  departamentoId: '',
  capacidadTanque: '',
  odometro: '',
}

export default function VehiculosPage() {
  const [vehiculos, setVehiculos] = useState([])
  const departamentos = useDepartamentos()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState(null)

  const [deactivatingId, setDeactivatingId] = useState(null)
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
    cargarVehiculos()
  }, [])

  function handleFormChange(e) {
    const { name, value } = e.target
    setForm((f) => ({ ...f, [name]: value }))
  }

  function openCreate() {
    setEditingId(null)
    setForm(EMPTY_FORM)
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
      capacidadTanque: String(veh.capacidadTanque),
      odometro: String(veh.odometro),
    })
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
    form.capacidadTanque

  async function handleSubmit(e) {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)

    const payload = {
      placa: form.placa.trim(),
      ficha: form.ficha.trim(),
      marca: form.marca.trim(),
      modelo: form.modelo.trim(),
      año: Number(form.año),
      tipo: form.tipo.trim(),
      departamentoId: Number(form.departamentoId),
      capacidadTanque: Number(form.capacidadTanque),
      odometro: form.odometro ? Number(form.odometro) : 0,
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

  async function handleDeactivate(veh) {
    if (!window.confirm(`¿Desactivar el vehículo ${veh.placa}?`)) return
    setActionError(null)
    setDeactivatingId(veh.id)
    try {
      await apiRequest(`/vehiculos/${veh.id}`, { method: 'DELETE' })
      await cargarVehiculos()
    } catch (e) {
      setActionError(e.message)
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <PageContainer title="Vehículos">
      <div className="mb-4 flex justify-end">
        <button
          type="button"
          onClick={openCreate}
          className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90"
        >
          + Nuevo vehículo
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
                {[
                  'Placa',
                  'Ficha',
                  'Marca',
                  'Modelo',
                  'Año',
                  'Tipo',
                  'Departamento',
                  'Capacidad tanque',
                  'Odómetro',
                  'Estado',
                  'Acciones',
                ].map((h) => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-acero/10 bg-white">
              {vehiculos.length === 0 && (
                <tr>
                  <td colSpan={11} className="px-4 py-6 text-center text-acero/70">
                    Sin vehículos registrados.
                  </td>
                </tr>
              )}
              {vehiculos.map((veh) => (
                <tr key={veh.id} className="hover:bg-fondo">
                  <td className="px-4 py-3 font-medium font-mono text-tinta">{veh.placa}</td>
                  <td className="px-4 py-3 font-mono text-acero">{veh.ficha}</td>
                  <td className="px-4 py-3 text-acero">{veh.marca}</td>
                  <td className="px-4 py-3 text-acero">{veh.modelo}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{veh.año}</td>
                  <td className="px-4 py-3 text-acero">{veh.tipo}</td>
                  <td className="px-4 py-3 text-acero">{veh.departamentoNombre}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{veh.capacidadTanque}</td>
                  <td className="px-4 py-3 font-mono num text-acero">{veh.odometro}</td>
                  <td className="px-4 py-3">
                    <StatusBadge active={veh.activo} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEdit(veh)}
                        className="rounded bg-tanque px-2 py-1 text-xs text-white hover:opacity-90"
                      >
                        Editar
                      </button>
                      {veh.activo && (
                        <button
                          type="button"
                          onClick={() => handleDeactivate(veh)}
                          disabled={deactivatingId === veh.id}
                          className="rounded bg-peligro px-2 py-1 text-xs text-white hover:opacity-90 disabled:opacity-50"
                        >
                          {deactivatingId === veh.id ? 'Desactivando...' : 'Desactivar'}
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
        <Modal title={editingId ? 'Editar vehículo' : 'Nuevo vehículo'} onClose={() => setShowForm(false)}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Field label="Placa">
              <input
                type="text"
                name="placa"
                value={form.placa}
                onChange={handleFormChange}
                required
                maxLength={10}
                className={inputCls}
              />
            </Field>
            <Field label="Ficha">
              <input
                type="text"
                name="ficha"
                value={form.ficha}
                onChange={handleFormChange}
                required
                maxLength={20}
                className={inputCls}
              />
            </Field>
            <Field label="Marca">
              <input
                type="text"
                name="marca"
                value={form.marca}
                onChange={handleFormChange}
                required
                maxLength={50}
                className={inputCls}
              />
            </Field>
            <Field label="Modelo">
              <input
                type="text"
                name="modelo"
                value={form.modelo}
                onChange={handleFormChange}
                required
                maxLength={50}
                className={inputCls}
              />
            </Field>
            <Field label="Año">
              <input
                type="number"
                name="año"
                value={form.año}
                onChange={handleFormChange}
                required
                min={1990}
                max={2100}
                className={inputCls}
              />
            </Field>
            <Field label="Tipo">
              <input
                type="text"
                name="tipo"
                value={form.tipo}
                onChange={handleFormChange}
                required
                maxLength={50}
                placeholder="Ej. Camión, Sedán, Motocicleta"
                className={inputCls}
              />
            </Field>
            <Field label="Departamento">
              <select
                name="departamentoId"
                value={form.departamentoId}
                onChange={handleFormChange}
                required
                className={inputCls}
              >
                <option value="">Seleccionar...</option>
                {departamentos.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.nombre}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Capacidad de tanque (gal)">
              <input
                type="number"
                name="capacidadTanque"
                value={form.capacidadTanque}
                onChange={handleFormChange}
                required
                min="0.0001"
                step="0.0001"
                className={inputCls}
              />
            </Field>
            <Field label="Odómetro">
              <input
                type="number"
                name="odometro"
                value={form.odometro}
                onChange={handleFormChange}
                min="0"
                step="0.0001"
                placeholder="0"
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
                {submitting ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Crear vehículo'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </PageContainer>
  )
}
