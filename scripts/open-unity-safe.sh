#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/open-unity-safe.sh [--check-only]

Guards Unity launch by blocking known copy-suffix artifacts in Assets
(for example: "MainTown 2.unity", "Foo 3.meta", ".gitkeep 2").

The check scans untracked files under Reloader/Assets and fails when a
copy-suffix file has a base sibling present.
EOF
}

check_only=0
if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
  usage
  exit 0
fi
if [[ "${1:-}" == "--check-only" ]]; then
  check_only=1
fi

repo_root="$(git rev-parse --show-toplevel)"
cd "$repo_root"

assets_root="Reloader/Assets"
if [[ ! -d "$assets_root" ]]; then
  echo "ERROR: expected Unity assets root not found: $assets_root" >&2
  exit 1
fi

declare -a violations=()

while IFS= read -r rel_path; do
  # Match copy-suffix patterns like "Foo 2.meta" or ".gitkeep 3"
  if [[ ! "$rel_path" =~ [[:space:]][0-9]+(\.[^/]+)?$ ]]; then
    continue
  fi

  base_path="$(printf '%s' "$rel_path" | sed -E 's/ ([0-9]+)(\.[^./]+)$/\2/; s/ ([0-9]+)$//')"
  if [[ "$base_path" == "$rel_path" ]]; then
    continue
  fi

  if [[ -e "$base_path" ]] || git ls-files --error-unmatch "$base_path" >/dev/null 2>&1; then
    violations+=("$rel_path -> $base_path")
  fi
done < <(git ls-files --others --exclude-standard "$assets_root")

if (( ${#violations[@]} > 0 )); then
  echo "ERROR: copy-suffix artifacts detected in Assets; refusing Unity launch." >&2
  for v in "${violations[@]}"; do
    echo "  - $v" >&2
  done
  echo "Remove these files, then retry." >&2
  exit 1
fi

if (( check_only == 1 )); then
  echo "OK: no blocking copy-suffix artifacts under $assets_root"
  exit 0
fi

open -a "Unity Hub" "$repo_root/Reloader"
echo "Launched Unity Hub for project: $repo_root/Reloader"
