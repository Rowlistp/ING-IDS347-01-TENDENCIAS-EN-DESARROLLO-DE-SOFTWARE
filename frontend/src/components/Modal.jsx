export default function Modal({ title, onClose, children }) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-tinta/60 backdrop-blur-xs p-2 sm:p-4">
      <div className="flex max-h-[95vh] sm:max-h-[90vh] w-full max-w-lg flex-col rounded-sm border border-acero/20 bg-white shadow-xl">
        <div className="flex shrink-0 items-center justify-between border-b border-acero/20 px-4 sm:px-6 py-3 sm:py-4">
          <h2 className="text-base font-semibold text-tanque truncate pr-2">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md text-2xl leading-none text-acero hover:bg-fondo hover:text-tinta min-h-[44px] min-w-[44px] transition-colors"
            aria-label="Cerrar modal"
          >
            &times;
          </button>
        </div>
        <div className="overflow-y-auto px-4 sm:px-6 py-4">{children}</div>
      </div>
    </div>
  )
}

