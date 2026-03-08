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

## 2026-03-08 Checkpoint 4

- Wired placeholder population rebuilding into the actual runtime lifecycle:
  - `CivilianPopulationRuntimeBridge.Start()` now seeds starter civilians when the scene boots with no existing runtime roster and immediately rebuilds placeholder civilians into the authored anchors
  - `CivilianPopulationRuntimeBridge.FinalizeAfterLoad()` now rebuilds the scene after copying loaded module state into runtime
  - stale placeholder civilians are cleared before loaded live civilians are rebuilt
- Added focused coverage for both lifecycle seams:
  - a PlayMode `MainTown` scene-load test now verifies automatic starter seeding and placeholder rebuild on load
  - an EditMode bridge test now verifies `FinalizeAfterLoad()` clears stale spawned civilians and rebuilds only living loaded civilians
- Architectural note from codebase review:
  - `FinalizeAfterLoad()` is the authoritative save-restore seam because `SaveCoordinator.Load()` already restores modules, validates them, and then dispatches runtime bridges through `SaveRuntimeBridgeRegistry`
  - scene-start seeding remains a separate bridge-local fallback for fresh unsaved `MainTown` loads

## Verification

- Red step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.FinalizeAfterLoad_RebuildsScenePopulationFromLoadedModuleAndClearsPriorSpawnedObjects tmp/civilian-runtime-finalize-red.xml tmp/civilian-runtime-finalize-red.log`
  - `tmp/civilian-runtime-finalize-red.xml`: `0/1` passed
  - failing assertion: stale spawned civilian remained after `FinalizeAfterLoad()`
- Red step:
  - `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests.MainTownPopulationRuntime_LoadScene_AutomaticallySeedsAndBuildsStarterPopulation tmp/maintown-population-auto-red.xml tmp/maintown-population-auto-red.log`
  - `tmp/maintown-population-auto-red.xml`: `0/1` passed
  - failing assertion: runtime roster count stayed `0` on fresh `MainTown` load
- Green step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.FinalizeAfterLoad_RebuildsScenePopulationFromLoadedModuleAndClearsPriorSpawnedObjects tmp/civilian-runtime-finalize-green.xml tmp/civilian-runtime-finalize-green.log`
  - `tmp/civilian-runtime-finalize-green.xml`: `1/1` passed
- Green step:
  - `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests.MainTownPopulationRuntime_LoadScene_AutomaticallySeedsAndBuildsStarterPopulation tmp/maintown-population-auto-green.xml tmp/maintown-population-auto-green.log`
  - `tmp/maintown-population-auto-green.xml`: `1/1` passed
- Regression sweep:
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-bridge-full.xml tmp/civilian-runtime-bridge-full.log`: `6/6` passed
  - `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests tmp/maintown-population-infra-lifecycle.xml tmp/maintown-population-infra-lifecycle.log`: `3/3` passed

## 2026-03-08 Checkpoint 5

- Added a live scene-level save-restore integration harness for `MainTown`:
  - extended `MainTownPopulationInfrastructurePlayModeTests` with a temp-save `SaveCoordinator.Load()` scenario
  - captured a real save envelope from the authored scene, replaced only the `CivilianPopulation` payload, and reloaded it through the default save coordinator
  - verified that the live bridge registry path clears starter placeholders and rebuilds only the living civilians from the loaded save into authored anchors
- Scope note:
  - this checkpoint adds end-to-end orchestration coverage without expanding runtime behavior
  - no new production code was required because the lifecycle wiring from Checkpoint 4 already satisfied the stronger coordinator-driven path

## Verification

- `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests.MainTownPopulationRuntime_SaveCoordinatorLoad_RebuildsAuthoredSceneFromLoadedPopulationModule tmp/maintown-population-save-load.xml tmp/maintown-population-save-load.log`: `1/1` passed
- The test verifies:
  - `SaveCoordinator.Load()` routes through the live runtime bridge registry
  - starter civilians are cleared after load
  - only the living loaded civilian is rebuilt into `MainTown`
  - loaded slot metadata and anchor positioning survive the full save/load path

## Next Step After This One

The next slice should move from infrastructure/harness work into deterministic replacement behavior: queue a dead civilian, execute the first replacement cycle against a stable slot, and verify the new occupant preserves slot identity while changing civilian identity. Final STYLE-driven visual assembly should still stay deferred until replacement lifecycle coverage is stable.
