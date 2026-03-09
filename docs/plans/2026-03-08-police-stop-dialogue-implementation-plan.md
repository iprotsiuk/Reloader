# Police Stop Dialogue MVP Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add the first police-stop dialogue slice so police interaction opens the shared dialogue runtime and confirmed replies drive real law-enforcement state changes.

**Architecture:** Reuse the dialogue foundation already landed on `feature/contract-policy-extensibility`. Extend `LawEnforcementInteractionCapability` into a dialogue-opening police capability, add one small police dialogue outcome consumer that maps explicit reply ids into the current police heat runtime, and prove the end-to-end behavior with targeted NPC/UI/law-enforcement tests.

**Tech Stack:** Unity 6.3, C#, ScriptableObject dialogue assets, NPC capability runtime under `Reloader/Assets/_Project/NPCs/**`, police heat runtime under `Reloader/Assets/_Project/Core/**`, NUnit EditMode/PlayMode tests, Unity MCP targeted test runs.

---

### Task 1: Add police-stop dialogue data contract tests

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Data/Definitions/Dialogue_PoliceStop.asset`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueDefinitionEditModeTests.cs`

**Step 1: Write the failing test**
- Add EditMode assertions that:
  - `Dialogue_PoliceStop.asset` exists
  - the asset is valid
  - it contains the expected police stop outcome ids (`police.stop.comply`, `police.stop.question`, `police.stop.leave`)

**Step 2: Run test to verify it fails**
- Run: targeted NPC EditMode tests for `DialogueDefinitionEditModeTests`
- Expected: FAIL because the police-stop asset is missing

**Step 3: Write the minimal implementation**
- Author `Dialogue_PoliceStop.asset` with one node and three replies.

**Step 4: Run test to verify it passes**
- Re-run the targeted NPC EditMode test class.

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/NPCs/Data/Definitions/Dialogue_PoliceStop.asset Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueDefinitionEditModeTests.cs
git commit -m "feat: add police stop dialogue asset"
```

### Task 2: Upgrade police interaction capability to open dialogue

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Capabilities/LawEnforcementInteractionCapability.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_Police.prefab`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcInteractionExecutionEditModeTests.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueCapabilityEditModeTests.cs` or a new police-specific test file if cleaner

**Step 1: Write the failing test**
- Add tests that prove:
  - police exposes the law-enforcement action only when a valid dialogue definition is assigned
  - executing the police action opens `DialogueRuntimeController`
  - the police prefab is wired to the authored police-stop dialogue asset

**Step 2: Run tests to verify they fail**
- Run: targeted NPC EditMode tests for the touched classes

**Step 3: Write the minimal implementation**
- Make `LawEnforcementInteractionCapability` implement action execution and reference `DialogueDefinition`
- Mirror the shared dialogue-open behavior already used by `DialogueCapability`
- Wire `Npc_Police.prefab` to the new asset

**Step 4: Run tests to verify they pass**
- Re-run the targeted NPC EditMode tests.

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/Capabilities/LawEnforcementInteractionCapability.cs Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_Police.prefab Reloader/Assets/_Project/NPCs/Tests/EditMode/*.cs
git commit -m "feat: wire police interaction into dialogue runtime"
```

### Task 3: Add a police dialogue outcome consumer

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/PoliceStopDialogueRuntime.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/PoliceHeatRuntime.cs` only if a narrow public seam is required
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/PoliceStopDialogueRuntimeTests.cs`

**Step 1: Write the failing test**
- Add tests that prove:
  - `police.stop.comply` does not escalate heat
  - `police.stop.leave` escalates into the current police heat/search path
  - unknown/non-police outcome ids are ignored

**Step 2: Run tests to verify they fail**
- Run: targeted Core EditMode tests for the new test file

**Step 3: Write the minimal implementation**
- Add a small runtime consumer that receives confirmed `DialogueOutcome`s and applies the minimum police-runtime effects needed for this slice
- Keep it data-agnostic except for explicit police-stop outcome ids

**Step 4: Run tests to verify they pass**
- Re-run the targeted Core EditMode tests

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/Core/Scripts/Runtime/PoliceStopDialogueRuntime.cs Reloader/Assets/_Project/Core/Tests/EditMode/PoliceStopDialogueRuntimeTests.cs
git commit -m "feat: add police stop dialogue outcome runtime"
```

### Task 4: Add end-to-end police stop PlayMode coverage

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/PlayerNpcInteractionPlayModeTests.cs`
- Create or modify: `Reloader/Assets/_Project/Core/Tests/PlayMode/PoliceStopDialoguePlayModeTests.cs`

**Step 1: Write the failing test**
- Add a PlayMode seam that proves:
  - looking at/interacting with police opens the shared conversation stack
  - selecting `leave` or equivalent confirms the expected police outcome
  - police runtime/heat reflects that reply

**Step 2: Run tests to verify they fail**
- Run: targeted NPC/Core PlayMode tests for the new seam

**Step 3: Write the minimal implementation**
- Wire the end-to-end police interaction through the existing shared runtime stack and the new police outcome consumer

**Step 4: Run tests to verify they pass**
- Re-run the targeted PlayMode tests

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/NPCs/Tests/PlayMode/PlayerNpcInteractionPlayModeTests.cs Reloader/Assets/_Project/Core/Tests/PlayMode/*.cs
git commit -m "feat: add police stop dialogue playmode seam"
```

### Task 5: Sync docs and status contracts

**Files:**
- Modify: `docs/design/law-enforcement.md`
- Modify: `docs/design/npcs-and-quests.md`
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`
- Create: `docs/plans/progress/2026-03-08-police-stop-dialogue-progress.md`

**Step 1: Write the failing documentation/check expectations**
- Identify where the current docs still describe police as abstract heat only or do not mention police-stop dialogue outcomes.

**Step 2: Run validation to capture current gaps**
- Run:
  - `bash scripts/verify-docs-and-context.sh`
  - `bash scripts/verify-extensible-development-contracts.sh`
  - `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

**Step 3: Write the minimal documentation updates**
- Record the police-stop dialogue slice and its outcome contract
- Update status to reflect the new police-specific conversation consumer if implemented

**Step 4: Run the full verification pass**
- Re-run targeted EditMode/PlayMode suites touched by this slice
- Re-run the three docs/context scripts above
- Run `git diff --check`

**Step 5: Commit**
```bash
git add docs/design/law-enforcement.md docs/design/npcs-and-quests.md docs/design/v0.1-demo-status-and-milestones.md docs/plans/progress/2026-03-08-police-stop-dialogue-progress.md
git commit -m "docs: record police stop dialogue contracts"
```
