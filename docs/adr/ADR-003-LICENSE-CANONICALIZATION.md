# ADR-003 — Formato canónico de licencia y JSON Canonicalization

- **Estado:** Propuesto
- **Fecha:** 2026-08-26
- **Ámbito:** Formato de licencia / Integridad
- **Decisores:** Equipo del reto ING-IDS347
- **Relacionado con:** ADR-001, ADR-002

## Contexto

Las firmas digitales no firman objetos abstractos; firman secuencias exactas de bytes.

Dos objetos JSON pueden representar los mismos datos pero generar una secuencia de bytes distinta debido a:

- orden de propiedades;
- espacios;
- saltos de línea;
- representación numérica;
- escapes;
- codificación.

Ejemplo:

```json
{"user_id":"123","expires_at":"2027-01-01"}
```

y:

```json
{
  "expires_at": "2027-01-01",
  "user_id": "123"
}
```

Semánticamente pueden representar lo mismo, pero no necesariamente producen los mismos bytes.

Si servidor y cliente serializan de forma diferente, una licencia legítima podría fallar durante la verificación.

## Objetivo

Definir un formato determinista y versionado para que:

```text
mismos datos
→ mismos bytes
→ misma entrada criptográfica
```

## Opciones consideradas

### Opción A — JSON normal

Firmar directamente el resultado de `json.dumps`, `JSON.stringify` o equivalente.

**Problema:** distintos frameworks pueden serializar de forma diferente.

### Opción B — Concatenación manual

Ejemplo:

```text
license_id|user_id|expires_at|...
```

**Problemas:**
- difícil de evolucionar;
- riesgo con delimitadores;
- mayor probabilidad de errores;
- poca claridad.

### Opción C — JSON canonicalizado

Convertir el payload a una representación determinista antes de firmar.

**Ventajas**
- legible;
- estructurado;
- reproducible;
- adecuado para interoperabilidad;
- compatible con versionado.

## Decisión

Se utilizará un **payload JSON canonicalizado** antes de firmar.

Propuesta de referencia:

```text
JCS / RFC 8785
```

o una implementación estrictamente compatible con sus reglas.

## Separación obligatoria

La licencia tendrá:

```json
{
  "payload": {},
  "signatures": {}
}
```

Solo se firma:

```text
canonicalize(payload)
```

Las firmas no forman parte del contenido firmado por ellas mismas.

## Esquema inicial

```json
{
  "payload": {
    "license_version": 1,
    "license_id": "LIC-...",
    "user_id": "USR-...",
    "product_id": "PRD-...",
    "issued_at": "2026-08-26T20:00:00Z",
    "valid_from": "2026-08-26T20:00:00Z",
    "expires_at": "2027-08-26T20:00:00Z",
    "offline_policy": {
      "max_offline_days": 7
    },
    "device_binding": {
      "required": false,
      "device_id": null
    },
    "features": [],
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

## Reglas de formato

### Fechas
Se utilizarán timestamps UTC normalizados.

Ejemplo:

```text
2026-08-26T20:00:00Z
```

### IDs
Los identificadores deberán ser strings estables.

### Nulos
Debe existir una regla explícita sobre si los campos opcionales:
- se omiten;
- o se incluyen como `null`.

La primera versión del formato deberá elegir una sola estrategia y mantenerla.

### Features
El orden debe tener significado definido o normalizarse antes de firmar.

### Algoritmos
Los identificadores serán strings versionables.

## Proceso de firma

```text
License Payload
      ↓
Schema Validation
      ↓
Canonicalization
      ↓
UTF-8 bytes
      ↓
ECDSA Sign
      ↓
ML-DSA Sign
      ↓
Attach signatures
```

## Proceso de verificación

```text
Read license
      ↓
Validate schema
      ↓
Extract payload
      ↓
Canonicalization
      ↓
UTF-8 bytes
      ↓
Verify ECDSA
      ↓
Verify ML-DSA
```

## Inmutabilidad

Los siguientes campos no deben editarse después de emitir la licencia:

- license_id;
- user_id;
- product_id;
- issued_at;
- valid_from;
- expires_at;
- device_binding;
- features;
- signature_policy;
- algorithms;
- key IDs.

Si cambia cualquiera:

```text
emitir nueva licencia
+
generar nuevas firmas
```

## Nota sobre estado

El estado de revocación autoritativo no debe depender únicamente de un campo editable dentro del archivo de licencia.

El servidor mantiene el estado real:

```text
ACTIVE
SUSPENDED
EXPIRED
REVOKED
```

La licencia contiene datos firmados de emisión, mientras que la revocación puede evolucionar externamente.

## Consecuencias

### Positivas
- interoperabilidad;
- verificaciones reproducibles;
- pruebas deterministas;
- separación clara entre datos y firmas;
- menor riesgo de falsos errores criptográficos.

### Negativas
- se añade una etapa adicional;
- toda implementación cliente debe respetar exactamente la canonicalización;
- cambios al formato deben versionarse.

## Riesgos

- canonicalizaciones diferentes;
- tratamiento inconsistente de `null`;
- diferencias en fechas;
- orden no definido en listas;
- cambios de esquema sin incrementar versión.

## Mitigaciones

- pruebas con vectores conocidos;
- fixtures compartidos;
- esquema versionado;
- una única función de canonicalización por lenguaje;
- pruebas cruzadas cliente/servidor.

## Vectores de prueba requeridos

Crear al menos un fixture:

```text
tests/fixtures/license-v1.json
```

y un archivo:

```text
tests/fixtures/license-v1.canonical.txt
```

El resultado canonicalizado debe ser exactamente reproducible.

También:

```text
license-v1.signature-ecdsa
license-v1.signature-mldsa
```

para pruebas de regresión.

## Criterios de aceptación

1. servidor y cliente producen los mismos bytes;
2. el mismo payload siempre produce la misma representación canónica;
3. cambiar el orden visual de las propiedades no cambia la representación canónica;
4. cambiar un valor sí cambia los bytes;
5. cambiar un valor invalida ambas firmas;
6. una versión desconocida es rechazada.
