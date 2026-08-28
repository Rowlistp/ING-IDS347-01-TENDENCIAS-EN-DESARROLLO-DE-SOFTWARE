import { NavLink } from 'react-router-dom'

const navItems = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/usuarios', label: 'Usuarios' },
  { to: '/empleados', label: 'Empleados' },
  { to: '/vehiculos', label: 'Vehículos' },
  { to: '/departamentos', label: 'Departamentos' },
  { to: '/solicitudes', label: 'Solicitudes' },
  { to: '/tickets', label: 'Tickets' },
]

export default function Sidebar() {
  return (
    <aside className="flex w-60 shrink-0 flex-col border-r border-gray-200 bg-white">
      <div className="px-4 py-5">
        <span className="text-lg font-bold text-gray-800">Combustible App</span>
      </div>
      <nav className="flex flex-col gap-1 px-2">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              `rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-blue-50 text-blue-700'
                  : 'text-gray-600 hover:bg-gray-100'
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
