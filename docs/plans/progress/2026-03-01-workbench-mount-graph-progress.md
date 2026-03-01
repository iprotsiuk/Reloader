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
- Focused MCP reruns passed on 2026-03-01 after `main` compile fix and MCP restart:
  - `d4576f61f3294550b43352a025bf5510`: `ReloadingWorkbenchUiToolkitPlayModeTests` -> passed `6/6`.
  - `57608a8565894da89feffb3b22c80cf8`: `WorkbenchLoadoutControllerPlayModeTests` -> passed `5/5`.
  - `1399ddc3ee8f4711b6ce72b76be0508b`: `WorkbenchMountFlowAcceptancePlayModeTests` -> passed `2/2`.
  - `5ddc30c992a94eb59c7c1d3c989717c4`: `PlayerDeviceAttachmentInstallEditModeTests` -> passed `5/5`.
- Broader regression reruns passed on 2026-03-01:
  - `ed8bab3ab8cb4c2a98d83588a4801b3c`: `ReloadingBenchInteractionPlayModeTests` -> passed `10/10`.
  - `ad8571d4ea3448ed96533fdd44c28a2b`: `PlayerInventoryControllerPlayModeTests` -> passed `23/23`.
  - `df185c3cd5e04786b8e4b6ec75005400`: `TabInventoryDeviceSectionPlayModeTests` -> passed `16/16`.
- Historical note: stale runner lock job `3a79c36cff4945a6bbe06bd535b78abb` is resolved after restart and compile fix.

## Notes

- Commits stay small and review-friendly to keep automated review cadence high.
- Scope authority delegated by user for modular/extensible implementation decisions.
- No active MCP blocker for focused/broader verification suites in this slice after restart on port `6402`.

## Follow-Up Progress (Vendor Routing + Reloading Access)

- Split vendor catalog data so storefronts now map to role-specific stock:
  - `AmmoStore_DefaultCatalog.asset` (ammo-only stock).
  - `WeaponStore_DefaultCatalog.asset` (weapon-only stock).
  - `ReloadingStore_DefaultCatalog.asset` reduced to reloading components/tools.
- Updated `EconomyController.prefab` vendor bindings so:
  - `vendor-ammo-store` -> ammo catalog.
  - `vendor-weapon-store` -> weapon catalog.
  - `vendor-reloading-store` -> reloading catalog.
- Updated `NpcVendorPrefabBuilder.WireEconomyInScene()` to preserve role-specific catalog mapping during future auto-wire runs.
- Added a scene-level reloading vendor near the player house/workbench area in `MainTown.unity`:
  - prefab instance named `ReloadingVendor_House`,
  - `_vendorId` override set to `vendor-reloading-store`,
  - positioned for quick reloading-loop access from house/workbench.
