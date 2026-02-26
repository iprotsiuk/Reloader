# UI Toolkit Runtime Big-Bang Migration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace all runtime uGUI screens (Belt HUD, TAB inventory, Ammo HUD, Trade UI, Reloading Workbench UI) with UI Toolkit using a data-model-first architecture with dumb views and explicit extension points.

**Architecture:** Implement per-screen UI modules with strict layer separation (state, controller, view binder). Centralize domain wiring in controller/adapters, and expose customization via action mapping, composition config, and naming contracts. Remove old runtime uGUI paths after parity.

**Tech Stack:** Unity 6.3 C#, UI Toolkit (`UIDocument`, `VisualElement`, `UXML`, `USS`), NUnit EditMode/PlayMode tests, existing runtime event ports/hub + domain runtimes (`Inventory`, `Economy`, `Reloading`, `Weapons`).

---

### Task 1: Add Shared UI Toolkit Runtime Foundation

**Files:**
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Runtime/UiToolkitRuntimeRoot.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Runtime/UiScreenRegistry.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Runtime/UiScreenCompositionConfig.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Runtime/UiActionMapConfig.cs`
- Test: `Reloader/Assets/_Project/UI/Toolkit/Tests/EditMode/UiToolkitRuntimeRootTests.cs`

**Step 1: Write the failing test**

Add tests for:
- registry returns configured screen modules;
- composition config resolves enabled components;
- action map lookup fails safely for missing keys.

**Step 2: Run test to verify it fails**

Run: `bash scripts/run-unity-tests.sh editmode "Reloader.UI.Tests.EditMode.UiToolkitRuntimeRootTests" "$(pwd)/tmp/uitk-task1.xml" "$(pwd)/tmp/uitk-task1.log"`

Expected: FAIL with missing runtime foundation types.

**Step 3: Write minimal implementation**

Implement registry, composition config, action map, and runtime root bootstrap with no screen-specific logic.

**Step 4: Run test to verify it passes**

Run the same command.

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/Scripts/Runtime Reloader/Assets/_Project/UI/Toolkit/Tests/EditMode/UiToolkitRuntimeRootTests.cs
git commit -m "feat: add ui toolkit runtime foundation"
```

### Task 2: Define Shared Contracts for Dumb Views

**Files:**
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Contracts/IUiViewBinder.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Contracts/IUiController.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Contracts/UiIntent.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Contracts/UiRenderState.cs`
- Test: `Reloader/Assets/_Project/UI/Toolkit/Tests/EditMode/UiContractGuardTests.cs`

**Step 1: Write the failing test**

Add guard tests validating controller-view handshake contracts and action key resolution.

**Step 2: Run test to verify it fails**

Run targeted EditMode tests for `UiContractGuardTests`.

Expected: FAIL.

**Step 3: Write minimal implementation**

Implement interfaces/contracts and minimal helpers required by tests.

**Step 4: Run test to verify it passes**

Run targeted tests again.

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/Scripts/Contracts Reloader/Assets/_Project/UI/Toolkit/Tests/EditMode/UiContractGuardTests.cs
git commit -m "feat: add dumb-view ui contracts"
```

### Task 3: Build Belt HUD UI Toolkit Module

**Files:**
- Create: `Reloader/Assets/_Project/UI/Toolkit/UXML/BeltHud.uxml`
- Create: `Reloader/Assets/_Project/UI/Toolkit/USS/BeltHud.uss`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/BeltHud/BeltHudUiState.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/BeltHud/BeltHudController.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/BeltHud/BeltHudViewBinder.cs`
- Test: `Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/BeltHudUiToolkitPlayModeTests.cs`

**Step 1: Write the failing test**

Cover:
- slot visuals update from state;
- selection highlight updates;
- click intent emits correct slot index.

**Step 2: Run test to verify it fails**

Run targeted PlayMode tests.

Expected: FAIL (module missing).

**Step 3: Write minimal implementation**

Implement UXML/USS layout + dumb binder + controller adapter to current inventory events.

**Step 4: Run test to verify it passes**

Run targeted PlayMode tests again.

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/UXML/BeltHud.uxml Reloader/Assets/_Project/UI/Toolkit/USS/BeltHud.uss Reloader/Assets/_Project/UI/Toolkit/Scripts/BeltHud Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/BeltHudUiToolkitPlayModeTests.cs
git commit -m "feat: migrate belt hud to ui toolkit"
```

### Task 4: Build Ammo HUD UI Toolkit Module

**Files:**
- Create: `Reloader/Assets/_Project/UI/Toolkit/UXML/AmmoHud.uxml`
- Create: `Reloader/Assets/_Project/UI/Toolkit/USS/AmmoHud.uss`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/AmmoHud/AmmoHudUiState.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/AmmoHud/AmmoHudController.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/AmmoHud/AmmoHudViewBinder.cs`
- Test: `Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/AmmoHudUiToolkitPlayModeTests.cs`

**Step 1: Write the failing test**

Assert ammo label state rendering and visibility behavior.

**Step 2: Run test to verify it fails**

Run targeted PlayMode test.

Expected: FAIL.

**Step 3: Write minimal implementation**

Implement UITK view/controller with existing ammo source adapter.

**Step 4: Run test to verify it passes**

Run targeted test.

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/UXML/AmmoHud.uxml Reloader/Assets/_Project/UI/Toolkit/USS/AmmoHud.uss Reloader/Assets/_Project/UI/Toolkit/Scripts/AmmoHud Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/AmmoHudUiToolkitPlayModeTests.cs
git commit -m "feat: migrate ammo hud to ui toolkit"
```

### Task 5: Build TAB Inventory UI Toolkit Module

**Files:**
- Create: `Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`
- Create: `Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/TabInventory/TabInventoryUiState.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/TabInventory/TabInventoryController.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/TabInventory/TabInventoryViewBinder.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/TabInventory/TabInventoryDragController.cs`
- Test: `Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/TabInventoryUiToolkitPlayModeTests.cs`

**Step 1: Write the failing test**

Cover:
- slot rendering for belt/backpack;
- open/close behavior;
- drag merge/swap intent emissions;
- tooltip panel render contract.

**Step 2: Run test to verify it fails**

Run targeted PlayMode tests.

Expected: FAIL.

**Step 3: Write minimal implementation**

Implement UXML/USS and dumb binder with cached element queries; route intents to controller only.

**Step 4: Run test to verify it passes**

Run targeted tests.

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss Reloader/Assets/_Project/UI/Toolkit/Scripts/TabInventory Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/TabInventoryUiToolkitPlayModeTests.cs
git commit -m "feat: migrate tab inventory ui to ui toolkit"
```

### Task 6: Build Trade UI Toolkit Module

**Files:**
- Create: `Reloader/Assets/_Project/UI/Toolkit/UXML/TradeUi.uxml`
- Create: `Reloader/Assets/_Project/UI/Toolkit/USS/TradeUi.uss`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Trade/TradeUiState.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Trade/TradeController.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Trade/TradeViewBinder.cs`
- Test: `Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/TradeUiToolkitPlayModeTests.cs`

**Step 1: Write the failing test**

Cover buy/sell tab flows, cart totals, order screen transition, and confirm actions.

**Step 2: Run test to verify it fails**

Run targeted PlayMode tests.

Expected: FAIL.

**Step 3: Write minimal implementation**

Implement UITK card/cart/order presentation and intent mapping to existing economy controller.

**Step 4: Run test to verify it passes**

Run targeted tests.

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/UXML/TradeUi.uxml Reloader/Assets/_Project/UI/Toolkit/USS/TradeUi.uss Reloader/Assets/_Project/UI/Toolkit/Scripts/Trade Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/TradeUiToolkitPlayModeTests.cs
git commit -m "feat: migrate trade ui to ui toolkit"
```

### Task 7: Build Reloading Workbench UI Toolkit Module

**Files:**
- Create: `Reloader/Assets/_Project/UI/Toolkit/UXML/ReloadingWorkbench.uxml`
- Create: `Reloader/Assets/_Project/UI/Toolkit/USS/ReloadingWorkbench.uss`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Reloading/ReloadingWorkbenchUiState.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Reloading/ReloadingWorkbenchController.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Reloading/ReloadingWorkbenchViewBinder.cs`
- Test: `Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/ReloadingWorkbenchUiToolkitPlayModeTests.cs`

**Step 1: Write the failing test**

Cover operation list rendering, operation selection, execute intent, and result panel updates.

**Step 2: Run test to verify it fails**

Run targeted PlayMode tests.

Expected: FAIL.

**Step 3: Write minimal implementation**

Implement module with state-driven operation buttons and result rendering.

**Step 4: Run test to verify it passes**

Run targeted tests.

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/UXML/ReloadingWorkbench.uxml Reloader/Assets/_Project/UI/Toolkit/USS/ReloadingWorkbench.uss Reloader/Assets/_Project/UI/Toolkit/Scripts/Reloading Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/ReloadingWorkbenchUiToolkitPlayModeTests.cs
git commit -m "feat: migrate reloading workbench ui to ui toolkit"
```

### Task 8: Integrate Screen Composition + Action Mapping Across All Screens

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Runtime/UiScreenCompositionConfig.cs`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/Scripts/Runtime/UiActionMapConfig.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/Tests/EditMode/UiCompositionAndActionMapTests.cs`

**Step 1: Write the failing test**

Add tests for required mapping keys and required screen components.

**Step 2: Run test to verify it fails**

Run targeted EditMode tests.

Expected: FAIL.

**Step 3: Write minimal implementation**

Populate default mappings and composition entries for all five screens.

**Step 4: Run test to verify it passes**

Run targeted tests.

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/Scripts/Runtime/UiScreenCompositionConfig.cs Reloader/Assets/_Project/UI/Toolkit/Scripts/Runtime/UiActionMapConfig.cs Reloader/Assets/_Project/UI/Toolkit/Tests/EditMode/UiCompositionAndActionMapTests.cs
git commit -m "feat: wire screen composition and action mappings"
```

### Task 9: Cut Over Runtime Scenes and Remove Old Runtime uGUI Wiring

**Files:**
- Modify: `Reloader/Assets/Scenes/MainWorld.unity` (and any active runtime scene using old UI presenters)
- Modify: `Reloader/Assets/_Project/UI/Scripts/BeltHudBootstrap.cs`
- Modify: `Reloader/Assets/_Project/Economy/Editor/EconomySceneWiring.cs` (remove runtime uGUI dependencies if present)
- Delete (runtime-only if unused):
  - `Reloader/Assets/_Project/UI/Prefabs/BeltHud.prefab`
  - `Reloader/Assets/_Project/UI/Prefabs/TabUi.prefab`
  - old runtime uGUI presenter scripts replaced by UITK modules
- Test: `Reloader/Assets/_Project/UI/Toolkit/Tests/PlayMode/UiRuntimeCutoverPlayModeTests.cs`

**Step 1: Write the failing test**

Add cutover tests asserting runtime path uses `UIDocument` modules and no old presenter components are required.

**Step 2: Run test to verify it fails**

Run targeted PlayMode tests.

Expected: FAIL.

**Step 3: Write minimal implementation**

Switch scene/runtime bootstrap to UITK root and delete/disable old uGUI runtime references.

**Step 4: Run test to verify it passes**

Run targeted tests.

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/Scenes/MainWorld.unity Reloader/Assets/_Project/UI/Scripts/BeltHudBootstrap.cs Reloader/Assets/_Project/Economy/Editor/EconomySceneWiring.cs Reloader/Assets/_Project/UI/Toolkit
git rm Reloader/Assets/_Project/UI/Prefabs/BeltHud.prefab Reloader/Assets/_Project/UI/Prefabs/TabUi.prefab
git commit -m "refactor: cut over runtime ui to ui toolkit and remove ugui runtime"
```

### Task 10: Verify Full Regression Suite and Update Design Docs

**Files:**
- Modify: `docs/design/inventory-and-economy.md`
- Modify: `docs/design/reloading-system.md`
- Modify: `docs/design/core-architecture.md` (UI stack note if needed)
- Modify: `docs/plans/2026-02-25-uitoolkit-runtime-bigbang-design.md` (if implementation diverges)

**Step 1: Run full targeted verification**

Run:
- EditMode inventory/core/UI toolkit tests
- PlayMode UI flow tests for all five screens

Expected: all PASS.

**Step 2: Update docs**

Document that runtime UI is now UI Toolkit with dumb-view/controller boundary and action/composition extension points.

**Step 3: Run docs validation**

Run:
```bash
bash scripts/verify-docs-and-context.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected: PASS.

**Step 4: Commit**

```bash
git add docs/design/inventory-and-economy.md docs/design/reloading-system.md docs/design/core-architecture.md docs/plans/2026-02-25-uitoolkit-runtime-bigbang-design.md
git commit -m "docs: record ui toolkit runtime architecture and migration"
```
