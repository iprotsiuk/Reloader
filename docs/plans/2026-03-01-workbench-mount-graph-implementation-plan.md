# Workbench Mount Graph Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a data-driven workbench mount system with nested slots, strict compatibility rules, setup/operate UI modes, operation gating, and save/load persistence.

**Architecture:** Implement a layered mount-graph core (`WorkbenchDefinition`, `MountableItemDefinition`, compatibility evaluator, runtime graph, loadout controller) first, then wire operation gating and UI on top. Persist mounted graph state via dedicated save module payload and migration-safe schema handling.

**Tech Stack:** Unity 6.x C#, ScriptableObject content definitions, existing Reloading runtime/controllers, UI Toolkit, NUnit EditMode/PlayMode tests, Unity MCP test runner.

---

### Task 1: Add Core Workbench Mount Contracts (Data-Driven Definitions)

**Files:**
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/WorkbenchDefinition.cs`
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/MountSlotDefinition.cs`
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/MountableItemDefinition.cs`
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/CompatibilityRuleSet.cs`
- Test: `Reloader/Assets/_Project/Reloading/Tests/EditMode/WorkbenchMountDefinitionsEditModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void MountSlotDefinition_RequiresAllTagsAndRejectsForbiddenTags()
{
    var slot = new MountSlotDefinition("press-slot", requiredTags: new[] { "press" }, forbiddenTags: new[] { "tool.scale" });
    var item = ScriptableObject.CreateInstance<MountableItemDefinition>();
    item.SetValuesForTests("press.single", new[] { "press" });

    Assert.That(slot.CanAccept(item), Is.True);
}
```

**Step 2: Run test to verify it fails**

Run via Unity MCP: Play/Edit `run_tests` with filter `WorkbenchMountDefinitionsEditModeTests`.
Expected: FAIL due to missing mount definition contracts.

**Step 3: Write minimal implementation**

- Implement definition models with deterministic serialization fields.
- Add minimal evaluation helpers needed by tests.

**Step 4: Run test to verify it passes**

Run via Unity MCP: `run_tests` filter `WorkbenchMountDefinitionsEditModeTests`.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Reloading/Scripts/Runtime/WorkbenchDefinition.cs Reloader/Assets/_Project/Reloading/Scripts/Runtime/MountSlotDefinition.cs Reloader/Assets/_Project/Reloading/Scripts/Runtime/MountableItemDefinition.cs Reloader/Assets/_Project/Reloading/Scripts/Runtime/CompatibilityRuleSet.cs Reloader/Assets/_Project/Reloading/Tests/EditMode/WorkbenchMountDefinitionsEditModeTests.cs
git commit -m "feat(reloading): add workbench mount definition contracts"
```

### Task 2: Implement Runtime Mount Graph State (Nested Slots)

**Files:**
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/WorkbenchRuntimeState.cs`
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/MountNode.cs`
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/MountSlotState.cs`
- Test: `Reloader/Assets/_Project/Reloading/Tests/EditMode/WorkbenchRuntimeStateEditModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void InstallItem_WithChildSlots_InstantiatesNestedSlotStates()
{
    // setup bench with one top-level slot and a press item exposing child slots
    // install press into top-level slot
    // assert child slot states are created in runtime graph
}
```

**Step 2: Run test to verify it fails**

Run via Unity MCP: `run_tests` filter `WorkbenchRuntimeStateEditModeTests`.
Expected: FAIL due to missing runtime graph model.

**Step 3: Write minimal implementation**

- Add mount graph storage keyed by bench slot ids + node ids.
- Instantiate child slots when mounting items with nested definitions.

**Step 4: Run test to verify it passes**

Run via Unity MCP: `run_tests` filter `WorkbenchRuntimeStateEditModeTests`.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Reloading/Scripts/Runtime/WorkbenchRuntimeState.cs Reloader/Assets/_Project/Reloading/Scripts/Runtime/MountNode.cs Reloader/Assets/_Project/Reloading/Scripts/Runtime/MountSlotState.cs Reloader/Assets/_Project/Reloading/Tests/EditMode/WorkbenchRuntimeStateEditModeTests.cs
git commit -m "feat(reloading): add nested workbench mount runtime graph"
```

### Task 3: Add Strict Compatibility Evaluator + Failure Diagnostics

**Files:**
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/WorkbenchCompatibilityEvaluator.cs`
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/WorkbenchCompatibilityResult.cs`
- Test: `Reloader/Assets/_Project/Reloading/Tests/EditMode/WorkbenchCompatibilityEvaluatorEditModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void Evaluate_ReturnsMissingRequiredTagFailure()
{
    // slot requires shellholder-coax
    // candidate item provides shellholder-classic
    // expect invalid + explicit missing/forbidden diagnostics
}
```

**Step 2: Run test to verify it fails**

Run via Unity MCP: `run_tests` filter `WorkbenchCompatibilityEvaluatorEditModeTests`.
Expected: FAIL due to missing evaluator/result contract.

**Step 3: Write minimal implementation**

- Evaluate `requiredTags`, `forbiddenTags`, and optional profile rule callbacks.
- Return structured diagnostic payload for UI and logs.

**Step 4: Run test to verify it passes**

Run via Unity MCP: `run_tests` filter `WorkbenchCompatibilityEvaluatorEditModeTests`.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Reloading/Scripts/Runtime/WorkbenchCompatibilityEvaluator.cs Reloader/Assets/_Project/Reloading/Scripts/Runtime/WorkbenchCompatibilityResult.cs Reloader/Assets/_Project/Reloading/Tests/EditMode/WorkbenchCompatibilityEvaluatorEditModeTests.cs
git commit -m "feat(reloading): add strict mount compatibility evaluator"
```

### Task 4: Implement Workbench Loadout Controller (Install/Uninstall API)

**Files:**
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/WorkbenchLoadoutController.cs`
- Modify: `Reloader/Assets/_Project/Reloading/Scripts/World/ReloadingBenchTarget.cs`
- Test: `Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchLoadoutControllerPlayModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void InstallRejectsIncompatibleItem_WithDiagnosticReason()
{
    // attempt incompatible mount in slot
    // assert install false + expected diagnostic key
}
```

**Step 2: Run test to verify it fails**

Run via Unity MCP: `run_tests` filter `WorkbenchLoadoutControllerPlayModeTests`.
Expected: FAIL due to missing loadout API.

**Step 3: Write minimal implementation**

- Add install/uninstall commands on loadout controller.
- Route compatibility checks through evaluator.
- Expose read-only graph snapshot for UI.

**Step 4: Run test to verify it passes**

Run via Unity MCP: `run_tests` filter `WorkbenchLoadoutControllerPlayModeTests`.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Reloading/Scripts/Runtime/WorkbenchLoadoutController.cs Reloader/Assets/_Project/Reloading/Scripts/World/ReloadingBenchTarget.cs Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchLoadoutControllerPlayModeTests.cs
git commit -m "feat(reloading): add workbench loadout install uninstall controller"
```

### Task 5: Add Reloading Operation Gate Based on Mounted Graph

**Files:**
- Create: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/ReloadingOperationGate.cs`
- Modify: `Reloader/Assets/_Project/Reloading/Scripts/Runtime/ReloadingFlowController.cs`
- Test: `Reloader/Assets/_Project/Reloading/Tests/EditMode/ReloadingOperationGateEditModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void SeatBullet_Disabled_WhenRequiredPressOrDieMissing()
{
    // mount graph missing seat die capability
    // assert operation gate returns disabled + reason
}
```

**Step 2: Run test to verify it fails**

Run via Unity MCP: `run_tests` filter `ReloadingOperationGateEditModeTests`.
Expected: FAIL due to missing operation gate.

**Step 3: Write minimal implementation**

- Define capability requirements per operation.
- Evaluate gate state from current loadout graph.
- Return per-operation reason text keys.

**Step 4: Run test to verify it passes**

Run via Unity MCP: `run_tests` filter `ReloadingOperationGateEditModeTests`.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Reloading/Scripts/Runtime/ReloadingOperationGate.cs Reloader/Assets/_Project/Reloading/Scripts/Runtime/ReloadingFlowController.cs Reloader/Assets/_Project/Reloading/Tests/EditMode/ReloadingOperationGateEditModeTests.cs
git commit -m "feat(reloading): gate operations from mounted workbench graph"
```

### Task 6: Persist Workbench Mounted Graph in Save/Load

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/WorkbenchLoadoutModule.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SchemaVersion.cs`
- Create: `Reloader/Assets/_Project/Core/Scripts/Save/Migrations/SchemaV4ToV5AddWorkbenchLoadoutMigration.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/WorkbenchLoadoutModuleTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void SaveLoad_RestoresNestedWorkbenchMountGraph()
{
    // arrange runtime state with mounted press + nested die/shellholder
    // save, clear runtime, load
    // assert graph equality
}
```

**Step 2: Run test to verify it fails**

Run via Unity MCP: `run_tests` filter `WorkbenchLoadoutModuleTests`.
Expected: FAIL due to missing save module + schema wiring.

**Step 3: Write minimal implementation**

- Add save DTO payload for recursive mount graph.
- Register module in save bootstrap.
- Add schema migration for absent legacy payloads.

**Step 4: Run test to verify it passes**

Run via Unity MCP: `run_tests` filter `WorkbenchLoadoutModuleTests`.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Save/Modules/WorkbenchLoadoutModule.cs Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs Reloader/Assets/_Project/Core/Scripts/Save/SchemaVersion.cs Reloader/Assets/_Project/Core/Scripts/Save/Migrations/SchemaV4ToV5AddWorkbenchLoadoutMigration.cs Reloader/Assets/_Project/Core/Tests/EditMode/WorkbenchLoadoutModuleTests.cs
git commit -m "feat(save): persist nested workbench loadout graph"
```

### Task 7: Expand Workbench UI to Setup + Operate Modes

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Toolkit/UXML/ReloadingWorkbench.uxml`
- Modify: `Reloader/Assets/_Project/UI/Toolkit/USS/ReloadingWorkbench.uss`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Reloading/ReloadingWorkbenchUiState.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Reloading/ReloadingWorkbenchViewBinder.cs`
- Modify: `Reloader/Assets/_Project/UI/Scripts/Toolkit/Reloading/ReloadingWorkbenchController.cs`
- Test: `Reloader/Assets/_Project/UI/Tests/PlayMode/ReloadingWorkbenchUiToolkitPlayModeTests.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void SetupMode_ShowsSlotTreeAndInstallDiagnostics()
{
    // open workbench UI
    // verify setup mode renders mount slots and reason text on invalid install attempt
}
```

**Step 2: Run test to verify it fails**

Run via Unity MCP: `run_tests` filter `ReloadingWorkbenchUiToolkitPlayModeTests`.
Expected: FAIL due to missing setup/operate UI state model.

**Step 3: Write minimal implementation**

- Add mode toggle and render state for mount graph tree.
- Bind install/uninstall intents.
- Surface gate reason text in operate mode.

**Step 4: Run test to verify it passes**

Run via Unity MCP: `run_tests` filter `ReloadingWorkbenchUiToolkitPlayModeTests`.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/UI/Toolkit/UXML/ReloadingWorkbench.uxml Reloader/Assets/_Project/UI/Toolkit/USS/ReloadingWorkbench.uss Reloader/Assets/_Project/UI/Scripts/Toolkit/Reloading/ReloadingWorkbenchUiState.cs Reloader/Assets/_Project/UI/Scripts/Toolkit/Reloading/ReloadingWorkbenchViewBinder.cs Reloader/Assets/_Project/UI/Scripts/Toolkit/Reloading/ReloadingWorkbenchController.cs Reloader/Assets/_Project/UI/Tests/PlayMode/ReloadingWorkbenchUiToolkitPlayModeTests.cs
git commit -m "feat(ui): add setup operate modes for reloading workbench"
```

### Task 8: End-to-End Bench Setup -> Operation -> Save/Load Acceptance

**Files:**
- Create: `Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchMountFlowAcceptancePlayModeTests.cs`

**Step 1: Write the failing test**

```csharp
[UnityTest]
public IEnumerator WorkbenchFlow_MountPressAndTools_EnablesOperation_AndRestoresAfterSaveLoad()
{
    // mount items in setup mode
    // assert operation becomes enabled
    // save/load cycle
    // assert mounted graph and operation gate preserved
}
```

**Step 2: Run test to verify it fails**

Run via Unity MCP: `run_tests` filter `WorkbenchMountFlowAcceptancePlayModeTests`.
Expected: FAIL until full wiring is complete.

**Step 3: Write minimal implementation glue**

- Add missing integration glue between loadout controller, operation gate, and save module if test still fails.

**Step 4: Run test to verify it passes**

Run via Unity MCP: `run_tests` filter `WorkbenchMountFlowAcceptancePlayModeTests`.
Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Reloading/Tests/PlayMode/WorkbenchMountFlowAcceptancePlayModeTests.cs
git commit -m "test(reloading): add end-to-end workbench mount flow acceptance"
```

### Task 9: Final Verification Evidence + Cleanup Workflow

**Files:**
- Modify: `docs/design/reloading-system.md`
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`
- Modify: `docs/plans/progress/2026-03-01-workbench-mount-graph-progress.md`

**Step 1: Run focused verification suites**

Run via Unity MCP:
- `ReloadingWorkbenchUiToolkitPlayModeTests`
- `WorkbenchLoadoutControllerPlayModeTests`
- `WorkbenchMountFlowAcceptancePlayModeTests`
- `PlayerDeviceAttachmentInstallEditModeTests`

Expected: all pass.

**Step 2: Run broader regression suites and capture MCP cleanup state**

Run via Unity MCP:
- `ReloadingBenchInteractionPlayModeTests`
- `PlayerInventoryControllerPlayModeTests`
- `TabInventoryDeviceSectionPlayModeTests`

Expected: all pass when Unity test execution is unlocked.

If `run_tests` returns `tests_running`:
- Record the blocking MCP job id and timestamp in progress docs.
- Treat this as an infrastructure blocker (not feature regression) and stop launching additional test jobs until lock is cleared.
- Keep the latest blocker snapshot in docs (current snapshot: job `3a79c36cff4945a6bbe06bd535b78abb`).

**Step 3: Update docs evidence, integration status, and blocker notes**

- Record landed workbench mount-graph scope and verification evidence.
- Record integration status on `main` and PR closure state (`#15` closed).
- Keep milestone board and design docs synchronized.

**Step 4: Commit**

```bash
git add docs/design/reloading-system.md docs/design/v0.1-demo-status-and-milestones.md docs/plans/progress/2026-03-01-workbench-mount-graph-progress.md
git commit -m "docs: sync workbench mount graph implementation status"
```

---

## Notes for Execution

- Follow strict TDD for each task: fail -> implement minimum -> pass.
- Keep commits small and isolated per task.
- Do not bundle schema/save changes with unrelated UI refactors.
- Preserve existing behavior paths until corresponding acceptance tests pass.

Plan complete and saved to `docs/plans/2026-03-01-workbench-mount-graph-implementation-plan.md`.

Two execution options:

1. Subagent-Driven (this session) - dispatch fresh subagent per task with review between tasks.
2. Parallel Session (separate) - open new session and execute with `superpowers:executing-plans`.
