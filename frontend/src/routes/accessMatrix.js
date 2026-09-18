// Matriz de acceso por rol y pantalla, según la sección 4.2 de
// FuelTrack_Backend_QA_Auditoria_SRS_18sep.pdf, verificada contra los
// [Authorize(Roles=...)] reales de cada controller (2026-09-18).
//
// No usar docs/08-ROLES-PERMISOS.md como fuente para esta matriz: quedó
// desactualizado/incompleto frente a los permisos reales del backend.
//
// Nota: en Tickets y Solicitudes, el rol Solicitante sí tiene acceso a la
// pantalla, pero el backend (OwnerFilter en TicketsController.GetAll/GetById
// y SolicitudesController.GetAll) ya devuelve solo sus propios registros —
// no es necesario ni correcto ocultar la pantalla completa para ese rol.

export const ROLES = {
  ADMINISTRADOR: 'Administrador',
  SUPERVISOR: 'Supervisor',
  DESPACHADOR: 'Despachador',
  AUDITOR: 'Auditor',
  CONSULTA: 'Consulta',
  SOLICITANTE: 'Solicitante',
}

const { ADMINISTRADOR, SUPERVISOR, DESPACHADOR, AUDITOR, CONSULTA, SOLICITANTE } = ROLES

const TODOS = [ADMINISTRADOR, SUPERVISOR, DESPACHADOR, AUDITOR, CONSULTA, SOLICITANTE]

// path -> roles con acceso a esa pantalla (ruta + link de Sidebar)
export const ROUTE_ROLES = {
  '/dashboard': [ADMINISTRADOR],
  '/usuarios': [ADMINISTRADOR],
  '/empleados': TODOS,
  '/vehiculos': TODOS,
  '/departamentos': TODOS,
  '/solicitudes': TODOS,
  '/inventario': TODOS,
  '/recepciones': TODOS,
  '/tanques': TODOS,
  '/proveedores': TODOS,
  '/tipos-combustible': TODOS,
  '/solicitudes-recurrentes': [ADMINISTRADOR, SUPERVISOR],
  '/tickets': TODOS,
  '/despachos': [ADMINISTRADOR, SUPERVISOR, DESPACHADOR, AUDITOR, CONSULTA],
  '/estaciones': [ADMINISTRADOR, SUPERVISOR, DESPACHADOR, AUDITOR, CONSULTA],
  '/cierres-diarios': [ADMINISTRADOR, SUPERVISOR, DESPACHADOR, AUDITOR],
  '/auditoria': [ADMINISTRADOR, AUDITOR],
  '/notificaciones': [ADMINISTRADOR, SUPERVISOR, AUDITOR],
  '/reportes': [ADMINISTRADOR, AUDITOR],
}

// Auditor tiene acceso de solo lectura a Cierre Diario (ve la pantalla y el
// historial, pero no puede generar un cierre nuevo — POST /cierres-diarios
// solo permite Administrador/Supervisor/Despachador).
export const CIERRE_DIARIO_LECTURA_ROLES = [AUDITOR]

// Pantalla de aterrizaje por rol al entrar por "/": no es la primera fila de
// la matriz en orden de tabla (eso llevaría a todos los roles no-Admin al
// mismo catálogo genérico de Empleados), sino la pantalla más relevante para
// la función de cada rol, dentro de lo que ese rol tiene permitido ver.
export const LANDING_ROUTE_BY_ROLE = {
  [ADMINISTRADOR]: '/dashboard',
  [SUPERVISOR]: '/solicitudes',
  [DESPACHADOR]: '/despachos',
  [AUDITOR]: '/auditoria',
  [CONSULTA]: '/inventario',
  [SOLICITANTE]: '/solicitudes',
}

export function rolesAllowRoute(userRoles, path) {
  const allowed = ROUTE_ROLES[path]
  if (!allowed) return false
  return (userRoles ?? []).some((r) => allowed.includes(r))
}

// Se recorre en orden de prioridad fijo (no en el orden en que el backend
// devuelva los roles del usuario) para que un usuario con varios roles
// siempre aterrice de forma predecible en la pantalla del rol de mayor
// jerarquía/acceso que tenga.
export function landingRouteForRoles(userRoles) {
  const roles = userRoles ?? []
  for (const role of TODOS) {
    if (roles.includes(role) && LANDING_ROUTE_BY_ROLE[role]) {
      return LANDING_ROUTE_BY_ROLE[role]
    }
  }
  return '/login'
}
