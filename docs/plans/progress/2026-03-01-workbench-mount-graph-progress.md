# 2026-03-01 Workbench Mount Graph Progress

## Save/Load Slice Completed
- Added `WorkbenchLoadoutModule` under core save modules to persist nested per-workbench mount slot graphs.
- Added schema migration `SchemaV4ToV5AddWorkbenchLoadoutMigration` to insert default `WorkbenchLoadout` block for legacy schema v4 saves.
- Wired `SaveBootstrapper` deterministic default pipeline registration order to include `WorkbenchLoadout` after `PlayerDevice`.
- Bumped default save schema version from `4` to `5` in `SaveBootstrapper.CreateDefaultCoordinator`.
- Updated `PlayerDeviceSaveModuleTests` schema assertion to `5` for bootstrap capture compatibility.

## Save Payload Contract Notes
- Module key: `WorkbenchLoadout`
- Module version: `1`
- Payload root: `workbenches[]`
- Nested graph shape:
  - `workbenchId`
  - `slotNodes[]`
  - recursive `childSlots[]` with `slotId` and optional `mountedItemId`
- Restore path normalizes malformed/null nodes and keeps payload JSON-first/migration-friendly.

## Tests
- Added `WorkbenchLoadoutModuleTests` covering:
  - bootstrap capture includes new module + schema v5,
  - schema v4 load path migrates missing module,
  - v4 -> v5 migration insert/preserve behavior,
  - nested graph roundtrip,
  - empty payload tolerance,
  - validation failure for invalid nested slot IDs.

## UI Setup/Operate Slice Completed
- Expanded reloading workbench UI Toolkit markup/styles with explicit mode split:
  - setup mode section for mounted slot summary + setup diagnostics,
  - operate mode section for operation list + execute + operation diagnostics.
- Extended `ReloadingWorkbenchUiState` to carry deterministic setup/operate render state:
  - explicit `ReloadingWorkbenchMode`,
  - per-operation enabled/disabled + diagnostic metadata,
  - dedicated setup/operate diagnostics text fields.
- Updated binder/controller wiring for explicit mode intents and deterministic rendering:
  - `reloading.mode.setup`,
  - `reloading.mode.operate`,
  - disabled operation styling and execute-button gating from selected operation state.
- Updated `ReloadingWorkbenchUiToolkitPlayModeTests` to cover setup mode rendering, operate-mode diagnostic rendering, and mode intent emission helpers.

## Acceptance Coverage Added
- Added `WorkbenchMountFlowAcceptancePlayModeTests` covering a minimal end-to-end mount flow with available runtime APIs:
  - incompatible candidate diagnostic capture (`WorkbenchCompatibilityEvaluator`) and install rejection,
  - compatible press install creating nested child slot availability,
  - child die install completing the minimal operate-readiness requirement contract used by acceptance helper assertions.

## Verification Status / Blocker
- Focused PlayMode verification is currently blocked in this workspace because Unity reports active in-progress test execution (`tests_running`) and compile state includes unrelated missing runtime classes from parallel worker-owned files (for example `WorkbenchLoadoutControllerPlayModeTests` references types not present yet).
- UI and acceptance test commands should be re-run after parallel runtime slice lands and Unity test queue is idle.
