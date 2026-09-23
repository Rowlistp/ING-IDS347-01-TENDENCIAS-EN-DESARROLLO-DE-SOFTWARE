#!/usr/bin/env python3
"""Configura Textbee localmente, sin poner secretos en argumentos ni en Git."""
import getpass
import json
from pathlib import Path
import re
import subprocess
from urllib.parse import urlparse

project = Path(__file__).resolve().parents[1] / 'FuelTrack.Api'
device = input('Device ID de Textbee: ').strip()
if not re.fullmatch(r'[a-fA-F0-9]{24}', device):
    raise SystemExit('Device ID inválido: debe contener 24 caracteres hexadecimales.')
key = getpass.getpass('API Key de Textbee (oculta): ').strip()
if not key or '\r' in key or '\n' in key:
    raise SystemExit('API Key inválida.')
base = input('URL pública HTTPS de FuelTrack, sin /api/v1: ').strip().rstrip('/')
url = urlparse(base)
if url.scheme != 'https' or not url.hostname or url.username or url.password or url.query or url.fragment or url.path not in ('', '/'):
    raise SystemExit('Se requiere el origen HTTPS público, por ejemplo https://fueltrack.example.com.')
config = {
    'Notifications:Sms:Provider': 'Textbee',
    'Notifications:Sms:BaseUrl': 'https://api.textbee.dev/api/v1/gateway/send-sms',
    'Notifications:Sms:DeviceId': device,
    'Notifications:Sms:ApiKey': key,
    'Notifications:Sms:AuthHeaderName': 'x-api-key',
    'Notifications:PublicBaseUrl': base,
    'Notifications:Sms:Enabled': 'false',
    'Notifications:WorkerEnabled': 'false',
}
result = subprocess.run(['dotnet', 'user-secrets', 'set', '--project', str(project)],
                        input=json.dumps(config), text=True, capture_output=True)
if result.returncode:
    raise SystemExit('No se pudo guardar la configuración. Verifica .NET y UserSecretsId del proyecto.')
print('Configuración guardada en user-secrets. SMS y worker permanecen apagados. Sigue docs/29-TEXTBEE.md antes de activarlos.')
