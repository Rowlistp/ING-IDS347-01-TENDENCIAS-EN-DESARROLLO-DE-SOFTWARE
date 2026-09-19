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

import { ROLES, ROUTE_ROLES as RBAC_ROUTE_ROLES, getDefaultRouteForUser } from '../utils/rbac'

export { ROLES }

export const ROUTE_ROLES = RBAC_ROUTE_ROLES

export const CIERRE_DIARIO_LECTURA_ROLES = [ROLES.AUDITOR, ROLES.CONSULTA]

export const LANDING_ROUTE_BY_ROLE = {
  [ROLES.ADMINISTRADOR]: '/dashboard',
  [ROLES.SUPERVISOR]: '/solicitudes',
  [ROLES.DESPACHADOR]: '/despachos',
  [ROLES.AUDITOR]: '/reportes',
  [ROLES.CONSULTA]: '/reportes',
  [ROLES.SOLICITANTE]: '/solicitudes',
}

export function rolesAllowRoute(userRoles, path) {
  const allowed = ROUTE_ROLES[path]
  if (!allowed) return false
  const roles = Array.isArray(userRoles) ? userRoles : (userRoles?.roles ?? [])
  if (roles.includes(ROLES.ADMINISTRADOR)) return true
  return roles.some((r) => allowed.includes(r))
}

export function landingRouteForRoles(userRoles) {
  return getDefaultRouteForUser(userRoles)
}
