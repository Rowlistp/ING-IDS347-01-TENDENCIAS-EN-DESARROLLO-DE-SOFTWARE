# ADR-002 — Esquema híbrido ECDSA + ML-DSA

- **Estado:** Propuesto
- **Fecha:** 2026-08-26
- **Ámbito:** Política de firma / Compatibilidad
- **Decisores:** Equipo del reto ING-IDS347
- **Relacionado con:** ADR-001, ADR-003

## Contexto

El reto solicita analizar cómo implementar una validación híbrida entre criptografía clásica y postcuántica.

Una transición inmediata desde un sistema clásico hacia uno exclusivamente postcuántico puede afectar compatibilidad con sistemas existentes. Sin embargo, permitir que una licencia sea válida con cualquiera de las dos firmas podría debilitar la seguridad.

## Objetivo

Definir una política que permita:

- mantener compatibilidad durante la transición;
- incorporar protección postcuántica;
- evitar que la seguridad dependa del algoritmo más débil;
- soportar versiones futuras.

## Opciones consideradas

### Opción A — Solo ECDSA

```text
ECDSA = VALID
→ LICENSE VALID
```

**Ventaja:** simplicidad.

**Problema:** no resuelve el objetivo postcuántico del reto.

### Opción B — Solo ML-DSA

```text
ML-DSA = VALID
→ LICENSE VALID
```

**Ventaja:** elimina dependencia de criptografía clásica.

**Problema:** reduce compatibilidad con sistemas existentes y no demuestra una transición híbrida.

### Opción C — ECDSA OR ML-DSA

```text
ECDSA = VALID
OR
ML-DSA = VALID
→ LICENSE VALID
```

**Ventaja:** máxima compatibilidad.

**Problema crítico:** si cualquiera de los algoritmos se rompe, el atacante solo necesita falsificar una de las dos firmas.

### Opción D — ECDSA AND ML-DSA

```text
ECDSA = VALID
AND
ML-DSA = VALID
→ LICENSE VALID
```

**Ventaja:** para nuevas licencias obliga a superar ambas verificaciones.

**Desventaja:** clientes que no soporten ML-DSA no podrán validar licencias híbridas sin actualización.

## Decisión

Se selecciona **ECDSA AND ML-DSA** como política híbrida para licencias nuevas.

Nombre inicial de política:

```text
HYBRID_V1
```

La validación será:

```text
classic_signature_valid == true
AND
post_quantum_signature_valid == true
```

Si cualquiera falla:

```text
LICENSE_CRYPTO_INVALID
```

## Compatibilidad

Se permitirá versionar políticas.

Ejemplo:

```text
CLASSIC_V1
HYBRID_V1
PQC_V1
```

Esto permite que el sistema reconozca licencias antiguas sin cambiar el contrato de las nuevas.

## Reglas

### Regla 1
Una licencia `HYBRID_V1` requiere ambas firmas.

### Regla 2
La política está dentro del payload firmado.

### Regla 3
El cliente no puede cambiar `HYBRID_V1` por `CLASSIC_V1` sin invalidar las firmas.

### Regla 4
Cada firma debe indicar el `key_id` correspondiente.

### Regla 5
El servidor debe rechazar políticas desconocidas.

## Formato conceptual

```json
{
  "payload": {
    "license_version": 1,
    "signature_policy": "HYBRID_V1",
    "classic_algorithm": "ECDSA",
    "pqc_algorithm": "ML-DSA",
    "classic_key_id": "ECDSA-2026-001",
    "pqc_key_id": "MLDSA-2026-001"
  },
  "signatures": {
    "classic": "...",
    "post_quantum": "..."
  }
}
```

## Flujo de validación

```text
Parsear licencia
      ↓
Validar versión
      ↓
Leer signature_policy
      ↓
¿HYBRID_V1?
      ↓
Verificar ECDSA
      ↓
Verificar ML-DSA
      ↓
¿Ambas válidas?
   /          \
 Sí            No
 ↓              ↓
Continuar      Rechazar
```

## Consecuencias

### Positivas
- protección híbrida real;
- compatibilidad controlada por versión;
- demostración clara del proceso de transición;
- facilidad para migrar a una política futura.

### Negativas
- mayor tamaño final de licencia;
- doble verificación;
- doble infraestructura de claves;
- mayor complejidad de pruebas.

## Riesgos

- degradación accidental a política clásica;
- políticas antiguas aceptadas indefinidamente;
- claves equivocadas para una política;
- diferencias de serialización entre ambas firmas.

## Mitigaciones

- firmar `signature_policy`;
- registrar `algorithm_id` y `key_id`;
- lista explícita de políticas permitidas;
- pruebas negativas de downgrade;
- canonicalización única compartida.

## Pruebas obligatorias

| ECDSA | ML-DSA | Resultado |
|---|---|---|
| válida | válida | VALID |
| válida | inválida | INVALID |
| inválida | válida | INVALID |
| inválida | inválida | INVALID |

Prueba adicional:

Modificar:

```text
signature_policy = HYBRID_V1
```

por:

```text
signature_policy = CLASSIC_V1
```

Resultado esperado:

```text
INVALID_SIGNATURE
```
