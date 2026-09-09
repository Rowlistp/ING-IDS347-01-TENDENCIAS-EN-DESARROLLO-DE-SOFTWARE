#!/usr/bin/env bash
set -euo pipefail
test_pg="fueltrack-full-pg-$$"
test_kc="fueltrack-full-kc-$$"
test_mail="fueltrack-full-mail-$$"
cleanup() { docker rm -f "$test_pg" "$test_kc" "$test_mail" >/dev/null 2>&1 || true; }
trap cleanup EXIT
docker run --rm -d --name "$test_mail" -p 127.0.0.1::1025 -p 127.0.0.1::8025 axllent/mailpit:v1.30.0 >/dev/null
export FUELTRACK_SMTP_PORT=$(docker port "$test_mail" 1025/tcp | awk -F: '{print $NF}')
test_mail_port=$(docker port "$test_mail" 8025/tcp | awk -F: '{print $NF}')
export FUELTRACK_MAILPIT_URL="http://127.0.0.1:$test_mail_port"
docker run --rm -d --name "$test_pg" -p 127.0.0.1::5432 \
  -e POSTGRES_DB=fueltrack_security_test -e POSTGRES_USER=fueltrack_test \
  -e POSTGRES_PASSWORD=integration-test-only-password postgres:16-alpine >/dev/null
docker run --rm -d --name "$test_kc" -p 127.0.0.1::8080 \
  -v "$PWD/infra/keycloak/fueltrack-realm.json:/opt/keycloak/data/import/fueltrack-realm.json:ro" \
  quay.io/keycloak/keycloak:26.7.3 start-dev --import-realm >/dev/null
test_pg_port=$(docker port "$test_pg" 5432/tcp | awk -F: '{print $NF}')
test_kc_port=$(docker port "$test_kc" 8080/tcp | awk -F: '{print $NF}')
for attempt in {1..55}; do
  if curl --fail --silent "http://127.0.0.1:$test_kc_port/realms/fueltrack/.well-known/openid-configuration" >/dev/null; then break; fi
  sleep 1
done
curl --fail --silent "http://127.0.0.1:$test_kc_port/realms/fueltrack/.well-known/openid-configuration" >/dev/null
dotnet restore backend/FuelTrack.slnx
dotnet build backend/FuelTrack.slnx --no-restore --configuration Release
export FUELTRACK_TEST_CONNECTION="Host=127.0.0.1;Port=$test_pg_port;Database=fueltrack_security_test;Username=fueltrack_test;Password=integration-test-only-password;Pooling=false"
export FUELTRACK_KEYCLOAK_URL="http://127.0.0.1:$test_kc_port"
dotnet test backend/FuelTrack.slnx --no-build --configuration Release --logger 'console;verbosity=minimal' "$@"
