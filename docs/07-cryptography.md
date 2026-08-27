# 07 — Estrategia criptográfica

## Objetivo
Garantizar autenticidad e integridad con una estrategia preparada para la transición postcuántica.

## Propuesta inicial
- Firma clásica: ECDSA.
- Firma postcuántica: ML-DSA.
- Política: ambas firmas deben ser válidas.

```text
ECDSA válida
AND
ML-DSA válida
= licencia criptográficamente válida
```

## Motivo
El esquema híbrido permite una transición gradual y evita depender de una única familia criptográfica.

## Alternativa de benchmark
SLH-DSA puede utilizarse para comparar tamaño y rendimiento.

## Abstracción
```text
SignatureProvider
  |- ECDSAProvider
  |- MLDSAProvider
  |- SLHDSAProvider
```

## Versionado
Cada licencia debe indicar:
- versión;
- política criptográfica;
- algoritmo;
- key_id.
