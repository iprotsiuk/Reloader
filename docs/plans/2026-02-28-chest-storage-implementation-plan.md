# Chest Storage Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a persistent, placeable chest system with a 20-slot chest near the outdoor MainTown workbench, plus side-by-side chest/player drag-drop UI and future workbench-link plumbing.

**Architecture:** Implement a generic container runtime keyed by stable `containerId`, integrate interaction + UI for one active container context, and persist container state in a new save module/migration path. Keep policies data-driven (`Persistent`, `DailyReset`) and add read-only workbench-linked container query plumbing for future crafting integration.

**Tech Stack:** Unity 6.3 C#, UI Toolkit, existing runtime event hub, save module pipeline (`SaveCoordinator`), NUnit EditMode/PlayMode tests.

---

### Task 1: Container Runtime + Transfer Engine

**Files:**
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/StorageContainerPolicy.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/StorageContainerRuntime.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/StorageContainerRegistry.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/StorageTransferEngine.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/StorageTransferEngineTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void Move_PlayerBackpack_ToChestSlot_MovesItem()
{
    var player = new PlayerInventoryRuntime();
    player.SetBackpackCapacity(4);
    player.BackpackItemIds.Add("powder-a");

    var chest = new StorageContainerRuntime("chest.mainTown.workbench.001", 20, StorageContainerPolicy.Persistent);
    var registry = new StorageContainerRegistry();
    registry.Upsert(chest);

    var moved = StorageTransferEngine.TryMove(
        player,
        registry,
        "backpack", 0,
        "container:chest.mainTown.workbench.001", 0);

    Assert.That(moved, Is.True);
    Assert.That(player.BackpackItemIds.Count, Is.EqualTo(0));
    Assert.That(chest.GetSlotItemId(0), Is.EqualTo("powder-a"));
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter StorageTransferEngineTests`  
Expected: FAIL due to missing storage runtime/transfer classes.

**Step 3: Write minimal implementation**

- Add container runtime with fixed slot list and bounds checks.
- Add registry keyed by `containerId`.
- Add transfer engine that resolves source/target container namespace:
  - `belt`
  - `backpack`
  - `container:{id}`
- Implement move + swap + merge path compatible with existing inventory behavior constraints.

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter StorageTransferEngineTests`  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Inventory/Scripts/Runtime/*.cs Reloader/Assets/_Project/Core/Tests/EditMode/StorageTransferEngineTests.cs
git commit -m "feat(storage): add generic container runtime and transfer engine"
```

### Task 2: World Interaction + Chest UI (Left Chest / Right Player)

**Files:**
- Create: `Reloader/Assets/_Project/Inventory/Scripts/World/Storage/WorldStorageContainer.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/World/Storage/IPlayerStorageContainerResolver.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/World/Storage/PlayerStorageContainerResolver.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/World/Storage/PlayerStorageContainerController.cs`
- Create: `Reloader/Assets/_Project/UI/Scripts/Toolkit/ChestInventory/ChestInventoryUiState.cs`
- Create: `Reloader/Assets/_Project/UI/Scripts/Toolkit/ChestInventory/ChestInventoryController.cs`
- Create: `Reloader/Assets/_Project/UI/Scripts/Toolkit/ChestInventory/ChestInventoryViewBinder.cs`
- Create: `Reloader/Assets/_Project/UI/Toolkit/UXML/ChestInventory.uxml`
- Create: `Reloader/Assets/_Project/UI/Toolkit/USS/ChestInventory.uss`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitRuntimeInstaller.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/ChestInventoryUiToolkitPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/Inventory/Tests/PlayMode/StorageContainerInteractionPlayModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void ChestInventoryController_RendersChestLeftAndPlayerRight()
{
    // initialize binder/controller with mock runtime
    // assert chest-left slot element receives chest item
    // assert player-right slot element receives belt/backpack item
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter ChestInventoryUiToolkitPlayModeTests`  
Expected: FAIL due to missing chest UI/controller.

**Step 3: Write minimal implementation**

- Add storage interaction provider to open/close active chest context.
- Add UI binder/controller that renders chest slots left and player inventory right.
- Reuse drag intent semantics (`inventory.drag.swap`/`inventory.drag.merge`) with container namespace support.
- Ensure menu-open state blocks conflicting interactions while chest is open.

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter "ChestInventoryUiToolkitPlayModeTests|StorageContainerInteractionPlayModeTests"`  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Inventory/Scripts/World/Storage/*.cs Reloader/Assets/_Project/UI/Scripts/Toolkit/ChestInventory/*.cs Reloader/Assets/_Project/UI/Toolkit/UXML/ChestInventory.uxml Reloader/Assets/_Project/UI/Toolkit/USS/ChestInventory.uss Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitRuntimeInstaller.cs Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs Reloader/Assets/_Project/UI/Tests/PlayMode/ChestInventoryUiToolkitPlayModeTests.cs Reloader/Assets/_Project/Inventory/Tests/PlayMode/StorageContainerInteractionPlayModeTests.cs
git commit -m "feat(storage-ui): add chest interaction and side-by-side chest/player UI"
```

### Task 3: Save Module + Migration + Forever Persistence Policy

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/ContainerStorageModule.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Save/Migrations/SchemaV2ToV3AddContainerStorageMigration.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/ContainerStorageSaveModuleTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void SaveBootstrapper_DefaultCoordinatorCapture_IncludesContainerStorageModule()
{
    var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
    var envelope = coordinator.CaptureEnvelope("0.3.0-dev", new SaveFeatureFlags());

    Assert.That(envelope.SchemaVersion, Is.EqualTo(3));
    Assert.That(envelope.Modules.ContainsKey("ContainerStorage"), Is.True);
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter ContainerStorageSaveModuleTests`  
Expected: FAIL due to missing module and migration.

**Step 3: Write minimal implementation**

- Add `ContainerStorage` payload with container records (`containerId`, `policy`, `slotItemIds`).
- Register module in deterministic order after existing modules.
- Add schema migration v2 -> v3 to insert default `{}` block when missing.
- Keep unknown/missing/corrupt behavior aligned with existing transactional load rules.

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter "ContainerStorageSaveModuleTests|WorldObjectStateSaveModuleTests"`  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Save/Modules/ContainerStorageModule.cs Reloader/Assets/_Project/Core/Scripts/Save/Migrations/SchemaV2ToV3AddContainerStorageMigration.cs Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs Reloader/Assets/_Project/Core/Tests/EditMode/ContainerStorageSaveModuleTests.cs
git commit -m "feat(save): add container storage save module and v2->v3 migration"
```

### Task 4: Workbench Link Plumbing + MainTown Chest Placement

**Files:**
- Create: `Reloader/Assets/_Project/Reloading/Scripts/World/WorkbenchContainerLink.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Runtime/StorageWorkbenchLinkQuery.cs`
- Modify (scene): `Reloader/Assets/_Project/World/Scenes/MainTown.unity` (via Unity MCP)
- Test: `Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchContainerLinkPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/World/Tests/PlayMode/MainTownChestPlacementPlayModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void WorkbenchContainerLink_EnumeratesLinkedContainerIds()
{
    // create workbench + link component
    // assert linked IDs are returned in deterministic order
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter "WorkbenchContainerLinkPlayModeTests|MainTownChestPlacementPlayModeTests"`  
Expected: FAIL due to missing link/plumbing/chest placement.

**Step 3: Write minimal implementation**

- Add link component for 1..N `containerId` references.
- Add read-only query helper from bench context to linked container runtime state.
- Use Unity MCP to place a chest next to outdoor workbench in `MainTown`:
  - `slotCapacity = 20`
  - `policy = Persistent`
  - stable `containerId`.
- Keep crafting consumption out of scope.

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter "WorkbenchContainerLinkPlayModeTests|MainTownChestPlacementPlayModeTests|RoundTripTravelPlayModeTests"`  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Reloading/Scripts/World/WorkbenchContainerLink.cs Reloader/Assets/_Project/Inventory/Scripts/Runtime/StorageWorkbenchLinkQuery.cs Reloader/Assets/_Project/World/Scenes/MainTown.unity Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchContainerLinkPlayModeTests.cs Reloader/Assets/_Project/World/Tests/PlayMode/MainTownChestPlacementPlayModeTests.cs
git commit -m "feat(world): place persistent 20-slot chest and add workbench container link plumbing"
```

### Task 5: Final Verification Sweep

**Files:**
- No code changes expected (verification only)

**Step 1: Run full targeted verification**

Run:

```bash
./.venv/bin/python -m ivan --unity-editmode-filter "StorageTransferEngineTests|ContainerStorageSaveModuleTests|InventoryModuleCompatibilityTests|WorldObjectStateSaveModuleTests"
./.venv/bin/python -m ivan --unity-playmode-filter "ChestInventoryUiToolkitPlayModeTests|StorageContainerInteractionPlayModeTests|WorkbenchContainerLinkPlayModeTests|MainTownChestPlacementPlayModeTests|RoundTripTravelPlayModeTests"
```

Expected: all selected suites pass.

**Step 2: Validate scene wiring/read-back evidence (Unity MCP)**

- Verify chest object exists near workbench.
- Verify component values: `containerId`, `slotCapacity=20`, `policy=Persistent`.
- Verify workbench link references chest `containerId`.

**Step 3: Commit if needed**

```bash
git add -A
git commit -m "test(storage): finalize chest storage verification coverage" || true
```

