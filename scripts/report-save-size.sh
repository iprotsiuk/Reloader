#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage:
  scripts/report-save-size.sh <save-file.json> [--baseline <baseline-save.json>] [--soft-kb N] [--hard-kb N] [--fail-on-soft] [--fail-on-growth]

Defaults:
  --soft-kb 500
  --hard-kb 1024

Notes:
  - Thresholds apply to uncompressed envelope file size in bytes.
  - Growth comparisons use per-module payloadJson byte sizes.
USAGE
}

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
  usage
  exit 0
fi

if [[ $# -lt 1 ]]; then
  usage
  exit 1
fi

SAVE_FILE="$1"
shift

BASELINE_FILE=""
SOFT_KB=500
HARD_KB=1024
FAIL_ON_SOFT=0
FAIL_ON_GROWTH=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --baseline)
      BASELINE_FILE="${2:-}"
      shift 2
      ;;
    --soft-kb)
      SOFT_KB="${2:-}"
      shift 2
      ;;
    --hard-kb)
      HARD_KB="${2:-}"
      shift 2
      ;;
    --fail-on-soft)
      FAIL_ON_SOFT=1
      shift
      ;;
    --fail-on-growth)
      FAIL_ON_GROWTH=1
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ ! -f "$SAVE_FILE" ]]; then
  echo "FAIL: Save file not found: $SAVE_FILE" >&2
  exit 1
fi

if [[ -n "$BASELINE_FILE" && ! -f "$BASELINE_FILE" ]]; then
  echo "FAIL: Baseline file not found: $BASELINE_FILE" >&2
  exit 1
fi

python3 - "$SAVE_FILE" "$BASELINE_FILE" "$SOFT_KB" "$HARD_KB" "$FAIL_ON_SOFT" "$FAIL_ON_GROWTH" <<'PY'
import json
import os
import sys
from typing import Dict

save_path = sys.argv[1]
baseline_path = sys.argv[2]
soft_kb = int(sys.argv[3])
hard_kb = int(sys.argv[4])
fail_on_soft = bool(int(sys.argv[5]))
fail_on_growth = bool(int(sys.argv[6]))

soft_bytes = soft_kb * 1024
hard_bytes = hard_kb * 1024

def load_envelope(path: str) -> Dict:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)

def module_payload_sizes(envelope: Dict) -> Dict[str, int]:
    modules = envelope.get("modules") or {}
    if not isinstance(modules, dict):
        raise ValueError("Invalid save envelope: 'modules' must be an object")
    sizes = {}
    for key, block in modules.items():
        payload = ""
        if isinstance(block, dict):
            payload = block.get("payloadJson") or ""
        sizes[key] = len(str(payload).encode("utf-8"))
    return sizes

def human_bytes(n: int) -> str:
    if n >= 1024 * 1024:
        return f"{n / (1024 * 1024):.2f} MB"
    return f"{n / 1024:.2f} KB"

envelope = load_envelope(save_path)
module_sizes = module_payload_sizes(envelope)
total_bytes = os.path.getsize(save_path)

print("Save Size Report")
print(f"- file: {save_path}")
print(f"- total: {total_bytes} bytes ({human_bytes(total_bytes)})")
print(f"- soft threshold: {soft_bytes} bytes ({soft_kb} KB)")
print(f"- hard threshold: {hard_bytes} bytes ({hard_kb} KB)")
print("- module payload sizes (payloadJson bytes):")

for name, size in sorted(module_sizes.items(), key=lambda x: x[1], reverse=True):
    print(f"  - {name}: {size} ({human_bytes(size)})")

hard_fail = total_bytes > hard_bytes
soft_warn = total_bytes > soft_bytes

if hard_fail:
    print(f"FAIL: Total save size exceeds hard threshold ({hard_kb} KB).")
elif soft_warn:
    print(f"WARN: Total save size exceeds soft threshold ({soft_kb} KB).")
else:
    print("OK: Total save size is within soft threshold.")

growth_fail = False
if baseline_path:
    baseline = load_envelope(baseline_path)
    baseline_sizes = module_payload_sizes(baseline)
    print(f"- baseline file: {baseline_path}")
    print("- module growth vs baseline:")
    all_modules = sorted(set(module_sizes.keys()) | set(baseline_sizes.keys()))
    for name in all_modules:
        current = module_sizes.get(name, 0)
        previous = baseline_sizes.get(name, 0)
        if previous == 0:
            if current == 0:
                pct = 0.0
            else:
                pct = 100.0
        else:
            pct = ((current - previous) / previous) * 100.0
        print(f"  - {name}: {previous} -> {current} ({pct:+.1f}%)")
        if previous > 0 and pct > 10.0:
            msg = f"WARN: Module '{name}' grew more than 10% (+{pct:.1f}%)."
            if fail_on_growth:
                msg = "FAIL: " + msg[6:]
                growth_fail = True
            print(msg)

if hard_fail:
    sys.exit(1)
if soft_warn and fail_on_soft:
    sys.exit(1)
if growth_fail:
    sys.exit(1)
PY
