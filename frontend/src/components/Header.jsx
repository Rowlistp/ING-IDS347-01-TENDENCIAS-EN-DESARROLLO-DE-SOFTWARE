export default function Header() {
  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b border-gray-200 bg-white px-6">
      <span className="text-sm text-gray-500">Panel de administración</span>
      <div className="flex items-center gap-3">
        <span className="text-sm font-medium text-gray-700">Usuario</span>
        <div className="h-8 w-8 rounded-full bg-gray-200" />
      </div>
    </header>
  )
}
