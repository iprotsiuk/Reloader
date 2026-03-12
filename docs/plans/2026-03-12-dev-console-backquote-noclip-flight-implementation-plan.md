# Dev Console Backquote And Noclip Flight Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the console open cleanly with the backquote key and upgrade noclip to fly along camera look direction with a default speed of five times walk speed.

**Architecture:** Keep the console fix in the UI binder so it intercepts the first opening key event without widening the input interface. Keep the noclip change in the existing mover and dev override path by resolving view-relative movement from the active camera and treating non-positive noclip speed as "use mover default."

**Tech Stack:** Unity, UI Toolkit, Unity Input System, NUnit, Unity PlayMode tests

---

### Task 1: Add the failing console opening test

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs`

**Step 1: Write the failing test**

Add a focused test that opens the binder, simulates the first backquote key event reaching the command field, and asserts the command text remains empty.

**Step 2: Run test to verify it fails**

Run: `bash scripts/run-unity-tests.sh playmode "Reloader.UI.Tests.PlayMode.DevConsoleScreenPlayModeTests.OpenConsole_FirstBackquoteKeyDown_IsSuppressed" tmp/dev-console-backquote-red.xml tmp/dev-console-backquote-red.log`
Expected: FAIL because the command field currently keeps the opening backquote.

**Step 3: Write minimal implementation**

Update the console binder to arm a one-shot backquote suppression on visibility open and consume the first matching key event.

**Step 4: Run test to verify it passes**

Run the same command and expect PASS.

**Step 5: Commit**

`git add Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs Reloader/Assets/_Project/DevTools/Scripts/UI/DevConsoleViewBinder.cs docs/plans/2026-03-12-dev-console-backquote-noclip-flight-design.md docs/plans/2026-03-12-dev-console-backquote-noclip-flight-implementation-plan.md`

### Task 2: Add the failing noclip behavior tests

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerDevNoclipPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerDevNoclipPlayModeTests.cs`

**Step 1: Write the failing tests**

Add one test proving noclip forward movement follows camera pitch into vertical motion, and a second test proving noclip defaults to `walkSpeed * 5` when no explicit speed override is provided.

**Step 2: Run tests to verify they fail**

Run: `bash scripts/run-unity-tests.sh playmode "Reloader.Player.Tests.PlayMode.PlayerDevNoclipPlayModeTests.Tick_NoclipEnabled_FollowsActiveCameraPitchForFlight|Reloader.Player.Tests.PlayMode.PlayerDevNoclipPlayModeTests.Tick_NoclipEnabled_WithoutExplicitSpeed_UsesFiveTimesWalkSpeed" tmp/dev-noclip-flight-red.xml tmp/dev-noclip-flight-red.log`
Expected: FAIL because noclip currently flattens movement and uses fixed literal defaults.

**Step 3: Write minimal implementation**

Update `PlayerMover` and `DevPlayerMovementOverride` so noclip uses the active camera basis and resolves default speed from walk speed when no explicit speed was set.

**Step 4: Run tests to verify they pass**

Run the same command and expect PASS.

**Step 5: Commit**

`git add Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerDevNoclipPlayModeTests.cs Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevPlayerMovementOverride.cs Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevToolsState.cs`

### Task 3: Run focused regression coverage

**Files:**
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerDevNoclipPlayModeTests.cs`

**Step 1: Run the focused suites**

Run: `bash scripts/run-unity-tests.sh playmode "Reloader.UI.Tests.PlayMode.DevConsoleScreenPlayModeTests|Reloader.Player.Tests.PlayMode.PlayerDevNoclipPlayModeTests" tmp/dev-console-noclip-regression.xml tmp/dev-console-noclip-regression.log`

**Step 2: Review failures if any**

Fix only regressions caused by this change. Separate any known baseline failures.

**Step 3: Re-run until green or baseline-understood**

Expect the new tests and nearby console/noclip coverage to pass.

**Step 4: Review diff**

Inspect the final diff to make sure the change stayed narrow.

**Step 5: Commit**

`git add Reloader/Assets/_Project/DevTools/Scripts/UI/DevConsoleViewBinder.cs Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevPlayerMovementOverride.cs Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevToolsState.cs Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerDevNoclipPlayModeTests.cs docs/plans/2026-03-12-dev-console-backquote-noclip-flight-design.md docs/plans/2026-03-12-dev-console-backquote-noclip-flight-implementation-plan.md`
