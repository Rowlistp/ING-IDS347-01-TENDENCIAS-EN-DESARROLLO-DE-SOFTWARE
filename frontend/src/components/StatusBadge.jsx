export default function StatusBadge({ active, activeText = 'Activo', inactiveText = 'Inactivo' }) {
  return (
    <span
      className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${
        active ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'
      }`}
    >
      {active ? activeText : inactiveText}
    </span>
  )
}
