const VARIANT_CLS = {
  green: 'bg-green-100 text-green-800',
  yellow: 'bg-yellow-100 text-yellow-800',
  red: 'bg-red-100 text-red-800',
  gray: 'bg-gray-100 text-gray-600',
}

export default function StatusBadge({ active, activeText = 'Activo', inactiveText = 'Inactivo', label, variant }) {
  if (label !== undefined) {
    return (
      <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${VARIANT_CLS[variant] ?? VARIANT_CLS.gray}`}>
        {label}
      </span>
    )
  }

  return (
    <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${active ? VARIANT_CLS.green : VARIANT_CLS.gray}`}>
      {active ? activeText : inactiveText}
    </span>
  )
}
