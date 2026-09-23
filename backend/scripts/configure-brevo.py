#!/usr/bin/env python3
"""Store SMTP credentials without exposing them in command arguments or Git."""
import getpass
import json
import subprocess
from pathlib import Path

login = input('Login SMTP de Brevo: ').strip()
secret = getpass.getpass('Clave SMTP (no la API Key): ').strip()
sender = input('Correo remitente verificado: ').strip()
if not login or not secret or '@' not in sender or any(c in login + secret + sender for c in '\r\n'):
    raise SystemExit('Configuración incompleta o inválida.')
settings = {
    'Notifications:Smtp:Host': 'smtp-relay.brevo.com',
    'Notifications:Smtp:Port': '2525',
    'Notifications:Smtp:StartTls': 'true',
    'Notifications:Smtp:UseSsl': 'false',
    'Notifications:Smtp:Username': login,
    'Notifications:Smtp:Password': secret,
    'Notifications:Smtp:FromAddress': sender,
    'Notifications:Smtp:FromName': 'FuelTrack',
    'Notifications:Smtp:Enabled': 'false',
    'Notifications:WorkerEnabled': 'false',
}
project = Path(__file__).resolve().parents[1] / 'FuelTrack.Api'
subprocess.run(['dotnet', 'user-secrets', 'set', '--project', str(project)],
               input=json.dumps(settings), text=True, check=True, stdout=subprocess.DEVNULL)
print('Configuración guardada. Envío desactivado: revisar destinatarios y cola antes de habilitarlo.')
