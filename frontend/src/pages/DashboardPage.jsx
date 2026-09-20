import { useCallback, useEffect, useState } from 'react'
import { formatDate } from '../utils/dates'
import PageContainer from '../components/PageContainer'
import apiRequest from '../services/api'

const ESTADOS_TICKET_ACTIVOS = ['Creado', 'Enviado', 'Pendiente', 'ProximoAVencer']
const AUTO_REFRESH_MS = 60_000

function StatCard({ label, value, sub }) {
  return (
    <div className="rounded-sm border border-acero/20 bg-white p-3.5 sm:p-5 flex flex-col justify-between shadow-xs">
      <p className="text-[11px] sm:text-xs font-semibold text-acero uppercase tracking-wider leading-snug">{label}</p>
      <p className="mt-1 font-mono num text-2xl sm:text-3xl font-semibold text-tinta truncate">{value}</p>
      {sub && <p className="mt-1 text-[11px] text-acero/70">{sub}</p>}
    </div>
  )
}

function BarRow({ label, value, max, color = 'bg-tanque', unit = 'gal' }) {
  const pct = max > 0 ? Math.round((value / max) * 100) : 0
  return (
    <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-3 text-sm py-1">
      <div className="flex justify-between items-center sm:w-32 sm:shrink-0">
        <span className="text-acero text-xs sm:text-sm truncate font-medium">{label}</span>
        <span className="sm:hidden font-mono num text-tinta font-semibold text-xs">{value.toFixed(1)} {unit}</span>
      </div>
      <div className="flex-1 h-2.5 sm:h-3 rounded-full bg-acero/10 overflow-hidden w-full">
        <div className={`h-full rounded-full ${color} transition-all duration-300`} style={{ width: `${pct}%` }} />
      </div>
      <span className="hidden sm:inline-block w-20 text-right font-mono num text-tinta font-semibold text-xs sm:text-sm">{value.toFixed(1)} {unit}</span>
    </div>
  )
}

function Section({ title, children }) {
  return (
    <div className="rounded-sm border border-acero/20 bg-white p-4 sm:p-5 shadow-xs">
      <h2 className="mb-3 sm:mb-4 text-xs sm:text-sm font-semibold text-tinta uppercase tracking-wider">{title}</h2>
      {children}
    </div>
  )
}


export default function DashboardPage() {
  const [data, setData] = useState(null)
  const [inventarioActual, setInventarioActual] = useState(null)
  const [ticketsStats, setTicketsStats] = useState(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState(null)
  const [lastUpdated, setLastUpdated] = useState(null)

  const load = useCallback(async (isInitial) => {
    // `loading` ya inicia en true (useState(true)); load(true) solo se invoca una
    // vez, desde el montaje, así que no hace falta volver a fijarlo aquí.
    if (!isInitial) setRefreshing(true)
    try {
      const [resumen, inventarios, tickets] = await Promise.all([
        apiRequest('/dashboard/resumen'),
        apiRequest('/inventario'),
        apiRequest('/tickets'),
      ])
      setData(resumen)
      setInventarioActual(inventarios.reduce((sum, i) => sum + i.existenciaActual, 0))
      setTicketsStats({
        activos: tickets.filter(t => ESTADOS_TICKET_ACTIVOS.includes(t.estado)).length,
        vencidos: tickets.filter(t => t.estado === 'Vencido').length,
      })
      setError(null)
      setLastUpdated(new Date())
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
      setRefreshing(false)
    }
  }, [])

  useEffect(() => {
    // `load` combina 3 llamadas a la API + cómputo de stats y se reutiliza tal
    // cual desde el botón "Actualizar" y el intervalo de auto-refresh; inlinearla
    // aquí solo para esquivar la regla duplicaría esa lógica en tres sitios.
    // oxlint-disable-next-line react/set-state-in-effect
    load(true)
    const interval = setInterval(() => load(false), AUTO_REFRESH_MS)
    return () => clearInterval(interval)
  }, [load])

  if (loading) return <PageContainer title="Dashboard"><p className="text-sm text-acero mt-2">Cargando...</p></PageContainer>
  if (error)   return <PageContainer title="Dashboard"><p className="text-sm text-peligro mt-2">{error}</p></PageContainer>

  const { hoy, ultimos7Dias, top3TanquesMasUsados, comparativaMes, distribucionPorTipoCombustible, eficienciaAprobacion, consumoPorDepartamento, consumoPorVehiculo } = data

  const maxVol7 = Math.max(...ultimos7Dias.map(d => d.volumenDespachado), 1)
  const maxTop3 = Math.max(...top3TanquesMasUsados.map(t => t.totalGalones), 1)
  const maxConsumoDepto = Math.max(...consumoPorDepartamento.map(c => c.totalGalones), 1)
  const maxConsumoVehiculo = Math.max(...consumoPorVehiculo.map(c => c.totalGalones), 1)

  return (
    <PageContainer title="Dashboard">
      <div className="mb-4 flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <p className="text-xs text-acero/70">
          {lastUpdated && `Actualizado ${lastUpdated.toLocaleTimeString()}`}
        </p>
        <button
          type="button"
          onClick={() => load(false)}
          disabled={refreshing}
          className="flex min-h-[44px] sm:min-h-[36px] items-center justify-center gap-1.5 rounded-md border border-acero/30 bg-white px-3 py-1.5 text-xs font-semibold text-tinta hover:bg-fondo hover:border-tanque transition-colors disabled:opacity-50 w-full sm:w-auto shadow-xs"
        >
          <svg className={`w-3.5 h-3.5 ${refreshing ? 'animate-spin' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          {refreshing ? 'Actualizando...' : 'Actualizar'}
        </button>
      </div>

      {/* Tarjetas hoy — Grid fluido auto-fit/minmax */}
      <div className="grid grid-cols-1 gap-3 sm:gap-4 [grid-template-columns:repeat(auto-fit,minmax(160px,1fr))] mb-6">
        <StatCard label="Despachos hoy"        value={hoy.totalDespachos} />
        <StatCard label="Volumen hoy (gal)"    value={hoy.volumenDespachado.toFixed(1)} />
        <StatCard label="Solicitudes pendientes" value={hoy.solicitudesPendientes} />
        <StatCard label="Tanques nivel bajo"   value={hoy.tanquesConInventarioBajo} />
        <StatCard label="Inventario actual (gal)" value={inventarioActual.toFixed(1)} />
        <StatCard label="Tickets activos / vencidos" value={`${ticketsStats.activos} / ${ticketsStats.vencidos}`} />
      </div>


      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2 mb-4">
        {/* Consumo por departamento */}
        <Section title="Consumo por departamento (30 días)">
          {consumoPorDepartamento.length === 0
            ? <p className="text-sm text-acero/70">Sin consumo registrado en los últimos 30 días.</p>
            : <div className="space-y-2">
                {consumoPorDepartamento.map(c => (
                  <BarRow key={c.departamentoId} label={c.departamento} value={c.totalGalones} max={maxConsumoDepto} color="bg-info" />
                ))}
              </div>
          }
        </Section>

        {/* Consumo por vehículo */}
        <Section title="Consumo por vehículo (30 días)">
          {consumoPorVehiculo.length === 0
            ? <p className="text-sm text-acero/70">Sin consumo registrado en los últimos 30 días.</p>
            : <div className="space-y-2">
                {consumoPorVehiculo.map(c => (
                  <BarRow key={c.vehiculoId} label={c.placa} value={c.totalGalones} max={maxConsumoVehiculo} color="bg-acero" />
                ))}
              </div>
          }
        </Section>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">

        {/* Últimos 7 días */}
        <Section title="Volumen despachado — últimos 7 días">
          <div className="space-y-2">
            {ultimos7Dias.map(d => (
              <BarRow
                key={d.fecha}
                label={formatDate(d.fecha, { weekday: 'short', day: 'numeric' })}
                value={d.volumenDespachado}
                max={maxVol7}
              />
            ))}
          </div>
        </Section>

        {/* Top 3 tanques */}
        <Section title="Top 3 tanques (últimos 30 días)">
          {top3TanquesMasUsados.length === 0
            ? <p className="text-sm text-acero/70">Sin datos.</p>
            : <div className="space-y-2">
                {top3TanquesMasUsados.map((t) => (
                  <BarRow key={t.tanqueId} label={t.identificacion} value={t.totalGalones} max={maxTop3} color="bg-info" />
                ))}
              </div>
          }
        </Section>

        {/* Comparativa mensual */}
        <Section title="Comparativa mensual">
          <div className="grid grid-cols-2 gap-4 text-center">
            {[
              { label: 'Mes actual', d: comparativaMes.mesActual },
              { label: 'Mes anterior', d: comparativaMes.mesAnterior },
            ].map(({ label, d }) => (
              <div key={label} className="rounded-md bg-fondo p-4">
                <p className="text-xs text-acero mb-2">{label}</p>
                <p className="font-mono num text-2xl font-semibold text-tinta">{d.volumenDespachado.toFixed(1)}</p>
                <p className="text-xs text-acero/70">gal despachados</p>
                <p className="mt-2 font-mono num text-lg font-medium text-tinta">{d.solicitudes}</p>
                <p className="text-xs text-acero/70">solicitudes</p>
              </div>
            ))}
          </div>
        </Section>

        {/* Distribución + Eficiencia */}
        <div className="space-y-4">
          <Section title="Distribución por combustible (30 días)">
            {distribucionPorTipoCombustible.length === 0
              ? <p className="text-sm text-acero/70">Sin datos.</p>
              : <div className="space-y-2">
                  {distribucionPorTipoCombustible.map(d => (
                    <BarRow key={d.tipoCombustible} label={d.tipoCombustible} value={d.porcentaje} max={100} unit="%" color="bg-exito" />
                  ))}
                </div>
            }
          </Section>

          <Section title="Eficiencia de aprobación (mes actual)">
            <div className="flex items-center gap-6">
              <div className="text-center">
                <p className="font-mono num text-3xl font-bold text-tanque">{eficienciaAprobacion.tasaAprobacion}%</p>
                <p className="text-xs text-acero/70 mt-1">tasa aprobación</p>
              </div>
              <div className="space-y-1 text-sm flex-1">
                <div className="flex justify-between"><span className="text-exito">Aprobadas</span><span className="font-mono num font-medium">{eficienciaAprobacion.aprobadas}</span></div>
                <div className="flex justify-between"><span className="text-peligro">Rechazadas</span><span className="font-mono num font-medium">{eficienciaAprobacion.rechazadas}</span></div>
                <div className="flex justify-between"><span className="inline-flex items-center gap-1.5 text-acero"><span className="h-2 w-2 rounded-full bg-advertencia" />Pendientes</span><span className="font-mono num font-medium">{eficienciaAprobacion.pendientes}</span></div>
              </div>
            </div>
          </Section>
        </div>

      </div>
    </PageContainer>
  )
}
