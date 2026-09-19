import { useEffect, useState } from 'react'
import PageContainer from '../components/PageContainer'
import ResponsiveTable from '../components/ResponsiveTable'
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
          <ResponsiveTable
            data={respuesta.elementos}
            keyField="id"
            emptyMessage="Sin registros de auditoría."
            columns={[
              {
                key: 'evento',
                label: 'Evento',
                primary: true,
                priority: 'high',
                render: (a) => (
                  <div>
                    <span className="font-semibold font-mono text-tanque">{a.evento}</span>
                    <div className="text-xs text-acero sm:hidden">{nombreUsuario(a)}</div>
                  </div>
                ),
              },
              {
                key: 'usuario',
                label: 'Usuario',
                priority: 'high',
                render: (a) => <span className="text-tinta font-medium">{nombreUsuario(a)}</span>,
              },
              {
                key: 'fechaHoraUtc',
                label: 'Fecha / Hora',
                priority: 'high',
                render: (a) => (
                  <span className="font-mono num text-acero text-xs sm:text-sm">
                    {formatFecha(a.fechaHoraUtc)} {formatHora(a.fechaHoraUtc)}
                  </span>
                ),
              },
              {
                key: 'entidadAfectada',
                label: 'Entidad afectada',
                priority: 'high',
                render: (a) => (
                  <span className="text-acero">
                    {a.entidadAfectada} <span className="font-mono text-acero/70">#{a.identificadorRegistro}</span>
                  </span>
                ),
              },
              {
                key: 'direccionIp',
                label: 'Dirección IP',
                priority: 'low',
                render: (a) => <span className="font-mono num text-acero text-xs">{a.direccionIp ?? '—'}</span>,
              },
            ]}
          />

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
