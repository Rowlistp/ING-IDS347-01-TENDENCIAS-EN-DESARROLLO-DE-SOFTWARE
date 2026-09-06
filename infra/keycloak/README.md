# Keycloak local de FuelTrack

Infraestructura reproducible para OAuth 2.0/OIDC de Fase 1. Usa Keycloak `26.7.3`, realm `fueltrack`, clientes públicos web/móvil con Authorization Code + PKCE S256 y un cliente API bearer-only.

```bash
cp infra/keycloak/.env.example infra/keycloak/.env
docker compose --env-file infra/keycloak/.env -f infra/keycloak/compose.yml up -d
```

El usuario administrador y las claves del realm son fixtures exclusivamente de pruebas locales; no son secretos de producción. Los clientes públicos no tienen secretos. Implicit Flow, Direct Access Grants y service accounts están deshabilitados.
