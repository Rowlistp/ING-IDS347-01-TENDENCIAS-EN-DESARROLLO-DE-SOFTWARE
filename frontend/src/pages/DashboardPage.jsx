import { useEffect, useState } from 'react'
import PageContainer from '../components/PageContainer'
import apiRequest from '../services/api'

function StatCard({ label, value, sub }) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white p-5">
      <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">{label}</p>
      <p className="mt-1 text-3xl font-semibold text-gray-800">{value}</p>
      {sub && <p className="mt-1 text-xs text-gray-400">{sub}</p>}
    </div>
  )
}

function BarRow({ label, value, max, color = 'bg-blue-500' }) {
  const pct = max > 0 ? Math.round((value / max) * 100) : 0
  return (
    <div className="flex items-center gap-3 text-sm">
      <span className="w-24 shrink-0 text-gray-500">{label}</span>
      <div className="flex-1 h-3 rounded-full bg-gray-100 overflow-hidden">
        <div className={`h-3 rounded-full ${color}`} style={{ width: `${pct}%` }} />
      </div>
      <span className="w-16 text-right text-gray-700 font-medium">{value.toFixed(1)}</span>
    </div>
  )
}

function Section({ title, children }) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white p-5">
      <h2 className="mb-4 text-sm font-semibold text-gray-700 uppercase tracking-wide">{title}</h2>
      {children}
    </div>
  )
}

export default function DashboardPage() {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    apiRequest('/api/v1/dashboard/resumen')
      .then(setData)
      .catch(e => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <PageContainer title="Dashboard"><p className="text-sm text-gray-500 mt-2">Cargando...</p></PageContainer>
  if (error)   return <PageContainer title="Dashboard"><p className="text-sm text-red-600 mt-2">{error}</p></PageContainer>

  const { hoy, ultimos7Dias, top3TanquesMasUsados, comparativaMes, distribucionPorTipoCombustible, eficienciaAprobacion } = data

  const maxVol7 = Math.max(...ultimos7Dias.map(d => d.volumenDespachado), 1)
  const maxTop3 = Math.max(...top3TanquesMasUsados.map(t => t.totalGalones), 1)

  return (
    <PageContainer title="Dashboard">
      {/* Tarjetas hoy */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4 mb-6">
        <StatCard label="Despachos hoy"        value={hoy.totalDespachos} />
        <StatCard label="Volumen hoy (gal)"    value={hoy.volumenDespachado.toFixed(1)} />
        <StatCard label="Solicitudes pendientes" value={hoy.solicitudesPendientes} />
        <StatCard label="Tanques nivel bajo"   value={hoy.tanquesConInventarioBajo} />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">

        {/* Últimos 7 días */}
        <Section title="Volumen despachado — últimos 7 días">
          <div className="space-y-2">
            {ultimos7Dias.map(d => (
              <BarRow
                key={d.fecha}
                label={new Date(d.fecha).toLocaleDateString('es', { weekday: 'short', day: 'numeric' })}
                value={d.volumenDespachado}
                max={maxVol7}
              />
            ))}
          </div>
        </Section>

        {/* Top 3 tanques */}
        <Section title="Top 3 tanques (últimos 30 días)">
          {top3TanquesMasUsados.length === 0
            ? <p className="text-sm text-gray-400">Sin datos.</p>
            : <div className="space-y-2">
                {top3TanquesMasUsados.map((t, i) => (
                  <BarRow key={t.tanqueId} label={t.identificacion} value={t.totalGalones} max={maxTop3} color="bg-indigo-500" />
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
              <div key={label} className="rounded-md bg-gray-50 p-4">
                <p className="text-xs text-gray-500 mb-2">{label}</p>
                <p className="text-2xl font-semibold text-gray-800">{d.volumenDespachado.toFixed(1)}</p>
                <p className="text-xs text-gray-400">gal despachados</p>
                <p className="mt-2 text-lg font-medium text-gray-700">{d.solicitudes}</p>
                <p className="text-xs text-gray-400">solicitudes</p>
              </div>
            ))}
          </div>
        </Section>

        {/* Distribución + Eficiencia */}
        <div className="space-y-4">
          <Section title="Distribución por combustible (30 días)">
            {distribucionPorTipoCombustible.length === 0
              ? <p className="text-sm text-gray-400">Sin datos.</p>
              : <div className="space-y-2">
                  {distribucionPorTipoCombustible.map(d => (
                    <BarRow key={d.tipoCombustible} label={d.tipoCombustible} value={d.porcentaje} max={100} color="bg-emerald-500" />
                  ))}
                </div>
            }
          </Section>

          <Section title="Eficiencia de aprobación (mes actual)">
            <div className="flex items-center gap-6">
              <div className="text-center">
                <p className="text-3xl font-bold text-blue-600">{eficienciaAprobacion.tasaAprobacion}%</p>
                <p className="text-xs text-gray-400 mt-1">tasa aprobación</p>
              </div>
              <div className="space-y-1 text-sm flex-1">
                <div className="flex justify-between"><span className="text-green-600">Aprobadas</span><span className="font-medium">{eficienciaAprobacion.aprobadas}</span></div>
                <div className="flex justify-between"><span className="text-red-600">Rechazadas</span><span className="font-medium">{eficienciaAprobacion.rechazadas}</span></div>
                <div className="flex justify-between"><span className="text-yellow-600">Pendientes</span><span className="font-medium">{eficienciaAprobacion.pendientes}</span></div>
              </div>
            </div>
          </Section>
        </div>

      </div>
    </PageContainer>
  )
}
