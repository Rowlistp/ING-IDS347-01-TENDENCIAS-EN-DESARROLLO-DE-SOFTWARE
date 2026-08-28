# Mocks

Datos de prueba para trabajar contra el contrato de `docs/06-API.md` mientras el backend no tenga un endpoint implementado.

Convención:

- Un archivo por recurso, ej. `usuarios.mock.js`, `tickets.mock.js`.
- Cada archivo exporta datos con la misma forma que devolvería la API real.
- Si el mock necesita interceptar `fetch` en vez de sustituir la llamada a mano, usar [MSW](https://mswjs.io/).
