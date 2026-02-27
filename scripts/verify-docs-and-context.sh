#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

fail() {
  echo "FAIL: $1"
  exit 1
}

echo "Running docs/context guardrails..."

if rg -n "2026-02-22-reloader-game-design.md" docs/design .cursor/rules .agent/skills >/dev/null 2>&1; then
  fail "Active docs/rules/skills still reference deprecated monolithic design doc path."
fi

if ! rg -n "EventBus pattern.*GameEvents|GameEvents.*EventBus pattern" \
  docs/design/core-architecture.md \
  docs/design/README.md >/dev/null 2>&1; then
  fail "EventBus/GameEvents terminology contract is missing."
fi

if ! rg -n "IGameEventsRuntimeHub|runtime event ports/hub" \
  docs/design/core-architecture.md \
  docs/design/README.md \
  .agent/skills/unity-project-conventions/SKILL.md >/dev/null 2>&1; then
  fail "Runtime event hub terminology contract is missing."
fi

if rg -n "\\bSaveManager\\b" \
  docs/design/core-architecture.md \
  docs/design/inventory-and-economy.md \
  .agent/skills/unity-project-conventions/SKILL.md >/dev/null 2>&1; then
  fail "SaveManager naming drift detected. Use SaveCoordinator as canonical current save term."
fi

if ! rg -n 'Current `GameEvents` surface \(implemented in repository\)' docs/design/core-architecture.md >/dev/null 2>&1; then
  fail "core-architecture is missing the implemented GameEvents status section."
fi

if ! rg -n 'Target cross-domain `GameEvents` surface \(planned\)' docs/design/core-architecture.md >/dev/null 2>&1; then
  fail "core-architecture is missing the planned GameEvents status section."
fi

if ! rg -n "Reloader/Library/\\*\\*.*Reloader/Temp/\\*\\*.*Reloader/Logs/\\*\\*" .cursor/agents.md >/dev/null 2>&1; then
  fail "Generated directory exclusion guidance is missing from .cursor/agents.md."
fi

if ! rg -n "Global superpowers are the default workflow baseline" .cursor/agents.md >/dev/null 2>&1; then
  fail "Missing global-vs-local workflow baseline statement in .cursor/agents.md."
fi

if ! rg -n "local guidance wins|takes precedence" .cursor/agents.md >/dev/null 2>&1; then
  fail "Missing explicit local-over-global precedence rule in .cursor/agents.md."
fi

if ! rg -n "design/planning/doc-framework phase|design/planning/doc framework phase" .cursor/agents.md >/dev/null 2>&1; then
  fail "Missing explicit planning-phase contract in .cursor/agents.md."
fi

if ! rg -n "\\*\\.meta.*unless" .cursor/agents.md >/dev/null 2>&1; then
  fail "Missing Unity .meta context-hygiene guidance in .cursor/agents.md."
fi

if ! rg -n "Core/Scripts/Events/\\*\\*.*core-events-context\\.mdc|core-events-context\\.mdc.*Core/Scripts/Events/\\*\\*" .cursor/agents.md >/dev/null 2>&1; then
  fail "Missing event-routing synchronization guidance in .cursor/agents.md."
fi

if ! rg -n "Core/Scripts/Persistence/\\*\\*" .cursor/agents.md >/dev/null 2>&1; then
  fail "Missing scene persistence placement guidance in .cursor/agents.md."
fi

if [[ -f .cursor/rules/gameplay-systems-context.mdc ]]; then
  fail "Deprecated broad gameplay-systems-context.mdc should be replaced by focused domain routers."
fi

for rule in \
  .cursor/rules/inventory-economy-context.mdc \
  .cursor/rules/hunting-competitions-context.mdc \
  .cursor/rules/npcs-quests-context.mdc \
  .cursor/rules/law-enforcement-context.mdc
do
  if [[ ! -f "$rule" ]]; then
    fail "Missing focused gameplay router: $rule"
  fi
done

if [[ ! -f .cursor/rules/core-events-context.mdc ]]; then
  fail "Missing focused Core events router: .cursor/rules/core-events-context.mdc"
fi

if ! rg -n "Reloader/Assets/_Project/Core/Scripts/Events/\\*\\*" .cursor/rules/core-events-context.mdc >/dev/null 2>&1; then
  fail "core-events-context must target Core/Scripts/Events/**."
fi

if ! rg -n "Reloader/Assets/_Project/Core/Scripts/Persistence/\\*\\*" .cursor/rules/scene-persistence-context.mdc >/dev/null 2>&1; then
  fail "scene-persistence-context must target Core/Scripts/Persistence/**."
fi

if rg -n "Reloader/Assets/_Project/\\*\\*/Scripts/\\*\\*" .cursor/rules/scene-persistence-context.mdc >/dev/null 2>&1; then
  fail "scene-persistence-context should avoid broad cross-domain script globs."
fi

if ! python3 - <<'PY'
import glob
import os
import sys

root = os.getcwd()
allowed_root = os.path.normpath(os.path.join(root, "Reloader/Assets/_Project/Core/Scripts/Persistence"))
patterns = [
    "Reloader/Assets/_Project/**/Scripts/**/*Persistence*.cs",
    "Reloader/Assets/_Project/**/Scripts/**/*WorldItem*.cs",
]

violations = []
for pattern in patterns:
    for path in glob.glob(os.path.join(root, pattern), recursive=True):
        if not os.path.isfile(path):
            continue
        norm = os.path.normpath(path)
        if not norm.startswith(allowed_root + os.sep):
            violations.append(os.path.relpath(norm, root))

if violations:
    print("Persistence-related scripts must live under Core/Scripts/Persistence/:")
    for path in sorted(set(violations)):
        print(f"- {path}")
    sys.exit(1)

print("Scene persistence script placement check passed.")
PY
then
  fail "Scene persistence script placement policy violated."
fi

if rg -n "^globs: .*\\*(Instance|State)\\*" .cursor/rules/save-schema-context.mdc >/dev/null 2>&1; then
  fail "save-schema-context still uses broad *Instance*/*State* globs."
fi

if rg -n "Reloader/Assets/_Project/\\*\\*(,|$)" .cursor/rules/*.mdc >/dev/null 2>&1; then
  fail "Found overly broad _Project/** context glob. Split routers by domain."
fi

for rule in .cursor/rules/*.mdc; do
  if ! rg -n "docs/design/" "$rule" >/dev/null 2>&1; then
    fail "Context router is missing docs/design references: $rule"
  fi

  if [[ "$rule" != ".cursor/rules/game-design-docs.mdc" ]] && ! rg -n "docs/design/core-architecture.md" "$rule" >/dev/null 2>&1; then
    fail "Context router must reference core-architecture.md: $rule"
  fi
done

while IFS= read -r doc_path; do
  if [[ ! -f "$doc_path" ]]; then
    fail "Context router references a missing design doc: $doc_path"
  fi
done < <(rg -o --no-filename "docs/design/[a-z0-9-]+\\.md" .cursor/rules/*.mdc | sort -u)

if ! rg -n "inventory-and-economy.md" .cursor/rules/inventory-economy-context.mdc >/dev/null 2>&1; then
  fail "inventory-economy-context must route to docs/design/inventory-and-economy.md."
fi

if ! rg -n "hunting-and-competitions.md" .cursor/rules/hunting-competitions-context.mdc >/dev/null 2>&1; then
  fail "hunting-competitions-context must route to docs/design/hunting-and-competitions.md."
fi

if ! rg -n "npcs-and-quests.md" .cursor/rules/npcs-quests-context.mdc >/dev/null 2>&1; then
  fail "npcs-quests-context must route to docs/design/npcs-and-quests.md."
fi

if ! rg -n "law-enforcement.md" .cursor/rules/law-enforcement-context.mdc >/dev/null 2>&1; then
  fail "law-enforcement-context must route to docs/design/law-enforcement.md."
fi

if [[ ! -f docs/design/world-scene-contracts.md ]]; then
  fail "Missing docs/design/world-scene-contracts.md."
fi

if [[ ! -f docs/design/extensible-development-contracts.md ]]; then
  fail "Missing docs/design/extensible-development-contracts.md."
fi

if ! rg -n "extensible-development-contracts.md" \
  docs/design/README.md \
  .cursor/rules/core-events-context.mdc \
  .cursor/rules/scene-persistence-context.mdc \
  .cursor/rules/world-vehicles-context.mdc \
  .cursor/rules/player-ui-audio-context.mdc \
  .cursor/rules/inventory-economy-context.mdc \
  .cursor/rules/npcs-quests-context.mdc \
  .agent/skills/using-unity-mcp/SKILL.md >/dev/null 2>&1; then
  fail "extensible-development-contracts.md is not wired into docs/rules/skills."
fi

if ! rg -n "IInteractionHintEvents" docs/design/core-architecture.md docs/design/extensible-development-contracts.md >/dev/null 2>&1; then
  fail "Interaction hint runtime port contract is missing from canonical docs."
fi

if [[ ! -f docs/design/world-scene-wiring-incident-2026-02-27.md ]]; then
  fail "Missing world scene wiring incident writeup."
fi

if [[ ! -f docs/plans/2026-02-27-main-town-indoor-range-mcp-authoring-checklist.md ]]; then
  fail "Missing MCP authoring checklist for world scene workflow."
fi

if ! rg -n "world-and-scenes.md" .cursor/rules/world-vehicles-context.mdc >/dev/null 2>&1; then
  fail "world-vehicles-context must route to world-and-scenes.md."
fi

if ! rg -n "world-scene-contracts.md" .cursor/rules/world-vehicles-context.mdc >/dev/null 2>&1; then
  fail "world-vehicles-context must route to world-scene-contracts.md."
fi

if ! rg -n "world-scene-contracts.md" .agent/skills/using-unity-mcp/SKILL.md >/dev/null 2>&1; then
  fail "using-unity-mcp skill must reference world scene contract guardrails."
fi

if ! rg -n "MainWorld\\.unity.*Current baseline scene scaffold" docs/design/core-architecture.md >/dev/null 2>&1; then
  fail "core-architecture project tree is missing current MainWorld scaffold wording."
fi

if ! rg -n "MainMenu\\.unity.*Planned menu scene" docs/design/core-architecture.md >/dev/null 2>&1; then
  fail "core-architecture project tree is missing planned MainMenu wording."
fi

if ! rg -n "Reloader/Assets/_Project/Core/Scripts/Save/" .cursor/rules/inventory-save-context.mdc >/dev/null 2>&1; then
  fail "inventory-save-context rule does not target core save orchestration paths."
fi

if rg -n "Reloader/Assets/_Project/\\*\\*/Data/\\*\\*|Reloader/Assets/Scenes/\\*\\*" .cursor/rules/inventory-save-context.mdc >/dev/null 2>&1; then
  fail "inventory-save-context should be pipeline-only and must not include broad Data/Scenes globs."
fi

if [[ ! -f .cursor/rules/save-schema-context.mdc || ! -f .cursor/rules/scene-persistence-context.mdc ]]; then
  fail "Missing one or more focused save context routers (save-schema-context.mdc, scene-persistence-context.mdc)."
fi

if [[ ! -f docs/design/save-contract-quick-reference.md ]]; then
  fail "Missing docs/design/save-contract-quick-reference.md."
fi

if ! rg -n "save-contract-quick-reference.md" docs/design/README.md .cursor/rules/inventory-save-context.mdc .cursor/rules/save-schema-context.mdc .cursor/rules/scene-persistence-context.mdc >/dev/null 2>&1; then
  fail "save-contract-quick-reference.md is not wired into routing docs/rules."
fi

if ! rg -n "Feature Flag / Module Coherence|feature flag" docs/design/save-contract-quick-reference.md >/dev/null 2>&1; then
  fail "save-contract-quick-reference.md is missing feature-flag/module coherence guidance."
fi

if ! rg -n 'Soft threshold.*500 KB|Hard threshold.*1 MB' docs/design/save-and-progression.md >/dev/null 2>&1; then
  fail "Save size thresholds (500 KB soft, 1 MB hard) are missing from save-and-progression.md."
fi

if [[ ! -f scripts/report-save-size.sh ]]; then
  fail "Missing scripts/report-save-size.sh."
fi

if [[ ! -f scripts/verify-extensible-development-contracts.sh ]]; then
  fail "Missing scripts/verify-extensible-development-contracts.sh."
fi

if ! rg -n "SOFT_KB=500|HARD_KB=1024" scripts/report-save-size.sh >/dev/null 2>&1; then
  fail "report-save-size.sh default thresholds do not match policy."
fi

if ! rg -n "only when creating/editing ScriptableObject data assets" .cursor/rules/reloading-context.mdc >/dev/null 2>&1; then
  fail "reloading-context is missing conditional routing for adding-game-content skill."
fi

if [[ ! -f .cursorignore || ! -f .codexignore || ! -f .ignore ]]; then
  fail "Missing one or more root ignore files (.cursorignore, .codexignore, .ignore)."
fi

if ! rg -n "Reloader/Library/\\*\\*" .cursorignore .codexignore .ignore >/dev/null 2>&1; then
  fail "Root ignore files do not exclude Reloader/Library/**."
fi

if ! rg -n "Reloader/Temp/\\*\\*" .cursorignore .codexignore .ignore >/dev/null 2>&1; then
  fail "Root ignore files do not exclude Reloader/Temp/**."
fi

if ! rg -n "Reloader/Logs/\\*\\*" .cursorignore .codexignore .ignore >/dev/null 2>&1; then
  fail "Root ignore files do not exclude Reloader/Logs/**."
fi

echo "All docs/context guardrails passed."
