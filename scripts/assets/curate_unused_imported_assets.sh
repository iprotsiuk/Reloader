#!/usr/bin/env bash
set -euo pipefail

MODE="dry-run"
TS="$(date +%Y-%m-%d-%H%M%S)"
DUMP_ROOT="${HOME}/Documents/SOUNDS/project-asset-dump/${TS}"
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
ASSETS_ROOT="${PROJECT_ROOT}/Assets"

if [[ ! -d "${ASSETS_ROOT}" ]]; then
  echo "Assets root not found: ${ASSETS_ROOT}" >&2
  exit 1
fi

CANDIDATE_ROOTS=(
  "${ASSETS_ROOT}/Cartoon_Texture_Pack"
  "${ASSETS_ROOT}/Free Wood Door Pack"
  "${ASSETS_ROOT}/YughuesFreeConcreteMaterials"
  "${ASSETS_ROOT}/Low Poly Weapon Pack 4_WWII_1"
  "${ASSETS_ROOT}/Low Poly Optic Pack 1"
  "${ASSETS_ROOT}/LowPoly Environment Pack"
  "${ASSETS_ROOT}/EasyRoads3D scenes"
)
CANDIDATE_GLOBS=(
  "!Reloader/Assets/Cartoon_Texture_Pack/**"
  "!Reloader/Assets/Free Wood Door Pack/**"
  "!Reloader/Assets/YughuesFreeConcreteMaterials/**"
  "!Reloader/Assets/Low Poly Weapon Pack 4_WWII_1/**"
  "!Reloader/Assets/Low Poly Optic Pack 1/**"
  "!Reloader/Assets/LowPoly Environment Pack/**"
  "!Reloader/Assets/EasyRoads3D scenes/**"
)

USED_GUIDS_FILE="$(mktemp)"
trap 'rm -f "${USED_GUIDS_FILE}"' EXIT

if [[ -z "${MANIFEST_PATH}" ]]; then
  MANIFEST_PATH="${REPO_ROOT}/tmp/asset-curation-manifest-${TS}.csv"
fi
mkdir -p "$(dirname "${MANIFEST_PATH}")"
printf "mode,action,size_bytes,asset_path,dump_path,guid\n" > "${MANIFEST_PATH}"

file_size_bytes() {
  local path="$1"
  local size=""

  if size="$(stat -f%z "${path}" 2>/dev/null)"; then
    printf "%s\n" "${size}"
    return 0
  fi

  if size="$(stat -c%s "${path}" 2>/dev/null)"; then
    printf "%s\n" "${size}"
    return 0
  fi

  size="$(wc -c < "${path}" | tr -d '[:space:]')"
  if [[ -n "${size}" ]]; then
    printf "%s\n" "${size}"
    return 0
  fi

  echo "Failed to determine file size: ${path}" >&2
  return 1
}

is_protected_path() {
  local path="$1"
  [[ "${path}" == *"/Resources/"* ]] && return 0
  [[ "${path}" == *"/StreamingAssets/"* ]] && return 0
  [[ "${path}" == *"/Editor/"* ]] && return 0
  return 1
}

is_protected_extension() {
  local path="$1"
  local lower
  lower="$(printf '%s' "${path}" | tr '[:upper:]' '[:lower:]')"
  [[ "${lower}" == *.cs ]] && return 0
  [[ "${lower}" == *.js ]] && return 0
  [[ "${lower}" == *.boo ]] && return 0
  [[ "${lower}" == *.asmdef ]] && return 0
  [[ "${lower}" == *.asmref ]] && return 0
  [[ "${lower}" == *.dll ]] && return 0
  [[ "${lower}" == *.so ]] && return 0
  [[ "${lower}" == *.dylib ]] && return 0
  [[ "${lower}" == *.aar ]] && return 0
  [[ "${lower}" == *.jar ]] && return 0
  [[ "${lower}" == *.shader ]] && return 0
  [[ "${lower}" == *.compute ]] && return 0
  [[ "${lower}" == *.cginc ]] && return 0
  [[ "${lower}" == *.hlsl ]] && return 0
  return 1
}

cd "${REPO_ROOT}"

# Build GUID set from all serializable text assets + settings/packages.
rg_args=(
  -No
  'guid: ([0-9a-f]{32})'
  Reloader/Assets
  Reloader/ProjectSettings
  Reloader/Packages
  --glob '!**/*.meta'
  --glob '!Reloader/Assets/ThirdParty/**'
  --glob '!Reloader/Assets/Infima Games/**'
)
for candidate_glob in "${CANDIDATE_GLOBS[@]}"; do
  rg_args+=(--glob "${candidate_glob}")
done

rg "${rg_args[@]}" \
  | sed -E 's/.*guid: ([0-9a-f]{32}).*/\1/' \
  | sort -u > "${USED_GUIDS_FILE}"

keep_count=0
skip_count=0
move_count=0
move_size_bytes=0

for root in "${CANDIDATE_ROOTS[@]}"; do
  if [[ ! -d "${root}" ]]; then
    continue
  fi

  while IFS= read -r -d '' asset_path; do
    [[ "${asset_path}" == *.meta ]] && continue

    if is_protected_path "${asset_path}"; then
      skip_count=$((skip_count + 1))
      continue
    fi

    if is_protected_extension "${asset_path}"; then
      skip_count=$((skip_count + 1))
      continue
    fi

    meta_path="${asset_path}.meta"
    if [[ ! -f "${meta_path}" ]]; then
      skip_count=$((skip_count + 1))
      continue
    fi

    guid="$(awk '/guid:/{print $2; exit}' "${meta_path}")"
    if [[ -z "${guid}" ]]; then
      skip_count=$((skip_count + 1))
      continue
    fi

    if grep -Fxq "${guid}" "${USED_GUIDS_FILE}"; then
      keep_count=$((keep_count + 1))
      continue
    fi

    size_bytes="$(file_size_bytes "${asset_path}")"
    move_count=$((move_count + 1))
    move_size_bytes=$((move_size_bytes + size_bytes))

    rel_asset_path="${asset_path#${ASSETS_ROOT}/}"
    dump_asset_path="${DUMP_ROOT}/${rel_asset_path}"
    dump_meta_path="${dump_asset_path}.meta"

    if [[ "${MODE}" == "apply" ]]; then
      mkdir -p "$(dirname "${dump_asset_path}")"
      mv "${asset_path}" "${dump_asset_path}"
      mv "${meta_path}" "${dump_meta_path}"
      action="moved"
    else
      action="would-move"
      echo "[dry-run] ${asset_path} -> ${dump_asset_path}"
    fi

    printf "%s,%s,%s,%s,%s,%s\n" \
      "${MODE}" "${action}" "${size_bytes}" "${asset_path}" "${dump_asset_path}" "${guid}" >> "${MANIFEST_PATH}"
  done < <(find "${root}" -type f -print0)
done

printf "Mode: %s\n" "${MODE}"
printf "Candidate roots scanned: %d\n" "${#CANDIDATE_ROOTS[@]}"
printf "Kept (referenced GUID): %d\n" "${keep_count}"
printf "Skipped (protected/no-meta): %d\n" "${skip_count}"
printf "Moved: %d\n" "${move_count}"
printf "Moved size (bytes): %d\n" "${move_size_bytes}"
printf "Manifest: %s\n" "${MANIFEST_PATH}"
if [[ "${MODE}" == "apply" ]]; then
  printf "Dump root: %s\n" "${DUMP_ROOT}"
fi
