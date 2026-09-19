/**
 * Utilidades centralizadas de validación y formateo para FuelTrack.
 * Soporta estándares de República Dominicana (RNC, Cédula, Teléfonos)
 * y reglas operacionales de combustibles y tanques.
 */

// --- RNC (Registro Nacional del Contribuyente - RD) ---
export function validateRnc(value) {
  if (!value || !String(value).trim()) {
    return 'El RNC es obligatorio.';
  }
  const clean = String(value).replace(/[\s-]/g, '');
  if (!/^\d+$/.test(clean)) {
    return 'El RNC debe contener únicamente dígitos numéricos.';
  }
  if (clean.length !== 9 && clean.length !== 11) {
    return 'El RNC debe tener exactamente 9 dígitos (personas jurídicas) u 11 dígitos (personas físicas).';
  }
  return null;
}

export function formatRnc(value) {
  if (!value) return '';
  const clean = String(value).replace(/[\s-]/g, '');
  if (clean.length === 9) {
    // 1-23-45678-9
    return `${clean.slice(0, 1)}-${clean.slice(1, 3)}-${clean.slice(3, 8)}-${clean.slice(8)}`;
  }
  if (clean.length === 11) {
    // 001-1234567-8
    return `${clean.slice(0, 3)}-${clean.slice(3, 10)}-${clean.slice(10)}`;
  }
  return clean;
}

// --- Cédula de Identidad y Electoral (RD) ---
export function validateCedula(value) {
  if (!value || !String(value).trim()) {
    return 'La cédula es obligatoria.';
  }
  const clean = String(value).replace(/[\s-]/g, '');
  if (!/^\d+$/.test(clean)) {
    return 'La cédula solo debe contener dígitos numéricos.';
  }
  if (clean.length !== 11) {
    return 'La cédula debe tener exactamente 11 dígitos (ej: 001-1234567-8).';
  }
  return null;
}

export function formatCedula(value) {
  if (!value) return '';
  const clean = String(value).replace(/[\s-]/g, '');
  if (clean.length === 11) {
    return `${clean.slice(0, 3)}-${clean.slice(3, 10)}-${clean.slice(10)}`;
  }
  return clean;
}

// --- Teléfonos República Dominicana (809, 829, 849) ---
export function validateTelefonoRD(value, required = true) {
  if (!value || !String(value).trim()) {
    return required ? 'El número telefónico es obligatorio.' : null;
  }
  const clean = String(value).replace(/[\s()+-]/g, '');
  if (!/^\d+$/.test(clean)) {
    return 'El teléfono debe contener solo dígitos.';
  }
  // Remove leading 1 if international Dominican format +1 809...
  const national = clean.length === 11 && clean.startsWith('1') ? clean.slice(1) : clean;
  if (national.length !== 10) {
    return 'El teléfono debe tener 10 dígitos (ej: 809-555-1234).';
  }
  const prefix = national.slice(0, 3);
  if (!['809', '829', '849'].includes(prefix)) {
    return 'El código de área debe ser 809, 829 u 849.';
  }
  return null;
}

export function formatTelefonoRD(value) {
  if (!value) return '';
  const clean = String(value).replace(/[\s()+-]/g, '');
  const national = clean.length === 11 && clean.startsWith('1') ? clean.slice(1) : clean;
  if (national.length === 10) {
    return `(${national.slice(0, 3)}) ${national.slice(3, 6)}-${national.slice(6)}`;
  }
  return clean;
}

export function normalizeTelefonoE164(value) {
  if (!value) return '';
  const trimmed = String(value).trim();
  const digits = trimmed.replace(/\D/g, '');
  if (trimmed.startsWith('+') && digits.length >= 8 && digits.length <= 15) {
    return `+${digits}`;
  }
  if (digits.length === 10) {
    return `+1${digits}`;
  }
  if (digits.length === 11 && digits.startsWith('1')) {
    return `+${digits}`;
  }
  if (digits.length >= 8 && digits.length <= 15) {
    return `+${digits}`;
  }
  return trimmed;
}

// --- Correo Electrónico ---
export function validateEmail(value, required = true) {
  if (!value || !String(value).trim()) {
    return required ? 'El correo electrónico es obligatorio.' : null;
  }
  const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!regex.test(String(value).trim())) {
    return 'Introduzca un correo electrónico válido (ej: usuario@empresa.com).';
  }
  return null;
}

// --- Código de Empleado ---
export function validateCodigoEmpleado(value) {
  if (!value || !String(value).trim()) {
    return 'El código de empleado es obligatorio.';
  }
  const clean = String(value).trim().toUpperCase();
  if (clean.length < 3 || clean.length > 20) {
    return 'El código debe tener entre 3 y 20 caracteres (ej: EMP-0042).';
  }
  if (!/^[A-Z0-9-]+$/.test(clean)) {
    return 'El código solo puede contener letras mayúsculas, números y guiones.';
  }
  return null;
}

// --- Placa de Vehículo (RD) ---
export function validatePlacaVehiculo(value) {
  if (!value || !String(value).trim()) {
    return 'La placa es obligatoria.';
  }
  const clean = String(value).trim().toUpperCase().replace(/[\s-]/g, '');
  if (clean.length < 5 || clean.length > 9) {
    return 'La placa debe tener entre 5 y 8 caracteres (ej: A123456, G987654).';
  }
  if (!/^[A-Z0-9]+$/.test(clean)) {
    return 'La placa solo debe contener letras y números.';
  }
  return null;
}

// --- Año de Fabricación ---
export function validateAnioVehiculo(value) {
  if (value === undefined || value === null || String(value).trim() === '') {
    return 'El año de fabricación es obligatorio.';
  }
  const num = Number(value);
  const currentYear = new Date().getFullYear();
  if (!Number.isInteger(num) || num < 1980 || num > currentYear + 1) {
    return `El año debe estar entre 1980 y ${currentYear + 1}.`;
  }
  return null;
}

// --- Capacidad de Tanque de Vehículo (galones) ---
export function validateCapacidadTanqueVehiculo(value) {
  if (value === undefined || value === null || String(value).trim() === '') {
    return 'La capacidad del tanque es obligatoria.';
  }
  const num = Number(value);
  if (isNaN(num) || num < 2 || num > 500) {
    return 'La capacidad del vehículo debe estar entre 2 y 500 galones.';
  }
  return null;
}

// --- Identificación de Tanque Estacionario ---
export function validateIdentificacionTanque(value) {
  if (!value || !String(value).trim()) {
    return 'La identificación del tanque es obligatoria.';
  }
  const clean = String(value).trim().toUpperCase();
  if (clean.length < 3 || clean.length > 25) {
    return 'La identificación debe tener entre 3 y 25 caracteres (ej: TNQ-DSL-01).';
  }
  if (!/^[A-Z0-9-_]+$/.test(clean)) {
    return 'Solo se permiten mayúsculas, números, guiones y guiones bajos.';
  }
  return null;
}

// Sugerencia de nomenclatura para tanques
export function suggestTanqueIdentificacion(tipoCombustibleNombre, correlativo = 1) {
  const code = (tipoCombustibleNombre || '').toLowerCase();
  let prefix = 'GEN';
  if (code.includes('gasoil') || code.includes('diesel') || code.includes('diésel')) prefix = 'DSL';
  else if (code.includes('premium')) prefix = 'GPR';
  else if (code.includes('regular')) prefix = 'GRG';
  else if (code.includes('gas') || code.includes('glp')) prefix = 'GLP';
  const numStr = String(correlativo).padStart(2, '0');
  return `TNQ-${prefix}-${numStr}`;
}

// --- Capacidad y Nivel Crítico de Tanques Estacionarios ---
export function validateCapacidadTanque(value) {
  if (value === undefined || value === null || String(value).trim() === '') {
    return 'La capacidad del tanque es obligatoria.';
  }
  const num = Number(value);
  if (isNaN(num) || num < 50 || num > 100000) {
    return 'La capacidad debe ser un número entre 50 y 100,000 galones.';
  }
  return null;
}

export function validateNivelCriticoTanque(nivelCritico, capacidad) {
  if (nivelCritico === undefined || nivelCritico === null || String(nivelCritico).trim() === '') {
    return 'El nivel crítico es obligatorio.';
  }
  const crit = Number(nivelCritico);
  const cap = Number(capacidad);
  if (isNaN(crit) || crit <= 0) {
    return 'El nivel crítico debe ser mayor que 0 galones.';
  }
  if (!isNaN(cap) && cap > 0 && crit >= cap) {
    return `El nivel crítico (${crit} gal) debe ser menor a la capacidad total (${cap} gal).`;
  }
  return null;
}

// --- Validación de Rango de Fechas ---
export function validateRangoFechas(fechaDesde, fechaHasta) {
  if (!fechaDesde || !fechaHasta) return null;
  if (new Date(fechaDesde) > new Date(fechaHasta)) {
    return 'La fecha inicial ("Desde") no puede ser posterior a la fecha final ("Hasta").';
  }
  return null;
}

// --- Validación genérica de texto mínimo ---
export function validateTextoMinimo(value, campo = 'Este campo', min = 3) {
  if (!value || !String(value).trim()) {
    return `${campo} es obligatorio.`;
  }
  if (String(value).trim().length < min) {
    return `${campo} debe tener al menos ${min} caracteres.`;
  }
  return null;
}

// --- Validación de volumen de combustible ---
export function validateCapacidadCombustible(value, campo = 'El volumen') {
  if (value === undefined || value === null || String(value).trim() === '') {
    return `${campo} es obligatorio.`;
  }
  const num = Number(value);
  if (isNaN(num) || num <= 0) {
    return `${campo} debe ser un número positivo mayor a 0 galones.`;
  }
  if (num > 100000) {
    return `${campo} no puede exceder 100,000 galones.`;
  }
  return null;
}

// Aliases para compatibilidad de nomenclatura
export const formatRNC = formatRnc;
export const validateRNC = validateRnc;
export const formatCedulaRD = formatCedula;
export const validateCedulaRD = validateCedula;
export const validatePlacaRD = validatePlacaVehiculo;
export const validateAnoVehiculo = validateAnioVehiculo;

