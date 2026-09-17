export default function Modal({ title, onClose, children }) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-tinta/50 p-4">
      <div className="flex max-h-[90vh] w-full max-w-lg flex-col rounded-sm border border-acero/20 bg-white shadow-xl">
        <div className="flex shrink-0 items-center justify-between border-b border-acero/20 px-6 py-4">
          <h2 className="text-base font-semibold text-tanque">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            className="text-xl leading-none text-acero hover:text-tinta"
          >
            &times;
          </button>
        </div>
        <div className="overflow-y-auto px-6 py-4">{children}</div>
      </div>
    </div>
  )
}
