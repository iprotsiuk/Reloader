#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

fail() {
  echo "FAIL: $1"
  exit 1
}

echo "Running extensible development contract checks..."

[[ -f docs/design/extensible-development-contracts.md ]] || fail "Missing docs/design/extensible-development-contracts.md"

if ! rg -n "Runtime Event Hub Contract|UI Toolkit Runtime Bridge Contract|Save and Persistence Contract|World/Scene/Checkpoint/NPC Integration Workflow" \
  docs/design/extensible-development-contracts.md >/dev/null 2>&1; then
  fail "Extensible contract doc is missing required core sections."
fi

if ! rg -n "IInteractionHintEvents|PlayerInteractionCoordinator|UiToolkitScreenRuntimeBridge|SaveCoordinator|WorldSceneContract|TravelSceneTrigger|NpcAgent" \
  docs/design/extensible-development-contracts.md >/dev/null 2>&1; then
  fail "Extensible contract doc is missing required concrete contract anchors."
fi

if ! rg -n "extensible-development-contracts.md" \
  docs/design/README.md \
  .cursor/rules/core-events-context.mdc \
  .cursor/rules/inventory-economy-context.mdc \
  .cursor/rules/player-ui-audio-context.mdc \
  .cursor/rules/scene-persistence-context.mdc \
  .cursor/rules/world-vehicles-context.mdc \
  .cursor/rules/npcs-quests-context.mdc >/dev/null 2>&1; then
  fail "Routing drift: extensible contract doc is not referenced by required routers/docs."
fi

if ! rg -n "Core/Scripts/Runtime/\*Events\*\.cs|Core/Scripts/Events/\*\*" .cursor/agents.md .cursor/rules/core-events-context.mdc >/dev/null 2>&1; then
  fail "Event routing guidance does not cover both runtime ports and payload files."
fi

if ! rg -n "verify-extensible-development-contracts.sh" docs/design/extensible-development-contracts.md >/dev/null 2>&1; then
  fail "Extensible contract doc must reference its guardrail verification script."
fi

echo "All extensible development contract checks passed."
