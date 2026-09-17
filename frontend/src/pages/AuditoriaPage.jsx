import { useEffect, useState } from 'react'
import PageContainer from '../components/PageContainer'
import apiRequest from '../services/api'

const TAMANO_PAGINA = 50

function formatFecha(value) {
  return value ? new Date(value).toLocaleDateString() : '—'
}

function formatHora(value) {
  return value ? new Date(value).toLocaleTimeString() : '—'
}

export default function AuditoriaPage() {
  const [pagina, setPagina] = useState(1)
  // `loading` se deriva comparando qué página pidió el usuario contra cuál está
  // realmente cargada, en vez de fijarlo con un setState síncrono al inicio del
  // efecto (este efecto se re-ejecuta en cada cambio de página, así que no es un
  // caso de "fetch solo al montar" como en las demás pantallas).
  const [paginaCargada, setPaginaCargada] = useState(null)
  const [respuesta, setRespuesta] = useState(null)
  const [error, setError] = useState(null)

  const loading = paginaCargada !== pagina

  useEffect(() => {
    let cancelado = false
    apiRequest(`/auditoria?pagina=${pagina}&tamanoPagina=${TAMANO_PAGINA}`)
      .then((data) => {
        if (cancelado) return
        setRespuesta(data)
        setPaginaCargada(pagina)
        setError(null)
      })
      .catch((e) => { if (!cancelado) setError(e.message) })
    return () => { cancelado = true }
  }, [pagina])

  const totalPaginas = respuesta ? Math.max(1, Math.ceil(respuesta.total / respuesta.tamanoPagina)) : 1

  // El backend ahora manda nombreUsuario directo en cada entrada (ya no hace falta
  // cruzar con GET /usuarios, que además es exclusivo de Administrador y bloqueaba
  // la resolución del nombre para el rol Auditor). nombreUsuario viene null solo en
  // dos casos reales: usuarioId null (evento del sistema, sin actor) o usuarioId
  // presente pero el usuario ya no existe (referencia huérfana).
  function nombreUsuario(a) {
    if (a.usuarioId == null) return 'Sistema'
    return a.nombreUsuario ?? 'Usuario eliminado'
  }

  return (
    <PageContainer title="Auditoría">
      <p className="mb-4 text-sm text-acero">
        Registro de solo lectura de creaciones, modificaciones, despachos, ajustes, anulaciones y accesos (RF-21).
        El backend solo soporta paginación — no hay filtro por fecha, usuario o tipo de evento en{' '}
        <span className="font-mono">GET /api/v1/auditoria</span>, así que no se agregó ningún filtro que solo
        mirara la página cargada.
      </p>

      {error && <p className="text-sm text-peligro">{error}</p>}
      {!error && loading && <p className="text-sm text-acero">Cargando...</p>}

      {!error && !loading && respuesta && (
        <>
          <div className="overflow-x-auto rounded-sm border border-acero/20">
            <table className="min-w-full divide-y divide-acero/20 text-sm">
              <thead className="bg-fondo">
                <tr>
                  {['Usuario', 'Fecha', 'Hora', 'Dirección IP', 'Evento', 'Entidad afectada'].map((h) => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-medium text-acero uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-acero/10 bg-white">
                {respuesta.elementos.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-4 py-6 text-center text-acero/70">
                      Sin registros de auditoría.
                    </td>
                  </tr>
                )}
                {respuesta.elementos.map((a) => (
                  <tr key={a.id} className="hover:bg-fondo">
                    <td className="px-4 py-3 text-tinta">{nombreUsuario(a)}</td>
                    <td className="px-4 py-3 font-mono num text-acero">{formatFecha(a.fechaHoraUtc)}</td>
                    <td className="px-4 py-3 font-mono num text-acero">{formatHora(a.fechaHoraUtc)}</td>
                    <td className="px-4 py-3 font-mono num text-acero">{a.direccionIp ?? '—'}</td>
                    <td className="px-4 py-3 font-mono text-tinta">{a.evento}</td>
                    <td className="px-4 py-3 text-acero">
                      {a.entidadAfectada}{' '}
                      <span className="font-mono text-acero/70">#{a.identificadorRegistro}</span>
                    </td>
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
