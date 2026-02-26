# Domain Event Ports Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Split runtime events into domain interfaces and migrate runtime systems from legacy static event facade call sites to narrow event dependencies for long-term scalability.

**Architecture:** Runtime event ports (`IInventoryEvents`, `IWeaponEvents`, `IShopEvents`, `IUiStateEvents`) and `IGameEventsRuntimeHub` are canonical. `RuntimeKernelBootstrapper` exposes typed channels so systems consume only what they need, reducing cross-domain coupling.

**Tech Stack:** Unity 6.3, C#, NUnit EditMode/PlayMode tests, existing runtime kernel/event hub.

---

### Task 1: Add Domain Event Port Interfaces + Typed Bootstrapper Channels

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/IInventoryEvents.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/IWeaponEvents.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/IShopEvents.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Runtime/IUiStateEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IGameEventsRuntimeHub.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/RuntimeKernelBootstrapper.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/RuntimeKernelTests.cs`

**Step 1: Write failing tests first**
- Add tests asserting `RuntimeKernelBootstrapper` exposes typed channels and they resolve to current hub instance.

**Step 2: Run tests and verify RED**
- Run targeted EditMode tests.

**Step 3: Implement minimal domain interfaces + channel properties**
- Partition current event surface into domain contracts without changing behavior.
- Keep `IGameEventsRuntimeHub` as aggregate interface extending domain ports.

**Step 4: Re-run tests and verify GREEN**
- Run targeted EditMode tests.

**Step 5: Commit**
- `refactor(core): add domain event ports and typed runtime channels`

### Task 2: Migrate Inventory Runtime to Narrow Event Dependency

**Files:**
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs`
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs`

**Step 1: Write failing tests first**
- Add test asserting controller can run with injected `IInventoryEvents` without relying on legacy static event facade access.

**Step 2: Run tests and verify RED**
- Run targeted PlayMode tests for inventory controller.

**Step 3: Implement minimal migration**
- Add optional event dependency injection in `Configure(...)`.
- Default to `RuntimeKernelBootstrapper.InventoryEvents` when not injected.
- Replace legacy static event usage in controller with `IInventoryEvents` dependency.

**Step 4: Re-run tests and verify GREEN**
- Run targeted PlayMode inventory tests.

**Step 5: Commit**
- `refactor(inventory): depend on inventory event port`

### Final Verification

1. Run modified EditMode tests:
- `RuntimeKernelTests`
- `RuntimeEventHubBehaviorTests`
- `InventoryEventContractsTests`
2. Run modified PlayMode tests:
- `PlayerInventoryControllerPlayModeTests`
- `ShopVendorInteractionPlayModeTests`
- `ReloadingBenchInteractionPlayModeTests`
3. Confirm `git status` and summarize changed files.
