import { NavLink } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { canAccessRoute, ROLES } from '../utils/rbac'

/* ────────────────────────────────────────────────────────────────────────────
 * Sidebar — navegación principal agrupada por categorías semánticas y filtrada por rol.
 * Soporta modo off-canvas en pantallas móviles/tablets (<768px).
 * ──────────────────────────────────────────────────────────────────────────── */

// Inline SVG icon components (20×20, stroke-based, currentColor)
const icons = {
  dashboard: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="7" height="7" rx="1" />
      <rect x="14" y="3" width="7" height="7" rx="1" />
      <rect x="3" y="14" width="7" height="7" rx="1" />
      <rect x="14" y="14" width="7" height="7" rx="1" />
    </svg>
  ),
  solicitudes: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z" />
      <path d="M14 2v6h6" />
      <path d="M12 18v-6" />
      <path d="m9 15 3-3 3 3" />
    </svg>
  ),
  solicitudesRecurrentes: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M21 12a9 9 0 1 1-9-9c2.52 0 4.93 1 6.74 2.74L21 8" />
      <path d="M21 3v5h-5" />
    </svg>
  ),
  tickets: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2 9a3 3 0 0 1 0 6v2a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-2a3 3 0 0 1 0-6V7a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2Z" />
      <path d="M13 5v2" />
      <path d="M13 17v2" />
      <path d="M13 11v2" />
    </svg>
  ),
  despachos: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M10 17h4V5H2v12h3" />
      <path d="M20 17h2v-3.34a4 4 0 0 0-1.17-2.83L19 9h-5v8h1" />
      <circle cx="7.5" cy="17.5" r="2.5" />
      <circle cx="17.5" cy="17.5" r="2.5" />
    </svg>
  ),
  cierreDiario: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="18" rx="2" />
      <path d="M16 2v4" />
      <path d="M8 2v4" />
      <path d="M3 10h18" />
      <path d="m9 16 2 2 4-4" />
    </svg>
  ),
  inventario: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="m7.5 4.27 9 5.15" />
      <path d="M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z" />
      <path d="m3.3 7 8.7 5 8.7-5" />
      <path d="M12 22V12" />
    </svg>
  ),
  recepciones: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z" />
      <path d="m3.3 7 8.7 5 8.7-5" />
      <path d="M12 22V12" />
      <path d="m16 12-4 4-4-4" />
    </svg>
  ),
  tanques: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M5 8a7 7 0 0 1 14 0" />
      <path d="M5.2 14.3A7 7 0 0 0 12 19a7 7 0 0 0 6.8-4.7" />
      <circle cx="12" cy="12" r="2" />
      <path d="M12 2v2" />
      <path d="m4.93 4.93 1.41 1.41" />
      <path d="m17.66 6.34 1.41-1.41" />
    </svg>
  ),
  tiposCombustible: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 2c1 3 2.5 3.5 3.5 4.5A5 5 0 0 1 17 10c0 3.31-2.69 6-6 6h-1" />
      <path d="M10 16a4 4 0 0 1-2-7.5" />
      <path d="M14 21v-4" />
      <path d="M10 21v-1" />
    </svg>
  ),
  proveedores: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M16 20V4a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" />
      <rect x="1" y="10" width="22" height="12" rx="2" />
      <path d="M12 14v4" />
      <path d="M10 16h4" />
    </svg>
  ),
  estaciones: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="6" width="12" height="16" rx="1" />
      <path d="M14 12h4a2 2 0 0 1 2 2v5a2 2 0 0 0 2 2" />
      <path d="M18 6V4a2 2 0 0 1 2-2" />
      <path d="M5 10h6" />
      <path d="M5 14h6" />
    </svg>
  ),
  empleados: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
      <path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  ),
  vehiculos: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M19 17h2c.6 0 1-.4 1-1v-3c0-.9-.7-1.7-1.5-1.9C18.7 10.6 16 10 16 10s-1.3-1.4-2.2-2.3c-.5-.4-1.1-.7-1.8-.7H5c-.6 0-1.1.4-1.4.9l-1.4 2.9A3.7 3.7 0 0 0 2 12v4c0 .6.4 1 1 1h2" />
      <circle cx="7" cy="17" r="2" />
      <path d="M9 17h6" />
      <circle cx="17" cy="17" r="2" />
    </svg>
  ),
  departamentos: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="7" width="20" height="14" rx="2" />
      <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" />
    </svg>
  ),
  usuarios: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 15c-3.87 0-7 1.79-7 4v2h14v-2c0-2.21-3.13-4-7-4Z" />
      <circle cx="12" cy="7" r="4" />
      <path d="M2 2l20 20" />
    </svg>
  ),
  reportes: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z" />
      <path d="M14 2v6h6" />
      <path d="M8 13h2" />
      <path d="M8 17h2" />
      <path d="M14 13h2" />
      <path d="M14 17h2" />
    </svg>
  ),
  auditoria: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z" />
      <path d="m9 12 2 2 4-4" />
    </svg>
  ),
  notificaciones: (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
      <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
    </svg>
  ),
}

/*
 * Categorías semánticas (Ley de Miller: 5 grupos ≤ 7±2).
 * Cada grupo lleva un encabezado legible y sus rutas.
 */
const navGroups = [
  {
    label: null, // General — sin etiqueta de grupo, Dashboard destaca solo
    items: [
      { to: '/dashboard', label: 'Dashboard', icon: icons.dashboard },
    ],
  },
  {
    label: 'Operaciones',
    items: [
      { to: '/solicitudes', label: 'Solicitudes', icon: icons.solicitudes },
      { to: '/solicitudes-recurrentes', label: 'Sol. Recurrentes', icon: icons.solicitudesRecurrentes },
      { to: '/tickets', label: 'Tickets', icon: icons.tickets },
      { to: '/despachos', label: 'Despachos', icon: icons.despachos },
      { to: '/cierres-diarios', label: 'Cierre Diario', icon: icons.cierreDiario },
    ],
  },
  {
    label: 'Inventario y Suministro',
    items: [
      { to: '/inventario', label: 'Inventario', icon: icons.inventario },
      { to: '/recepciones', label: 'Recepciones', icon: icons.recepciones },
      { to: '/tanques', label: 'Tanques', icon: icons.tanques },
      { to: '/tipos-combustible', label: 'Tipos Combustible', icon: icons.tiposCombustible },
      { to: '/proveedores', label: 'Proveedores', icon: icons.proveedores },
      { to: '/estaciones', label: 'Estaciones', icon: icons.estaciones },
    ],
  },
  {
    label: 'Organización',
    items: [
      { to: '/empleados', label: 'Empleados', icon: icons.empleados },
      { to: '/vehiculos', label: 'Vehículos', icon: icons.vehiculos },
      { to: '/departamentos', label: 'Departamentos', icon: icons.departamentos },
    ],
  },
  {
    label: 'Control y Sistema',
    items: [
      { to: '/reportes', label: 'Reportes', icon: icons.reportes },
      { to: '/auditoria', label: 'Auditoría', icon: icons.auditoria },
      { to: '/notificaciones', label: 'Notificaciones', icon: icons.notificaciones },
      { to: '/usuarios', label: 'Usuarios', icon: icons.usuarios },
    ],
  },
]

export default function Sidebar({ isOpen = false, onClose }) {
  const { user } = useAuth()
  const isSolicitanteOnly =
    user?.roles?.includes(ROLES.SOLICITANTE) &&
    !user?.roles?.includes(ROLES.ADMINISTRADOR) &&
    !user?.roles?.includes(ROLES.SUPERVISOR)

  // Filtrar grupos y sus items por rol según el RBAC
  const visibleGroups = navGroups
    .map((g) => ({
      ...g,
      items: g.items
        .filter((item) => canAccessRoute(user, item.to))
        .map((item) => {
          if (item.to === '/tickets' && isSolicitanteOnly) {
            return { ...item, label: 'Mis Tickets' }
          }
          return item
        }),
    }))
    .filter((g) => g.items.length > 0)

  return (
    <>
      {/* Overlay de fondo en móvil */}
      {isOpen && (
        <div
          className="fixed inset-0 z-40 bg-tinta/60 backdrop-blur-xs md:hidden"
          onClick={onClose}
          aria-hidden="true"
        />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-50 flex w-64 shrink-0 flex-col border-r border-acero/30 bg-tanque transition-transform duration-200 ease-in-out md:static md:w-60 md:translate-x-0 ${
          isOpen ? 'translate-x-0 shadow-2xl' : '-translate-x-full'
        }`}
      >
        {/* Logo / Branding y botón de cierre en móvil */}
        <div className="flex items-center justify-between border-b border-white/10 px-4 py-4 md:py-5">
          <div className="flex items-center gap-2.5">
            {/* Fuel gauge mini-icon */}
            <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-medidor">
              <path d="M5 8a7 7 0 0 1 14 0" />
              <circle cx="12" cy="12" r="2" />
              <path d="M12 2v2" />
              <path d="m4.93 4.93 1.41 1.41" />
              <path d="m17.66 6.34 1.41-1.41" />
              <path d="M12 14v4" />
            </svg>
            <span className="text-lg font-bold tracking-tight text-white">FuelTrack</span>
          </div>

          {/* Botón cerrar off-canvas en pantallas táctiles */}
          <button
            type="button"
            onClick={onClose}
            className="flex h-10 w-10 items-center justify-center rounded-md text-white/70 hover:bg-white/10 hover:text-white md:hidden min-h-[44px] min-w-[44px]"
            aria-label="Cerrar menú"
          >
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Navigation — scroll independiente del contenido principal */}
        <nav className="flex flex-1 flex-col gap-1 overflow-y-auto px-2 py-3 pb-20 md:pb-3" aria-label="Navegación principal">
          {visibleGroups.map((group, gi) => (
            <div key={gi} className={gi > 0 ? 'mt-2' : undefined}>
              {/* Encabezado de sección (Gestalt: Región Común) */}
              {group.label && (
                <h3 className="mb-1 px-3 text-[11px] font-bold uppercase tracking-wider text-white/50" aria-hidden="true">
                  {group.label}
                </h3>
              )}

              {group.items.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  onClick={onClose}
                  className={({ isActive }) =>
                    `group flex min-h-[44px] items-center gap-3 rounded-md border-l-4 px-3 py-2.5 text-sm font-medium transition-colors ${
                      isActive
                        ? 'border-medidor bg-white/12 text-white'
                        : 'border-transparent text-white/70 hover:bg-white/5 hover:text-white'
                    }`
                  }
                >
                  {({ isActive }) => (
                    <>
                      <span className={`shrink-0 ${isActive ? 'text-medidor' : 'text-white/50 group-hover:text-white/70'}`}>
                        {item.icon}
                      </span>
                      <span>{item.label}</span>
                    </>
                  )}
                </NavLink>
              ))}
            </div>
          ))}
        </nav>
      </aside>
    </>
  )
}

