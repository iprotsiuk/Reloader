# Dialogue Orchestration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Unify player-started and NPC-started conversations behind one dialogue orchestration layer while preserving the existing live-world overlay, focus mode, and structured reply outcomes.

**Architecture:** Keep `DialogueRuntimeController` as the owner of active conversation state, but introduce a `DialogueOrchestrator` that becomes the canonical start path for all conversations. Route current `DialogueCapability` through it, add one nearby-NPC initiator seam and one scripted helper seam, and prove both start sources use the same runtime/UI/camera/input flow.

**Tech Stack:** Unity 6.3, C#, ScriptableObject dialogue data, NPC runtime/capability seams under `Reloader/Assets/_Project/NPCs/**`, UI Toolkit overlay runtime under `Reloader/Assets/_Project/UI/**`, NUnit EditMode/PlayMode tests, Unity MCP plus disposable batchmode verification for targeted PlayMode seams.

---

### Task 1: Add orchestration request/result contracts and orchestrator tests

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueStartSourceKind.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueInterruptPolicy.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueStartRequest.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueStartResult.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueOrchestrator.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueOrchestratorEditModeTests.cs`

**Step 1: Write the failing tests**
- Add EditMode tests for:
  - valid request starts a conversation on the shared runtime
  - invalid definition or speaker is denied with explicit reason
  - overlapping requests are denied by default while a conversation is active
  - player-start and NPC-start requests both succeed through the same orchestrator API

**Step 2: Run the test to verify it fails**
- Run: targeted NPC EditMode tests for `DialogueOrchestratorEditModeTests`
- Expected: FAIL because the request/result contracts and orchestrator do not exist yet

**Step 3: Write the minimal implementation**
- Add the request/result enums and structs
- Add `DialogueOrchestrator.TryStartConversation(...)`
- Resolve `DialogueRuntimeController` via `DialogueRuntimeLocator`
- Deny overlap by default

**Step 4: Run the test to verify it passes**
- Re-run the targeted NPC EditMode test class

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueStartSourceKind.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueInterruptPolicy.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueStartRequest.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueStartResult.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueOrchestrator.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueOrchestratorEditModeTests.cs
git commit -m "feat: add dialogue orchestration contracts"
```

### Task 2: Route the existing player-talk path through the orchestrator

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Capabilities/DialogueCapability.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Capabilities/LawEnforcementInteractionCapability.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueRuntimeLocator.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueCapabilityEditModeTests.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/PlayerNpcInteractionPlayModeTests.cs`

**Step 1: Write the failing tests**
- Add tests that prove:
  - `DialogueCapability` uses the orchestrator, not direct runtime opening
  - player-triggered dialogue still provisions the player-host runtime correctly
  - player-triggered dialogue still preserves the foreign-runtime host-affinity fix

**Step 2: Run the tests to verify they fail**
- Run targeted NPC EditMode/PlayMode tests for the touched seams

**Step 3: Write the minimal implementation**
- Replace direct `TryOpenConversation(...)` calls with `DialogueOrchestrator.TryStartConversation(...)`
- Keep the same outward `NpcActionExecutionResult` semantics
- Keep `LawEnforcementInteractionCapability` on the same unified path

**Step 4: Run the tests to verify they pass**
- Re-run the targeted NPC EditMode/PlayMode tests

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/Capabilities/DialogueCapability.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/Capabilities/LawEnforcementInteractionCapability.cs Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueRuntimeLocator.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueCapabilityEditModeTests.cs Reloader/Assets/_Project/NPCs/Tests/PlayMode/PlayerNpcInteractionPlayModeTests.cs
git commit -m "refactor: route player dialogue through orchestrator"
```

### Task 3: Add a nearby NPC-initiated conversation seam

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueProximityInitiator.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueProximityInitiatorEditModeTests.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/DialogueInitiationPlayModeTests.cs`

**Step 1: Write the failing tests**
- Add tests that prove:
  - a nearby initiator can start a conversation without player interaction input
  - it uses the same shared runtime as the player-start path
  - it does not reopen repeatedly while a conversation is already active

**Step 2: Run the tests to verify they fail**
- Run targeted NPC EditMode/PlayMode tests for the new initiator

**Step 3: Write the minimal implementation**
- Add `DialogueProximityInitiator` with:
  - dialogue definition
  - trigger distance
  - optional one-shot / cooldown fields
  - simple player-host detection
- Route all starts through `DialogueOrchestrator`

**Step 4: Run the tests to verify they pass**
- Re-run the targeted initiator tests

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueProximityInitiator.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueProximityInitiatorEditModeTests.cs Reloader/Assets/_Project/NPCs/Tests/PlayMode/DialogueInitiationPlayModeTests.cs
git commit -m "feat: add nearby dialogue initiation seam"
```

### Task 4: Add a scripted-start helper seam for future quests and events

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueScriptStarter.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueScriptStarterEditModeTests.cs`

**Step 1: Write the failing tests**
- Add tests that prove:
  - runtime code can request a conversation through a small helper API
  - scripted starts still use the same orchestrator path and denial reasons

**Step 2: Run the tests to verify they fail**
- Run targeted NPC EditMode tests for the new helper

**Step 3: Write the minimal implementation**
- Add a thin helper wrapper around `DialogueOrchestrator`
- Keep it generic; no quest-specific behavior yet

**Step 4: Run the tests to verify they pass**
- Re-run the targeted helper tests

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueScriptStarter.cs Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueScriptStarterEditModeTests.cs
git commit -m "feat: add scripted dialogue start seam"
```

### Task 5: Prove unified UI/camera/input behavior for both start sources

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/World/DialogueConversationModeController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Dialogue/DialogueRuntimeOverlayBridge.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/DialogueConversationModePlayModeTests.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/DialogueOverlayUiToolkitPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/DialogueInitiationPlayModeTests.cs`

**Step 1: Write the failing tests**
- Add PlayMode coverage that proves:
  - NPC-initiated starts engage the same focus target and player lock state as player-initiated starts
  - the overlay renders correctly regardless of source kind
  - closing the conversation still restores state cleanly in both flows

**Step 2: Run the tests to verify they fail**
- Run targeted NPC/UI PlayMode tests

**Step 3: Write the minimal implementation**
- Adjust runtime/conversation-mode/overlay resolution only where needed so initiation source does not matter
- Keep the existing live-world overlay and current controls unchanged

**Step 4: Run the tests to verify they pass**
- Re-run the targeted PlayMode tests

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/NPCs/Scripts/World/DialogueConversationModeController.cs Reloader/Assets/_Project/UI/Scripts/Toolkit/Dialogue/DialogueRuntimeOverlayBridge.cs Reloader/Assets/_Project/NPCs/Tests/PlayMode/DialogueConversationModePlayModeTests.cs Reloader/Assets/_Project/UI/Tests/PlayMode/DialogueOverlayUiToolkitPlayModeTests.cs Reloader/Assets/_Project/NPCs/Tests/PlayMode/DialogueInitiationPlayModeTests.cs
git commit -m "test: lock unified dialogue initiation behavior"
```

### Task 6: Sync design docs and progress status

**Files:**
- Modify: `docs/design/npcs-and-quests.md`
- Modify: `docs/design/extensible-development-contracts.md`
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`
- Create: `docs/plans/progress/2026-03-08-dialogue-orchestration-progress.md`

**Step 1: Write the failing documentation/check expectations**
- Identify where current docs still imply dialogue is only player-triggered or capability-local

**Step 2: Run validation to capture current doc/context gaps**
- Run:
  - `bash scripts/verify-docs-and-context.sh`
  - `bash scripts/verify-extensible-development-contracts.sh`
  - `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

**Step 3: Write the minimal documentation updates**
- Record the orchestrator as the canonical dialogue entrypoint
- Record nearby/scripted start seams as implemented if landed
- Keep police as a future consumer, not the center of the system

**Step 4: Run the full verification pass**
- Re-run targeted NPC EditMode tests
- Re-run targeted NPC/UI PlayMode tests
- Re-run the three docs/context scripts above
- Run `git diff --check`

**Step 5: Commit**
```bash
git add docs/design/npcs-and-quests.md docs/design/extensible-development-contracts.md docs/design/v0.1-demo-status-and-milestones.md docs/plans/progress/2026-03-08-dialogue-orchestration-progress.md
git commit -m "docs: record dialogue orchestration contracts"
```

---

## Recommended Execution Split

Use parallel workers on disjoint ownership:
- Worker A: orchestration contracts and player-path reroute
- Worker B: nearby/scripted initiation seams and tests
- Worker C: UI/camera/input unification hardening plus docs after runtime stabilizes

Keep the main thread responsible for integration, PlayMode verification, and GitHub review resolution.

---

## Final Acceptance

The slice is complete when:
- player-started conversations route through the orchestrator
- nearby NPC-started conversations route through the orchestrator
- future scripted systems have a clean helper seam to start conversations
- both start sources use the same runtime, overlay, focus mode, cursor behavior, and teardown path
- police remains just one future consumer of the unified system, not a special-case foundation
