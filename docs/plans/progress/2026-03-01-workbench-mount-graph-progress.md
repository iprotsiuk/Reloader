# Workbench Mount Graph Progress

**Date:** 2026-03-01
**Branch:** main
**Integration:** Integrated into `main` (post-review commits landed directly on `main`)
**PR:** https://github.com/iprotsiuk/Reloader/pull/15 (`#15`, closed on 2026-03-01)

## Completed

- Initialized implementation branch/worktree.
- Imported approved design + implementation plan docs.
- Opened PR early for iterative review cycle.
- Implemented core mount contracts:
  - `WorkbenchDefinition`, `MountSlotDefinition`, `MountableItemDefinition`, `CompatibilityRuleSet`.
- Implemented nested runtime graph:
  - `WorkbenchRuntimeState`, `MountNode`, `MountSlotState`.
- Implemented strict compatibility evaluation:
  - `WorkbenchCompatibilityEvaluator`, `WorkbenchCompatibilityResult`.
- Implemented runtime integration slice (tasks 4-5):
  - `WorkbenchLoadoutController` install/uninstall API + diagnostics.
  - `ReloadingOperationGate` capability-based operation gating.
  - `ReloadingFlowController` operation gate hook.
  - `ReloadingBenchTarget` loadout/runtime-state exposure.
- Added runtime save bridge slice:
  - `WorkbenchRuntimeSaveBridge` with `SetWorkbenchLoadoutModuleForRuntime`, `CaptureToModule`, `RestoreFromModule`.
  - recursive nested-slot capture/restore by `workbenchId`.
  - regression coverage in `WorkbenchRuntimeSaveBridgeEditModeTests`.
- Added save-runtime orchestration hooks:
  - `ISaveRuntimeBridge` contract + `SaveRuntimeBridgeRegistry`.
  - `SaveCoordinator` now invokes runtime bridge hooks before capture and after restore.
- Implemented save/load persistence slice:
  - `WorkbenchLoadoutModule` with recursive payload graph.
  - `SchemaV4ToV5AddWorkbenchLoadoutMigration`.
  - `SaveBootstrapper` registration + schema bump to v5.
- Expanded UI setup/operate slice:
  - `ReloadingWorkbench.uxml/.uss` for setup + operate sections.
  - `ReloadingWorkbenchUiState` mode-aware render state.
  - `ReloadingWorkbenchViewBinder` mode intents + diagnostics rendering.
  - `ReloadingWorkbenchController` mode switching + operation diagnostics.
- Added live bench-context UI slice:
  - `ReloadingWorkbenchUiSnapshot` + `ReloadingWorkbenchUiContextStore`.
  - `PlayerReloadingBenchController` now publishes/clears bench snapshots on lifecycle transitions.
  - `ReloadingWorkbenchController` now consumes live snapshot data with fallback to serialized defaults.
- Added acceptance coverage:
  - `WorkbenchMountFlowAcceptancePlayModeTests` for mount flow/gating/save behavior surface.
- Aligned existing core save tests to v5 bootstrap schema:
  - `ContainerStorageSaveModuleTests`
  - `WorldObjectStateSaveModuleTests`
- Addressed PR review P1 on mount graph indexing:
  - child slots are now indexed by unique graph keys (`<ownerNodeId>/<slotId>`) to avoid collisions across parallel mounted branches.
  - added duplicate-child-slot regression coverage in `WorkbenchRuntimeStateEditModeTests`.

## Save Payload Contract Notes

- Module key: `WorkbenchLoadout`
- Module version: `1`
- Payload root: `workbenches[]`
- Nested graph shape:
  - `workbenchId`
  - `slotNodes[]`
  - recursive `childSlots[]` with `slotId` and optional `mountedItemId`
- Restore path normalizes malformed/null nodes and keeps payload JSON-first/migration-friendly.

## Tests Added

- `WorkbenchMountDefinitionsEditModeTests`
- `WorkbenchRuntimeStateEditModeTests`
- `WorkbenchCompatibilityEvaluatorEditModeTests`
- `WorkbenchLoadoutModuleTests`
- `WorkbenchLoadoutControllerPlayModeTests`
- `ReloadingOperationGateEditModeTests`
- `ReloadingWorkbenchUiToolkitPlayModeTests` (expanded)
- `WorkbenchMountFlowAcceptancePlayModeTests`
- `WorkbenchRuntimeSaveBridgeEditModeTests`
- `ReloadingBenchInteractionPlayModeTests` (snapshot lifecycle coverage)
- `UiToolkitScreenFlowPlayModeTests` (snapshot fallback/consumption coverage)

## Recent Review Fixes

- Resolved Codex P1 on nested save/load collisions:
  - `WorkbenchRuntimeSaveBridge` now persists graph-qualified slot IDs (`GraphSlotId`).
  - `WorkbenchRuntimeState` now builds deterministic/path-based graph keys for child slots (stable across restore), replacing GUID-based child key generation.
- Resolved Codex P1 on save transaction ordering:
  - `SaveCoordinator.Load` now validates restored module state before invoking `SaveRuntimeBridgeRegistry.FinalizeAfterLoad`.
  - runtime bridge side effects no longer run when restored state fails validation and is rolled back.
- Resolved Codex P1/P2 on runtime restore target handling:
  - `WorkbenchRuntimeSaveBridge.RestoreFromModule` now clears all resolved live benches before applying saved records, so benches omitted from payload are reset instead of left stale.
  - `WorkbenchRuntimeSaveBridge.ResolveBenchTargets` now re-discovers bench targets on each pass and merges new scene targets while pruning destroyed references.
  - Added regression coverage for empty/missing bench records and dynamic bench discovery during repeated capture.

## Final Evidence + Active Blockers

- Workbench mount graph implementation is integrated on `main`.
- PR `#15` is closed after review cycle completion.
- Integration evidence on `main` includes:
  - `b5bd24d` (`test(reloading): add workbench mount flow acceptance coverage`)
  - `3ab73d4` (`feat(reloading): gate operations from mounted workbench graph`)
  - `fd97e00` (`fix(save): finalize runtime bridges only after successful validation`)
  - `9867e64` (`fix(reloading): clear omitted benches and rediscover dynamic targets`)
- Focused/broader MCP verification reruns remain blocked by Unity test-runner lock state.
- Baseline triage from latest MCP full-suite runs:
  - EditMode: 252 executed, failures include pre-existing schema expectation drift in active Unity checkout.
  - PlayMode: 263 executed, broad unrelated baseline failures across Economy/NPCs/Player/PlayerDevice/UI.

## Notes

- Commits stay small and review-friendly to keep automated review cadence high.
- Scope authority delegated by user for modular/extensible implementation decisions.
- Latest MCP blocker: editor state reports stale running PlayMode job `3a79c36cff4945a6bbe06bd535b78abb` and rejects new test jobs as `tests_running`.
