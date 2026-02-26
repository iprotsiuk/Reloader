# Belt Inventory (5 Slots) Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement an event-driven player inventory with 5 belt slots (`1..5`), pickup via `E`, slot-based selection, and save/load persistence for future TAB/backpack expansion.

**Architecture:** Introduce a runtime inventory core (`PlayerInventoryRuntime`) and a thin input/controller bridge (`PlayerInventoryController`). Extend runtime event ports/hub contracts (`IInventoryEvents` via `IGameEventsRuntimeHub`) to publish inventory events and evolve `InventoryModule` payload shape with backward-compatible deserialization. Keep runtime state independent from TAB UI so future menu tabs can consume the same model.

**Tech Stack:** Unity 6 C#, Unity Input System, NUnit (EditMode/PlayMode), existing `SaveCoordinator` + `InventoryModule` JSON pipeline.

---

Implementation principles for every task:
- Use `@test-driven-development` for each behavior change.
- Use `@verification-before-completion` before claiming each task done.
- Keep commits small and focused.

### Task 1: Add inventory event contracts in Core

**Files:**
- Modify (historical/retired path): `Reloader/Assets/_Project/Core/Scripts/Events/GameEvents.cs`
- Runtime contract target: `IGameEventsRuntimeHub` + `IInventoryEvents` under `Core/Scripts/Events` runtime contracts.
- Create: `Reloader/Assets/_Project/Core/Scripts/Events/InventoryEventsTypes.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs`

**Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using Reloader.Core.Events;

namespace Reloader.Core.Tests.EditMode
{
    public class InventoryEventContractsTests
    {
        [Test]
        public void InventoryEvents_AreRaised_WithExpectedPayloads()
        {
            string storedItemId = null;
            InventoryArea storedArea = default;
            int storedIndex = -1;

            runtimeEvents.Inventory.OnItemStored += (itemId, area, index) =>
            {
                storedItemId = itemId;
                storedArea = area;
                storedIndex = index;
            };

            runtimeEvents.Inventory.RaiseItemStored("item-1", InventoryArea.Belt, 0);

            Assert.That(storedItemId, Is.EqualTo("item-1"));
            Assert.That(storedArea, Is.EqualTo(InventoryArea.Belt));
            Assert.That(storedIndex, Is.EqualTo(0));
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run:
```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform editmode -testResults "$(pwd)/.tmp/editmode-task1.xml" -testFilter "Reloader.Core.Tests.EditMode.InventoryEventContractsTests" -quit
```
Expected: FAIL with missing `OnItemStored`/`RaiseItemStored` and missing inventory enums.

**Step 3: Write minimal implementation**

```csharp
// InventoryEventsTypes.cs
namespace Reloader.Core.Events
{
    public enum InventoryArea { Belt, Backpack }
    public enum PickupRejectReason { NoSpace, InvalidItem }
}

// Runtime inventory event contract additions (IInventoryEvents / runtime hub)
event Action<string> OnItemPickupRequested;
event Action<string, InventoryArea, int> OnItemStored;
event Action<string, PickupRejectReason> OnItemPickupRejected;
event Action<int> OnBeltSelectionChanged;
event Action OnInventoryChanged;

void RaiseItemPickupRequested(string itemId);
void RaiseItemStored(string itemId, InventoryArea area, int index);
void RaiseItemPickupRejected(string itemId, PickupRejectReason reason);
void RaiseBeltSelectionChanged(int selectedBeltIndex);
void RaiseInventoryChanged();
```

**Step 4: Run test to verify it passes**

Run same command as Step 2.
Expected: PASS for `InventoryEventContractsTests`.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Events/*Runtime* \
  Reloader/Assets/_Project/Core/Scripts/Events/InventoryEventsTypes.cs \
  Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs
git commit -m "feat: add inventory event contracts"
```

### Task 2: Implement `PlayerInventoryRuntime` belt/backpack state rules

**Files:**
- Create: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryRuntime.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/Reloader.Inventory.asmdef` (if missing)
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerInventoryRuntimeTests.cs`

**Step 1: Write failing tests**

```csharp
[Test]
public void TryStoreItem_FillsFirstEmptyBeltSlot() { /* assert belt[0] contains id */ }

[Test]
public void TryStoreItem_WhenNoSpaceAndBackpackLocked_ReturnsNoSpace() { /* assert reject */ }

[Test]
public void SelectBeltSlot_AllowsSelectingEmptySlot() { /* assert selected index updates */ }

[Test]
public void SelectBeltSlot_SameIndex_IsNoOp() { /* assert no duplicate event/state change */ }
```

**Step 2: Run tests to verify they fail**

Run:
```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform editmode -testResults "$(pwd)/.tmp/editmode-task2.xml" -testFilter "Reloader.Core.Tests.EditMode.PlayerInventoryRuntimeTests" -quit
```
Expected: FAIL because `PlayerInventoryRuntime` does not exist.

**Step 3: Write minimal implementation**

```csharp
public sealed class PlayerInventoryRuntime
{
    public const int BeltSlotCount = 5;
    public string[] BeltSlotItemIds { get; } = new string[BeltSlotCount];
    public List<string> BackpackItemIds { get; } = new();
    public int BackpackCapacity { get; private set; }
    public int SelectedBeltIndex { get; private set; } = -1;

    public bool TryStoreItem(string itemId, out InventoryArea area, out int index, out PickupRejectReason rejectReason) { ... }
    public void SelectBeltSlot(int index) { ... }
}
```

**Step 4: Run tests to verify they pass**

Run same command as Step 2.
Expected: PASS for `PlayerInventoryRuntimeTests`.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryRuntime.cs \
  Reloader/Assets/_Project/Inventory/Scripts/Reloader.Inventory.asmdef \
  Reloader/Assets/_Project/Core/Tests/EditMode/PlayerInventoryRuntimeTests.cs
git commit -m "feat: add player inventory runtime model"
```

### Task 3: Bridge input and pickup flow with controller + events

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs`
- Create: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs`

**Step 1: Write failing tests**

```csharp
[Test]
public void InputReader_ConsumePickupPressed_ReturnsTrueOncePerPress() { ... }

[Test]
public void InventoryController_OnBeltKeyPress_UpdatesSelectedSlot() { ... }

[Test]
public void InventoryController_OnPickup_RequestsItemStorage() { ... }
```

**Step 2: Run tests to verify they fail**

Run:
```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform playmode -testResults "$(pwd)/.tmp/playmode-task3.xml" -testFilter "Reloader.Player.Tests.PlayMode.PlayerInventoryControllerPlayModeTests" -quit
```
Expected: FAIL due to missing input consume APIs/controller.

**Step 3: Write minimal implementation**

```csharp
// IPlayerInputSource additions
bool ConsumePickupPressed();
int ConsumeBeltSelectPressed();

// PlayerInputReader additions
// Track Pickup action and belt actions 1..5; return one queued slot index or -1.

// PlayerInventoryController
// On update: consume belt select -> runtime.SelectBeltSlot(index)
// consume pickup -> resolve looked-at world item id -> runtimeEvents.Inventory.RaiseItemPickupRequested(itemId)
// subscribe to pickup-request and call runtime.TryStoreItem, then RaiseItemStored/RaiseItemPickupRejected + RaiseInventoryChanged
```

**Step 4: Run tests to verify they pass**

Run same command as Step 2.
Expected: PASS for `PlayerInventoryControllerPlayModeTests`.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs \
  Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs \
  Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs \
  Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs
git commit -m "feat: wire belt selection and pickup flow"
```

### Task 4: Persist new inventory fields in save module

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/InventoryModule.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/SaveSkeletonTddTests.cs`
- Create: `Reloader/Assets/_Project/Core/Tests/EditMode/InventoryModuleCompatibilityTests.cs`

**Step 1: Write failing tests**

```csharp
[Test]
public void InventoryModule_RoundTrip_PreservesBeltBackpackCapacityAndSelection() { ... }

[Test]
public void InventoryModule_Restore_LegacyPayloadWithOnlyCarriedItemIds_DefaultsNewFields() { ... }
```

**Step 2: Run tests to verify they fail**

Run:
```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform editmode -testResults "$(pwd)/.tmp/editmode-task4.xml" -testFilter "Reloader.Core.Tests.EditMode.InventoryModuleCompatibilityTests" -quit
```
Expected: FAIL because payload does not include new fields.

**Step 3: Write minimal implementation**

```csharp
// InventoryPayload additions
public List<string> BeltSlotItemIds { get; set; } = new();
public List<string> BackpackItemIds { get; set; } = new();
public int BackpackCapacity { get; set; }
public int SelectedBeltIndex { get; set; }

// Module state additions + backward-compatible restore defaults.
```

**Step 4: Run tests to verify they pass**

Run same command as Step 2.
Expected: PASS for compatibility tests and existing save tests.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Save/Modules/InventoryModule.cs \
  Reloader/Assets/_Project/Core/Tests/EditMode/SaveSkeletonTddTests.cs \
  Reloader/Assets/_Project/Core/Tests/EditMode/InventoryModuleCompatibilityTests.cs
git commit -m "feat: persist belt inventory state in save module"
```

### Task 5: Project wiring, docs, and full verification

**Files:**
- Modify: `docs/design/save-and-progression.md`
- Modify: `docs/design/inventory-and-economy.md`
- Modify: `docs/design/core-architecture.md` (if event contract list is updated)
- Optional: `docs/design/save-contract-quick-reference.md`

**Step 1: Update docs to match implemented contracts**

- Document new inventory events.
- Document inventory module payload fields.
- Document belt selection semantics.

**Step 2: Run docs/context verifier**

Run:
```bash
./scripts/verify-docs-and-context.sh
```
Expected: `SUCCESS` or zero exit code.

**Step 3: Run full tests used in this feature**

Run:
```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform editmode -testResults "$(pwd)/.tmp/editmode-final.xml" -quit

UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity" \
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform playmode -testResults "$(pwd)/.tmp/playmode-final.xml" -quit
```
Expected: PASS (or documented known failures not introduced by this work).

**Step 4: Commit final wiring/docs**

```bash
git add docs/design/inventory-and-economy.md docs/design/save-and-progression.md docs/design/core-architecture.md docs/design/save-contract-quick-reference.md
git commit -m "docs: align inventory and save contracts with belt system"
```

**Step 5: Prepare review request**

Run:
```bash
git status --short
git log --oneline -n 8
```
Expected: clean working tree and clear, scoped commits.

## Notes for Execution
- If Unity editor path differs, adjust `UNITY_EDITOR` before running batch tests.
- Keep runtime independent from TAB menu implementation.
- Do not add equip/unequip state; selection is slot-index only.
