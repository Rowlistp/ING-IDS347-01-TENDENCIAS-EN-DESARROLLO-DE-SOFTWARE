import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import Header from './Header'
import Sidebar from './Sidebar'
import BottomNav from './BottomNav'

export default function Layout({ children }) {
  const [sidebarOpen, setSidebarOpen] = useState(false)

  return (
    <div className="flex h-screen w-full max-w-full overflow-hidden bg-fondo">
      {/* Sidebar (fijo en desktop ≥768px, off-canvas en móviles/tablets) */}
      <Sidebar isOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />

      <div className="flex flex-1 flex-col min-w-0 overflow-hidden">
        <Header onToggleSidebar={() => setSidebarOpen((prev) => !prev)} />
        <main className="flex-1 overflow-y-auto overflow-x-hidden pb-16 md:pb-0">
          {children ?? <Outlet />}
        </main>
      </div>

      {/* Bottom Navigation para móviles (<768px) */}
      <BottomNav onToggleSidebar={() => setSidebarOpen(true)} />
    </div>
  )
}

