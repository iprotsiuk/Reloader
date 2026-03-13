# HUD Compass Navigation Assist Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a reusable top-center UI Toolkit compass HUD that shows scrolling cardinal directions from true north and an active-contract target marker on the XZ plane.

**Architecture:** Extend the existing UI Toolkit runtime composition with a dedicated compass screen wired through `UiToolkitRuntimeInstaller` and `UiToolkitScreenRuntimeBridge`. Keep the heading math and marker projection in a compact compass-state layer so future POI markers can reuse the same bearing-to-lane-position contract.

**Tech Stack:** Unity 6 C#, UI Toolkit (`UIDocument`, `VisualTreeAsset`, USS/UXML), existing runtime composition bridge, contract runtime provider seam, civilian population runtime bridge, NUnit EditMode/PlayMode tests.

---

### Task 1: Define compass render-state and heading math with TDD

**Files:**
- Create: `Reloader/Assets/_Project/UI/Scripts/Toolkit/CompassHud/CompassHudUiState.cs`
- Create: `Reloader/Assets/_Project/UI/Scripts/Toolkit/CompassHud/CompassHeadingMath.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/EditMode/CompassHeadingMathEditModeTests.cs`

**Step 1: Write the failing test**
- Cover:
  - `+Z` maps to north, `+X` maps to east
  - signed delta wraps correctly at `0/360`
  - target bearing ignores Y and uses XZ only
  - render-state entries support both cardinal labels and generic markers

**Step 2: Run test to verify it fails**
Run: Unity EditMode test filtered to `CompassHeadingMathEditModeTests`
Expected: FAIL because the compass math/state files do not exist yet.

**Step 3: Write minimal implementation**
- Add a small math helper that:
  - derives heading from a forward vector on XZ
  - derives bearing from viewer-to-target on XZ
  - computes signed shortest-angle deltas
- Add a UI state model that can represent visible labels and markers.

**Step 4: Run test to verify it passes**
Run: the same filtered EditMode test
Expected: PASS

**Step 5: Commit**
```bash
git add docs/plans/2026-03-13-hud-compass-navigation-assist-design.md docs/plans/2026-03-13-hud-compass-navigation-assist-implementation-plan.md Reloader/Assets/_Project/UI/Scripts/Toolkit/CompassHud/ Reloader/Assets/_Project/UI/Tests/EditMode/CompassHeadingMathEditModeTests.cs
git commit -m "feat: add compass heading math foundation"
```

### Task 2: Add the compass view binder and UI assets

**Files:**
- Create: `Reloader/Assets/_Project/UI/Scripts/Toolkit/CompassHud/CompassHudViewBinder.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/UXML/CompassHud.uxml`
- Create: `Reloader/Assets/_Project/UI/Toolkit/USS/CompassHud.uss`
- Test: `Reloader/Assets/_Project/UI/Tests/EditMode/CompassHudViewBinderEditModeTests.cs`

**Step 1: Write the failing test**
- Verify the binder can:
  - initialize from the compass UXML names
  - render repeated cardinal labels
  - show/hide the contract marker element
  - position entries horizontally from render-state offsets

**Step 2: Run test to verify it fails**
Run: Unity EditMode test filtered to `CompassHudViewBinderEditModeTests`
Expected: FAIL because the binder/UXML/USS do not exist yet.

**Step 3: Write minimal implementation**
- Create a top-center compass root with a masked lane and center tick.
- Implement binder logic that populates label/marker elements from `CompassHudUiState`.
- Keep entry creation generic so future POI marker kinds can reuse the same binder.

**Step 4: Run test to verify it passes**
Run: the same filtered EditMode test
Expected: PASS

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/UI/Scripts/Toolkit/CompassHud/CompassHudViewBinder.cs Reloader/Assets/_Project/UI/Toolkit/UXML/CompassHud.uxml Reloader/Assets/_Project/UI/Toolkit/USS/CompassHud.uss Reloader/Assets/_Project/UI/Tests/EditMode/CompassHudViewBinderEditModeTests.cs
git commit -m "feat: add compass hud view binder"
```

### Task 3: Add the runtime controller and contract-target projection

**Files:**
- Create: `Reloader/Assets/_Project/UI/Scripts/Toolkit/CompassHud/CompassHudController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiRuntimeCompositionIds.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitRuntimeInstaller.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/CompassHudRuntimeBridgePlayModeTests.cs`

**Step 1: Write the failing test**
- Verify the runtime bridge:
  - binds the compass screen
  - renders cardinals from viewer heading
  - shows a contract marker when an active target is resolvable
  - hides the marker when there is no active contract or no live target fix

**Step 2: Run test to verify it fails**
Run: Unity PlayMode test filtered to `CompassHudRuntimeBridgePlayModeTests`
Expected: FAIL because the compass screen/controller wiring does not exist yet.

**Step 3: Write minimal implementation**
- Add new runtime composition ids.
- Install the new compass UXML in the runtime installer.
- Bind a `CompassHudController` in the screen runtime bridge.
- In the controller:
  - use the viewer transform from the runtime bridge seam
  - resolve active contract snapshot through `IContractRuntimeProvider`
  - resolve live target transform through `CivilianPopulationRuntimeBridge`
  - build the render-state with cardinals plus optional active-contract marker

**Step 4: Run test to verify it passes**
Run: the same filtered PlayMode test
Expected: PASS

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/UI/Scripts/Toolkit/CompassHud/CompassHudController.cs Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiRuntimeCompositionIds.cs Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitRuntimeInstaller.cs Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs Reloader/Assets/_Project/UI/Tests/PlayMode/CompassHudRuntimeBridgePlayModeTests.cs
git commit -m "feat: wire compass hud runtime bridge"
```

### Task 4: Add scene/runtime coverage for the installed HUD

**Files:**
- Modify: runtime-installed scene assets only if required by the current installer flow
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/UiToolkitRuntimeInstallerPlayModeTests.cs` or create a focused compass installer test if coverage is missing

**Step 1: Write the failing test**
- Verify `UiToolkitRuntimeInstaller.ExecuteCutover()` creates a compass screen document alongside the existing HUD screens.

**Step 2: Run test to verify it fails**
Run: the focused PlayMode test
Expected: FAIL because installer coverage does not yet know about the compass screen.

**Step 3: Write minimal implementation**
- Extend installer coverage only as needed.
- Avoid scene-local one-off wiring if the runtime installer already owns this path.

**Step 4: Run test to verify it passes**
Run: the same focused PlayMode test
Expected: PASS

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/UI/Tests/PlayMode
git commit -m "test: cover compass hud installer composition"
```

### Task 5: Verify end-to-end behavior and prepare branch for review

**Files:**
- Modify: any touched implementation/tests/docs from previous tasks as needed

**Step 1: Run focused verification**
- Run the new EditMode and PlayMode compass tests.
- Run the existing UI runtime bridge tests that overlap the modified bridge/installer files.

**Step 2: Fix any regressions**
- Keep fixes minimal and localized.

**Step 3: Run verification again**
- Re-run the same focused suite until clean.

**Step 4: Commit**
```bash
git add Reloader/Assets/_Project/UI docs/plans
git commit -m "test: verify compass hud navigation assist"
```
