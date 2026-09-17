import { useEffect, useState } from 'react'
import PageContainer from '../components/PageContainer'
import apiRequest, { apiDownload } from '../services/api'
import { useDepartamentos } from '../hooks/useDepartamentos'
import { useEmpleados } from '../hooks/useEmpleados'
import { useTanques } from '../hooks/useTanques'
import { useVehiculos } from '../hooks/useVehiculos'

const TAMANO_PAGINA = 20

// El backend valida contra estos 5 tipos (ReporteService.TiposValidos).
const TIPOS_REPORTE = [
  { value: 'solicitudes', label: 'Solicitudes de combustible' },
  { value: 'despachos', label: 'Despachos' },
  { value: 'inventario', label: 'Movimientos de inventario' },
  { value: 'cierres', label: 'Cierres diarios' },
  { value: 'tickets', label: 'Tickets' },
]

// tanqueId solo aplica a estos dos tipos en el backend real (ReporteService.cs);
// solicitudes/cierres/tickets lo ignorarían aunque se enviara.
const TIPOS_CON_FILTRO_TANQUE = ['despachos', 'inventario']

// empleadoId/vehiculoId/departamentoId solo aplican a estos tres tipos en el
// backend real; inventario/cierres no tienen esos campos y lo ignorarían.
const TIPOS_CON_FILTRO_PERSONA = ['solicitudes', 'despachos', 'tickets']

const COLUMNAS_POR_TIPO = {
  solicitudes: [
    { key: 'id', label: '#' },
    { key: 'fechaSolicitud', label: 'Fecha', fecha: true },
    { key: 'empleado', label: 'Empleado' },
    { key: 'vehiculo', label: 'Vehículo', mono: true },
    { key: 'departamento', label: 'Departamento' },
    { key: 'tipoCombustible', label: 'Combustible' },
    { key: 'cantidadSolicitada', label: 'Solicitado', num: true },
    { key: 'cantidadAutorizada', label: 'Autorizado', num: true },
    { key: 'estado', label: 'Estado' },
  ],
  despachos: [
    { key: 'id', label: '#' },
    { key: 'fecha', label: 'Fecha', fecha: true },
    { key: 'hora', label: 'Hora' },
    { key: 'codigoTicket', label: 'Ticket', mono: true },
    { key: 'empleado', label: 'Empleado' },
    { key: 'vehiculo', label: 'Vehículo', mono: true },
    { key: 'galonesServidos', label: 'Galones', num: true },
    { key: 'tanque', label: 'Tanque', mono: true },
    { key: 'estacion', label: 'Estación' },
    { key: 'operador', label: 'Operador' },
    { key: 'inventarioRestante', label: 'Inv. restante', num: true },
  ],
  inventario: [
    { key: 'id', label: '#' },
    { key: 'fechaHora', label: 'Fecha/Hora', fecha: true },
    { key: 'tanque', label: 'Tanque', mono: true },
    { key: 'tipoCombustible', label: 'Combustible' },
    { key: 'tipo', label: 'Tipo movimiento' },
    { key: 'volumen', label: 'Volumen', num: true },
    { key: 'referenciaOperacion', label: 'Referencia', mono: true },
  ],
  cierres: [
    { key: 'id', label: '#' },
    { key: 'fecha', label: 'Fecha', fecha: true },
    { key: 'totalDespachos', label: 'Despachos', num: true },
    { key: 'volumenDespachado', label: 'Vol. despachado', num: true },
    { key: 'inventarioFinal', label: 'Inv. final', num: true },
    { key: 'diferencias', label: 'Diferencias', num: true },
    { key: 'creadoPor', label: 'Creado por' },
  ],
  tickets: [
    { key: 'id', label: '#' },
    { key: 'codigo', label: 'Código', mono: true },
    { key: 'fechaCreacion', label: 'Fecha creación', fecha: true },
    { key: 'fechaVencimiento', label: 'Fecha vencimiento', fecha: true },
    { key: 'estado', label: 'Estado' },
    { key: 'cantidadAutorizada', label: 'Autorizado', num: true },
    { key: 'empleado', label: 'Empleado' },
    { key: 'vehiculo', label: 'Vehículo', mono: true },
    { key: 'departamento', label: 'Departamento' },
  ],
}

const FORMATOS = [
  { value: 'csv', label: 'CSV', ext: 'csv' },
  { value: 'excel', label: 'Excel', ext: 'xlsx' },
  { value: 'pdf', label: 'PDF', ext: 'pdf' },
]

const FILTROS_VACIOS = { fechaDesde: '', fechaHasta: '', tanqueId: '', empleadoId: '', vehiculoId: '', departamentoId: '' }

function formatCelda(col, value) {
  if (value === null || value === undefined || value === '') return '—'
  if (col.fecha) return new Date(value).toLocaleString()
  if (col.num) return Number(value).toFixed(4)
  return String(value)
}

export default function ReportesPage() {
  const tanques = useTanques()
  const empleados = useEmpleados()
  const vehiculos = useVehiculos()
  const departamentos = useDepartamentos()

  const [tipo, setTipo] = useState('solicitudes')
  const [pagina, setPagina] = useState(1)
  const [filtros, setFiltros] = useState(FILTROS_VACIOS)
  const [filtrosPendientes, setFiltrosPendientes] = useState(FILTROS_VACIOS)

  const [respuesta, setRespuesta] = useState(null)
  const [claveCargada, setClaveCargada] = useState(null)
  const [error, setError] = useState(null)

  const [descargando, setDescargando] = useState(null)
  const [descargaError, setDescargaError] = useState(null)

  const claveActual = JSON.stringify({ tipo, pagina, ...filtros })
  const loading = claveCargada !== claveActual

  useEffect(() => {
    let cancelado = false
    const params = new URLSearchParams({ tipo, pagina: String(pagina), tamanoPagina: String(TAMANO_PAGINA) })
    if (filtros.fechaDesde) params.set('fechaDesde', filtros.fechaDesde)
    if (filtros.fechaHasta) params.set('fechaHasta', filtros.fechaHasta)
    if (filtros.tanqueId && TIPOS_CON_FILTRO_TANQUE.includes(tipo)) params.set('tanqueId', filtros.tanqueId)
    if (TIPOS_CON_FILTRO_PERSONA.includes(tipo)) {
      if (filtros.empleadoId) params.set('empleadoId', filtros.empleadoId)
      if (filtros.vehiculoId) params.set('vehiculoId', filtros.vehiculoId)
      if (filtros.departamentoId) params.set('departamentoId', filtros.departamentoId)
    }

    apiRequest(`/reportes?${params.toString()}`)
      .then((data) => {
        if (cancelado) return
        setRespuesta(data)
        setClaveCargada(claveActual)
        setError(null)
      })
      .catch((e) => { if (!cancelado) setError(e.message) })
    return () => { cancelado = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [claveActual])

  function handleTipoChange(e) {
    setTipo(e.target.value)
    setPagina(1)
    setFiltros(FILTROS_VACIOS)
    setFiltrosPendientes(FILTROS_VACIOS)
  }

  function handleFiltroChange(e) {
    const { name, value } = e.target
    setFiltrosPendientes((f) => ({ ...f, [name]: value }))
  }

  function handleAplicarFiltros(e) {
    e.preventDefault()
    setPagina(1)
    setFiltros(filtrosPendientes)
  }

  function handleLimpiarFiltros() {
    setFiltrosPendientes(FILTROS_VACIOS)
    setFiltros(FILTROS_VACIOS)
    setPagina(1)
  }

  async function handleExportar(formato) {
    setDescargaError(null)
    setDescargando(formato.value)
    try {
      const params = new URLSearchParams({ tipo, formato: formato.value })
      if (filtros.fechaDesde) params.set('fechaDesde', filtros.fechaDesde)
      if (filtros.fechaHasta) params.set('fechaHasta', filtros.fechaHasta)
      if (filtros.tanqueId && TIPOS_CON_FILTRO_TANQUE.includes(tipo)) params.set('tanqueId', filtros.tanqueId)
      if (TIPOS_CON_FILTRO_PERSONA.includes(tipo)) {
        if (filtros.empleadoId) params.set('empleadoId', filtros.empleadoId)
        if (filtros.vehiculoId) params.set('vehiculoId', filtros.vehiculoId)
        if (filtros.departamentoId) params.set('departamentoId', filtros.departamentoId)
      }

      // El backend no expone Content-Disposition vía CORS (mismo hallazgo que en
      // Tickets/CierreDiario), así que el nombre del archivo se arma en el cliente.
      const { blob } = await apiDownload(`/reportes/exportar?${params.toString()}`)
      const fecha = new Date().toISOString().slice(0, 10)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `reporte-${tipo}-${fecha}.${formato.ext}`
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (e) {
      setDescargaError(e.message)
    } finally {
      setDescargando(null)
    }
  }

  const columnas = COLUMNAS_POR_TIPO[tipo]
  const totalPaginas = respuesta ? Math.max(1, Math.ceil(respuesta.total / respuesta.tamanoPagina)) : 1

  return (
    <PageContainer title="Reportes">
      <p className="mb-4 text-sm text-acero">
        Los filtros disponibles varían según el tipo de reporte: fecha aplica a todos; tanque solo a despachos e
        inventario; empleado, vehículo y departamento a solicitudes, despachos y tickets. Solo se muestran los
        filtros que el backend soporta de verdad para el tipo seleccionado.
      </p>

      <div className="mb-4">
        <label className="mb-1 block text-sm font-medium text-acero">Tipo de reporte</label>
        <select
          value={tipo}
          onChange={handleTipoChange}
          className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
        >
          {TIPOS_REPORTE.map((t) => (
            <option key={t.value} value={t.value}>{t.label}</option>
          ))}
        </select>
      </div>

      <form onSubmit={handleAplicarFiltros} className="mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label className="mb-1 block text-sm font-medium text-acero">Desde</label>
          <input
            type="date"
            name="fechaDesde"
            value={filtrosPendientes.fechaDesde}
            onChange={handleFiltroChange}
            className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-acero">Hasta</label>
          <input
            type="date"
            name="fechaHasta"
            value={filtrosPendientes.fechaHasta}
            onChange={handleFiltroChange}
            className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
          />
        </div>
        {TIPOS_CON_FILTRO_TANQUE.includes(tipo) && (
          <div>
            <label className="mb-1 block text-sm font-medium text-acero">Tanque</label>
            <select
              name="tanqueId"
              value={filtrosPendientes.tanqueId}
              onChange={handleFiltroChange}
              className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
            >
              <option value="">Todos</option>
              {tanques.map((t) => (
                <option key={t.id} value={t.id}>{t.identificacion}</option>
              ))}
            </select>
          </div>
        )}
        {TIPOS_CON_FILTRO_PERSONA.includes(tipo) && (
          <>
            <div>
              <label className="mb-1 block text-sm font-medium text-acero">Empleado</label>
              <select
                name="empleadoId"
                value={filtrosPendientes.empleadoId}
                onChange={handleFiltroChange}
                className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
              >
                <option value="">Todos</option>
                {empleados.map((e) => (
                  <option key={e.id} value={e.id}>{e.nombreCompleto}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-acero">Vehículo</label>
              <select
                name="vehiculoId"
                value={filtrosPendientes.vehiculoId}
                onChange={handleFiltroChange}
                className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
              >
                <option value="">Todos</option>
                {vehiculos.map((v) => (
                  <option key={v.id} value={v.id}>{v.placa}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-acero">Departamento</label>
              <select
                name="departamentoId"
                value={filtrosPendientes.departamentoId}
                onChange={handleFiltroChange}
                className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
              >
                <option value="">Todos</option>
                {departamentos.map((d) => (
                  <option key={d.id} value={d.id}>{d.nombre}</option>
                ))}
              </select>
            </div>
          </>
        )}
        <button type="submit" className="rounded-md bg-tanque px-4 py-2 text-sm font-medium text-white hover:opacity-90">
          Filtrar
        </button>
        <button type="button" onClick={handleLimpiarFiltros} className="rounded-md border px-4 py-2 text-sm text-tinta hover:bg-fondo">
          Limpiar
        </button>

        <div className="ml-auto flex gap-2">
          {FORMATOS.map((f) => (
            <button
              key={f.value}
              type="button"
              onClick={() => handleExportar(f)}
              disabled={descargando !== null}
              className="rounded-md bg-acero px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
            >
              {descargando === f.value ? 'Descargando...' : `Exportar ${f.label}`}
            </button>
          ))}
        </div>
      </form>

      {error && <p className="text-sm text-peligro">{error}</p>}
      {descargaError && <p className="text-sm text-peligro">{descargaError}</p>}
      {!error && loading && <p className="text-sm text-acero">Cargando...</p>}

      {!error && !loading && respuesta && (
        <>
          <div className="overflow-x-auto rounded-sm border border-acero/20">
            <table className="min-w-full divide-y divide-acero/20 text-sm">
              <thead className="bg-fondo">
                <tr>
                  {columnas.map((c) => (
                    <th key={c.key} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                      {c.label}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-acero/10 bg-white">
                {respuesta.items.length === 0 && (
                  <tr>
                    <td colSpan={columnas.length} className="px-4 py-6 text-center text-acero/70">
                      Sin registros para los filtros aplicados.
                    </td>
                  </tr>
                )}
                {respuesta.items.map((item, i) => (
                  <tr key={item.id ?? i} className="hover:bg-fondo">
                    {columnas.map((c) => (
                      <td
                        key={c.key}
                        className={`px-4 py-3 ${c.mono || c.num ? 'font-mono' : ''} ${c.num ? 'num' : ''} ${c.key === 'id' ? 'font-mono num' : ''} text-acero`}
                      >
                        {formatCelda(c, item[c.key])}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-4 flex items-center justify-between text-sm">
            <span className="font-mono num text-acero">
              Página {respuesta.pagina} de {totalPaginas} — {respuesta.total} registros
            </span>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setPagina((p) => Math.max(1, p - 1))}
                disabled={pagina === 1}
                className="rounded-md border border-acero/40 px-3 py-1.5 text-tinta hover:bg-fondo disabled:opacity-50"
              >
                Anterior
              </button>
              <button
                type="button"
                onClick={() => setPagina((p) => p + 1)}
                disabled={pagina >= totalPaginas}
                className="rounded-md border border-acero/40 px-3 py-1.5 text-tinta hover:bg-fondo disabled:opacity-50"
              >
                Siguiente
              </button>
            </div>
          </div>
        </>
      )}
    </PageContainer>
  )
}
