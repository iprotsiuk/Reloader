# Player Device Hub Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a permanent TAB-integrated player device with notes baseline, attachment install/uninstall plumbing, target binding, and true geometric angular-space MOA group tracking (including moving targets).

**Architecture:** Implement a dedicated `PlayerDevice` runtime/controller module and keep `TabInventory` as a thin UI shell. Add target metrics contract + dummy target implementation, wire impact ingestion to active bound target, compute group spread in angular space, and expose command methods that TAB uses now and future hotkeys can call later.

**Tech Stack:** Unity 6.3 C#, existing runtime event hub, UI Toolkit (`TabInventory`), NUnit EditMode/PlayMode tests.

---

### Task 1: Core Device Runtime State + Contracts

**Files:**
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/DeviceAttachmentType.cs`
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/DeviceAttachmentState.cs`
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/DeviceShotSample.cs`
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/DeviceGroupSession.cs`
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/PlayerDeviceRuntimeState.cs`
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/IRangeTargetMetrics.cs`
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/DeviceTargetBinding.cs`
- Test: `Reloader/Assets/_Project/PlayerDevice/Tests/EditMode/PlayerDeviceRuntimeStateEditModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void InstallThenUninstallAttachment_TransitionsStateCorrectly()
{
    var state = new PlayerDeviceRuntimeState();
    Assert.That(state.IsAttachmentInstalled(DeviceAttachmentType.Rangefinder), Is.False);

    state.InstallAttachment(DeviceAttachmentType.Rangefinder);
    Assert.That(state.IsAttachmentInstalled(DeviceAttachmentType.Rangefinder), Is.True);

    state.UninstallAttachment(DeviceAttachmentType.Rangefinder);
    Assert.That(state.IsAttachmentInstalled(DeviceAttachmentType.Rangefinder), Is.False);
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter PlayerDeviceRuntimeStateEditModeTests`  
Expected: FAIL due to missing device runtime types.

**Step 3: Write minimal implementation**

- Add runtime state with:
  - installed attachment set,
  - selected target binding,
  - active group session,
  - saved group session list,
  - notes text payload.
- Add deterministic APIs for install/uninstall/bind/clear/save operations.

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter PlayerDeviceRuntimeStateEditModeTests`  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/*.cs Reloader/Assets/_Project/PlayerDevice/Tests/EditMode/PlayerDeviceRuntimeStateEditModeTests.cs
git commit -m "feat(device): add core runtime state and contracts"
```

### Task 2: Angular-Space MOA Math Engine

**Files:**
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/DeviceGroupMetricsCalculator.cs`
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/DeviceGroupMetrics.cs`
- Test: `Reloader/Assets/_Project/PlayerDevice/Tests/EditMode/DeviceGroupMetricsCalculatorEditModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void Calculate_UsesAngularSpaceAndReturnsTrueMoa()
{
    var shots = new []
    {
        new DeviceShotSample(new Vector2(0f, 0f), 100f),
        new DeviceShotSample(new Vector2(0.0254f, 0f), 100f),
    };

    var metrics = DeviceGroupMetricsCalculator.Calculate(shots);
    Assert.That(metrics.ShotCount, Is.EqualTo(2));
    Assert.That(metrics.Moa, Is.GreaterThan(0f));
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter DeviceGroupMetricsCalculatorEditModeTests`  
Expected: FAIL due to missing calculator.

**Step 3: Write minimal implementation**

- Implement angular-space pairwise extreme spread:
  - `thetaX = atan(x / d)`, `thetaY = atan(y / d)`
  - max pair angular delta
  - convert to MOA with radians-to-arcminutes factor.
- Return both MOA and linear spread metrics.
- Guard invalid distances and insufficient sample counts.

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter DeviceGroupMetricsCalculatorEditModeTests`  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/DeviceGroupMetrics*.cs Reloader/Assets/_Project/PlayerDevice/Tests/EditMode/DeviceGroupMetricsCalculatorEditModeTests.cs
git commit -m "feat(device): add angular-space group metrics calculator"
```

### Task 3: Device Controller + Attachment Install/Uninstall Plumbing

**Files:**
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceController.cs`
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/World/DeviceAttachmentCatalog.cs`
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs`
- Test: `Reloader/Assets/_Project/PlayerDevice/Tests/EditMode/PlayerDeviceAttachmentInstallEditModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void TryUninstallAttachment_ReturnsItemToBeltFirst_BackpackFallback()
{
    // setup inventory runtime with available belt slot then full belt scenario
    // assert uninstall returns item to belt when possible
    // assert backpack used only when belt path unavailable
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter PlayerDeviceAttachmentInstallEditModeTests`  
Expected: FAIL due to missing controller plumbing.

**Step 3: Write minimal implementation**

- Add `PlayerDeviceController` commands:
  - `TryInstallSelectedAttachmentFromInventory()`
  - `TryUninstallAttachment(attachmentType)`
- Map attachment items to device attachment types through catalog.
- Install path consumes selected item from inventory.
- Uninstall path returns item with `belt-first` then backpack fallback.

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-editmode-filter PlayerDeviceAttachmentInstallEditModeTests`  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/PlayerDevice/Scripts/World/*.cs Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs Reloader/Assets/_Project/PlayerDevice/Tests/EditMode/PlayerDeviceAttachmentInstallEditModeTests.cs
git commit -m "feat(device): add attachment install/uninstall inventory plumbing"
```

### Task 4: Target Binding Flow + Confirmation Message

**Files:**
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceTargetSelectionController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/World/DummyTargetDamageable.cs`
- Create: `Reloader/Assets/_Project/Weapons/Scripts/World/DummyTargetRangeMetrics.cs`
- Test: `Reloader/Assets/_Project/PlayerDevice/Tests/PlayMode/PlayerDeviceTargetSelectionPlayModeTests.cs`

**Step 1: Write the failing test**

```csharp
[UnityTest]
public IEnumerator BeginTargetSelection_BindsTargetOnClick_AndPublishesConfirmation()
{
    // begin selection from device controller
    // simulate click raycast to target implementing IRangeTargetMetrics
    // assert selected target id set and confirmation hint raised
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter PlayerDeviceTargetSelectionPlayModeTests`  
Expected: FAIL due to missing selection controller / metrics implementation.

**Step 3: Write minimal implementation**

- Implement pending target selection mode:
  - command enters mode,
  - closes TAB via UI state event,
  - next click raycast binds valid target.
- Publish transient confirmation (`Target selected`) through existing interaction hint channel.
- Add `DummyTargetRangeMetrics` implementing `IRangeTargetMetrics` with authoritative distance.

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter PlayerDeviceTargetSelectionPlayModeTests`  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceTargetSelectionController.cs Reloader/Assets/_Project/Weapons/Scripts/World/DummyTargetRangeMetrics.cs Reloader/Assets/_Project/Weapons/Scripts/World/DummyTargetDamageable.cs Reloader/Assets/_Project/PlayerDevice/Tests/PlayMode/PlayerDeviceTargetSelectionPlayModeTests.cs
git commit -m "feat(device): add choose-target click binding flow"
```

### Task 5: Device TAB Section + UI Intents

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryUiState.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/TabInventoryController.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryDeviceSectionPlayModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void DeviceTab_SelectsDeviceSection_AndRaisesChooseTargetIntent()
{
    // initialize tab view binder + controller
    // click device tab + choose target button
    // assert controller forwards command to player device controller
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter TabInventoryDeviceSectionPlayModeTests`  
Expected: FAIL due to missing device section and intents.

**Step 3: Write minimal implementation**

- Add new `Device` tab and section to UXML/USS.
- Add view binder intent routing for:
  - choose target,
  - save group,
  - clear group,
  - install/uninstall hooks (plumbing level).
- Keep controller thin and delegate all behavior to device controller commands.

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter TabInventoryDeviceSectionPlayModeTests`  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml Reloader/Assets/_Project/UI/Toolkit/USS/TabInventory.uss Reloader/Assets/_Project/UI/Scripts/Toolkit/TabInventory/*.cs Reloader/Assets/_Project/UI/Scripts/Toolkit/Runtime/UiToolkitScreenRuntimeBridge.cs Reloader/Assets/_Project/UI/Tests/PlayMode/TabInventoryDeviceSectionPlayModeTests.cs
git commit -m "feat(ui): add player device tab section and intent routing"
```

### Task 6: Impact Ingestion + Save/Clear Group + Marker Clearing

**Files:**
- Modify: `Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/World/DummyTargetDamageable.cs`
- Create: `Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/IDeviceTargetMarkerClearable.cs`
- Test: `Reloader/Assets/_Project/PlayerDevice/Tests/PlayMode/PlayerDeviceGroupSessionPlayModeTests.cs`

**Step 1: Write the failing test**

```csharp
[UnityTest]
public IEnumerator ClearCurrentGroup_ResetsMetrics_AndClearsTargetMarkers()
{
    // bind target, feed impact samples, verify count > 0
    // invoke clear
    // assert active session reset and target marker root emptied
}
```

**Step 2: Run test to verify it fails**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter PlayerDeviceGroupSessionPlayModeTests`  
Expected: FAIL due to missing group clear/marker clear contract.

**Step 3: Write minimal implementation**

- Subscribe device controller to weapon/impact event path.
- Append samples only when selected target matches impact target.
- Recompute metrics on each shot.
- Implement `SaveCurrentGroup` and `ClearCurrentGroup`.
- `ClearCurrentGroup` invokes marker-clearing contract on selected target.
- Add marker clear implementation to dummy target (`Destroy` marker children under markers root).

**Step 4: Run test to verify it passes**

Run: `./.venv/bin/python -m ivan --unity-playmode-filter PlayerDeviceGroupSessionPlayModeTests`  
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/PlayerDevice/Scripts/World/PlayerDeviceController.cs Reloader/Assets/_Project/PlayerDevice/Scripts/Runtime/IDeviceTargetMarkerClearable.cs Reloader/Assets/_Project/Weapons/Scripts/World/DummyTargetDamageable.cs Reloader/Assets/_Project/PlayerDevice/Tests/PlayMode/PlayerDeviceGroupSessionPlayModeTests.cs
git commit -m "feat(device): add group session tracking save/clear and marker reset"
```

### Task 7: Final Verification Sweep

**Files:**
- No functional code changes expected.

**Step 1: Run focused EditMode tests**

Run:

```bash
./.venv/bin/python -m ivan --unity-editmode-filter "PlayerDeviceRuntimeStateEditModeTests|DeviceGroupMetricsCalculatorEditModeTests|PlayerDeviceAttachmentInstallEditModeTests"
```

Expected: PASS.

**Step 2: Run focused PlayMode tests**

Run:

```bash
./.venv/bin/python -m ivan --unity-playmode-filter "PlayerDeviceTargetSelectionPlayModeTests|TabInventoryDeviceSectionPlayModeTests|PlayerDeviceGroupSessionPlayModeTests|DummyTargetDamageablePlayModeTests"
```

Expected: PASS.

**Step 3: Manual smoke checklist**

- Open TAB and verify `Device` section exists.
- Verify notes visible at T0.
- Install rangefinder attachment from inventory item.
- Choose target from device UI and confirm transient `Target selected` message.
- Fire several shots on selected target and verify count/spread/MOA update.
- Save group then clear group and verify marker cleanup.

**Step 4: Commit any verification-only adjustments**

```bash
git add -A
git commit -m "test(device): finalize player device hub verification" || true
```

---

## Execution Log Update (2026-03-01)

### Implemented Across Waves

- Wave 1 delivered runtime contracts, angular-space metrics engine, and TAB `Device` UI skeleton.
- Wave 2 delivered attachment install/uninstall plumbing, target-selection controller + target metrics component, and TAB controller/bridge wiring.
- Wave 3 delivered impact-ingestion plumbing, group clear marker-clearing contract, and target marker clearing implementation.

### Integration Adjustments

- Added `Reloader.PlayerDevice.asmdef` and `.meta` coverage for new PlayerDevice files/folders.
- Broke asmdef cycle by keeping dependency direction as `Reloader.Weapons -> Reloader.PlayerDevice` (not vice versa).
- Added `Unity.InputSystem` reference to `Reloader.PlayerDevice.asmdef` for `PlayerDeviceTargetSelectionController`.
- Refactored device impact ingestion API to avoid direct `ProjectileImpactPayload` dependency in PlayerDevice assembly.

### Verification Reality

- Required `ivan` commands could not be executed in this environment because `./.venv/bin/python` was missing.
- Unity MCP PlayMode sweep executed and revealed broad cross-domain failures; PlayerDevice compile blockers from cyclic asmdef and `InputSystem` were fixed during this run.
- Latest observed user-reported runtime blocker:
  - `can you do all that? I tried to play test but buttons under \`Device\` tab are not clickable`

### Current Known Issues / Blockers

1. Device tab buttons can appear non-clickable in runtime despite UI skeleton being present.
2. Full PlayMode suite currently has many non-PlayerDevice failures; do not use full-suite red status as sole acceptance signal for this feature.
3. Focused feature verification should be rerun once local runner path (`.venv/bin/python -m ivan`) is restored.
