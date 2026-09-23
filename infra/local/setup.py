#!/usr/bin/env python3
"""Create independent local secrets once; never use the hosted database or keys."""
import base64
import os
from pathlib import Path
import secrets
import subprocess

root = Path(__file__).resolve().parents[2]
target = root / '.env.local'
if target.exists():
    print('Se conserva .env.local existente. No se cambian claves ni contraseñas.')
    raise SystemExit(0)
private = subprocess.check_output([
    'openssl', 'genpkey', '-algorithm', 'EC', '-pkeyopt',
    'ec_paramgen_curve:P-256', '-outform', 'DER'], stderr=subprocess.DEVNULL)
private = subprocess.check_output([
    'openssl', 'pkcs8', '-topk8', '-nocrypt', '-inform', 'DER', '-outform', 'DER'], input=private)
public = subprocess.check_output([
    'openssl', 'pkey', '-inform', 'DER', '-pubout', '-outform', 'DER'], input=private)
values = {
    'LOCAL_PORT': '5351',
    'LOCAL_DB_PASSWORD': secrets.token_urlsafe(32),
    'LOCAL_JWT_KEY': secrets.token_urlsafe(48),
    'LOCAL_SIGNING_PRIVATE': base64.b64encode(private).decode(),
    'LOCAL_SIGNING_PUBLIC': base64.b64encode(public).decode(),
    'LOCAL_ADMIN_PASSWORD': secrets.token_urlsafe(24) + 'A7!',
}
fd = os.open(target, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
with os.fdopen(fd, 'w') as stream:
    stream.write('\n'.join(f'{key}={value}' for key, value in values.items()) + '\n')
print('Configuración local creada. Usuario: local.admin; contraseña en .env.local (LOCAL_ADMIN_PASSWORD).')
print('Arranque: docker compose --env-file .env.local -f compose.local.yaml up --build -d')
print('Abrir http://127.0.0.1:5351. Los datos y las claves son exclusivos de este entorno.')
