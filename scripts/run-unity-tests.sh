#!/usr/bin/env bash
set -euo pipefail

# Unity command-line test runner helper.
# Important: do NOT pass -quit here.
# In this project/Unity version, -quit can terminate before command-line tests execute.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_PATH="${PROJECT_PATH:-$ROOT_DIR/Reloader}"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity}"
PLATFORM="${1:-editmode}"
FILTER="${2:-}"
RESULTS_PATH="${3:-$ROOT_DIR/tmp/test-results-${PLATFORM}.xml}"
LOG_PATH="${4:-$ROOT_DIR/tmp/test-${PLATFORM}.log}"

mkdir -p "$(dirname "$RESULTS_PATH")" "$(dirname "$LOG_PATH")"

if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo "Unity editor not found or not executable: $UNITY_EDITOR" >&2
  exit 1
fi

CMD=(
  "$UNITY_EDITOR"
  -batchmode
  -projectPath "$PROJECT_PATH"
  -runTests
  -testPlatform "$PLATFORM"
  -testResults "$RESULTS_PATH"
  -logFile "$LOG_PATH"
)

if [[ -n "$FILTER" ]]; then
  CMD+=( -testFilter "$FILTER" )
fi

"${CMD[@]}"

if [[ ! -f "$RESULTS_PATH" ]]; then
  echo "Test run finished but results file is missing: $RESULTS_PATH" >&2
  echo "See log: $LOG_PATH" >&2
  exit 2
fi

echo "Results: $RESULTS_PATH"
echo "Log:     $LOG_PATH"
