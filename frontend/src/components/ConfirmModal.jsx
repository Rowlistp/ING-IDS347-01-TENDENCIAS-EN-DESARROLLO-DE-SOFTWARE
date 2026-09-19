import React from 'react'

/**
 * ConfirmModal
 * Modal de advertencia y confirmación con la estética industrial de FuelTrack.
 *
 * Props:
 * - isOpen: boolean
 * - title: string
 * - message: string | ReactNode
 * - consequences: string[] (opcional - lista de advertencias o impactos)
 * - type: 'danger' | 'warning' | 'info' | 'success' (default: 'danger')
 * - confirmText: string (default: 'Confirmar')
 * - cancelText: string (default: 'Cancelar')
 * - onConfirm: () => void
 * - onClose: () => void
 * - isLoading: boolean (opcional)
 */
export default function ConfirmModal({
  isOpen,
  title,
  message,
  consequences = [],
  type = 'danger',
  confirmText = 'Confirmar',
  cancelText = 'Cancelar',
  onConfirm,
  onClose,
  isLoading = false,
}) {
  if (!isOpen) return null

  const typeConfig = {
    danger: {
      iconBg: 'bg-peligro/10 text-peligro border-peligro/30',
      btnBg: 'bg-peligro hover:bg-peligro/90 text-white',
      bannerBg: 'bg-peligro/5 border-peligro/20 text-tinta',
      bannerTitle: 'text-peligro',
      icon: (
        <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
      ),
    },
    warning: {
      iconBg: 'bg-medidor/15 text-medidor border-medidor/40',
      btnBg: 'bg-medidor hover:bg-medidor/90 text-white',
      bannerBg: 'bg-medidor/10 border-medidor/30 text-tinta',
      bannerTitle: 'text-medidor',
      icon: (
        <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
    },
    info: {
      iconBg: 'bg-info/10 text-info border-info/30',
      btnBg: 'bg-info hover:bg-info/90 text-white',
      bannerBg: 'bg-info/5 border-info/20 text-tinta',
      bannerTitle: 'text-info',
      icon: (
        <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
    },
    success: {
      iconBg: 'bg-exito/10 text-exito border-exito/30',
      btnBg: 'bg-exito hover:bg-exito/90 text-white',
      bannerBg: 'bg-exito/5 border-exito/20 text-tinta',
      bannerTitle: 'text-exito',
      icon: (
        <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
    },
  }

  const currentConfig = typeConfig[type] || typeConfig.danger

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-tinta/50 backdrop-blur-xs animate-fadeIn">
      <div
        className="w-full max-w-md rounded-2xl border border-acero/20 bg-white p-6 shadow-xl transition-all"
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-start gap-4">
          <div
            className={`flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-xl border ${currentConfig.iconBg}`}
          >
            {currentConfig.icon}
          </div>
          <div className="space-y-1">
            <h3 className="text-base font-bold text-tanque leading-tight">{title}</h3>
            <div className="text-xs text-acero leading-relaxed">{message}</div>
          </div>
        </div>

        {consequences && consequences.length > 0 && (
          <div className={`my-4 rounded-lg border p-3 text-xs space-y-1 ${currentConfig.bannerBg}`}>
            <div className={`font-semibold flex items-center gap-1.5 ${currentConfig.bannerTitle}`}>
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              Consecuencias operativas:
            </div>
            <ul className="list-disc list-inside text-xs text-acero space-y-0.5 pl-1">
              {consequences.map((item, idx) => (
                <li key={idx}>{item}</li>
              ))}
            </ul>
          </div>
        )}

        <div className="flex flex-col-reverse sm:flex-row sm:items-center justify-end gap-3 pt-3">
          <button
            type="button"
            disabled={isLoading}
            onClick={onClose}
            className="flex min-h-[44px] items-center justify-center rounded-lg border border-acero/30 bg-white px-4 py-2 text-xs font-semibold text-tinta hover:bg-fondo transition disabled:opacity-50"
          >
            {cancelText}
          </button>
          <button
            type="button"
            disabled={isLoading}
            onClick={onConfirm}
            className={`flex min-h-[44px] items-center justify-center gap-2 rounded-lg px-5 py-2 text-xs font-semibold shadow-sm transition disabled:opacity-50 ${currentConfig.btnBg}`}
          >
            {isLoading && (
              <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
              </svg>
            )}
            {confirmText}
          </button>
        </div>
      </div>
    </div>
  )
}
