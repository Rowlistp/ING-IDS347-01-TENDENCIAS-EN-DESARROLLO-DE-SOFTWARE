import { useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { getRoleBadgeInfo } from '../utils/rbac'

/* ────────────────────────────────────────────────────────────────────────────
 * Header — Barra superior con breadcrumb dinámico y zona de usuario.
 *
 *   · Breadcrumb: calculado desde la ruta activa para orientación espacial
 *     (Visibilidad del Estado del Sistema — Nielsen #1).
 *   · Avatar con iniciales: reconocimiento antes que recuerdo (Nielsen #6).
 *   · Badge de rol: da contexto de los permisos del usuario.
 *   · Botón logout min-h-[44px] (Ley de Fitts).
 *   · Contraste: todos los textos pasan WCAG AA sobre bg-tanque (#16333a).
 * ──────────────────────────────────────────────────────────────────────────── */

// Mapeo de rutas → { sección, página } para el breadcrumb
const routeMap = {
  '/dashboard':               { section: null, page: 'Dashboard' },
  '/solicitudes':             { section: 'Operaciones', page: 'Solicitudes' },
  '/solicitudes-recurrentes': { section: 'Operaciones', page: 'Sol. Recurrentes' },
  '/tickets':                 { section: 'Operaciones', page: 'Tickets' },
  '/despachos':               { section: 'Operaciones', page: 'Despachos' },
  '/cierres-diarios':         { section: 'Operaciones', page: 'Cierre Diario' },
  '/inventario':              { section: 'Inventario y Suministro', page: 'Inventario' },
  '/recepciones':             { section: 'Inventario y Suministro', page: 'Recepciones' },
  '/tanques':                 { section: 'Inventario y Suministro', page: 'Tanques' },
  '/tipos-combustible':       { section: 'Inventario y Suministro', page: 'Tipos Combustible' },
  '/proveedores':             { section: 'Inventario y Suministro', page: 'Proveedores' },
  '/estaciones':              { section: 'Inventario y Suministro', page: 'Estaciones' },
  '/empleados':               { section: 'Organización', page: 'Empleados' },
  '/vehiculos':               { section: 'Organización', page: 'Vehículos' },
  '/departamentos':           { section: 'Organización', page: 'Departamentos' },
  '/reportes':                { section: 'Control y Sistema', page: 'Reportes' },
  '/auditoria':               { section: 'Control y Sistema', page: 'Auditoría' },
  '/notificaciones':          { section: 'Control y Sistema', page: 'Notificaciones' },
  '/usuarios':                { section: 'Control y Sistema', page: 'Usuarios' },
}

// Separa las iniciales del username (hasta 2 letras)
function getInitials(name) {
  if (!name) return '?'
  const parts = name.trim().split(/\s+/)
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase()
  return name.slice(0, 2).toUpperCase()
}

export default function Header({ onToggleSidebar }) {
  const { user, logout } = useAuth()
  const { pathname } = useLocation()

  const route = routeMap[pathname] ?? { section: null, page: 'Panel' }
  const roleBadge = getRoleBadgeInfo(user)

  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b border-acero/20 bg-white px-3 sm:px-6">
      {/* Zona izquierda: Menú hamburguesa (móvil) + Breadcrumb */}
      <div className="flex items-center gap-2 overflow-hidden">
        {/* Botón hamburguesa en móvil */}
        <button
          type="button"
          onClick={onToggleSidebar}
          className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md text-acero hover:bg-fondo hover:text-tanque md:hidden min-h-[44px] min-w-[44px] transition-colors"
          aria-label="Abrir menú de navegación"
        >
          <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>

        {/* Título en móvil (<768px) */}
        <h1 className="text-base font-bold text-tanque truncate md:hidden">
          {route.page}
        </h1>

        {/* Breadcrumb completo en desktop (≥768px) */}
        <nav aria-label="Breadcrumb" className="hidden md:block">
          <ol className="flex items-center gap-1.5 text-sm">
            <li className="text-acero">
              {/* Home icon */}
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="inline-block">
                <path d="m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
                <polyline points="9 22 9 12 15 12 15 22" />
              </svg>
            </li>
            {route.section && (
              <>
                <li aria-hidden="true" className="text-acero/40">/</li>
                <li className="text-acero">{route.section}</li>
              </>
            )}
            <li aria-hidden="true" className="text-acero/40">/</li>
            <li className="font-semibold text-tanque" aria-current="page">{route.page}</li>
          </ol>
        </nav>
      </div>

      {/* User area */}
      <div className="flex items-center gap-2 sm:gap-3">
        {/* Initials avatar */}
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-tanque text-xs font-bold text-white" aria-hidden="true">
          {getInitials(user?.nombreUsuario)}
        </div>

        <div className="hidden sm:flex flex-col items-start gap-0.5">
          <p className="text-sm font-semibold leading-tight text-tinta">{user?.nombreUsuario ?? 'Usuario'}</p>
          <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-semibold border ${roleBadge.bg}`}>
            <span className={`w-1.5 h-1.5 rounded-full ${roleBadge.dot}`} />
            {roleBadge.label}
          </span>
        </div>

        <div className="hidden sm:block mx-1 h-6 w-px bg-acero/20" aria-hidden="true" />

        <button
          type="button"
          onClick={logout}
          className="flex min-h-[44px] min-w-[44px] sm:min-w-0 items-center justify-center gap-1.5 rounded-md px-2.5 sm:px-3 py-2 text-sm font-medium text-acero transition-colors hover:bg-fondo hover:text-peligro"
          title="Cerrar sesión"
        >
          {/* Logout icon */}
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
            <polyline points="16 17 21 12 16 7" />
            <line x1="21" y1="12" x2="9" y2="12" />
          </svg>
          <span className="hidden sm:inline">Salir</span>
        </button>
      </div>
    </header>
  )
}

