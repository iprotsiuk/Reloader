# Dev Console Input Capture Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the developer console readable and prevent keyboard gameplay input from leaking while typing in the console.

**Architecture:** Keep the change narrow. Render the console's readable text styling in the binder so existing UI playmode tests can verify it directly, and gate keyboard-driven gameplay input in `PlayerInputReader` whenever the dev console is visible.

**Tech Stack:** Unity, UI Toolkit, Unity Input System, NUnit, Unity PlayMode tests

---

### Task 1: Add the failing console readability test

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs`

**Step 1: Write the failing test**

Add a playmode test that opens the console, renders prompt/status/suggestions, and asserts each text surface uses the same dark readable color.

**Step 2: Run test to verify it fails**

Run: `bash scripts/run-unity-tests.sh playmode "Reloader.UI.Tests.PlayMode.DevConsoleScreenPlayModeTests.OpenConsole_RendersReadableDarkTextAcrossConsoleSurface" tmp/dev-console-color-red.xml tmp/dev-console-color-red.log`
Expected: FAIL because the binder does not set explicit readable colors yet.

**Step 3: Write minimal implementation**

Update the binder and stylesheet to render the console text in a single readable dark tone.

**Step 4: Run test to verify it passes**

Run the same command and expect PASS.

**Step 5: Commit**

`git add docs/plans/2026-03-12-dev-console-input-capture-design.md docs/plans/2026-03-12-dev-console-input-capture-implementation-plan.md Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs Reloader/Assets/_Project/DevTools/Scripts/UI/DevConsoleViewBinder.cs Reloader/Assets/_Project/DevTools/UI/USS/DevConsole.uss`

### Task 2: Add the failing keyboard suppression test

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`

**Step 1: Write the failing test**

Add a playmode test that opens the dev console, sends keyboard movement and gameplay inputs, and asserts move, sprint, jump, reload, pickup, zeroing, and belt-slot hotkeys are suppressed.

**Step 2: Run test to verify it fails**

Run: `bash scripts/run-unity-tests.sh playmode "Reloader.Player.Tests.PlayMode.PlayerControllerPlayModeTests.PlayerInputReader_Update_SuppressesKeyboardGameplayInput_WhenDevConsoleIsVisible" tmp/dev-console-input-red.xml tmp/dev-console-input-red.log`
Expected: FAIL because `PlayerInputReader` still reads movement and gameplay keys while the console is visible.

**Step 3: Write minimal implementation**

Update `PlayerInputReader` to neutralize keyboard-driven gameplay input while preserving console controls.

**Step 4: Run test to verify it passes**

Run the same command and expect PASS.

**Step 5: Commit**

`git add Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs`

### Task 3: Run focused regression coverage

**Files:**
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`

**Step 1: Run the focused suites**

Run: `bash scripts/run-unity-tests.sh playmode "Reloader.UI.Tests.PlayMode.DevConsoleScreenPlayModeTests|Reloader.Player.Tests.PlayMode.PlayerControllerPlayModeTests" tmp/dev-console-regression.xml tmp/dev-console-regression.log`

**Step 2: Review failures if any**

Fix only regressions caused by this change.

**Step 3: Re-run until green**

Expect the targeted console and player-input tests to pass.

**Step 4: Review diff**

Inspect the final diff to make sure the change stayed narrow.

**Step 5: Commit**

`git add Reloader/Assets/_Project/DevTools/Scripts/UI/DevConsoleViewBinder.cs Reloader/Assets/_Project/DevTools/UI/USS/DevConsole.uss Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs docs/plans/2026-03-12-dev-console-input-capture-design.md docs/plans/2026-03-12-dev-console-input-capture-implementation-plan.md`
