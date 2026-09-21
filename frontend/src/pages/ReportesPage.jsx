import { useEffect, useState } from 'react'
import { formatDateTime } from '../utils/dates'
import Field, { inputCls } from '../components/Field'
import PageContainer from '../components/PageContainer'
import apiRequest, { apiDownload } from '../services/api'
import { getSequentialFilename, downloadBlob } from '../utils/download'
import { useDepartamentos } from '../hooks/useDepartamentos'
import { useEmpleados } from '../hooks/useEmpleados'
import { useTanques } from '../hooks/useTanques'
import { useVehiculos } from '../hooks/useVehiculos'
import { validateRangoFechas } from '../utils/validators'

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

const FILTROS_VACIOS = { fechaDesde: '', fechaHasta: '', tanqueId: '', empleadoId: '', vehiculoId: '', departamentoId: '', tipoCombustibleId: '', estadoTicket: '' }

function formatCelda(col, value) {
  if (value === null || value === undefined || value === '') return '—'
  if (col.fecha) return formatDateTime(value)
  if (col.num) return Number(value).toFixed(4)
  return String(value)
}

export default function ReportesPage() {
  const [tiposCombustible, setTiposCombustible] = useState([])
  useEffect(() => {
    let cancelled = false
    apiRequest('/tipos-combustible').then((data) => { if (!cancelled) setTiposCombustible(data) }).catch(() => {})
    return () => { cancelled = true }
  }, [])
  const tanques = useTanques()
  const empleados = useEmpleados(true)
  const vehiculos = useVehiculos()
  const departamentos = useDepartamentos()

  const [tipo, setTipo] = useState('solicitudes')
  const [pagina, setPagina] = useState(1)
  const [filtros, setFiltros] = useState(FILTROS_VACIOS)
  const [filtrosPendientes, setFiltrosPendientes] = useState(FILTROS_VACIOS)
  const [filtroError, setFiltroError] = useState(null)

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
    if (filtros.tipoCombustibleId && tipo !== 'cierres') params.set('tipoCombustibleId', filtros.tipoCombustibleId)
    if (filtros.estadoTicket && tipo === 'tickets') params.set('estadoTicket', filtros.estadoTicket)
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
    setFiltroError(null)
  }

  function handleFiltroChange(e) {
    const { name, value } = e.target
    setFiltrosPendientes((f) => ({ ...f, [name]: value }))
    if (filtroError) setFiltroError(null)
  }

  function handleAplicarFiltros(e) {
    e.preventDefault()
    setFiltroError(null)
    const err = validateRangoFechas(filtrosPendientes.fechaDesde, filtrosPendientes.fechaHasta)
    if (err) {
      setFiltroError(err)
      return
    }
    setPagina(1)
    setFiltros(filtrosPendientes)
  }

  function handleLimpiarFiltros() {
    setFiltrosPendientes(FILTROS_VACIOS)
    setFiltros(FILTROS_VACIOS)
    setFiltroError(null)
    setPagina(1)
  }

  async function handleExportar(formato) {
    setDescargaError(null)
    setDescargando(formato.value)
    try {
      const params = new URLSearchParams({ tipo, formato: formato.value })
      if (filtros.tipoCombustibleId && tipo !== 'cierres') params.set('tipoCombustibleId', filtros.tipoCombustibleId)
      if (filtros.estadoTicket && tipo === 'tickets') params.set('estadoTicket', filtros.estadoTicket)
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
      const basePrefix = `reporte-${tipo}-${fecha}`
      const filename = getSequentialFilename(basePrefix, formato.ext)
      downloadBlob(blob, filename)
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
        Selecciona el reporte y aplica los filtros que necesites. La exportación conserva los filtros aplicados.
      </p>

      <div className="mb-4">
        <label htmlFor="reporte-tipo-reporte" className="mb-1 block text-sm font-medium text-acero">Tipo de reporte</label>
        <select id="reporte-tipo-reporte"
          value={tipo}
          onChange={handleTipoChange}
          className="rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta"
        >
          {TIPOS_REPORTE.map((t) => (
            <option key={t.value} value={t.value}>{t.label}</option>
          ))}
        </select>
      </div>

      {filtroError && (
        <div className="mb-4 flex items-center gap-2 rounded-md bg-peligro/10 border border-peligro/20 p-3 text-sm text-peligro">
          <svg className="h-4 w-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <span>{filtroError}</span>
        </div>
      )}

      <form onSubmit={handleAplicarFiltros} className="mb-4 flex flex-col md:flex-row md:items-end flex-wrap gap-3 rounded-lg border border-acero/15 bg-white p-3.5 shadow-sm">
        <div className="w-full sm:w-auto">
          <label htmlFor="reporte-fechaDesde" className="mb-1 block text-xs font-semibold text-acero uppercase tracking-wider">Desde</label>
          <input
            type="date"
            id="reporte-fechaDesde" name="fechaDesde"
            value={filtrosPendientes.fechaDesde}
            onChange={handleFiltroChange}
            max={filtrosPendientes.fechaHasta || undefined}
            className="w-full rounded-md border border-acero/40 px-3 py-1.5 text-sm text-tinta min-h-[38px]"
          />
        </div>
        <div className="w-full sm:w-auto">
          <label htmlFor="reporte-fechaHasta" className="mb-1 block text-xs font-semibold text-acero uppercase tracking-wider">Hasta</label>
          <input
            type="date"
            id="reporte-fechaHasta" name="fechaHasta"
            value={filtrosPendientes.fechaHasta}
            onChange={handleFiltroChange}
            min={filtrosPendientes.fechaDesde || undefined}
            className="w-full rounded-md border border-acero/40 px-3 py-1.5 text-sm text-tinta min-h-[38px]"
          />
        </div>
        {TIPOS_CON_FILTRO_TANQUE.includes(tipo) && (
          <div className="w-full sm:w-auto">
            <label htmlFor="reporte-tanqueId" className="mb-1 block text-xs font-semibold text-acero uppercase tracking-wider">Tanque</label>
            <select
              id="reporte-tanqueId" name="tanqueId"
              value={filtrosPendientes.tanqueId}
              onChange={handleFiltroChange}
              className="w-full rounded-md border border-acero/40 px-3 py-1.5 text-sm text-tinta min-h-[38px]"
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
            <div className="w-full sm:w-auto">
              <label htmlFor="reporte-empleadoId" className="mb-1 block text-xs font-semibold text-acero uppercase tracking-wider">Empleado</label>
              <select
                id="reporte-empleadoId" name="empleadoId"
                value={filtrosPendientes.empleadoId}
                onChange={handleFiltroChange}
                className="w-full rounded-md border border-acero/40 px-3 py-1.5 text-sm text-tinta min-h-[38px]"
              >
                <option value="">Todos</option>
                {empleados.map((e) => (
                  <option key={e.id} value={e.id}>{e.nombreCompleto}</option>
                ))}
              </select>
            </div>
            <div className="w-full sm:w-auto">
              <label htmlFor="reporte-vehiculoId" className="mb-1 block text-xs font-semibold text-acero uppercase tracking-wider">Vehículo</label>
              <select
                id="reporte-vehiculoId" name="vehiculoId"
                value={filtrosPendientes.vehiculoId}
                onChange={handleFiltroChange}
                className="w-full rounded-md border border-acero/40 px-3 py-1.5 text-sm text-tinta min-h-[38px]"
              >
                <option value="">Todos</option>
                {vehiculos.map((v) => (
                  <option key={v.id} value={v.id}>{v.placa}</option>
                ))}
              </select>
            </div>
            <div className="w-full sm:w-auto">
              <label htmlFor="reporte-departamentoId" className="mb-1 block text-xs font-semibold text-acero uppercase tracking-wider">Departamento</label>
              <select
                id="reporte-departamentoId" name="departamentoId"
                value={filtrosPendientes.departamentoId}
                onChange={handleFiltroChange}
                className="w-full rounded-md border border-acero/40 px-3 py-1.5 text-sm text-tinta min-h-[38px]"
              >
                <option value="">Todos</option>
                {departamentos.map((d) => (
                  <option key={d.id} value={d.id}>{d.nombre}</option>
                ))}
              </select>
            </div>
          </>
        )}
        {tipo !== 'cierres' && <Field label="Combustible">
          <select name="tipoCombustibleId" value={filtrosPendientes.tipoCombustibleId} onChange={handleFiltroChange} className={inputCls}>
            <option value="">Todos</option>
            {tiposCombustible.map((item) => <option key={item.id} value={item.id}>{item.nombre}</option>)}
          </select>
        </Field>}
        {tipo === 'tickets' && <Field label="Estado del ticket">
          <select name="estadoTicket" value={filtrosPendientes.estadoTicket} onChange={handleFiltroChange} className={inputCls}>
            <option value="">Todos</option>
            {['Creado', 'Enviado', 'Pendiente', 'ProximoAVencer', 'Vencido', 'Consumido', 'Anulado'].map((estado) => <option key={estado} value={estado}>{estado === 'ProximoAVencer' ? 'Próximo a vencer' : estado}</option>)}
          </select>
        </Field>}
        <div className="flex gap-2 w-full sm:w-auto">
          <button type="submit" className="flex-1 sm:flex-initial inline-flex items-center justify-center gap-1.5 rounded-md bg-tanque px-4 py-2 text-sm font-semibold text-white hover:opacity-90 shadow-sm min-h-[38px] active:scale-[0.98]">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
            </svg>
            Filtrar
          </button>
          <button type="button" onClick={handleLimpiarFiltros} className="flex-1 sm:flex-initial rounded-md border border-acero/30 bg-white px-3 py-2 text-sm font-medium text-tinta hover:bg-fondo min-h-[38px] active:scale-[0.98]">
            Limpiar
          </button>
        </div>

        <div className="flex flex-wrap gap-2 w-full md:w-auto md:ml-auto pt-2 md:pt-0">
          {FORMATOS.map((f) => (
            <button
              key={f.value}
              type="button"
              onClick={() => handleExportar(f)}
              disabled={descargando !== null}
              className="flex-1 sm:flex-initial inline-flex items-center justify-center gap-1.5 rounded-md bg-acero px-3 py-2 text-xs font-semibold text-white hover:opacity-90 disabled:opacity-50 shadow-sm transition-colors min-h-[38px] active:scale-[0.98]"
            >
              <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
              </svg>
              {descargando === f.value ? 'Exportando...' : `Exportar ${f.label}`}
            </button>
          ))}
        </div>
      </form>

      {error && <p className="text-sm text-peligro">{error}</p>}
      {descargaError && <p className="text-sm text-peligro">{descargaError}</p>}
      {!error && loading && <p className="text-sm text-acero">Cargando...</p>}

      {!error && !loading && respuesta && (
        <>
          <div className="overflow-x-auto table-responsive-container rounded-sm border border-acero/20">
            <table className="min-w-full divide-y divide-acero/20 text-sm">
              <thead className="bg-fondo">
                <tr>
                  {columnas.map((c) => (
                    <th key={c.key} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider whitespace-nowrap">
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
                        className={`px-4 py-3 whitespace-nowrap ${c.mono || c.num ? 'font-mono' : ''} ${c.num ? 'num' : ''} ${c.key === 'id' ? 'font-mono num' : ''} text-acero`}
                      >
                        {formatCelda(c, item[c.key])}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-4 flex flex-col sm:flex-row items-center justify-between gap-3 text-sm">
            <span className="font-mono num text-acero order-2 sm:order-1">
              Página {respuesta.pagina} de {totalPaginas} — {respuesta.total} registros
            </span>
            <div className="flex w-full sm:w-auto gap-2 order-1 sm:order-2">
              <button
                type="button"
                onClick={() => setPagina((p) => Math.max(1, p - 1))}
                disabled={pagina === 1}
                className="flex-1 sm:flex-initial min-h-[42px] rounded-md border border-acero/30 bg-white px-4 py-2 text-tinta hover:bg-fondo disabled:opacity-50 active:scale-[0.98]"
              >
                Anterior
              </button>
              <button
                type="button"
                onClick={() => setPagina((p) => p + 1)}
                disabled={pagina >= totalPaginas}
                className="flex-1 sm:flex-initial min-h-[42px] rounded-md border border-acero/30 bg-white px-4 py-2 text-tinta hover:bg-fondo disabled:opacity-50 active:scale-[0.98]"
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
