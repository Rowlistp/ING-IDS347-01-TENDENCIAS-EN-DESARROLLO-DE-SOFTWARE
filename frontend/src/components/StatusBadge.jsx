// "Bandas de medidor": bordes definidos, sin el look de pill genérico de SaaS.
// Cada variante mapea a un token semántico exacto de la paleta FuelTrack —
// el ámbar (medidor) queda reservado a estados que sí requieren atención real
// (ej. ProximoAVencer), no se usa como decoración.
//
// "orange" y "yellow" usan fondo sólido + texto oscuro (no la banda clara con
// texto de color) porque medidor/advertencia como texto sobre fondo claro no
// alcanzan 4.5:1 de contraste (WCAG AA); con fondo sólido y texto tinta llegan
// a ~7.8:1 y ~5.6:1 respectivamente, sin aproximar los valores de la paleta.
const VARIANT_CLS = {
  green: 'border-exito/40 bg-exito/10 text-exito',
  yellow: 'border-advertencia bg-advertencia text-tinta',
  red: 'border-peligro/40 bg-peligro/10 text-peligro',
  gray: 'border-acero/30 bg-acero/10 text-acero',
  blue: 'border-info/40 bg-info/10 text-info',
  orange: 'border-medidor bg-medidor text-tinta',
  purple: 'border-tanque/30 bg-tanque/10 text-tanque',
}

export default function StatusBadge({ active, activeText = 'Activo', inactiveText = 'Inactivo', label, variant }) {
  const base = 'inline-flex items-center rounded-sm border px-2 py-0.5 text-xs font-semibold uppercase tracking-wide'

  if (label !== undefined) {
    return (
      <span className={`${base} ${VARIANT_CLS[variant] ?? VARIANT_CLS.gray}`}>
        {label}
      </span>
    )
  }

  return (
    <span className={`${base} ${active ? VARIANT_CLS.green : VARIANT_CLS.gray}`}>
      {active ? activeText : inactiveText}
    </span>
  )
}
