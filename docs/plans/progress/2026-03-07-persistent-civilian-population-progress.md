# Persistent Civilian Population Progress

## Status

- [x] Design direction approved
- [x] Implementation plan written
- [x] Runtime implementation started

## 2026-03-07 Checkpoint

- Added the first green runtime checkpoint for the data foundation:
  - registered `CivilianPopulation` as a required save module
  - bumped the runtime save schema from `v6` to `v7`
  - added persistent civilian + queued-replacement payload DTOs
  - added a minimal civilian appearance generator contract in `Reloader.NPCs`
- Verified the red-first tests passed after implementation:
  - `Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests`
  - `Reloader.NPCs.Tests.EditMode.CivilianAppearanceGeneratorTests`
- Follow-up still pending in this slice:
  - save-creation roster generation
  - MainTown spawn-from-records bridge
  - civilian retirement + replacement queue runtime wiring

## 2026-03-07 Checkpoint 2

- Added `CivilianPopulationRuntimeBridge` under `Reloader.NPCs.Runtime`.
- The bridge now handles the first runtime seam for this slice:
  - seeds the initial civilian roster when a fresh runtime/module pair is empty during save capture
  - restores the in-memory civilian roster and queued replacements from `CivilianPopulationModule` after load
- Added focused EditMode coverage in `CivilianPopulationRuntimeBridgeTests`.
- This checkpoint still stops short of scene spawning:
  - civilians are persisted and mirrored into runtime memory
  - `MainTown` does not yet instantiate civilians from that runtime roster

## 2026-03-07 Checkpoint 3

- Extended `CivilianPopulationRuntimeBridge` with retirement handling for persistent civilians.
- `TryRetireCivilian(...)` now:
  - marks the civilian dead
  - disables contract eligibility
  - records `retiredAtDay`
  - queues exactly one replacement-debt record using the retired civilian's spawn anchor
- Added focused EditMode coverage to lock:
  - correct dead/retired state after retirement
  - no duplicate replacement debt when retirement is reported twice

## 2026-03-07 Checkpoint 4

- Hardened the initial foundation after PR review:
  - `CivilianPopulationModule.ValidateModuleState()` now rejects civilians missing required appearance identifiers or `spawnAnchorId`
  - `CivilianAppearanceLibrary` now uses Unity-serializable backing fields so configured arrays persist through inspector/runtime serialization
  - `CivilianPopulationRuntimeBridge.PrepareForSave(...)` now preserves loaded module data by hydrating the runtime roster before capture when runtime state is still empty
- Added focused regression coverage for those review fixes in the existing save-module, appearance-generator, and runtime-bridge suites

## 2026-03-07 Checkpoint 5

- Clarified the approved model before the next implementation slice:
  - `MainTown` should own one map-wide persistent population roster
  - the roster should later be partitioned into stable pools like `townsfolk`, `quarry_workers`, `hobos`, and `cops`
  - each occupant should eventually live in a stable `populationSlotId` so replacements inherit the same world role
  - contract targets must later be selected only from living current occupants, never from dead civilians
  - vendors remain the only protected contract exclusion for the first targeting pass
  - if the Monday `08:00` refresh happens while `MainTown` is unloaded, replacements should already be in place on the next load rather than arriving late on-screen

## 2026-03-08 Checkpoint

- Bumped the runtime save schema from `v7` to `v8` after slot metadata became a required part of the civilian payload contract.
- Older schema-7 saves now fail at the envelope schema gate instead of reaching `CivilianPopulationModule.ValidateModuleState()` and aborting deeper in restore.

## Scope Notes

- New saves should generate a persistent `MainTown` civilian roster.
- That roster should evolve toward one scene-wide `MainTown` population with stable slots and pools rather than settlement-local ownership.
- Save/load must preserve the same generated civilians within one save.
- Vendors and other protected authored roles stay outside the generated civilian pool.
- Civilian death retires the civilian from the save instead of rerolling the entire town.
- Each retired civilian creates one owed replacement entry.
- Replacements should later arrive during the Monday `08:00` population refresh at the bus stop.

## Explicit Non-Goals For This Slice

- contract target selection from the population
- generated contract briefings or portrait capture
- professions, dialogue, voices, schedules, and wandering zones
- executing the Monday refresh beyond recording the replacement debt

## Next Slice After This One

Once the persistent population foundation is stable, the next slice should wire contracts to it:

- define a `MainTownPopulationDefinition` with stable pools and `populationSlotId`s
- fill map-wide `MainTown` slots with generated occupants
- pick random living civilians as contract targets
- derive contract descriptions from the selected civilian appearance snapshot
- preserve vendor/protected-role exclusions in target selection
- exclude dead civilians from target selection
- execute the Monday `08:00` replacement queue so vacancies eventually refill in-world
