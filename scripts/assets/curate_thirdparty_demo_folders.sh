#!/usr/bin/env bash
set -euo pipefail

MODE="dry-run"
TS="$(date +%Y-%m-%d-%H%M%S)"
DUMP_ROOT="${HOME}/Documents/SOUNDS/project-asset-dump/thirdparty-demo-prune-${TS}"
MANIFEST_PATH=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --apply)
      MODE="apply"
      shift
      ;;
    --dump-root)
      DUMP_ROOT="$2"
      shift 2
      ;;
    --manifest)
      MANIFEST_PATH="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      echo "Usage: $0 [--apply] [--dump-root <absolute-path>] [--manifest <file>]" >&2
      exit 1
      ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PROJECT_ROOT="${REPO_ROOT}/Reloader"
THIRDPARTY_ROOT="${PROJECT_ROOT}/Assets/ThirdParty"

if [[ ! -d "${THIRDPARTY_ROOT}" ]]; then
  echo "ThirdParty root not found: ${THIRDPARTY_ROOT}" >&2
  exit 1
fi

if [[ -z "${MANIFEST_PATH}" ]]; then
  MANIFEST_PATH="${REPO_ROOT}/tmp/thirdparty-demo-curation-manifest-${TS}.csv"
fi
mkdir -p "$(dirname "${MANIFEST_PATH}")"
printf "mode,action,folder_path,dump_path,size_bytes,external_ref_count\n" > "${MANIFEST_PATH}"

python3 - "$MODE" "$REPO_ROOT" "$PROJECT_ROOT" "$THIRDPARTY_ROOT" "$DUMP_ROOT" "$MANIFEST_PATH" <<'PY'
import os,re,sys,shutil
from pathlib import Path

mode, repo_root, project_root, thirdparty_root, dump_root, manifest = sys.argv[1:]
repo_root = Path(repo_root)
project_root = Path(project_root)
thirdparty_root = Path(thirdparty_root)
dump_root = Path(dump_root)
manifest_path = Path(manifest)

candidate_re = re.compile(r"(demo|demonstration|demo[_-]|demoscene|/test$)", re.IGNORECASE)
scan_exts = {'.unity','.prefab','.asset','.mat','.controller','.anim','.playable','.overridecontroller','.asmdef','.asmref','.inputactions','.json','.txt','.cs','.uss','.uxml','.shader','.shadervariants'}

# Gather all text file guid references once.
file_guid_refs = {}
for base in [project_root/'Assets', project_root/'ProjectSettings', project_root/'Packages']:
    if not base.exists():
        continue
    for p in base.rglob('*'):
        if p.is_dir() or p.suffix == '.meta':
            continue
        if p.suffix.lower() not in scan_exts:
            continue
        try:
            txt = p.read_text(encoding='utf-8', errors='ignore')
        except Exception:
            continue
        guids = set(re.findall(r'guid:\s*([0-9a-f]{32})', txt))
        if guids:
            file_guid_refs[p] = guids

# Index guid -> files referencing it.
guid_to_files = {}
for f, guids in file_guid_refs.items():
    for g in guids:
        guid_to_files.setdefault(g, set()).add(f)

# candidate dirs
candidates = []
for d in sorted(thirdparty_root.rglob('*')):
    if not d.is_dir():
        continue
    rel = '/' + str(d.relative_to(thirdparty_root)).replace('\\','/')
    if candidate_re.search(rel):
        candidates.append(d)

# remove nested candidates if parent already candidate
pruned = []
for d in candidates:
    if any(parent in candidates for parent in d.parents if parent != d):
        continue
    pruned.append(d)

moved = kept = 0
moved_bytes = 0

with manifest_path.open('a', encoding='utf-8') as mf:
    for folder in pruned:
        # collect guids inside folder
        guids = set()
        for meta in folder.rglob('*.meta'):
            try:
                txt = meta.read_text(encoding='utf-8', errors='ignore')
            except Exception:
                continue
            m = re.search(r'guid:\s*([0-9a-f]{32})', txt)
            if m:
                guids.add(m.group(1))

        external_refs = set()
        for g in guids:
            for ref_file in guid_to_files.get(g, ()):  # files referencing this guid
                # ignore references inside the same candidate folder
                try:
                    ref_file.relative_to(folder)
                    continue
                except ValueError:
                    external_refs.add(ref_file)

        # byte size of folder
        size = 0
        for p in folder.rglob('*'):
            if p.is_file():
                try:
                    size += p.stat().st_size
                except OSError:
                    pass

        rel = folder.relative_to(project_root/'Assets')
        dump_target = dump_root / rel

        if external_refs:
            kept += 1
            mf.write(f"{mode},kept,{folder},{dump_target},{size},{len(external_refs)}\n")
            continue

        if mode == 'apply':
            dump_target.parent.mkdir(parents=True, exist_ok=True)
            shutil.move(str(folder), str(dump_target))
            folder_meta = folder.with_suffix(folder.suffix + '.meta') if folder.suffix else Path(str(folder) + '.meta')
            if folder_meta.exists():
                shutil.move(str(folder_meta), str(dump_target) + '.meta')

        moved += 1
        moved_bytes += size
        action = 'moved' if mode == 'apply' else 'would-move'
        mf.write(f"{mode},{action},{folder},{dump_target},{size},0\n")

print(f"Mode: {mode}")
print(f"Candidate folders: {len(pruned)}")
print(f"Moved folders: {moved}")
print(f"Kept folders (externally referenced): {kept}")
print(f"Moved size (bytes): {moved_bytes}")
print(f"Manifest: {manifest_path}")
if mode == 'apply':
    print(f"Dump root: {dump_root}")
PY
