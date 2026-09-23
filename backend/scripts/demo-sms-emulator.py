#!/usr/bin/env python3
"""Inject a clearly labeled local SMS into an Android emulator, never a carrier."""
import argparse
import subprocess
from urllib.parse import urlsplit

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('url', help='URL HTTPS pública de la demo, sin tokens ni credenciales')
parser.add_argument('--device', default='emulator-5554')
args = parser.parse_args()
url = urlsplit(args.url)
if not args.device.startswith('emulator-') or url.scheme != 'https' or not url.hostname or url.username or url.query or url.fragment:
    parser.error('Usar solo un emulador y una URL HTTPS sin secretos ni parámetros.')
message = '[SIMULACION LOCAL] FuelTrack: abre la demo ' + args.url + ' . Este mensaje no uso una SIM ni la red del operador.'
subprocess.run(['adb', '-s', args.device, 'emu', 'sms', 'send', '+18095550101', message], check=True)
print('SMS local simulado. No acredita entrega real de Textbee.')
