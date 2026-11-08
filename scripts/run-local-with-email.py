#!/usr/bin/env python3
import os
import pathlib
import subprocess
import sys

root = pathlib.Path(__file__).resolve().parents[1]
config = root / '.env.resend'
if not config.exists():
    raise SystemExit('Create .env.resend using .env.example; never commit credentials.')
environment = os.environ.copy()
for line in config.read_text().splitlines():
    if line.strip() and not line.lstrip().startswith('#'):
        name, value = line.split('=', 1)
        if not name.startswith('Email__'):
            raise SystemExit('Only Email__ settings are allowed in .env.resend.')
        environment[name] = value
raise SystemExit(subprocess.call([
    'dotnet', 'run', '--project', str(root / 'NovaTech.TerraTech.Platform'),
    '-c', 'Debug', '--no-launch-profile', *sys.argv[1:]
], env=environment, cwd=root))
