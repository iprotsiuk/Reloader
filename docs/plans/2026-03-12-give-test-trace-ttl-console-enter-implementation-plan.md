# Dev Console Starter Kit, Trace TTL, and Enter Autocomplete Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add `give test`, replace `traces persistent ...` with `trace <seconds>`, and make `Enter` accept highlighted autocomplete without executing.

**Architecture:** Keep console input behavior in `DevConsoleController`, extend `DevCommandContext` so give-command logic can reach the existing `PlayerWeaponController`, and move dev trace state from a bool flag to a TTL float consumed directly by `DevTraceRuntime`.

**Tech Stack:** Unity C#, NUnit, Unity PlayMode/EditMode tests, UI Toolkit.

---

### Task 1: Console Enter Acceptance

**Files:**
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/UI/DevConsoleController.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/DevConsoleScreenPlayModeTests.cs`

**Step 1: Write the failing tests**

Add playmode coverage proving:
- `Enter` accepts the highlighted suggestion when suggestions are visible.
- That keypress does not execute the command.
- The caret lands at the end of the accepted text.

**Step 2: Run the focused console test slice to verify RED**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.UI.Tests.PlayMode.DevConsoleScreenPlayModeTests.OpenConsole_EnterAcceptsHighlightedSuggestionWithoutExecuting" tmp/dev-console-enter-red.xml tmp/dev-console-enter-red.log
```

Expected: FAIL because submit still executes immediately and/or caret position is unchanged.

**Step 3: Write the minimal implementation**

Update `DevConsoleController.Tick()` so submit keys first attempt suggestion acceptance, mirror tab behavior, set the command text, move the caret to EOL, refresh suggestions, and return without executing on that frame.

**Step 4: Run the focused console test slice to verify GREEN**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.UI.Tests.PlayMode.DevConsoleScreenPlayModeTests.OpenConsole_EnterAcceptsHighlightedSuggestionWithoutExecuting" tmp/dev-console-enter-green.xml tmp/dev-console-enter-green.log
```

Expected: PASS.

### Task 2: Trace TTL Command

**Files:**
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandCatalog.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevTracesCommand.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevToolsRuntime.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevToolsState.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevTraceRuntime.cs`
- Test: `Reloader/Assets/_Project/DevTools/Tests/EditMode/DevCommandCatalogTests.cs`
- Test: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevToolsRuntimePlayModeTests.cs`
- Test: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevTraceRuntimePlayModeTests.cs`

**Step 1: Write the failing tests**

Add tests for:
- `trace` existing in the default catalog and ordered command list.
- `trace -1`, `trace 0`, and `trace 1` command behavior.
- TTL-driven segment expiration and disable-clears behavior.

**Step 2: Run the focused trace tests to verify RED**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.DevTools.Tests.PlayMode.DevToolsRuntimePlayModeTests|Reloader.DevTools.Tests.PlayMode.DevTraceRuntimePlayModeTests" tmp/dev-trace-ttl-red.xml tmp/dev-trace-ttl-red.log
```

Expected: FAIL because the runtime still expects `traces persistent ...` and hardcodes a 5-second lifetime.

**Step 3: Write the minimal implementation**

Rename the command surface to `trace`, parse a numeric TTL, store it on `DevToolsState`, and have `DevTraceRuntime` compute visible lifetime from the current TTL, including permanent and disabled semantics.

**Step 4: Run the focused trace tests to verify GREEN**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.DevTools.Tests.PlayMode.DevToolsRuntimePlayModeTests|Reloader.DevTools.Tests.PlayMode.DevTraceRuntimePlayModeTests" tmp/dev-trace-ttl-green.xml tmp/dev-trace-ttl-green.log
```

Expected: PASS.

### Task 3: `give test` Starter Kit

**Files:**
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandContext.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevCommandExecutors/DevGiveItemCommand.cs`
- Test: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevGiveItemCommandPlayModeTests.cs`

**Step 1: Write the failing tests**

Add a playmode test proving `give test`:
- grants Kar98k, scope, and `ammo-308 x500`
- equips/selects the Kar98k
- applies the scope attachment
- seeds a loaded weapon state with the expected reserve ammo

**Step 2: Run the focused give-command test slice to verify RED**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.DevTools.Tests.PlayMode.DevGiveItemCommandPlayModeTests.GiveTest_GrantsStarterKitAndEquipsLoadedScopedKar98k" tmp/dev-give-test-red.xml tmp/dev-give-test-red.log
```

Expected: FAIL because `give test` is not recognized and the context does not expose the weapon controller.

**Step 3: Write the minimal implementation**

Extend `DevCommandContext` with weapon-controller resolution and add a special-case `give test` flow in `DevGiveItemCommand` that grants the kit, selects the weapon on the belt, and applies runtime attachment/ammo state through `PlayerWeaponController`.

**Step 4: Run the focused give-command test slice to verify GREEN**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.DevTools.Tests.PlayMode.DevGiveItemCommandPlayModeTests.GiveTest_GrantsStarterKitAndEquipsLoadedScopedKar98k" tmp/dev-give-test-green.xml tmp/dev-give-test-green.log
```

Expected: PASS.

### Task 4: Narrow Regression Verification

**Files:**
- Verify only

**Step 1: Run the combined regression slice**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.UI.Tests.PlayMode.DevConsoleScreenPlayModeTests|Reloader.DevTools.Tests.PlayMode.DevGiveItemCommandPlayModeTests|Reloader.DevTools.Tests.PlayMode.DevToolsRuntimePlayModeTests|Reloader.DevTools.Tests.PlayMode.DevTraceRuntimePlayModeTests|Reloader.DevTools.Tests.EditMode.DevCommandCatalogTests" tmp/dev-console-give-trace-regression.xml tmp/dev-console-give-trace-regression.log
```

Expected: PASS except for any already-known unrelated batchmode baseline failures, which must be called out explicitly if present.

**Step 2: Run repo guardrails relevant to docs/contracts**

Run:

```bash
scripts/verify-docs-and-context.sh
scripts/verify-extensible-development-contracts.sh
.agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected: PASS.
