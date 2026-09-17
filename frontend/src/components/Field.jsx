export const inputCls =
  'w-full rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta focus:outline-none focus:ring-2 focus:ring-tanque/50 focus:border-tanque'

export default function Field({ label, children }) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-acero">{label}</label>
      {children}
    </div>
  )
}
