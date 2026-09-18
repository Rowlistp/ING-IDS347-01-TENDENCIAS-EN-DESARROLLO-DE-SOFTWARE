/* ────────────────────────────────────────────────────────────────────────────
 * RBAC (Role-Based Access Control) Configuration and Helpers
 * Conforme a la sección 3 y 4 del SRS de FuelTrack.
 * ──────────────────────────────────────────────────────────────────────────── */

export const ROLES = {
  ADMINISTRADOR: 'Administrador',
  SUPERVISOR: 'Supervisor',
  DESPACHADOR: 'Despachador',
  SOLICITANTE: 'Solicitante',
  AUDITOR: 'Auditor',
  CONSULTA: 'Consulta',
}

/**
 * Mapeo canónico de rutas y los roles con permiso de acceso.
 */
export const ROUTE_ROLES = {
  '/dashboard': [ROLES.ADMINISTRADOR],
  '/solicitudes': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.SOLICITANTE, ROLES.AUDITOR],
  '/solicitudes-recurrentes': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR],
  '/tickets': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.DESPACHADOR, ROLES.SOLICITANTE, ROLES.AUDITOR, ROLES.CONSULTA],
  '/despachos': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.DESPACHADOR, ROLES.AUDITOR, ROLES.CONSULTA],
  '/cierres-diarios': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.DESPACHADOR, ROLES.AUDITOR, ROLES.CONSULTA],
  '/inventario': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.AUDITOR, ROLES.CONSULTA],
  '/recepciones': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR],
  '/tanques': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR],
  '/tipos-combustible': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR],
  '/proveedores': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR],
  '/estaciones': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.DESPACHADOR, ROLES.AUDITOR, ROLES.CONSULTA],
  '/empleados': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.AUDITOR],
  '/vehiculos': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.AUDITOR],
  '/departamentos': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.AUDITOR],
  '/reportes': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.AUDITOR, ROLES.CONSULTA],
  '/auditoria': [ROLES.ADMINISTRADOR, ROLES.AUDITOR],
  '/notificaciones': [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.AUDITOR],
  '/usuarios': [ROLES.ADMINISTRADOR],
}

/**
 * Obtiene los roles del usuario como un array normalizado.
 */
export function getUserRoles(user) {
  if (!user) return []
  if (Array.isArray(user.roles)) return user.roles
  if (Array.isArray(user)) return user
  return []
}

/**
 * Verifica si el usuario posee al menos uno de los roles especificados.
 */
export function hasAnyRole(user, allowedRoles = []) {
  if (!allowedRoles || allowedRoles.length === 0) return true
  const userRoles = getUserRoles(user)
  if (userRoles.includes(ROLES.ADMINISTRADOR)) return true
  return userRoles.some((r) => allowedRoles.includes(r))
}

/**
 * Evalúa si el usuario autenticado tiene permiso para acceder a una ruta específica.
 */
export function canAccessRoute(user, path) {
  const allowedRoles = ROUTE_ROLES[path]
  if (!allowedRoles) return true
  return hasAnyRole(user, allowedRoles)
}

/**
 * Retorna la ruta inicial de aterrizaje óptima según el rol del usuario.
 */
export function getDefaultRouteForUser(user) {
  const roles = getUserRoles(user)
  if (roles.includes(ROLES.ADMINISTRADOR)) return '/dashboard'
  if (roles.includes(ROLES.SUPERVISOR)) return '/solicitudes'
  if (roles.includes(ROLES.DESPACHADOR)) return '/despachos'
  if (roles.includes(ROLES.SOLICITANTE)) return '/solicitudes'
  if (roles.includes(ROLES.AUDITOR) || roles.includes(ROLES.CONSULTA)) return '/reportes'
  return '/solicitudes'
}

/**
 * Determina si el usuario solo tiene roles de lectura / inspección.
 */
export function isReadOnlyRole(user) {
  const roles = getUserRoles(user)
  if (roles.includes(ROLES.ADMINISTRADOR) || roles.includes(ROLES.SUPERVISOR) || roles.includes(ROLES.DESPACHADOR) || roles.includes(ROLES.SOLICITANTE)) {
    return false
  }
  return roles.includes(ROLES.AUDITOR) || roles.includes(ROLES.CONSULTA)
}

/**
 * Autoriza aprobación / rechazo de solicitudes de combustible (SRS 3.2).
 */
export function canApproveRequests(user) {
  return hasAnyRole(user, [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR])
}

/**
 * Autoriza emisión o anulación manual de tickets digitales (SRS 3.2).
 */
export function canEmitTickets(user) {
  return hasAnyRole(user, [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR])
}

/**
 * Autoriza ejecución y registro de despacho con escáner QR (SRS 3.3).
 */
export function canPerformDispatch(user) {
  return hasAnyRole(user, [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.DESPACHADOR])
}

/**
 * Autoriza ajuste manual de existencias de inventario (SRS 3.2).
 */
export function canAdjustInventory(user) {
  return hasAnyRole(user, [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR])
}

/**
 * Autoriza creación de cierre diario de turno o estación (SRS 3.3).
 */
export function canCreateDailyClose(user) {
  return hasAnyRole(user, [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR, ROLES.DESPACHADOR])
}

/**
 * Autoriza creación y modificación en catálogos maestros.
 */
export function canManageCatalogs(user) {
  return hasAnyRole(user, [ROLES.ADMINISTRADOR, ROLES.SUPERVISOR])
}

/**
 * Paleta de colores e información de insignia para el Header según el rol principal.
 */
export function getRoleBadgeInfo(user) {
  const roles = getUserRoles(user)
  if (roles.includes(ROLES.ADMINISTRADOR)) {
    return { label: 'Administrador', bg: 'bg-tanque text-white border-tanque', dot: 'bg-medidor' }
  }
  if (roles.includes(ROLES.SUPERVISOR)) {
    return { label: 'Supervisor', bg: 'bg-sky-900 text-sky-100 border-sky-700', dot: 'bg-sky-400' }
  }
  if (roles.includes(ROLES.DESPACHADOR)) {
    return { label: 'Despachador', bg: 'bg-emerald-900 text-emerald-100 border-emerald-700', dot: 'bg-emerald-400' }
  }
  if (roles.includes(ROLES.SOLICITANTE)) {
    return { label: 'Solicitante', bg: 'bg-indigo-900 text-indigo-100 border-indigo-700', dot: 'bg-indigo-400' }
  }
  if (roles.includes(ROLES.AUDITOR)) {
    return { label: 'Auditor', bg: 'bg-purple-900 text-purple-100 border-purple-700', dot: 'bg-purple-400' }
  }
  if (roles.includes(ROLES.CONSULTA)) {
    return { label: 'Consulta', bg: 'bg-zinc-800 text-zinc-200 border-zinc-600', dot: 'bg-zinc-400' }
  }
  return { label: 'Usuario', bg: 'bg-acero/20 text-tinta border-acero/30', dot: 'bg-acero' }
}
