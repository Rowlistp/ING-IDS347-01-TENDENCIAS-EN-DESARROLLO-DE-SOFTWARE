# ADR-001 — Selección de ML-DSA como firma postcuántica principal

- **Estado:** Propuesto
- **Fecha:** 2026-08-26
- **Ámbito:** Criptografía / Firma digital
- **Decisores:** Equipo del reto ING-IDS347
- **Relacionado con:** ADR-002, ADR-003

## Contexto

El reto exige diseñar e implementar un sistema de licenciamiento resistente a amenazas futuras de computación cuántica. El documento del reto menciona como candidatos Dilithium, Falcon y SPHINCS+ y plantea explícitamente la necesidad de determinar qué algoritmo postcuántico es más adecuado para firmar licencias.

El sistema necesita una firma digital postcuántica que permita:

- firmar licencias;
- verificar autenticidad e integridad;
- funcionar online y offline;
- integrarse con un esquema híbrido;
- mantener un impacto razonable en rendimiento y tamaño;
- permitir evolución futura sin rehacer toda la arquitectura.

## Opciones consideradas

### Opción A — ML-DSA

ML-DSA es el estándar de firma digital postcuántica derivado de CRYSTALS-Dilithium.

**Ventajas**
- adecuado para firmas digitales;
- diseño orientado a uso general;
- buen equilibrio entre rendimiento y tamaño;
- apropiado para validaciones frecuentes;
- encaja bien como algoritmo principal del sistema.

**Desventajas**
- firmas y claves mayores que las de algoritmos clásicos como ECDSA;
- requiere librerías con soporte postcuántico moderno;
- la interoperabilidad depende de implementaciones compatibles.

### Opción B — SLH-DSA

SLH-DSA está basado en SPHINCS+.

**Ventajas**
- enfoque basado en hashes;
- construcción conservadora desde el punto de vista criptográfico;
- útil como alternativa para comparación.

**Desventajas**
- firmas significativamente más grandes;
- potencial mayor impacto en almacenamiento, tráfico y tiempos;
- menos conveniente como primera opción para licencias pequeñas y frecuentes.

### Opción C — Falcon / FN-DSA

Falcon fue considerado por el reto.

**Ventajas**
- firmas compactas;
- buen rendimiento en ciertos escenarios.

**Desventajas**
- mayor complejidad de implementación;
- menor conveniencia para un MVP académico;
- no se selecciona como base inicial del proyecto.

## Decisión

Se selecciona **ML-DSA como algoritmo postcuántico principal** del sistema.

La primera implementación deberá encapsularlo detrás de una interfaz abstracta para evitar acoplar el sistema a un único algoritmo.

Interfaz conceptual:

```text
SignatureProvider
    sign(data)
    verify(data, signature)
    algorithm_id()
    key_id()
```

Implementación inicial:

```text
MLDSAProvider
```

Implementaciones futuras:

```text
SLHDSAProvider
FNDSAProvider
```

## Razones

1. Responde directamente al objetivo postcuántico del reto.
2. Es apropiado para firmas digitales.
3. Permite validar licencias localmente usando solo la clave pública.
4. Facilita una transición híbrida con una firma clásica.
5. Ofrece un equilibrio razonable para un MVP.
6. Permite realizar benchmarks contra ECDSA y SLH-DSA.

## Consecuencias

### Positivas
- protección postcuántica explícita;
- arquitectura preparada para evolución;
- validación offline posible;
- comparación cuantitativa con criptografía clásica;
- evidencia clara durante la demostración.

### Negativas
- aumento del tamaño de licencias;
- dependencia de una librería PQC;
- necesidad de gestionar claves postcuánticas adicionales;
- mayor complejidad que una solución puramente clásica.

## Riesgos

- incompatibilidad entre librerías;
- formatos de claves no interoperables;
- API cambiante en librerías experimentales;
- aumento inesperado del tamaño final de licencia.

## Mitigaciones

- encapsular ML-DSA dentro de `MLDSAProvider`;
- no almacenar formatos internos de una librería como contrato de dominio;
- versionar algoritmos y claves;
- ejecutar benchmarks desde el POC;
- mantener pruebas conocidas de firma y verificación.

## Criterios de aceptación

ADR-001 se considerará validado cuando podamos demostrar:

1. generación de par de claves ML-DSA;
2. firma de un payload de licencia;
3. verificación correcta de la firma;
4. rechazo después de modificar un byte del payload;
5. medición de tiempo de firma;
6. medición de tiempo de verificación;
7. medición de tamaño de clave pública, clave privada y firma.

## Resultado esperado del POC

```text
Payload original
    ↓
ML-DSA Sign
    ↓
Signature
    ↓
ML-DSA Verify
    ↓
VALID

Modificar payload
    ↓
ML-DSA Verify
    ↓
INVALID
```
