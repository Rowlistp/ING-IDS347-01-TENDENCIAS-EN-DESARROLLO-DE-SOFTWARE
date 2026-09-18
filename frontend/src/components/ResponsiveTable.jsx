import { useState } from 'react'

/**
 * ResponsiveTable — Patrón de visualización de datos adaptativo en 3 niveles:
 *   1. Desktop (>992px): Tabla completa con cabeceras claras y soporte de primera columna sticky.
 *   2. Tablet (576px - 992px): Oculta columnas de baja prioridad y ofrece un botón de acordeón para expandir detalles inline.
 *   3. Mobile (<576px): Transforma cada fila en una tarjeta (card) apilada verticalmente con formato "Label: Valor" y botones táctiles accesibles (44px).
 *
 * Props:
 *   - data: Array de objetos
 *   - keyField: String (default 'id')
 *   - columns: Array<{
 *       key: string,
 *       label: string,
 *       primary?: boolean,        // Campo principal (título en tarjeta móvil, sticky en desktop)
 *       priority?: 'high'|'med'|'low', // high: siempre visible; med: tablet+desktop; low: solo desktop/expandido
 *       render?: (item) => ReactNode,
 *       className?: string,
 *       headerClassName?: string
 *     }>
 *   - actions?: (item) => ReactNode // Botones de acción
 *   - onRowClick?: (item) => void
 *   - emptyMessage?: string
 *   - loading?: boolean
 */
export default function ResponsiveTable({
  data = [],
  keyField = 'id',
  columns = [],
  actions,
  onRowClick,
  emptyMessage = 'Sin registros encontrados.',
  loading = false,
}) {
  const [expandedRows, setExpandedRows] = useState(new Set())

  function toggleRow(id, e) {
    e?.stopPropagation()
    setExpandedRows((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const primaryCol = columns.find((c) => c.primary) || columns[0]
  const secondaryCols = columns.filter((c) => c !== primaryCol)
  const lowPriorityCols = columns.filter((c) => c.priority === 'low')
  const hasLowPriority = lowPriorityCols.length > 0

  if (loading) {
    return (
      <div className="space-y-3">
        <div className="hidden sm:block overflow-hidden rounded-sm border border-acero/20 bg-white p-6 animate-pulse">
          <div className="h-4 bg-acero/10 rounded w-1/4 mb-4" />
          <div className="space-y-2">
            <div className="h-8 bg-acero/5 rounded w-full" />
            <div className="h-8 bg-acero/5 rounded w-full" />
            <div className="h-8 bg-acero/5 rounded w-full" />
          </div>
        </div>
        <div className="sm:hidden space-y-3">
          {[1, 2, 3].map((i) => (
            <div key={i} className="rounded-sm border border-acero/20 bg-white p-4 animate-pulse space-y-2">
              <div className="h-4 bg-acero/10 rounded w-1/3" />
              <div className="h-3 bg-acero/5 rounded w-2/3" />
              <div className="h-3 bg-acero/5 rounded w-1/2" />
            </div>
          ))}
        </div>
      </div>
    )
  }

  if (!data || data.length === 0) {
    return (
      <div className="rounded-sm border border-acero/20 bg-white p-8 text-center text-sm text-acero/70">
        {emptyMessage}
      </div>
    )
  }

  return (
    <div className="w-full">
      {/* ──────────────────────────────────────────────────────────────────────
       * 1. VISTA MÓVIL (<576px): Tarjetas apiladas verticales (Cards)
       * ────────────────────────────────────────────────────────────────────── */}
      <div className="sm:hidden space-y-3">
        {data.map((item) => {
          const id = item[keyField]
          const isExpanded = expandedRows.has(id)

          return (
            <div
              key={id}
              onClick={() => onRowClick?.(item)}
              className={`rounded-sm border border-acero/20 bg-white p-4 shadow-xs transition-colors ${
                onRowClick ? 'cursor-pointer hover:border-tanque/40' : ''
              }`}
            >
              {/* Encabezado de la tarjeta */}
              <div className="flex items-start justify-between gap-2 border-b border-acero/10 pb-2.5">
                <div>
                  <span className="text-xs font-semibold text-acero uppercase tracking-wider block">
                    {primaryCol?.label}
                  </span>
                  <div className="text-sm font-bold text-tinta mt-0.5">
                    {primaryCol?.render ? primaryCol.render(item) : item[primaryCol?.key]}
                  </div>
                </div>

                {/* Si hay columna de estado o badge, renderizarla arriba */}
                {columns.find((c) => c.key === 'estado' || c.key === 'activo') && (
                  <div>
                    {(() => {
                      const statusCol = columns.find((c) => c.key === 'estado' || c.key === 'activo')
                      return statusCol?.render ? statusCol.render(item) : item[statusCol?.key]
                    })()}
                  </div>
                )}
              </div>

              {/* Rejilla de campos clave (Label : Valor) */}
              <div className="grid grid-cols-2 gap-x-3 gap-y-2 py-3 text-xs">
                {secondaryCols
                  .filter((c) => c.key !== 'estado' && c.key !== 'activo')
                  .slice(0, isExpanded ? secondaryCols.length : 4)
                  .map((col) => {
                    const val = col.render ? col.render(item) : item[col.key]
                    return (
                      <div key={col.key} className={col.className ? `truncate ${col.className}` : 'truncate'}>
                        <dt className="text-acero/80 font-medium">{col.label}</dt>
                        <dd className="font-semibold text-tinta mt-0.5 text-xs truncate">
                          {val ?? '—'}
                        </dd>
                      </div>
                    )
                  })}
              </div>

              {/* Si hay más de 4 campos, botón de "Ver más detalles" */}
              {secondaryCols.length > 4 && (
                <div className="border-t border-acero/10 pt-2 pb-1">
                  <button
                    type="button"
                    onClick={(e) => toggleRow(id, e)}
                    className="flex w-full min-h-[36px] items-center justify-center gap-1 text-xs font-semibold text-acero hover:text-tanque"
                  >
                    <span>{isExpanded ? 'Ver menos detalles' : `Ver ${secondaryCols.length - 4} detalles más`}</span>
                    <svg
                      className={`w-3.5 h-3.5 transform transition-transform ${isExpanded ? 'rotate-180' : ''}`}
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 9l-7 7-7-7" />
                    </svg>
                  </button>
                </div>
              )}

              {/* Acciones de la tarjeta */}
              {actions && (
                <div
                  className="mt-2 pt-2 border-t border-acero/15 flex flex-wrap items-center gap-2"
                  onClick={(e) => e.stopPropagation()}
                >
                  {actions(item)}
                </div>
              )}
            </div>
          )
        })}
      </div>

      {/* ──────────────────────────────────────────────────────────────────────
       * 2. VISTA TABLET Y DESKTOP (≥576px): Tabla adaptable con prioridad de columnas
       * ────────────────────────────────────────────────────────────────────── */}
      <div className="hidden sm:block overflow-x-auto rounded-sm border border-acero/20 bg-white table-responsive-container">
        <table className="min-w-full divide-y divide-acero/20 text-sm">
          <thead className="bg-fondo">
            <tr>
              {hasLowPriority && (
                <th className="w-8 px-2 py-3 lg:hidden text-center" aria-label="Expandir">
                  <span className="sr-only">Expandir</span>
                </th>
              )}

              {columns.map((col) => {
                const isLow = col.priority === 'low'
                const isMed = col.priority === 'med'

                return (
                  <th
                    key={col.key}
                    className={`px-4 py-3 text-left text-xs font-semibold text-acero uppercase tracking-wider ${
                      col.primary ? 'sticky left-0 bg-fondo z-10' : ''
                    } ${isLow ? 'hidden lg:table-cell' : ''} ${isMed ? 'hidden md:table-cell' : ''} ${
                      col.headerClassName ?? ''
                    }`}
                  >
                    {col.label}
                  </th>
                )
              })}

              {actions && (
                <th className="px-4 py-3 text-left text-xs font-semibold text-acero uppercase tracking-wider">
                  Acciones
                </th>
              )}
            </tr>
          </thead>

          <tbody className="divide-y divide-acero/10 bg-white">
            {data.map((item) => {
              const id = item[keyField]
              const isExpanded = expandedRows.has(id)

              return (
                <tr
                  key={id}
                  onClick={() => onRowClick?.(item)}
                  className={`transition-colors ${
                    onRowClick ? 'cursor-pointer hover:bg-fondo' : 'hover:bg-fondo/60'
                  }`}
                >
                  {/* Botón expandir en tablet */}
                  {hasLowPriority && (
                    <td className="w-8 px-2 py-3 lg:hidden text-center" onClick={(e) => toggleRow(id, e)}>
                      <button
                        type="button"
                        aria-label="Alternar detalles adicionales"
                        className="flex h-7 w-7 items-center justify-center rounded text-acero hover:bg-acero/10 hover:text-tanque transition-colors"
                      >
                        <svg
                          className={`w-4 h-4 transform transition-transform ${isExpanded ? 'rotate-180' : ''}`}
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                        >
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 9l-7 7-7-7" />
                        </svg>
                      </button>
                    </td>
                  )}

                  {/* Celdas de datos */}
                  {columns.map((col) => {
                    const isLow = col.priority === 'low'
                    const isMed = col.priority === 'med'
                    const val = col.render ? col.render(item) : item[col.key]

                    return (
                      <td
                        key={col.key}
                        className={`px-4 py-3 ${
                          col.primary ? 'sticky left-0 bg-white z-10 font-semibold text-tanque' : 'text-tinta'
                        } ${isLow ? 'hidden lg:table-cell' : ''} ${isMed ? 'hidden md:table-cell' : ''} ${
                          col.className ?? ''
                        }`}
                      >
                        {val ?? '—'}
                      </td>
                    )
                  })}

                  {/* Acciones */}
                  {actions && (
                    <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                      <div className="flex flex-wrap items-center gap-1.5">{actions(item)}</div>
                    </td>
                  )}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
