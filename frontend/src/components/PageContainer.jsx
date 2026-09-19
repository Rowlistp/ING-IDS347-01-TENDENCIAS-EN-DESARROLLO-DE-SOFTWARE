export default function PageContainer({ title, children }) {
  return (
    <div className="p-3 sm:p-4 md:p-6 w-full max-w-full overflow-x-hidden">
      <h1 className="text-xl sm:text-2xl font-semibold text-tanque tracking-tight">{title}</h1>
      {children ? (
        <div className="mt-3 sm:mt-4">{children}</div>
      ) : (
        <p className="mt-2 text-sm text-acero">Pantalla en construcción.</p>
      )}
    </div>
  )
}

