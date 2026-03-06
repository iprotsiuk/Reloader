# Assassin Sandbox Pivot Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Ship the first playable assassination-contract vertical slice: accept a generated contract, prepare a long-range loadout, kill the target, escape police heat, and resolve payout or failure.

**Architecture:** Keep the existing world, weapon, optics, inventory, and save foundations. Add a dedicated `Contracts` domain for mission state, a simple police-heat runtime under `LawEnforcement`, and contract-aware UI/world hooks that reframe the current range/prep systems instead of discarding them.

**Tech Stack:** Unity 6.3, C#, ScriptableObjects, existing `_Project` runtime/save/event architecture, PlayMode/EditMode tests, `.venv/bin/python -m ivan` Unity test helpers, Unity MCP for scene/prefab authoring where required.

---

### Task 1: Add contract runtime and event contracts

**Files:**
- Create: `Reloader/Assets/_Project/Contracts/Scripts/Reloader.Contracts.asmdef`
- Create: `Reloader/Assets/_Project/Contracts/Scripts/Runtime/AssassinationContractArchetype.cs`
- Create: `Reloader/Assets/_Project/Contracts/Scripts/Runtime/AssassinationContractDefinition.cs`
- Create: `Reloader/Assets/_Project/Contracts/Scripts/Runtime/AssassinationContractRuntimeState.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IGameEventsRuntimeHub.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEventContractsTests.cs`

**Step 1: Write the failing test**

Add EditMode assertions for:
- contract accepted/completed/failed events
- runtime state holding `contractId`, `targetId`, `distanceBand`, and `payout`

```csharp
[Test]
public void ContractRuntimeState_HoldsCoreContractFields()
{
    var state = new AssassinationContractRuntimeState("contract.alpha", "target.window", 420f, 1500f);
    Assert.That(state.ContractId, Is.EqualTo("contract.alpha"));
    Assert.That(state.TargetId, Is.EqualTo("target.window"));
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter ContractEventContractsTests`
Expected: FAIL because the contract domain and event members do not exist yet.

**Step 3: Write minimal implementation**

Create the runtime contract types and add event members like:

```csharp
event Action<string> OnContractAccepted;
event Action<string> OnContractFailed;
event Action<string, float> OnContractCompleted;
```

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter "ContractEventContractsTests|RuntimeKernelTests"`
Expected: PASS for the new contract event surface and no runtime-hub regression.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Contracts/Scripts \
        Reloader/Assets/_Project/Core/Scripts/Runtime/IGameEventsRuntimeHub.cs \
        Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs \
        Reloader/Assets/_Project/Core/Tests/EditMode/ContractEventContractsTests.cs
git commit -m "feat: add assassination contract runtime contracts"
```

### Task 2: Add police heat state and LOS-escape logic

**Files:**
- Create: `Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceHeatLevel.cs`
- Create: `Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceHeatState.cs`
- Create: `Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceHeatController.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IGameEventsRuntimeHub.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/PoliceHeatControllerTests.cs`

**Step 1: Write the failing test**

Add EditMode assertions for:
- `Clear -> Alerted -> ActivePursuit -> Search -> Clear`
- search countdown only progressing after LOS break

```csharp
[Test]
public void PoliceHeatController_LosBreakStartsSearchCountdown()
{
    var controller = new PoliceHeatController(searchDurationSeconds: 45f);
    controller.ReportCrime(CrimeType.Murder);
    controller.ReportLineOfSightLost();
    Assert.That(controller.CurrentState, Is.EqualTo(PoliceHeatLevel.Search));
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter PoliceHeatControllerTests`
Expected: FAIL because the heat controller/runtime types do not exist yet.

**Step 3: Write minimal implementation**

Implement:
- heat enum/state container
- controller methods for `ReportCrime`, `ReportLineOfSightLost`, `Advance`
- runtime event emission for `OnHeatChanged`

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter "PoliceHeatControllerTests|GameEventsRuntimeBridgeTests"`
Expected: PASS with the simple wanted-state shape working.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime \
        Reloader/Assets/_Project/Core/Scripts/Runtime/IGameEventsRuntimeHub.cs \
        Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs \
        Reloader/Assets/_Project/Core/Tests/EditMode/PoliceHeatControllerTests.cs
git commit -m "feat: add police heat and search runtime"
```

### Task 3: Persist contract and police-heat state, then apply arrest/death confiscation

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveFeatureFlags.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveCoordinator.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/ContractStateModule.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/PoliceHeatStateModule.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Save/Migrations/SchemaV5ToV6AddContractAndPoliceHeatStateMigration.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/SaveSkeletonTddTests.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerInventoryRuntimeTests.cs`

**Step 1: Write the failing test**

Add EditMode assertions for:
- save feature flags containing contract + police heat modules
- carried inventory being cleared on arrest/death penalty application

```csharp
[Test]
public void SaveFeatureFlags_ExposeContractAndPoliceHeatFlags()
{
    Assert.That(SaveFeatureFlags.Contracts, Is.Not.EqualTo(0));
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter "SaveSkeletonTddTests|PlayerInventoryRuntimeTests"`
Expected: FAIL because the new flags/modules/penalty behavior do not exist.

**Step 3: Write minimal implementation**

Add:
- new save feature flags
- registered save modules and migration
- a small confiscation helper that removes carried inventory on arrest/death while leaving non-carried storage alone

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter "SaveSkeletonTddTests|PlayerInventoryRuntimeTests|PlayerDeviceSaveModuleTests"`
Expected: PASS with module registration and confiscation rules covered.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Save \
        Reloader/Assets/_Project/Core/Tests/EditMode/SaveSkeletonTddTests.cs \
        Reloader/Assets/_Project/Core/Tests/EditMode/PlayerInventoryRuntimeTests.cs
git commit -m "feat: persist contract state and arrest penalties"
```

### Task 4: Repoint NPC role taxonomy and target-facing world contracts

**Files:**
- Modify: `Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcRoleKind.cs`
- Modify: `Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcContractsAndDataEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/TravelActivityType.cs`
- Modify: `Reloader/Assets/_Project/World/Data/SceneContracts/MainTownWorldSceneContract.asset`
- Modify: `Reloader/Assets/_Project/World/Data/SceneContracts/IndoorRangeInstanceWorldSceneContract.asset`
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/TravelContextEditModeTests.cs`

**Step 1: Write the failing test**

Add EditMode assertions for:
- NPC roles including `Handler`, `Target`, `Witness`, `Police`
- travel activity values covering contract-prep / contract-execution language instead of only range flow

```csharp
[Test]
public void NpcRoleKind_ContainsHandlerAndTargetRoles()
{
    Assert.That(Enum.IsDefined(typeof(NpcRoleKind), "Handler"), Is.True);
    Assert.That(Enum.IsDefined(typeof(NpcRoleKind), "Target"), Is.True);
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter "NpcContractsAndDataEditModeTests|TravelContextEditModeTests"`
Expected: FAIL because the new roles/travel language are not authored yet.

**Step 3: Write minimal implementation**

Update:
- role enum and tests
- travel activity enum naming
- world scene contracts so MainTown/IndoorRange read as contract-hub + prep space rather than the old fantasy

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter "NpcContractsAndDataEditModeTests|TravelContextEditModeTests|WorldSceneContractValidatorEditModeTests"`
Expected: PASS with updated taxonomy and scene-contract metadata.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/NPCs/Scripts/Runtime/NpcRoleKind.cs \
        Reloader/Assets/_Project/NPCs/Tests/EditMode/NpcContractsAndDataEditModeTests.cs \
        Reloader/Assets/_Project/World/Scripts/Runtime/Travel/TravelActivityType.cs \
        Reloader/Assets/_Project/World/Data/SceneContracts/MainTownWorldSceneContract.asset \
        Reloader/Assets/_Project/World/Data/SceneContracts/IndoorRangeInstanceWorldSceneContract.asset \
        Reloader/Assets/_Project/World/Tests/EditMode/TravelContextEditModeTests.cs
git commit -m "refactor: repoint npc roles and world contracts"
```

### Task 5: Reframe the player device and contract-prep UI

**Files:**
- Modify: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/PlayerDeviceRuntimeState.cs`
- Modify: `Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceController.cs`
- Modify: `Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceTargetSelectionController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryDeviceSectionPlayModeTests.cs`
- Test: `Reloader/Assets/_Project/PlayerDevice/Tests/PlayMode/PlayerDeviceTargetSelectionPlayModeTests.cs`

**Step 1: Write the failing test**

Add PlayMode assertions for:
- contract-prep copy instead of range-only copy
- device state holding contract-facing intel/validation data without breaking current grouping metrics

```csharp
[UnityTest]
public IEnumerator TabInventoryDeviceSection_ShowsContractPrepMessaging()
{
    yield return OpenDeviceSection();
    Assert.That(GetHeaderText(), Does.Contain("contract"));
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter "TabInventoryDeviceSectionPlayModeTests|PlayerDeviceTargetSelectionPlayModeTests"`
Expected: FAIL because the UI/runtime still frames the device as a range-only tool.

**Step 3: Write minimal implementation**

Keep the grouping/range math, but reframe it as contract-prep instrumentation:
- setup validation
- target intel marker selection
- contract notes/history hooks

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter "TabInventoryDeviceSectionPlayModeTests|PlayerDeviceTargetSelectionPlayModeTests|PlayerDeviceGroupSessionPlayModeTests"`
Expected: PASS with old grouping behavior preserved and new messaging in place.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/PlayerDevice/Scripts \
        Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs \
        Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml \
        Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryDeviceSectionPlayModeTests.cs \
        Reloader/Assets/_Project/PlayerDevice/Tests/PlayMode/PlayerDeviceTargetSelectionPlayModeTests.cs
git commit -m "feat: reframe device ui for contract prep"
```

### Task 6: Ship the first vertical-slice integration test

**Files:**
- Create: `Reloader/Assets/_Project/Contracts/Tests/PlayMode/AssassinationContractFlowPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/SceneTopologySmokeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write the failing test**

Add a focused PlayMode flow that proves:
- a contract can be accepted
- a target can be eliminated
- heat can rise and then clear after LOS break
- payout resolves only after escape succeeds

```csharp
[UnityTest]
public IEnumerator AssassinationContractFlow_CompletesAfterKillAndEscape()
{
    yield return AcceptContract("contract.alpha");
    yield return EliminateTarget("target.window");
    yield return BreakLineOfSightAndWait();
    Assert.That(GetCurrentPayout(), Is.GreaterThan(0f));
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter "AssassinationContractFlowPlayModeTests|RoundTripTravelPlayModeTests|PlayerWeaponControllerPlayModeTests"`
Expected: FAIL because the vertical slice is not wired together yet.

**Step 3: Write minimal implementation**

Wire only what the vertical slice needs:
- contract acceptance
- target completion
- heat raise/clear
- payout resolution
- arrest/death consequence hooks

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter "AssassinationContractFlowPlayModeTests|RoundTripTravelPlayModeTests|PlayerWeaponControllerPlayModeTests|TabInventoryDeviceSectionPlayModeTests"`
Expected: PASS for the contract slice plus the supporting travel/device surfaces.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Contracts/Tests/PlayMode/AssassinationContractFlowPlayModeTests.cs \
        Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs \
        Reloader/Assets/_Project/World/Tests/PlayMode/SceneTopologySmokeTests.cs \
        Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "feat: add assassination contract vertical slice test"
```
