import { useAuth } from '../hooks/useAuth'

export default function Header() {
  const { user, logout } = useAuth()

  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b border-acero/20 bg-tanque px-6">
      <span className="text-sm text-white/60">Panel de administración</span>
      <div className="flex items-center gap-3">
        <span className="text-sm font-medium text-white">{user?.nombreUsuario ?? 'Usuario'}</span>
        <div className="h-8 w-8 rounded-full bg-white/10" />
        <button
          type="button"
          onClick={logout}
          className="rounded-md border border-white/20 px-3 py-1.5 text-sm text-white/80 hover:bg-white/10"
        >
          Cerrar sesión
        </button>
      </div>
    </header>
  )
}
