# Startup Menu Contract Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make `Bootstrap` the explicit front door with `New Game`, `Continue`, `Settings`, and `Quit`, while preserving the canonical scene-independent player and save flow.

**Architecture:** Keep `Bootstrap` as the first build scene and remove its auto-load of `MainTown`. Introduce a thin startup coordinator that owns the menu actions and routes `New Game` to `MainTown` at `entry.maintown.spawn`, and `Continue` to the latest save. Reuse the existing UI Toolkit runtime pattern so the new front door matches the rest of the project instead of inventing a parallel UI stack.

**Tech Stack:** Unity scenes/prefabs, UI Toolkit, existing save coordinator/repository, existing travel/world contracts.

---

### Task 1: Make Bootstrap a menu host

**Files:**
- Modify: `Reloader/Assets/Scenes/Bootstrap.unity`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs`
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs`

**Step 1: Write the failing test**

Add or extend a bootstrap contract test that asserts `BootstrapWorldRoot` no longer auto-enters `MainTown` on load.

**Step 2: Run test to verify it fails**

Run the targeted bootstrap/player editmode test filter.

**Step 3: Write minimal implementation**

Remove unconditional `MainTown` auto-entry from bootstrap load path.

**Step 4: Run test to verify it passes**

Run the targeted bootstrap/player editmode test filter again.

**Step 5: Commit**

Commit only the bootstrap/menu-host slice.

### Task 2: Add explicit startup actions

**Files:**
- Create: `Reloader/Assets/_Project/Startup/Scripts/Runtime/StartupMenuController.cs`
- Create: `Reloader/Assets/_Project/Startup/Scripts/Runtime/StartupMenuState.cs`
- Create: `Reloader/Assets/_Project/Startup/Tests/EditMode/StartupMenuControllerEditModeTests.cs`
- Modify: `Reloader/Assets/Scenes/Bootstrap.unity`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/...` only if a minimal bootstrap menu UXML is needed

**Step 1: Write the failing test**

Add a test that `New Game` routes to `WorldTravelCoordinator.TryLoadSceneAtEntry("MainTown", "entry.maintown.spawn")`.

**Step 2: Run test to verify it fails**

Run the new startup/menu controller test filter.

**Step 3: Write minimal implementation**

Implement a thin controller with `New Game`, `Continue`, `Settings`, and `Quit` actions.

**Step 4: Run test to verify it passes**

Run the startup/menu controller test filter again.

**Step 5: Commit**

Commit only the startup/menu controller slice.

### Task 3: Add latest-save lookup

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/IO/SaveFileRepository.cs`
- Create: `Reloader/Assets/_Project/Core/Tests/EditMode/SaveFileRepositoryEditModeTests.cs`

**Step 1: Write the failing test**

Add a test for latest-save discovery over a temp directory.

**Step 2: Run test to verify it fails**

Run the repository test filter.

**Step 3: Write minimal implementation**

Add a thin latest-save lookup seam only.

**Step 4: Run test to verify it passes**

Run the repository test filter again.

**Step 5: Commit**

Commit the repository lookup slice.

