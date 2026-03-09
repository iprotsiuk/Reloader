# Dialogue Foundation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a non-pausing NPC dialogue foundation with camera focus, locked movement/look, and an extendable node-based runtime that future police/vendor/fixer conversations can reuse.

**Architecture:** Reuse the existing `NpcAgent -> action -> interaction controller` path, but replace the stub dialogue execution with a shared dialogue runtime and dedicated UI Toolkit overlay. The first slice ships one-node conversations, structured outcomes, keyboard/mouse reply selection, and explicit conversation-mode camera/input handling so future multi-step dialogue and police interaction can extend the same seams.

**Tech Stack:** Unity 6.3, C#, ScriptableObject dialogue data assets, existing NPC capability/runtime seams under `Reloader/Assets/_Project/NPCs/**`, UI Toolkit runtime bridge under `Reloader/Assets/_Project/UI/**`, NUnit EditMode/PlayMode tests, Unity MCP targeted test runs.

---

### Task 1: Add dialogue data contracts and runtime state tests

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Data/DialogueDefinition.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueNodeDefinition.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueReplyDefinition.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueRenderState.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueOutcome.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueRuntimeEditModeTests.cs`

**Step 1: Write the failing tests**
- Add EditMode tests for:
  - opening a dialogue with one node exposes speaker text and replies
  - selecting a reply yields a structured outcome
  - empty/invalid definitions fail gracefully

**Step 2: Run the test to verify it fails**
- Run: targeted NPC EditMode test command for the new test class

**Step 3: Write the minimal implementation**
- Add SO/serializable data contracts and lightweight render/outcome state types.

**Step 4: Run the test to verify it passes**
- Re-run the targeted EditMode test command.

**Step 5: Commit**
- `git add` the new runtime/data/test files
- `git commit -m "feat: add dialogue data contracts"`

---

### Task 2: Replace the dialogue stub with a shared dialogue runtime

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueRuntimeController.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/IDialogueRuntime.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Capabilities/DialogueCapability.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcActionExecutionResult.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueRuntimeEditModeTests.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcInteractionExecutionEditModeTests.cs`

**Step 1: Write the failing tests**
- Add tests that prove:
  - `DialogueCapability` opens the runtime instead of just echoing payload text
  - reply confirmation emits a structured dialogue outcome
  - only one active conversation exists at a time

**Step 2: Run the tests to verify they fail**
- Run targeted NPC EditMode tests for dialogue runtime + interaction execution.

**Step 3: Write the minimal implementation**
- Implement the shared runtime open/close/select flow.
- Extend `DialogueCapability` to reference a dialogue definition and use the shared runtime.
- Extend execution results only as needed to carry dialogue-open or outcome identifiers safely.

**Step 4: Run the tests to verify they pass**
- Re-run the targeted NPC EditMode tests.

**Step 5: Commit**
- `git add` modified runtime/test files
- `git commit -m "feat: add shared dialogue runtime"`

---

### Task 3: Add conversation mode for input lock and camera focus

**Files:**
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/DialogueConversationMode.cs`
- Create: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/Dialogue/IDialogueFocusTarget.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/World/PlayerNpcInteractionController.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/PlayerNpcInteractionUiBridge.cs`
- Modify: player look/movement or interaction-mode bridge files discovered during implementation
- Test: `Reloader/Assets/_Project/NPCs/Tests/EditMode/DialogueConversationModeEditModeTests.cs`
- Test: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/PlayerNpcInteractionPlayModeTests.cs`

**Step 1: Write the failing tests**
- Add tests that prove:
  - opening dialogue locks movement/look interaction paths
  - the active focus target is the NPC being spoken to
  - the conversation closes if the NPC target becomes invalid

**Step 2: Run the tests to verify they fail**
- Run targeted dialogue/NPC tests.

**Step 3: Write the minimal implementation**
- Add explicit conversation-mode state owned by dialogue runtime.
- Bridge it into player interaction/camera seams with the smallest reusable contract.
- Keep world unpaused while cursor visibility and focus state change.

**Step 4: Run the tests to verify they pass**
- Re-run the targeted tests.

**Step 5: Commit**
- `git add` the dialogue mode/runtime changes
- `git commit -m "feat: add dialogue conversation mode"`

---

### Task 4: Build the UI Toolkit dialogue overlay and input intents

**Files:**
- Create: `Reloader/Assets/_Project/UI/Toolkit/UXML/DialogueOverlay.uxml`
- Create: `Reloader/Assets/_Project/UI/Toolkit/USS/DialogueOverlay.uss`
- Create: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Dialogue/DialogueOverlayController.cs`
- Create: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Dialogue/DialogueOverlayViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Modify: composition/install files discovered during implementation
- Test: `Reloader/Assets/_Project/UI/Tests/EditMode/DialogueOverlayUxmlEditModeTests.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/DialogueOverlayPlayModeTests.cs`

**Step 1: Write the failing tests**
- Add UI tests for:
  - speaker line renders
  - replies render with one selected entry
  - overlay hides when no dialogue is active
  - keyboard and mouse intents route through binder/controller

**Step 2: Run the tests to verify they fail**
- Run the targeted UI EditMode/PlayMode tests.

**Step 3: Write the minimal implementation**
- Add the overlay UXML/USS.
- Add binder/controller intent mapping for up/down/confirm and hover/click.
- Wire the runtime bridge to the new dialogue render state.

**Step 4: Run the tests to verify they pass**
- Re-run the targeted UI tests.

**Step 5: Commit**
- `git add` the overlay/binder/controller/test files
- `git commit -m "feat: add dialogue overlay UI"`

---

### Task 5: Add an end-to-end NPC dialogue proof point

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Prefabs/Roles/Npc_FrontDeskClerk.prefab` or another low-risk dialogue-capable prefab
- Create or modify: dialogue asset(s) under `Reloader/Assets/_Project/NPCs/Data/**`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/PlayerNpcInteractionPlayModeTests.cs`
- Create if needed: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/NpcDialogueOverlayPlayModeTests.cs`

**Step 1: Write the failing tests**
- Add an end-to-end playmode seam:
  - interact with NPC
  - dialogue opens
  - focus/lock state engages
  - choose a reply
  - dialogue closes with expected outcome

**Step 2: Run the tests to verify they fail**
- Run the targeted NPC PlayMode test command.

**Step 3: Write the minimal implementation**
- Author one real dialogue asset and wire one role prefab or scene NPC to it.
- Keep content small and reusable.

**Step 4: Run the tests to verify they pass**
- Re-run the targeted PlayMode tests.

**Step 5: Commit**
- `git add` updated content/runtime/tests
- `git commit -m "feat: add NPC dialogue proof point"`

---

### Task 6: Sync docs/contracts and run final verification

**Files:**
- Modify: `docs/design/npcs-and-quests.md`
- Modify: `docs/design/extensible-development-contracts.md`
- Modify: any `.cursor/rules/*.mdc` files only if new runtime folders require routing

**Step 1: Write the failing documentation/check expectations**
- Identify outdated NPC/dialogue contract wording and any missing routing references.

**Step 2: Run validation to capture current failures or gaps**
- Run:
  - `bash scripts/verify-docs-and-context.sh`
  - `bash scripts/verify-extensible-development-contracts.sh`
  - `bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh`

**Step 3: Write the minimal documentation updates**
- Document the shipped dialogue foundation and conversation-mode contract.

**Step 4: Run the full verification pass**
- Re-run:
  - targeted NPC EditMode tests
  - targeted NPC PlayMode tests
  - targeted UI EditMode/PlayMode tests
  - the three docs/context scripts above
  - `git diff --check`

**Step 5: Commit**
- `git add` docs/routing/test updates
- `git commit -m "docs: record dialogue foundation contracts"`

---

## Recommended Execution Split

Use parallel workers on disjoint ownership:
- Worker A: dialogue data/runtime core
- Worker B: UI overlay/binder/controller
- Worker C: docs/routing follow-through after runtime/UI stabilize

Keep the camera/input conversation mode on the main thread or a dedicated single worker because it crosses NPC/player/UI seams and is the highest integration risk.

---

## Review Checkpoints

After each task:
- run spec-compliance review first
- then run code-quality review
- do not move to the next task with open review findings

---

## Final Acceptance

The slice is complete when:
- an NPC can open a dialogue overlay through the existing interaction seam
- the conversation is non-pausing
- movement/look are locked while dialogue is active
- camera focus stays on the NPC
- replies work with `W/S + E` and mouse
- selecting a reply closes with a structured outcome
- the runtime shape clearly supports multi-step dialogue later without rewrite
