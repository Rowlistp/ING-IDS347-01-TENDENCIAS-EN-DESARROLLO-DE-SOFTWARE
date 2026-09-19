export const inputCls =
  'w-full rounded-md border border-acero/40 px-3 py-2 text-sm text-tinta bg-white focus:outline-none focus:ring-2 focus:ring-tanque/50 focus:border-tanque transition-colors'

export const inputClsError =
  'w-full rounded-md border border-peligro/80 px-3 py-2 text-sm text-tinta bg-peligro/5 focus:outline-none focus:ring-2 focus:ring-peligro/40 focus:border-peligro transition-colors'

export default function Field({ label, required, error, hint, children }) {
  return (
    <div className="space-y-1">
      {label && (
        <label className="block text-sm font-medium text-acero">
          {label}
          {required && <span className="ml-1 text-peligro font-bold" title="Campo obligatorio">*</span>}
        </label>
      )}
      {children}
      {hint && !error && (
        <p className="text-xs text-acero/80">{hint}</p>
      )}
      {error && (
        <p className="text-xs text-peligro font-medium flex items-center gap-1 mt-1">
          <svg className="w-3.5 h-3.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
          </svg>
          {error}
        </p>
      )}
    </div>
  )
}

