# 01 — Planteamiento del reto

## Problema
El reto parte de un sistema de licenciamiento de software basado en criptografía clásica, principalmente RSA/ECC, utilizado para:
- firmar licencias;
- validar autenticidad;
- prevenir copias no autorizadas;
- validar online y offline.

El riesgo planteado es que los avances en computación cuántica podrían debilitar estos mecanismos de firma y permitir falsificación de licencias, ruptura de firmas y activaciones ilegítimas.

## Objetivo
Diseñar e implementar un sistema de validación de licencias que:
- sea resistente a ataques cuánticos;
- garantice autenticidad e integridad;
- permita validación offline segura;
- evite falsificación futura;
- sea escalable y adaptable.

## Subproblemas planteados
1. Selección de algoritmos postcuánticos para firmas.
2. Validación híbrida clásica + postcuántica.
3. Impacto en rendimiento y tamaño de claves.
4. Compatibilidad con sistemas existentes.
5. Revocación de licencias en entornos offline.

## Resultado esperado
Un sistema de licenciamiento que:
- use un esquema postcuántico;
- valide online y offline;
- permita cancelación;
- sea adaptable;
- resista ataques clásicos y futuros ataques cuánticos.

## Requisitos funcionales explícitos del reto
1. Generar licencias válidas.
2. Validar licencias online y offline.
3. Cancelar licencias manual y automáticamente.
4. Redirigir a enlaces paramétricos.
5. Redirigir a una página de error cuando la licencia sea inválida o esté vencida.
6. Verificar la licencia asignada al usuario.
