export default function PageContainer({ title, children }) {
  return (
    <div className="p-6">
      <h1 className="text-2xl font-semibold text-tanque">{title}</h1>
      {children ? (
        <div className="mt-4">{children}</div>
      ) : (
        <p className="mt-2 text-acero">Pantalla en construcción.</p>
      )}
    </div>
  )
}
