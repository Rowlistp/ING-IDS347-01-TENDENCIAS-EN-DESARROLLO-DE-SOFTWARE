# 15 — Benchmark

## Objetivo
Medir el impacto real del uso de criptografía postcuántica.

## Métricas
Por algoritmo:
- generación de claves;
- firma;
- verificación;
- tamaño de clave pública;
- tamaño de clave privada;
- tamaño de firma;
- tamaño final de licencia.

## Comparaciones
- ECDSA.
- ML-DSA.
- ECDSA + ML-DSA.
- Opcional: SLH-DSA.

## Metodología propuesta
Ejecutar múltiples iteraciones y reportar:
- mínimo;
- máximo;
- promedio;
- mediana.

## Evidencia
Guardar resultados en archivos reproducibles dentro de `benchmarks/`.
