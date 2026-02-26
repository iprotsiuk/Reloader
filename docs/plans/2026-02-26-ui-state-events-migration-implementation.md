# UI State Events Migration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate UI visibility and cursor-lock flows from static `GameEvents` usage to typed domain ports (`IUiStateEvents` and `IShopEvents`).

**Architecture:** Controllers receive optional typed event dependencies and default to `RuntimeKernelBootstrapper` channels. Existing `GameEvents` remains compatibility facade; runtime consumers stop depending on it directly.

**Tech Stack:** Unity 6.3, C#, NUnit PlayMode tests.

---

### Task 1: Migrate Runtime/UI Controllers to Typed UI State Channels

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerCursorLockController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`
- Modify: `Reloader/Assets/_Project/Reloading/Scripts/World/PlayerReloadingBenchController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Reloading/ReloadingWorkbenchController.cs`

**Step 1: Write failing tests first**
- Add tests proving controllers use injected `IUiStateEvents` / `IShopEvents` channels.

**Step 2: Run targeted suites to verify RED**
- Run affected PlayMode tests.

**Step 3: Implement migration**
- Add optional typed dependency injection + runtime fallback.
- Replace direct `GameEvents` visibility subscriptions/raises with typed interfaces.
- Keep behavior identical.

**Step 4: Re-run tests to verify GREEN**
- Run targeted PlayMode suites.

### Task 2: Stabilize tests around injected channels

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/UiToolkitScreenFlowPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Reloading/Tests/PlayMode/ReloadingBenchInteractionPlayModeTests.cs`

**Step 1: Add/adjust tests for injection + unsubscribe safety.**
**Step 2: Verify target test suites pass.**

### Final Verification

1. `PlayerControllerPlayModeTests`
2. `UiToolkitScreenFlowPlayModeTests`
3. `ReloadingBenchInteractionPlayModeTests`
4. `ShopVendorInteractionPlayModeTests`
5. `PlayerInventoryControllerPlayModeTests`
6. `GameEventsRuntimeBridgeTests`
