# Reloading UI + Legacy Events Cleanup Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove runtime reflection menu-gating from reloading bench flow, clean scene-wiring path drift, and deprecate legacy `GameEvents` API without runtime breakage.

**Architecture:** Introduce a typed external menu state abstraction shared through `Reloader.Core`, provide an Inventory-backed implementation, and inject it into `PlayerReloadingBenchController` so storage-menu checks are typed and testable. Refactor editor scene wiring to a deterministic candidate scene list aligned with current world topology while retaining compatibility fallback. Keep `GameEvents` behavior intact but mark the surface obsolete to direct callers to runtime event ports.

**Tech Stack:** Unity C#, NUnit (EditMode/PlayMode), Unity Editor tooling (`MenuItem`, `EditorSceneManager`)

---

### Task 1: Add failing tests for typed external menu state + deprecation contracts

**Files:**
- Modify: `Reloader/Assets/_Project/Reloading/Tests/PlayMode/ReloadingBenchInteractionPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/GameEventsRuntimeBridgeTests.cs`

**Step 1: Write failing test for bench gating via typed dependency**
- Add PlayMode test that configures `PlayerReloadingBenchController` with an injected external-menu-state reader returning open, presses pickup, and expects bench not to open.

**Step 2: Verify test fails**
Run: Unity targeted PlayMode test for `ReloadingBenchInteractionPlayModeTests`
Expected: compile/test failure because controller lacks typed dependency path.

**Step 3: Write failing tests for `GameEvents` obsolete markers**
- Add EditMode test asserting `GameEvents` type and raise methods have `ObsoleteAttribute` with `IsError == false`.

**Step 4: Verify tests fail**
Run: Unity targeted EditMode test for `RuntimeEventHubBehaviorTests`
Expected: assertion failure because attributes are missing.

### Task 2: Implement typed dependency and keep runtime compatibility

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/IExternalMenuStateReader.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/StorageUiSessionMenuStateReader.cs`
- Modify: `Reloader/Assets/_Project/Reloading/Scripts/World/PlayerReloadingBenchController.cs`

**Step 1: Minimal interface + adapter**
- Add `IExternalMenuStateReader` in Core.
- Add Inventory adapter MonoBehaviour implementing the interface and returning `StorageUiSession.IsOpen`.

**Step 2: Wire into controller**
- Add serialized behaviour slot + interface resolution path.
- Extend `Configure` with optional reader argument.
- Replace per-tick reflection call with typed reader call.

**Step 3: Run targeted reloading PlayMode test**
Run: Unity targeted PlayMode test class for reloading bench interactions.
Expected: new + existing tests pass.

### Task 3: Refactor ItemIcon scene wiring path handling and add tests

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Editor/ItemIconSceneWiring.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/EditMode/Reloader.UI.Tests.EditMode.asmdef`
- Create: `Reloader/Assets/_Project/UI/Editor/AssemblyInfo.cs`
- Create: `Reloader/Assets/_Project/UI/Tests/EditMode/ItemIconSceneWiringEditModeTests.cs`

**Step 1: Add failing editor test**
- Assert candidate scene list has no duplicates, prioritizes active topology (`Bootstrap`, `MainTown`, `IndoorRangeInstance`) and includes `MainWorld` compatibility fallback.

**Step 2: Implement wiring refactor**
- Centralize candidate path list.
- Wire all existing candidate scenes (skip missing scenes safely).
- Deduplicate menu/path logic and logging.

**Step 3: Run targeted UI EditMode tests**
Run: Unity targeted EditMode tests for `UiContractGuardTests` and `ItemIconSceneWiringEditModeTests`.
Expected: pass.

### Task 4: Mark legacy `GameEvents` as warning-only obsolete

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Events/GameEvents.cs`

**Step 1: Add warning-only obsolete annotations**
- Apply `[Obsolete("...", false)]` to type/events/raise methods as needed.

**Step 2: Re-run targeted core EditMode tests**
Run: Unity targeted EditMode test class `RuntimeEventHubBehaviorTests`.
Expected: pass including obsolete contract assertions.

### Task 5: Verification and reporting

**Files:**
- N/A (verification)

**Step 1: Run targeted suites for all touched areas**
- Core EditMode runtime event bridge tests
- Reloading PlayMode bench interaction tests
- UI EditMode tests touched by editor wiring changes

**Step 2: Summarize outcomes**
- Changed files
- Behavior impact
- Test evidence + any blockers
