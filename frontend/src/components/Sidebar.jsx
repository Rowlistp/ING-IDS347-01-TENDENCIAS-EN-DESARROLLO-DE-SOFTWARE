import { NavLink } from 'react-router-dom'
import { getUser } from '../services/auth'
import { rolesAllowRoute } from '../routes/accessMatrix'

const navItems = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/usuarios', label: 'Usuarios' },
  { to: '/empleados', label: 'Empleados' },
  { to: '/vehiculos', label: 'Vehículos' },
  { to: '/departamentos', label: 'Departamentos' },
  { to: '/solicitudes', label: 'Solicitudes' },
  { to: '/solicitudes-recurrentes', label: 'Solicitudes Recurrentes' },
  { to: '/tickets', label: 'Tickets' },
  { to: '/inventario', label: 'Inventario' },
  { to: '/recepciones', label: 'Recepciones' },
  { to: '/despachos', label: 'Despachos' },
  { to: '/estaciones', label: 'Estaciones' },
  { to: '/cierres-diarios', label: 'Cierre Diario' },
  { to: '/tanques', label: 'Tanques' },
  { to: '/proveedores', label: 'Proveedores' },
  { to: '/tipos-combustible', label: 'Tipos de Combustible' },
  { to: '/auditoria', label: 'Auditoría' },
  { to: '/notificaciones', label: 'Notificaciones' },
  { to: '/reportes', label: 'Reportes' },
]

export default function Sidebar() {
  // Unión de lo que permite cada rol del usuario: si al menos uno de sus
  // roles tiene acceso a la pantalla, el link se muestra.
  const userRoles = getUser()?.roles ?? []
  const visibleItems = navItems.filter((item) => rolesAllowRoute(userRoles, item.to))

  return (
    <aside className="flex w-60 shrink-0 flex-col border-r border-acero/30 bg-tanque">
      <div className="border-b border-white/10 px-4 py-5">
        <span className="text-lg font-bold tracking-tight text-white">FuelTrack</span>
      </div>
      <nav className="flex flex-col gap-0.5 px-2 py-3">
        {visibleItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              `rounded-sm border-l-2 px-3 py-2 text-sm font-medium transition-colors ${
                isActive
                  ? 'border-medidor bg-white/10 text-white'
                  : 'border-transparent text-white/60 hover:bg-white/5 hover:text-white'
              }`
            }
          >
            {item.label}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
