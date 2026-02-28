# Inventory UI Container Framework Implementation Plan

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Deliver a shared container-driven inventory/trade UI framework with standard tooltips, contextual drag/drop for player containers, and cart-based vendor buy/sell checkout with an order/delivery step.

**Architecture:** Build a reusable runtime container + transfer engine layer that all inventory-like UIs consume. Keep vendor interaction button/cart driven (non-draggable), while TAB/storage/car use shared drag/drop and tooltip services. Preserve economy authority in `EconomyController`/`EconomyRuntime` and event-driven integration via runtime event ports/hub.

**Tech Stack:** Unity 6.3 C#, uGUI (`Canvas`, `Image`, `Button`, `TMP`/`Text`), Unity EventSystem drag interfaces, NUnit EditMode/PlayMode tests, existing runtime event hub and economy runtime.

---

### Task 1: Add Per-Item Max Stack Contract

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Items/ItemDefinition.cs` (or concrete item definition base used by inventory items)
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/World/ItemSpawnDefinition.cs` (if spawn defaults depend on stack size)
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/ItemDefinitionStackRulesTests.cs` (new)

**Step 1: Write the failing test**

Add tests asserting:
- Default `maxStack` is at least `1`.
- Item definitions can author distinct stack sizes (`.22lr` style high stack, `.50bmg` style lower stack).
- Invalid configured values clamp to `>=1`.

**Step 2: Run test to verify it fails**

Run:
```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform editmode -testResults "$(pwd)/.tmp/editmode-task1.xml" -testFilter "Reloader.Core.Tests.EditMode.ItemDefinitionStackRulesTests" -quit
```
Expected: FAIL because max stack contract is missing.

**Step 3: Write minimal implementation**

Add serialized `maxStack` backing field + safe getter in item definition layer used by inventory items.

**Step 4: Run test to verify it passes**

Run the same command. Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Items/ItemDefinition.cs Reloader/Assets/_Project/Inventory/Scripts/World/ItemSpawnDefinition.cs Reloader/Assets/_Project/Core/Tests/EditMode/ItemDefinitionStackRulesTests.cs
git commit -m "feat: add per-item max stack contract"
```

### Task 2: Introduce Shared Container + Stack Runtime Types

**Files:**
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/ItemStackState.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/InventoryContainerType.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/ContainerPermissions.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/InventoryContainerState.cs`
- Test: `Reloader/Assets/_Project/Inventory/Tests/EditMode/InventoryContainerStateTests.cs` (new)

**Step 1: Write the failing test**

Add tests for:
- Container initializes slots and permissions.
- Slot insert/remove preserves valid quantities.
- Container rejects invalid index operations.

**Step 2: Run test to verify it fails**

Run targeted EditMode tests for `InventoryContainerStateTests`.
Expected: FAIL due to missing runtime types.

**Step 3: Write minimal implementation**

Implement immutable/lightweight models and mutators needed by tests only.

**Step 4: Run test to verify it passes**

Run targeted EditMode tests again.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Inventory/Scripts/Runtime Reloader/Assets/_Project/Inventory/Tests/EditMode/InventoryContainerStateTests.cs
git commit -m "feat: add inventory container runtime primitives"
```

### Task 3: Build Transfer Engine With Overflow Merge Remainder Rules

**Files:**
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/InventoryTransferEngine.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/InventoryTransferResult.cs`
- Test: `Reloader/Assets/_Project/Inventory/Tests/EditMode/InventoryTransferEngineTests.cs` (new)

**Step 1: Write the failing test**

Cover all required outcomes:
- Empty target move.
- Occupied different item swap.
- Same item merge under max stack.
- Same item merge over max stack keeps remainder in source.
- Permission-denied transfer fails without mutation.

**Step 2: Run test to verify it fails**

Run targeted EditMode tests for `InventoryTransferEngineTests`.
Expected: FAIL.

**Step 3: Write minimal implementation**

Implement deterministic transfer execution and non-mutating failure paths.

**Step 4: Run test to verify it passes**

Run targeted EditMode tests again.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Inventory/Scripts/Runtime/InventoryTransferEngine.cs Reloader/Assets/_Project/Inventory/Scripts/Runtime/InventoryTransferResult.cs Reloader/Assets/_Project/Inventory/Tests/EditMode/InventoryTransferEngineTests.cs
git commit -m "feat: add transfer engine with overflow merge behavior"
```

### Task 4: Adapt Player Inventory Runtime to Container/Stack Rules

**Files:**
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryRuntime.cs`
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerInventoryRuntimeTests.cs` (extend or create under current namespace)

**Step 1: Write the failing test**

Add tests ensuring:
- Move/swap/merge behavior follows transfer engine contracts.
- Overflow merge leaves remainder in source slot.
- Existing pickup/store logic still fires inventory events.

**Step 2: Run test to verify it fails**

Run targeted EditMode + PlayMode tests.
Expected: FAIL before integration changes.

**Step 3: Write minimal implementation**

Bridge old belt/backpack arrays to container runtime without breaking current public API used by presenters.

**Step 4: Run test to verify it passes**

Run targeted tests again.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryRuntime.cs Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs Reloader/Assets/_Project/Core/Tests/EditMode/PlayerInventoryRuntimeTests.cs
git commit -m "refactor: route player inventory through shared container transfer rules"
```

### Task 5: Add Shared Tooltip Service and UI Tooltip Presenter

**Files:**
- Create: `Reloader/Assets/_Project/UI/Scripts/InventoryTooltipModel.cs`
- Create: `Reloader/Assets/_Project/UI/Scripts/InventoryTooltipService.cs`
- Create: `Reloader/Assets/_Project/UI/Scripts/InventoryTooltipPresenter.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/TabUiSlotDragHandle.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/InventoryTooltipPresenterPlayModeTests.cs` (new)

**Step 1: Write the failing test**

Add PlayMode tests validating hover on slot produces standard tooltip fields:
- Name
- Quantity
- Category
- Short stats

**Step 2: Run test to verify it fails**

Run targeted PlayMode tooltip tests.
Expected: FAIL (presenter/service missing).

**Step 3: Write minimal implementation**

Implement hover enter/exit slot hooks and tooltip model binding with null-safe behavior.

**Step 4: Run test to verify it passes**

Run targeted PlayMode tests again.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Scripts/InventoryTooltipModel.cs Reloader/Assets/_Project/UI/Scripts/InventoryTooltipService.cs Reloader/Assets/_Project/UI/Scripts/InventoryTooltipPresenter.cs Reloader/Assets/_Project/UI/Scripts/TabUiSlotDragHandle.cs Reloader/Assets/_Project/UI/Tests/PlayMode/InventoryTooltipPresenterPlayModeTests.cs
git commit -m "feat: add shared inventory tooltip service and presenter"
```

### Task 6: Replace TAB-Only Drag Logic With Shared Drag Controller

**Files:**
- Create: `Reloader/Assets/_Project/UI/Scripts/InventoryUiDragController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/TabUiPresenter.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/TabUiSlotDragHandle.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabUiPresenterPlayModeTests.cs`

**Step 1: Write the failing test**

Add/adjust PlayMode tests to assert:
- Drag controller performs contextual swap/merge.
- Overflow merge case preserves remainder in source.
- Invalid drop returns failure and keeps state unchanged.

**Step 2: Run test to verify it fails**

Run targeted TAB presenter PlayMode tests.
Expected: FAIL with old drag assumptions.

**Step 3: Write minimal implementation**

Move transfer orchestration out of presenter into shared drag controller using transfer engine.

**Step 4: Run test to verify it passes**

Run targeted tests again.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Scripts/InventoryUiDragController.cs Reloader/Assets/_Project/UI/Scripts/TabUiPresenter.cs Reloader/Assets/_Project/UI/Scripts/TabUiSlotDragHandle.cs Reloader/Assets/_Project/UI/Tests/PlayMode/TabUiPresenterPlayModeTests.cs
git commit -m "refactor: use shared drag controller for tab inventory interactions"
```

### Task 7: Rebuild Vendor UI to Cart + Order + Sell Cart Flow

**Files:**
- Modify: `Reloader/Assets/_Project/Economy/Scripts/UI/TradeUiPresenter.cs`
- Modify: `Reloader/Assets/_Project/Economy/Editor/EconomyPrefabBuilder.cs`
- Modify: `Reloader/Assets/_Project/Economy/Prefabs/TradeUi.prefab`
- Create: `Reloader/Assets/_Project/Economy/Scripts/UI/TradeCartState.cs`
- Create: `Reloader/Assets/_Project/Economy/Scripts/UI/TradeOrderState.cs`
- Test: `Reloader/Assets/_Project/Economy/Tests/PlayMode/TradeUiPresenterPlayModeTests.cs`

**Step 1: Write the failing test**

Add PlayMode tests for:
- Add-to-cart with quantity from vendor card.
- Cart total calculation.
- Transition to order screen.
- Purchase event dispatch for cart items.
- Sell tab cart checkout flow.

**Step 2: Run test to verify it fails**

Run targeted `TradeUiPresenterPlayModeTests`.
Expected: FAIL.

**Step 3: Write minimal implementation**

Implement cart model, buy/sell tab state, order screen state, and button handlers. Keep vendor slots non-draggable by design.

**Step 4: Run test to verify it passes**

Run targeted trade UI tests again.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Economy/Scripts/UI/TradeUiPresenter.cs Reloader/Assets/_Project/Economy/Editor/EconomyPrefabBuilder.cs Reloader/Assets/_Project/Economy/Prefabs/TradeUi.prefab Reloader/Assets/_Project/Economy/Scripts/UI/TradeCartState.cs Reloader/Assets/_Project/Economy/Scripts/UI/TradeOrderState.cs Reloader/Assets/_Project/Economy/Tests/PlayMode/TradeUiPresenterPlayModeTests.cs
git commit -m "feat: add vendor cart/order and sell cart checkout ui flow"
```

### Task 8: Extend Economy Controller for Cart Checkout Batching and Delivery Selection

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IShopEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- Modify: `Reloader/Assets/_Project/Economy/Scripts/Runtime/EconomyController.cs`
- Modify: `Reloader/Assets/_Project/Economy/Scripts/Runtime/EconomyRuntime.cs`
- Test: `Reloader/Assets/_Project/Economy/Tests/EditMode/EconomyRuntimeTests.cs`
- Test: `Reloader/Assets/_Project/Economy/Tests/PlayMode/TradeUiPresenterPlayModeTests.cs`

**Step 1: Write the failing test**

Add tests for:
- Batch buy checkout processes all cart lines atomically or with deterministic partial handling (define behavior explicitly in test first).
- Batch sell checkout for multiple line items.
- Delivery option impacts final price/metadata for purchase result.

**Step 2: Run test to verify it fails**

Run targeted economy EditMode + PlayMode tests.
Expected: FAIL.

**Step 3: Write minimal implementation**

Add batch request/result events and controller handlers, preserving existing money/inventory safety checks.

**Step 4: Run test to verify it passes**

Run targeted tests again.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Runtime/IShopEvents.cs Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs Reloader/Assets/_Project/Economy/Scripts/Runtime/EconomyController.cs Reloader/Assets/_Project/Economy/Scripts/Runtime/EconomyRuntime.cs Reloader/Assets/_Project/Economy/Tests/EditMode/EconomyRuntimeTests.cs Reloader/Assets/_Project/Economy/Tests/PlayMode/TradeUiPresenterPlayModeTests.cs
git commit -m "feat: support cart checkout batching and delivery options in economy flow"
```

### Task 9: Final Integration Verification and Scene Wiring Validation

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Editor/TabUiPrefabBuilder.cs` (if new tooltip/drag refs needed)
- Modify: `Reloader/Assets/_Project/Economy/Editor/EconomySceneWiring.cs` (if new trade presenter refs needed)
- Modify: `docs/design/inventory-and-economy.md` (contracts changed)

**Step 1: Write/adjust failing smoke assertions**

Add minimal integration assertions in existing PlayMode suites for:
- Vendor cannot be rearranged by drag.
- Player containers still support drag/swap/merge.
- Tooltip appears in both TAB and trade lists.

**Step 2: Run targeted suites to verify failures**

Run targeted PlayMode suites for UI + economy.
Expected: FAIL before wiring fixes.

**Step 3: Implement minimal wiring/doc updates**

Wire references in prefab builders and scene wiring utilities. Update design doc contract language for cart/order and shared container framework.

**Step 4: Run full verification**

Run:
```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform editmode -testResults "$(pwd)/.tmp/editmode-final-inventory-ui.xml" -quit

UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform playmode -testResults "$(pwd)/.tmp/playmode-final-inventory-ui.xml" -quit
```
Expected: PASS for all edited suites.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Editor/TabUiPrefabBuilder.cs Reloader/Assets/_Project/Economy/Editor/EconomySceneWiring.cs docs/design/inventory-and-economy.md
git commit -m "chore: finalize inventory ui container framework wiring and docs"
```

## Notes

- If Unity editor path differs, adjust `UNITY_EDITOR` in each command.
- Keep changes incremental and compilable after each task.
- Do not introduce vendor drag handlers; vendor remains card/cart driven.
- Keep EventBus-based integration; avoid direct cross-system coupling outside existing controller boundaries.
