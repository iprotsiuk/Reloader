# Tab Menu UX Revamp Implementation Plan

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make Tab menu reliably open/close via input contract and ship a non-overflowing inventory-first tab UX with Inventory/Quests/Journal/Calendar views.

**Architecture:** Route Tab menu toggle through `IPlayerInputSource` so UI relies on shared runtime input semantics. Keep section switching intent-driven in `TabInventoryController` while redesigning UXML/USS to stable responsive constraints.

**Tech Stack:** Unity C#, UI Toolkit (UXML/USS), NUnit PlayMode tests.

---

### Task 1: Input Contract + Red Test

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/UiToolkitScreenFlowPlayModeTests.cs`

**Step 1: Write failing test**
- Add test asserting `TabInventoryController` toggles open when menu-toggle input is consumed.

**Step 2: Run red test**
- Run: Unity PlayMode test for the new test only.
- Expected: compile/test failure due to missing input contract wiring.

### Task 2: Implement Input Contract Wiring

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Modify: all `TestInputSource` implementations implementing `IPlayerInputSource`

**Step 1: Minimal implementation**
- Add `ConsumeMenuTogglePressed()` contract.
- Queue and consume a new action in `PlayerInputReader`.
- Inject input source into `TabInventoryController`, remove direct keyboard polling.

**Step 2: Run green tests**
- Run targeted PlayMode tests for UI + Player/NPC/Weapons/Economy suites impacted by interface change.

### Task 3: Tab UX + Layout Revamp

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`

**Step 1: Red test for section IDs**
- Add/update test for `journal` section switching.

**Step 2: Implement UX changes**
- Replace Events with Journal tab/section.
- Ensure default section remains inventory.
- Update layout to constrained panel, readable tabs, non-overlapping content.

**Step 3: Green verification**
- Run UI PlayMode tests.

### Task 4: Final Verification

**Files:**
- No code changes required unless failures found.

**Step 1: Run verification commands**
- Run relevant PlayMode test commands and capture pass/fail.

**Step 2: Report evidence**
- Summarize files changed and exact verification outcomes.
