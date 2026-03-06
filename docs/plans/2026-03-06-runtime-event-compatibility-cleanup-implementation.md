# Runtime Event Compatibility Cleanup Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove obsolete non-save runtime-event compatibility so the codebase exposes only typed runtime event ports and typed shop-trade result payloads.

**Architecture:** Delete the legacy static `GameEvents` type, remove the string-based shop-trade compatibility bridge from the runtime contract, and update directly affected tests/fakes to compile against the typed `IShopEvents` API only. Preserve current runtime behavior for typed consumers.

**Tech Stack:** Unity 6.3 C#, existing runtime event hub (`IGameEventsRuntimeHub`, `IShopEvents`, `DefaultRuntimeEvents`), NUnit EditMode/PlayMode tests, repository Unity CLI test runner.

---

### Task 1: Capture the failing contract state

**Files:**
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/GameEventsRuntimeBridgeTests.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs`

**Step 1: Run targeted EditMode tests**

Run: `bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.RuntimeEventHubBehaviorTests|Reloader.Core.Tests.EditMode.InventoryEventContractsTests" tmp/runtime-event-cleanup-red.xml tmp/runtime-event-cleanup-red.log`
Expected: failure before or during tests showing the branch's current state; record exact blocker.

### Task 2: Remove legacy production compatibility

**Files:**
- Delete: `Reloader/Assets/_Project/Core/Scripts/Events/GameEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Events/ShopEventsTypes.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IShopEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`

**Step 1: Delete `GameEvents`**

- Remove the legacy static type entirely.

**Step 2: Shrink shop payload helpers**

- Remove the legacy string failure parser from `ShopTradeResultPayload`.

**Step 3: Remove legacy shop-trade runtime members**

- Delete obsolete `OnShopTradeResult` and string-based `RaiseShopTradeResult(...)` from `IShopEvents`.
- Delete matching legacy event/method/bridge logic from `DefaultRuntimeEvents`.

**Step 4: Keep typed behavior intact**

- Preserve `OnShopTradeResultReceived` and `RaiseShopTradeResult(ShopTradeResultPayload)`.

### Task 3: Update directly affected tests and fakes

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/GameEventsRuntimeBridgeTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs`
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Economy/Tests/PlayMode/EconomyControllerCheckoutPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/ShopVendorInteractionPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TradeUiToolkitPlayModeTests.cs`

**Step 1: Keep contract assertions aligned**

- Ensure core tests still assert the legacy type/members are absent and typed delivery still works.

**Step 2: Update fake `IShopEvents` implementations**

- Remove deleted legacy members and parsing calls from directly affected test doubles.

### Task 4: Verify the cleanup

**Files:**
- N/A

**Step 1: Re-run targeted EditMode tests**

Run: `bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.RuntimeEventHubBehaviorTests|Reloader.Core.Tests.EditMode.InventoryEventContractsTests" tmp/runtime-event-cleanup-edit.xml tmp/runtime-event-cleanup-edit.log`

**Step 2: Run directly affected PlayMode tests**

Run: `bash scripts/run-unity-tests.sh playmode "Reloader.Economy.Tests.PlayMode.EconomyControllerCheckoutPlayModeTests|Reloader.UI.Tests.PlayMode.TradeUiToolkitPlayModeTests|Reloader.NPCs.Tests.PlayMode.ShopVendorInteractionPlayModeTests|Reloader.Player.Tests.PlayMode.PlayerControllerPlayModeTests" tmp/runtime-event-cleanup-play.xml tmp/runtime-event-cleanup-play.log`

**Step 3: Report evidence**

- Summarize changed files.
- Include exact targeted commands and whether they passed or were blocked by unrelated compiler errors.
