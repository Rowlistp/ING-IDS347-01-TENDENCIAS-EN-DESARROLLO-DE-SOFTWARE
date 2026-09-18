import { Outlet } from 'react-router-dom'
import Header from './Header'
import Sidebar from './Sidebar'

export default function Layout({ children }) {
  return (
    <div className="flex h-screen overflow-hidden bg-fondo">
      {/* Sidebar con su propio scroll independiente (overflow-y-auto en nav interno) */}
      <Sidebar />
      <div className="flex flex-1 flex-col overflow-hidden">
        <Header />
        <main className="flex-1 overflow-y-auto">
          {children ?? <Outlet />}
        </main>
      </div>
    </div>
  )
}
