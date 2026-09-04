#!/usr/bin/env bash
set -euo pipefail

container_name="fueltrack-security-test-$$"
database_name="fueltrack_security_test"
database_user="fueltrack_test"
database_password="$(openssl rand -hex 24)"

cleanup() {
  docker rm -f "$container_name" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker run --detach --rm \
  --name "$container_name" \
  --env POSTGRES_DB="$database_name" \
  --env POSTGRES_USER="$database_user" \
  --env POSTGRES_PASSWORD="$database_password" \
  --publish 127.0.0.1::5432 \
  postgres:16-alpine >/dev/null

database_port="$(docker port "$container_name" 5432/tcp | awk -F: '{print $NF}')"

for _ in $(seq 1 30); do
  if docker exec "$container_name" pg_isready \
      --username "$database_user" \
      --dbname "$database_name" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

docker exec "$container_name" pg_isready \
  --username "$database_user" \
  --dbname "$database_name" >/dev/null

export FUELTRACK_TEST_CONNECTION="Host=127.0.0.1;Port=$database_port;Database=$database_name;Username=$database_user;Password=$database_password;Pooling=false"

dotnet test backend/FuelTrack.Api.Tests/FuelTrack.Api.Tests.csproj \
  --filter TestCategory=PostgreSQL \
  --logger "console;verbosity=normal"
