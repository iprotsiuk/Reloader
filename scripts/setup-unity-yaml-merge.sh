#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity" ./scripts/setup-unity-yaml-merge.sh

UNITY_EDITOR="${UNITY_EDITOR:-}"
if [[ -z "$UNITY_EDITOR" ]]; then
  echo "ERROR: UNITY_EDITOR is not set."
  echo "Example: UNITY_EDITOR=\"/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity\" ./scripts/setup-unity-yaml-merge.sh"
  exit 1
fi

UNITY_YAML_MERGE="$(dirname "$UNITY_EDITOR")/Tools/UnityYAMLMerge"
if [[ ! -x "$UNITY_YAML_MERGE" ]]; then
  echo "ERROR: UnityYAMLMerge not found at: $UNITY_YAML_MERGE"
  exit 1
fi

git config --local merge.unityyamlmerge.name "Unity SmartMerge"
git config --local merge.unityyamlmerge.driver "\"$UNITY_YAML_MERGE\" merge -p %O %A %B %A"
git config --local merge.unityyamlmerge.recursive binary

echo "Configured Unity SmartMerge for this repo."
echo "Driver: $UNITY_YAML_MERGE"
