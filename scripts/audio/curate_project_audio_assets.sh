#!/usr/bin/env bash
set -euo pipefail

MODE="dry-run"
DUMP_ROOT="${HOME}/Documents/SOUNDS/project-audio-dump/$(date +%Y-%m-%d-%H%M%S)"

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
    *)
      echo "Unknown argument: $1" >&2
      echo "Usage: $0 [--apply] [--dump-root <absolute-path>]" >&2
      exit 1
      ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
ASSETS_ROOT="${REPO_ROOT}/Reloader/Assets/_Project/Audio/SFX"

if [[ ! -d "${ASSETS_ROOT}" ]]; then
  echo "Audio SFX folder not found: ${ASSETS_ROOT}" >&2
  exit 1
fi

USED_GUIDS_FILE="$(mktemp)"
trap 'rm -f "${USED_GUIDS_FILE}"' EXIT

cd "${REPO_ROOT}"
rg -No 'guid: ([0-9a-f]{32})' Reloader/Assets \
  --glob '!**/*.meta' \
  --glob '!Reloader/Assets/ThirdParty/**' \
  | sed -E 's/.*guid: ([0-9a-f]{32}).*/\1/' \
  | sort -u > "${USED_GUIDS_FILE}"

keep_count=0
move_count=0
move_size_bytes=0

while IFS= read -r -d '' clip_path; do
  meta_path="${clip_path}.meta"
  if [[ ! -f "${meta_path}" ]]; then
    continue
  fi

  guid="$(awk '/guid:/{print $2; exit}' "${meta_path}")"
  if [[ -z "${guid}" ]]; then
    continue
  fi

  if grep -Fxq "${guid}" "${USED_GUIDS_FILE}"; then
    keep_count=$((keep_count + 1))
    continue
  fi

  clip_size="$(stat -f%z "${clip_path}")"
  move_count=$((move_count + 1))
  move_size_bytes=$((move_size_bytes + clip_size))

  rel_path="${clip_path#${ASSETS_ROOT}/}"
  dump_clip_path="${DUMP_ROOT}/${rel_path}"
  dump_meta_path="${dump_clip_path}.meta"

  if [[ "${MODE}" == "apply" ]]; then
    mkdir -p "$(dirname "${dump_clip_path}")"
    mv "${clip_path}" "${dump_clip_path}"
    mv "${meta_path}" "${dump_meta_path}"
  else
    echo "[dry-run] would move: ${clip_path} -> ${dump_clip_path}"
  fi
done < <(find "${ASSETS_ROOT}" -type f \( -name '*.wav' -o -name '*.mp3' -o -name '*.ogg' -o -name '*.aif' -o -name '*.aiff' \) -print0)

printf "Mode: %s\n" "${MODE}"
printf "Kept clips: %d\n" "${keep_count}"
printf "Moved clips: %d\n" "${move_count}"
printf "Moved size (bytes): %d\n" "${move_size_bytes}"
if [[ "${MODE}" == "apply" ]]; then
  printf "Dump root: %s\n" "${DUMP_ROOT}"
fi
