# Shop Events Migration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate shop gameplay flow to `IShopEvents` domain port so vendor interaction, trade UI intents, and economy runtime are decoupled from static `GameEvents`.

**Architecture:** Keep `GameEvents` as compatibility facade but move runtime consumers to injected/typed domain dependency (`IShopEvents`) with default fallback via `RuntimeKernelBootstrapper.ShopEvents`. This narrows coupling and enables module-level testability.

**Tech Stack:** Unity 6.3, C#, NUnit PlayMode/EditMode tests, runtime kernel/event hub.

---

### Task 1: Migrate Vendor + Trade UI Controllers to IShopEvents

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/World/PlayerShopVendorController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Trade/TradeController.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/PlayMode/ShopVendorInteractionPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/TradeUiToolkitPlayModeTests.cs`

**Step 1: Write failing tests first**
- Add tests that controllers work with injected `IShopEvents` fake (no direct static dependency).

**Step 2: Run targeted tests to verify RED**
- Run relevant PlayMode suites.

**Step 3: Implement minimal migration**
- Add optional `IShopEvents` injection in configure/wiring points.
- Default to `RuntimeKernelBootstrapper.ShopEvents` when not injected.
- Replace direct `GameEvents` calls/subscriptions with resolved `IShopEvents` dependency.

**Step 4: Re-run tests to verify GREEN**
- Run same targeted PlayMode suites.

**Step 5: Commit**
- `refactor(shop): migrate vendor and trade ui to IShopEvents`

### Task 2: Migrate EconomyController to IShopEvents

**Files:**
- Modify: `Reloader/Assets/_Project/Economy/Scripts/Runtime/EconomyController.cs`
- Modify: `Reloader/Assets/_Project/Economy/Tests/PlayMode/EconomyControllerCheckoutPlayModeTests.cs`

**Step 1: Write failing tests first**
- Add test proving controller handles shop events through injected `IShopEvents` channel.

**Step 2: Run targeted tests to verify RED**
- Run economy PlayMode suite.

**Step 3: Implement minimal migration**
- Add optional `IShopEvents` injection and default fallback.
- Migrate subscriptions and raises currently tied to shop events to `IShopEvents` dependency.
- Keep inventory and money events untouched in this slice.

**Step 4: Re-run tests to verify GREEN**
- Run economy PlayMode suite.

**Step 5: Commit**
- `refactor(economy): consume shop domain event port`

### Final Verification

1. Run modified PlayMode suites:
- `ShopVendorInteractionPlayModeTests`
- `TradeUiToolkitPlayModeTests`
- `EconomyControllerCheckoutPlayModeTests`
- `PlayerInventoryControllerPlayModeTests`
2. Run modified EditMode suites:
- `RuntimeKernelTests`
- `GameEventsRuntimeBridgeTests`
3. Confirm no regression in previous stale-input tests:
- `ReloadingBenchInteractionPlayModeTests`
4. Summarize changed files and remaining direct `GameEvents` shop-call sites.
