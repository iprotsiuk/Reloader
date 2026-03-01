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
