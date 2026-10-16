#!/usr/bin/env python3
"""Restore omitted starter assets without copying its scene or custom scripts."""
import hashlib
import io
from pathlib import Path
import shutil
import subprocess
import tarfile

ROOT = Path(__file__).resolve().parents[1]
STARTER = ROOT / 'DigiPhantStarter'
REVISION = '4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f'
POSE_SHA256 = '4eaa5eb7a98365221087693fcc286334cf0858e2eb6e15b506aa4a7ecdcec4ad'


def restore():
    if not STARTER.exists():
        subprocess.run(['git', 'clone', '--no-checkout',
                        'https://github.com/kommanderpi/studentstarter.git', str(STARTER)], check=True)
    subprocess.run(['git', '-C', str(STARTER), 'cat-file', '-e', REVISION + '^{commit}'], check=True)
    archive = subprocess.check_output(['git', '-C', str(STARTER), 'archive', REVISION,
                                      'Elephant', 'Elephant.meta', 'Tracking/pose_landmarker_full.task'])
    with tarfile.open(fileobj=io.BytesIO(archive)) as source:
        members = {m.name: m for m in source.getmembers() if m.isfile()}
        pose = source.extractfile(members['Tracking/pose_landmarker_full.task']).read()
        if hashlib.sha256(pose).hexdigest() != POSE_SHA256:
            raise RuntimeError('Pinned starter pose checksum mismatch')
        elephant = ROOT / 'Assets/Elephant'
        meta = ROOT / 'Assets/Elephant.meta'
        if elephant.exists() != meta.exists():
            raise RuntimeError('Partial Elephant import: preserve/repair folder and meta together before retrying')
        if elephant.exists():
            print('Existing Assets/Elephant retained; restoring neither it nor its materials.')
        else:
            for name, member in members.items():
                if name == 'Elephant.meta' or name.startswith('Elephant/'):
                    relative = Path(name)
                    if '..' in relative.parts or relative.is_absolute():
                        raise RuntimeError('Unexpected archive path')
                    destination = ROOT / 'Assets' / relative
                    destination.parent.mkdir(parents=True, exist_ok=True)
                    destination.write_bytes(source.extractfile(member).read())
            overrides = ROOT / 'SharedAssetOverrides/Elephant'
            for item in overrides.rglob('*'):
                if item.is_file():
                    destination = elephant / item.relative_to(overrides)
                    destination.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(item, destination)
            print('Restored pinned Elephant assets and custom material overrides.')
        destination = ROOT / 'StudentTracking/pose_landmarker_full.task'
        if destination.exists() and hashlib.sha256(destination.read_bytes()).hexdigest() != POSE_SHA256:
            raise RuntimeError('Existing pose model differs; move it aside before restoring')
        if not destination.exists():
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(pose)
        print('Pose model verified. The performance scene and scripts were not replaced.')


if __name__ == '__main__':
    restore()
