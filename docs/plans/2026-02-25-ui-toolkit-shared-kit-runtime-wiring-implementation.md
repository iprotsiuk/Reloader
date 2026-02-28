# UI Toolkit Shared Kit + Runtime Wiring Implementation Plan

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Deliver working, separate UI Toolkit screens (belt HUD, TAB menu with placeholder tabs, vendor, workbench) that share reusable UI kit elements and correctly reflect runtime state/events.

**Architecture:** Add a runtime wiring layer that binds each screen's UIDocument to its controller/view binder and subscribes to runtime event ports/hub channels. Keep each screen isolated but styled through shared USS primitives/tokens so visual components are reused. Implement missing controller logic for tab inventory and trade/workbench open-close flows using typed runtime event contracts.

**Tech Stack:** Unity 6.3, UI Toolkit (UXML/USS), C#, NUnit PlayMode tests.

---

### Task 1: Add failing tests for runtime wiring and menu behavior

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/UiRuntimeCutoverPlayModeTests.cs`
- Create: `Reloader/Assets/_Project/UI/Tests/PlayMode/UiToolkitScreenFlowPlayModeTests.cs`

**Step 1: Write failing tests**
- Test that belt HUD document reflects occupied/selected classes after inventory updates.
- Test that TAB menu toggles open/closed and renders placeholder tab labels.
- Test that vendor menu opens on `OnShopTradeOpened` and closes on `OnShopTradeClosed`.
- Test that workbench menu opens/closes from workbench open state events/bridge.

**Step 2: Run tests to verify RED**
- Run: `./scripts/test.sh playmode Reloader.UI.Tests.PlayMode.UiRuntimeCutoverPlayModeTests`
- Run: `./scripts/test.sh playmode Reloader.UI.Tests.PlayMode.UiToolkitScreenFlowPlayModeTests`
- Expected: failures due to missing runtime wiring and controller behavior.

### Task 2: Implement shared UiKit visual primitives and separate screens

**Files:**
- Create: `Reloader/Assets/_Project/UI/Toolkit/USS/UiKit.uss`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/BeltHud.uxml`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/USS/BeltHud.uss`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/TradeUi.uxml`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/USS/TradeUi.uss`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/ReloadingWorkbench.uxml`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/USS/ReloadingWorkbench.uss`

**Step 1: Keep failing tests in place**
- Do not alter tests except for deterministic selectors needed by UI kit classes.

**Step 2: Implement minimal reusable visual system**
- Add shared classes/tokens (`ui-kit__panel`, `ui-kit__tab`, `ui-kit__slot`, `ui-kit__button`).
- Keep belt, tab, vendor, workbench as separate documents with their own root/panel layout.
- Prepopulate TAB menu with mock tabs/content placeholders: Inventory, Quests, Calendar, Events.

**Step 3: Run relevant binder tests**
- Run existing UI playmode tests for belt/tab/trade/reloading binders.

### Task 3: Implement runtime screen wiring + controller logic

**Files:**
- Create: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitRuntimeInstaller.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Trade/TradeController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Reloading/ReloadingWorkbenchController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Reloading/ReloadingWorkbenchViewBinder.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IUiStateEvents.cs` (only if new explicit UI events are required)
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs` (only if new explicit UI events are required)

**Step 1: Implement minimal wiring**
- Instantiate controllers and view binders against each UIDocument root at runtime.
- Bind intents with `UiContractGuard.Bind`.
- Resolve inventory/economy/workbench dependencies through existing scene components.

**Step 2: Implement missing behavior**
- `TabInventoryController`: toggle open/close, populate belt/backpack occupancy, mock TAB section switching.
- `TradeController`: respond to shop open/close events and render placeholder state.
- `ReloadingWorkbenchController`: open/close visibility hooks plus existing operation select/execute.

**Step 3: Run tests to verify GREEN**
- Run all UI playmode tests and any updated event contract tests.

### Task 4: Refactor and verify full suite scope

**Files:**
- Modify only files touched above as needed.

**Step 1: Refactor for clarity**
- Keep screen-specific logic isolated; avoid cross-screen coupling.

**Step 2: Final verification**
- Run: `./scripts/test.sh playmode Reloader.UI.Tests.PlayMode`
- Run: `./scripts/test.sh playmode Reloader.UI.Tests`
- Confirm no regressions in UI toolkit test coverage.

**Step 3: Review changed files**
- Run: `git status --short`
- Run: `git diff -- Reloader/Assets/_Project/UI`
