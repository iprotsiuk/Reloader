#!/usr/bin/env python3
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "Reloader"
MANIFEST = PROJECT / "Packages" / "manifest.json"

REQUIRED_PACKAGES = {
    "com.unity.cinemachine",
    "com.unity.nuget.newtonsoft-json",
    "com.unity.postprocessing",
}

BAD_SUFFIX_PATTERN = re.compile(r"\s\d+\.(asset|json|prefab|unity)$", re.IGNORECASE)
SUSPICIOUS_NAME_PATTERN = re.compile(
    r"(^|[-_.])(dst|resourcedst|conflict|merged)([-_.]|$)", re.IGNORECASE
)
SCAN_DIRS = [
    PROJECT / "ProjectSettings",
    PROJECT / "Assets" / "_Project",
]


def check_manifest(errors):
    if not MANIFEST.exists():
        errors.append(f"Missing manifest: {MANIFEST}")
        return

    try:
        data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"Failed to parse manifest.json: {exc}")
        return

    deps = data.get("dependencies", {})
    missing = sorted(pkg for pkg in REQUIRED_PACKAGES if pkg not in deps)
    if missing:
        errors.append(
            "manifest.json missing required packages: " + ", ".join(missing)
        )


def iter_changed_paths():
    try:
        output = subprocess.check_output(
            ["git", "status", "--porcelain"],
            cwd=ROOT,
            text=True,
        )
    except Exception:
        return []

    paths = []
    for line in output.splitlines():
        if not line:
            continue
        # porcelain format: XY <path> [-> <newpath>]
        raw = line[3:]
        if " -> " in raw:
            raw = raw.split(" -> ", 1)[1]
        paths.append(raw.strip())
    return paths


def check_conflict_artifacts(errors):
    suspicious = []
    changed = iter_changed_paths()
    for rel in changed:
        p = (ROOT / rel).resolve()
        if not p.exists() or not p.is_file():
            continue

        in_scope = any(
            str(p).startswith(str(base.resolve()) + "/")
            for base in SCAN_DIRS
            if base.exists()
        )
        if not in_scope:
            continue

        name = p.name.lower()
        if BAD_SUFFIX_PATTERN.search(p.name) or SUSPICIOUS_NAME_PATTERN.search(name):
            suspicious.append(rel)

    if suspicious:
        first = "\n  - ".join(sorted(suspicious)[:25])
        errors.append(
            "Suspicious possible conflict artifacts detected:\n  - " + first
        )


def main():
    errors = []
    check_manifest(errors)
    check_conflict_artifacts(errors)

    if errors:
        print("Unity VCS health check FAILED:\n")
        for issue in errors:
            print(f"- {issue}\n")
        return 1

    print("Unity VCS health check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
