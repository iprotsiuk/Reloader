# Startup Menu Contract Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Turn `Bootstrap` into the explicit front door with `New Game`, `Continue`, `Settings`, and `Quit`, without reintroducing scene-owned player ownership or floating-origin work.

**Architecture:** Keep `Bootstrap` as the first build scene, but stop it from auto-entering `MainTown` on load. Add a thin startup/menu coordinator that owns menu actions, routes `New Game` to the authored `MainTown` spawn anchor, and routes `Continue` through latest-save discovery into the existing save/world restore path. Keep the menu minimal and reuse the project’s existing UI Toolkit approach rather than building a parallel UI stack.

**Tech Stack:** Unity scenes/prefabs, UI Toolkit, existing save coordinator/repository, existing world travel contracts, EditMode tests.

---

### Task 1: Remove Bootstrap auto-entry

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs`
- Modify: `Reloader/Assets/Scenes/Bootstrap.unity`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs`

**Step 1: Write the failing test**

Add a bootstrap contract test that asserts loading `Bootstrap` does not immediately auto-load `MainTown`.

**Step 2: Run test to verify it fails**

Run the targeted bootstrap/player editmode test filter.

**Step 3: Write minimal implementation**

Remove the unconditional `MainTown` auto-entry from bootstrap load.

**Step 4: Run test to verify it passes**

Run the same targeted test filter again.

**Step 5: Commit**

Commit only the bootstrap-auto-entry removal slice.

### Task 2: Add a minimal main menu front door

**Files:**
- Create: `Reloader/Assets/_Project/Startup/Scripts/Runtime/StartupMenuController.cs`
- Create: `Reloader/Assets/_Project/Startup/Scripts/Runtime/StartupMenuState.cs`
- Modify: `Reloader/Assets/Scenes/Bootstrap.unity`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/...` only if a minimal menu UXML is needed
- Test: `Reloader/Assets/_Project/Startup/Tests/EditMode/StartupMenuControllerEditModeTests.cs`

**Step 1: Write the failing test**

Add a test for `New Game`, `Continue`, `Settings`, and `Quit` menu actions.

**Step 2: Run test to verify it fails**

Run the startup/menu controller test filter.

**Step 3: Write minimal implementation**

Implement a thin controller that exposes the menu actions and routes them to the existing travel/save seams.

**Step 4: Run test to verify it passes**

Run the startup/menu controller test filter again.

**Step 5: Commit**

Commit only the menu-front-door slice.

### Task 3: Add latest-save discovery

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/IO/SaveFileRepository.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/SaveFileRepositoryEditModeTests.cs`

**Step 1: Write the failing test**

Add a test that discovers the newest save in a temp directory.

**Step 2: Run test to verify it fails**

Run the repository test filter.

**Step 3: Write minimal implementation**

Add the smallest latest-save lookup seam only.

**Step 4: Run test to verify it passes**

Run the repository test filter again.

**Step 5: Commit**

Commit only the latest-save lookup slice.

