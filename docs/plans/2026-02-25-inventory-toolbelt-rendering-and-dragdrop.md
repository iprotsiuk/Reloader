# Inventory Toolbelt Rendering And Drag Drop Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Ensure picked-up rifle/ammo render in UI Toolkit toolbelt/inventory cells, keep ammo text HUD, enable drag-and-drop between belt and inventory, and default backpack capacity to 9 for testing.

**Architecture:** Preserve existing event-driven UI flow (runtime event ports/hub -> controller refresh -> binder render). Add visual rendering to slot binders using existing item-icon catalog conventions. Keep move semantics in `PlayerInventoryRuntime`/`PlayerInventoryController` and map UI drag/drop intents into existing transfer calls.

**Tech Stack:** Unity 6.3, UI Toolkit (UXML/USS), C# MonoBehaviours/controllers/view binders, NUnit play mode tests.

---

### Task 1: Fix Toolbelt/Inventory Cell Item Rendering

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/BeltHud/BeltHudViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Optional modify: `Reloader/Assets/_Project/UI/Toolkit/USS/BeltHud.uss`
- Optional modify: `Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/BeltHudUiToolkitPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryUiToolkitPlayModeTests.cs`

**Steps:**
1. Add failing tests that assert occupied slots render concrete in-cell item visuals (not only occupied class).
2. Run targeted tests to verify failure.
3. Implement minimal binder rendering for icon/name/stack overlays inside slot elements.
4. Re-run targeted tests to pass.

### Task 2: Implement Drag-And-Drop UX For Inventory Slots

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs` (only if needed)
- Optional modify: `Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryUiToolkitPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/UiToolkitScreenFlowPlayModeTests.cs`

**Steps:**
1. Add failing tests for pointer-driven drag start/drag-over/drop flows.
2. Verify failure.
3. Implement pointer event handlers and drag state visuals, raising existing drag intents.
4. Re-run targeted tests to pass.

### Task 3: Set Backpack Default Capacity To 9 And Align UI/Tests

**Files:**
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Modify tests under `Reloader/Assets/_Project/UI/Tests/PlayMode/*.cs` and `Reloader/Assets/_Project/Player/Tests/PlayMode/*.cs` as required

**Steps:**
1. Add failing tests for runtime/UI using 9 default backpack slots.
2. Verify failure.
3. Implement minimum default capacity and remove 16-slot hard minimum where runtime-backed UI should reflect actual capacity.
4. Run targeted tests and then broader impacted suites.

### Task 4: Integrate And Verify End-To-End

**Files:**
- Modify only files needed from tasks above.

**Steps:**
1. Merge worker outputs.
2. Run full impacted play mode/edit mode tests.
3. Fix regressions and rerun.
4. Produce summary with known risks.
