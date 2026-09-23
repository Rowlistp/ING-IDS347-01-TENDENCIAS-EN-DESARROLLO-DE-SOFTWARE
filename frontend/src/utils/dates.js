// Una fecha civil no representa medianoche UTC: conservar su día en cualquier zona.
export function parseCivilDate(value) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ?? '')
  return match ? new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]), 12) : new Date(value)
}

export function formatDate(value, options) {
  return value ? parseCivilDate(value).toLocaleDateString('es-DO', options) : '—'
}

export function formatDateTime(value) {
  if (!value) return '—'
  return /^\d{4}-\d{2}-\d{2}$/.test(value)
    ? formatDate(value)
    : new Date(value).toLocaleString('es-DO')
}
