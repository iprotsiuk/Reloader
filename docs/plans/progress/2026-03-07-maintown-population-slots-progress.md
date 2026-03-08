# MainTown Population Slots Progress

## Status

- [x] Design direction approved
- [x] Implementation plan written
- [x] Runtime implementation started

## 2026-03-07 Checkpoint

- Approved the next structural slice after the persistent population foundation:
  - `MainTown` owns one map-wide population roster
  - the roster is partitioned into fixed pools like `townsfolk`, `quarry_workers`, `hobos`, and `cops`
  - each occupant should eventually live in a stable `populationSlotId`
  - the slot owns the durable world role while the occupant can change over time
  - replacements should later refill the same slot with a new civilian identity and appearance
- Locked future targeting guardrails:
  - contracts should later choose from the whole living `MainTown` population
  - vendors are the first protected exclusion
  - dead occupants must never be eligible for contract targeting
- Deferred from this slice:
  - contract target selection
  - Monday `08:00` replacement execution
  - professions, schedules, wandering zones, dialogue, and voices
  - committed STYLE appearance-part curation

## 2026-03-07 Checkpoint 2

- Added the first green slot-model runtime checkpoint:
  - introduced `MainTownPopulationDefinition`, pool definitions, and slot definitions under `Reloader.NPCs.Generation`
  - extended `CivilianPopulationRecord` with:
    - `populationSlotId`
    - `poolId`
    - `isProtectedFromContracts`
    - `areaTag`
  - taught `CivilianPopulationRuntimeBridge` to seed occupants from a `MainTownPopulationDefinition` when present instead of only using anonymous count-based spawning
  - preserved the new slot metadata through runtime/module cloning
- Added focused EditMode coverage for:
  - duplicate `populationSlotId` rejection in `MainTownPopulationDefinition`
  - protected vendor slot acceptance
  - one-occupant-per-slot assignment through the runtime bridge
- Updated the existing save-module fixture to include the new slot metadata so its validation still isolates the intended failure.

## Verification

- `Reloader.NPCs.Tests.EditMode.MainTownPopulationDefinitionTests`: `2/2` passed
- `Reloader.NPCs.Tests.EditMode.CivilianPopulationSlotAssignmentTests`: `1/1` passed
- `Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests`: `4/4` passed
- `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests`: `5/5` passed

## 2026-03-08 Checkpoint

- Authored the first MainTown scene infrastructure checkpoint:
  - added `MainTownPopulationRuntime` to `MainTown`
  - added four starter child anchors under that root for the first conservative pool placeholders
  - created `Assets/_Project/NPCs/Data/Population/MainTownPopulationDefinition.asset`
  - assigned starter pools: `townsfolk`, `quarry_workers`, `hobos`, `cops`
  - serialized a minimal non-empty `CivilianAppearanceLibrary` directly on the runtime bridge to keep this slice infrastructure-focused
  - hardened `MainTownPopulationDefinition.Validate()` so empty pool arrays fail fast instead of silently producing a zero-civilian roster
- Kept the deferred boundary explicit:
  - no committed STYLE appearance-part curation yet
  - no final civilian prefab assembly/appearance pipeline yet
  - no claim that the placeholder IDs map to final approved art content

## Verification

- `Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests`: `1/1` passed in `tmp/maintown-population-infra-play.xml`
- `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests tmp/maintown-population-infra-play.xml tmp/maintown-population-infra-play.log` exited `3` because Unity emitted an unrelated package GUID-conflict log during startup:
  - `Packages/com.coplaydev.unity-mcp/Editor/Clients/Configurators/QwenCodeConfigurator.cs`
  - `Packages/com.unity.ai.assistant/Modules/Unity.AI.Generators.IO/Srp/AssemblyInfo.cs`
  - the target test itself still recorded `Passed` in the NUnit XML result

## 2026-03-08 Checkpoint 2

- Removed `com.unity.ai.assistant` from the Unity package set after confirming it was the source of the GUID collision with the pinned `com.coplaydev.unity-mcp` package.
- Kept the newer Unity MCP pin in place while eliminating the startup log that had been contaminating batch PlayMode verification.

## Verification

- `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests tmp/maintown-population-infra-play.xml tmp/maintown-population-infra-play.log`: exit `0`
- `Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests`: `1/1` passed in `tmp/maintown-population-infra-play.xml`
- `tmp/maintown-population-infra-play.log` no longer contains:
  - `GUID [7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d]`
  - `QwenCodeConfigurator`
  - `UnhandledLogMessageException`

## 2026-03-08 Checkpoint 3

- Added the first live scene-population rebuild seam:
  - extended `MainTownPopulationInfrastructurePlayModeTests` with a red-to-green runtime spawn assertion
  - added `CivilianPopulationRuntimeBridge.RebuildScenePopulation()` as the minimal entry point for placeholder scene rebuilding
  - spawned one placeholder `NpcAgent` per live persisted civilian record at the authored anchor transform
  - skipped dead civilians during rebuilding
  - attached `MainTownPopulationSpawnedCivilian` metadata so runtime objects preserve `civilianId`, `populationSlotId`, `poolId`, `spawnAnchorId`, and `areaTag`
- Kept the deferred boundary explicit:
  - `RebuildScenePopulation()` is callable infrastructure, not yet automatic scene-load hydration
  - placeholder civilians are runtime shells, not final curated STYLE-driven art prefabs
  - replacement execution and contract-target eligibility changes remain deferred

## Verification

- Red step:
  - `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests.MainTownPopulationRuntime_RebuildScenePopulation_SpawnsLiveOccupantsAndSkipsDeadSlots tmp/maintown-population-rebuild-red.xml tmp/maintown-population-rebuild-red.log`
  - `tmp/maintown-population-rebuild-red.xml`: `0/1` passed
  - failing assertion: missing public `RebuildScenePopulation()` on `CivilianPopulationRuntimeBridge`
- Green step:
  - `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests.MainTownPopulationRuntime_RebuildScenePopulation_SpawnsLiveOccupantsAndSkipsDeadSlots tmp/maintown-population-rebuild-green.xml tmp/maintown-population-rebuild-green.log`
  - `tmp/maintown-population-rebuild-green.xml`: `1/1` passed
- Regression sweep:
  - `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests tmp/maintown-population-infra-full.xml tmp/maintown-population-infra-full.log`: `2/2` passed
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-bridge-edit.xml tmp/civilian-runtime-bridge-edit.log`: `5/5` passed

## Next Step After This One

The next slice should wire `MainTown` scene hydration so `RebuildScenePopulation()` is invoked automatically from the correct runtime/load seam, then verify teardown/rebuild behavior across scene reload or save restore. Final STYLE-driven visual assembly should stay deferred until that lifecycle hook is stable.
