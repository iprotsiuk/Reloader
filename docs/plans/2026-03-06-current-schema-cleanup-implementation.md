# Current Schema Cleanup Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove save/runtime compatibility baggage from the assassination-pivot branch so the repo targets one current schema and one typed runtime-event surface.

**Architecture:** Treat save data as current-schema-only and fail fast on incompatible envelopes instead of migrating them. In parallel, remove legacy runtime-event adapters that no longer have live callers and keep only the typed contracts that the pivot branch actually uses.

**Tech Stack:** Unity 6.3, C#, Newtonsoft.Json, existing `_Project` runtime/save modules, EditMode and PlayMode tests, `gh`, `bash scripts/run-unity-tests.sh`.

---

### Task 1: Enforce current-schema-only save pipeline

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveCoordinator.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveEnvelope.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/IO/SaveFileRepository.cs`
- Delete: `Reloader/Assets/_Project/Core/Scripts/Save/SaveFeatureFlags.cs`
- Delete: `Reloader/Assets/_Project/Core/Scripts/Save/ISaveMigration.cs`
- Delete: `Reloader/Assets/_Project/Core/Scripts/Save/MigrationRunner.cs`
- Delete: `Reloader/Assets/_Project/Core/Scripts/Save/Migrations/*`

**Step 1: Write the failing test**

Rewrite save tests to require:
- `CaptureEnvelope(buildVersion)` without feature flags
- schema mismatch throws
- missing required module blocks throw

**Step 2: Run test to verify it fails**

Run: `bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.SaveSkeletonTddTests|Reloader.Core.Tests.EditMode.WorldObjectStateSaveModuleTests|Reloader.Core.Tests.EditMode.ContainerStorageSaveModuleTests|Reloader.Core.Tests.EditMode.PlayerDeviceSaveModuleTests|Reloader.Core.Tests.EditMode.WorkbenchLoadoutModuleTests|Reloader.Reloading.Tests.EditMode.WorkbenchRuntimeSaveBridgeEditModeTests" tmp/save-cleanup-red.xml tmp/save-cleanup-red.log`

**Step 3: Write minimal implementation**

Remove the migration runner/feature flags from the coordinator and make load reject any schema that does not exactly match `currentSchemaVersion`.

**Step 4: Run test to verify it passes**

Run: `bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.SaveSkeletonTddTests|Reloader.Core.Tests.EditMode.WorldObjectStateSaveModuleTests|Reloader.Core.Tests.EditMode.ContainerStorageSaveModuleTests|Reloader.Core.Tests.EditMode.PlayerDeviceSaveModuleTests|Reloader.Core.Tests.EditMode.WorkbenchLoadoutModuleTests|Reloader.Reloading.Tests.EditMode.WorkbenchRuntimeSaveBridgeEditModeTests|Reloader.Core.Tests.EditMode.InventoryModuleStateTests|Reloader.Core.Tests.EditMode.WeaponSaveModuleTests" tmp/save-cleanup-edit.xml tmp/save-cleanup-edit.log`
Expected: PASS for the current-schema save contract and module suites.

**Step 5: Commit**

```bash
git add docs/plans/2026-03-06-current-schema-cleanup-implementation.md \
        Reloader/Assets/_Project/Core/Scripts/Save \
        Reloader/Assets/_Project/Core/Tests/EditMode \
        Reloader/Assets/_Project/Reloading/Tests/EditMode/WorkbenchRuntimeSaveBridgeEditModeTests.cs
git commit -m "refactor: remove legacy save migration pipeline"
```

### Task 2: Remove legacy runtime-event compatibility

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Events/GameEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Events/ShopEventsTypes.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IShopEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/GameEventsRuntimeBridgeTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs`

**Step 1: Audit live callers**

Confirm the branch still uses the compatibility shims before deleting them.

**Step 2: Remove unused compatibility**

Keep the typed runtime ports and drop legacy event aliases/translation layers if the callers are gone.

**Step 3: Verify affected tests**

Run: `bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.RuntimeEventHubBehaviorTests|Reloader.Core.Tests.EditMode.InventoryEventContractsTests" tmp/runtime-event-cleanup-edit.xml tmp/runtime-event-cleanup-edit.log`

### Task 3: Sweep docs/rules after contract changes

**Files:**
- Modify any docs/rules that still describe migration-based saves or legacy event compatibility as supported contracts.

**Step 1: Grep for stale language**

Search for `SaveFeatureFlags`, `MigrationRunner`, `ISaveMigration`, `GameEvents`, and `legacy`.

**Step 2: Update only stale contract references**

Keep repo instructions aligned with the new cleanup so future work does not reintroduce removed compatibility.

**Step 3: Final verification**

Run focused tests plus `git diff --check` before claiming completion.
