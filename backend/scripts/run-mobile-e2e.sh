#!/usr/bin/env bash
set -euo pipefail
mobile_pg="fueltrack-mobile-e2e-$$"
cleanup() { docker rm -f "$mobile_pg" >/dev/null 2>&1 || true; }
trap cleanup EXIT
docker run --rm -d --name "$mobile_pg" -p 127.0.0.1::5432 -e POSTGRES_DB=fueltrack_mobile_test \
  -e POSTGRES_USER=fueltrack_test -e POSTGRES_PASSWORD=integration-test-only-password postgres:16-alpine >/dev/null
mobile_pg_port=$(docker port "$mobile_pg" 5432/tcp | awk -F: '{print $NF}')
for attempt in {1..30}; do
  if docker exec "$mobile_pg" pg_isready -U fueltrack_test >/dev/null 2>&1; then break; fi
  sleep 1
done
export FUELTRACK_TEST_CONNECTION="Host=127.0.0.1;Port=$mobile_pg_port;Database=fueltrack_mobile_test;Username=fueltrack_test;Password=integration-test-only-password;Pooling=false"
dotnet build backend/FuelTrack.Api --configuration Release
dotnet run --project backend/FuelTrack.MobileHarness --configuration Release
