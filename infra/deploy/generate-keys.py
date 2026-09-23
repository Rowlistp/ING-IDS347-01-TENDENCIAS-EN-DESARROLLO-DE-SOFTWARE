#!/usr/bin/env python3
"""Write deployment keys once to a private file outside the repository."""
import base64
import os
import secrets
import subprocess
from pathlib import Path

folder = Path.home() / '.config' / 'fueltrack'
folder.mkdir(mode=0o700, parents=True, exist_ok=True)
file = folder / 'deployment-keys.env'
key = subprocess.check_output(['openssl', 'genpkey', '-algorithm', 'EC', '-pkeyopt', 'ec_paramgen_curve:P-256', '-outform', 'DER'], stderr=subprocess.DEVNULL)
key = subprocess.check_output(['openssl', 'pkcs8', '-topk8', '-nocrypt', '-inform', 'DER', '-outform', 'DER'], input=key)
public = subprocess.check_output(['openssl', 'pkey', '-inform', 'DER', '-pubout', '-outform', 'DER'], input=key)
values = {
    'Jwt__Key': secrets.token_urlsafe(48),
    'Tickets__SigningPrivateKeyPkcs8Base64': base64.b64encode(key).decode(),
    'Tickets__SigningPublicKeySpkiBase64': base64.b64encode(public).decode(),
    'BootstrapAdmin__Username': 'demo.admin',
    'BootstrapAdmin__Password': secrets.token_urlsafe(24) + 'A7!',
}
fd = os.open(file, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
with os.fdopen(fd, 'w') as stream:
    stream.write('\n'.join(f'{k}={v}' for k, v in values.items()) + '\n')
print(f'Claves guardadas en {file}. No publicar. Conservarlas entre despliegues.')
