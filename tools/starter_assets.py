#!/usr/bin/env python3
"""Restore omitted shared assets safely, or verify imported starter provenance."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

COMMIT = "4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f"
URL = "https://github.com/kommanderpi/studentstarter.git"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("restore", "check"))
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.project.resolve()
    if not all((root / n).is_dir() for n in ("Assets", "Packages", "ProjectSettings")):
        parser.error("--project must be a Unity project root")
    starter = root / "DigiPhantStarter"
    if not starter.exists() and args.action == "restore":
        subprocess.run(["git", "clone", URL, str(starter)], check=True)
        subprocess.run(["git", "-C", str(starter), "checkout", "--detach", COMMIT], check=True)
    revision = subprocess.check_output(["git", "-C", str(starter), "rev-parse", "HEAD"], text=True).strip()
    if revision != COMMIT:
        raise SystemExit(f"Starter is at {revision}; expected {COMMIT}. Existing clone left untouched.")
    hashes = json.loads((starter / "asset-checksums.json").read_text())
    for relative, expected in hashes.items():
        if relative.startswith(("Elephant/", "DigiPhant/", "Tracking/")) or relative in ("Elephant.meta", "DigiPhant.meta"):
            path = starter / relative
            if hashlib.sha256(path.read_bytes()).hexdigest() != expected:
                raise SystemExit(f"Starter checksum mismatch: {relative}")
    if args.action == "restore":
        folder, meta = root / "Assets/Elephant", root / "Assets/Elephant.meta"
        if folder.exists() != meta.exists():
            raise SystemExit("Incomplete Elephant import; reconcile manually. Nothing overwritten.")
        if not folder.exists():
            shutil.copytree(starter / "Elephant", folder)
            shutil.copy2(starter / "Elephant.meta", meta)
    checked = 0
    adapted = []
    for relative, expected in hashes.items():
        if relative.startswith(("Elephant/", "DigiPhant/")) or relative in ("Elephant.meta", "DigiPhant.meta"):
            path = root / "Assets" / relative
            if not path.is_file():
                raise SystemExit(f"Missing imported asset: {path}")
            if hashlib.sha256(path.read_bytes()).hexdigest() != expected:
                if relative.startswith("DigiPhant/"):
                    adapted.append(relative)
                else:
                    raise SystemExit(f"Shared Elephant asset differs: {path}; existing work left untouched")
            checked += 1
    print(f"Checked {checked} imported files; Elephant intact; {len(adapted)} adapted DigiPhant files.")
    for path in adapted:
        print(f"Adapted: {path}")
    print("Python dependencies and live Unity/camera checks are separate; see DIGIPHANT_SETUP.md.")


if __name__ == "__main__":
    main()
