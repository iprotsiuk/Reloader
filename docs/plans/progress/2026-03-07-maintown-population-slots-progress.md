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

## 2026-03-08 Checkpoint 6

- Added the first deterministic replacement execution path:
  - `CivilianPopulationRuntimeBridge.ExecutePendingReplacements(int currentDay)` now consumes matured replacement debt
  - replacement execution preserves stable slot ownership from the vacated civilian:
    - `populationSlotId`
    - `poolId`
    - `spawnAnchorId`
    - `areaTag`
    - `isProtectedFromContracts`
  - replacement execution issues a new deterministic `civilianId` using the existing `citizen.mainTown.####` sequence
  - replacement execution rebuilds the live placeholder civilian after inserting the new occupant and clearing the debt
- Added focused coverage for both synthetic and authored-scene seams:
  - EditMode bridge tests now verify matured replacement debt creates a new civilian in the same slot and future-dated debt does nothing
  - PlayMode `MainTown` infrastructure coverage now verifies queued replacement execution rebuilds the authored starter slot with the new civilian identity
- Scope note:
  - this is still infrastructure-level replacement behavior only
  - no weekly scheduler or Monday `08:00` orchestration was added yet
  - no final STYLE-driven visual assembly changes were introduced

## Verification

- Red step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.ExecutePendingReplacements tmp/civilian-runtime-replacement-red.xml tmp/civilian-runtime-replacement-red.log`
  - `tmp/civilian-runtime-replacement-red.xml`: `0/2` passed
  - failing assertion: missing public `ExecutePendingReplacements(int currentDay)` on `CivilianPopulationRuntimeBridge`
- Green step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.ExecutePendingReplacements tmp/civilian-runtime-replacement-green.xml tmp/civilian-runtime-replacement-green.log`
  - `tmp/civilian-runtime-replacement-green.xml`: `2/2` passed
- Regression sweep:
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-bridge-replacements.xml tmp/civilian-runtime-bridge-replacements.log`: `8/8` passed
  - `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests.MainTownPopulationRuntime_ExecutePendingReplacements_RebuildsStableSlotWithNewCivilian tmp/maintown-population-replacement-play.xml tmp/maintown-population-replacement-play.log`: `1/1` passed
  - `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests tmp/maintown-population-infra-replacements-full.xml tmp/maintown-population-infra-replacements-full.log`: `5/5` passed

## 2026-03-08 Checkpoint 7

- Addressed the new PR review comment on duplicate slot occupants with the codebase-correct invariant:
  - `CivilianPopulationModule.ValidateModuleState()` now rejects duplicate live occupants for the same `populationSlotId`
  - dead historical civilians are still allowed to share a slot with their live replacement because replacement execution intentionally preserves slot history
- Added save-module regression coverage for both sides of that contract:
  - duplicate live occupants in one slot are rejected at validation/load time
  - one dead retired civilian plus one live replacement in the same slot remains valid
- Scope note:
  - no runtime population behavior changed here
  - this checkpoint narrows save validation to the actual runtime contract enforced by `RebuildScenePopulation()` and replacement history retention

## Verification

- Red step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-pop-save-dup-slot-red.xml tmp/civilian-pop-save-dup-slot-red.log`
  - `tmp/civilian-pop-save-dup-slot-red.xml`: `6/7` passed
  - failing assertion: duplicate live `populationSlotId` was not rejected by `ValidateModuleState()`
- Green step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-pop-save-dup-slot-green.xml tmp/civilian-pop-save-dup-slot-green.log`: `7/7` passed
- Regression sweep:
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-bridge-post-save-module.xml tmp/civilian-runtime-bridge-post-save-module.log`: `8/8` passed
  - `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests tmp/maintown-population-infra-post-save-module.xml tmp/maintown-population-infra-post-save-module.log`: `5/5` passed

## 2026-03-08 Checkpoint 8

- Addressed the new runtime review comment on duplicate pending replacement debts:
  - `CivilianPopulationModule.ValidateModuleState()` now rejects duplicate `pendingReplacements` entries targeting the same `vacatedCivilianId`
  - `CivilianPopulationRuntimeBridge.ExecutePendingReplacements()` now collapses duplicate matured debts defensively so malformed in-memory state cannot spawn multiple live replacements for one slot
- Added focused regression coverage for both layers:
  - save-module validation now fails fast on duplicate pending debts
  - bridge replacement execution now proves duplicate matured debts still produce only one replacement civilian and one live placeholder
- Scope note:
  - this is save/runtime hardening only
  - no weekly scheduler or Monday `08:00` orchestration was added yet
  - no appearance-pipeline or final visual assembly behavior changed

## Verification

- Red step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests.CivilianPopulationModule_ValidateModuleState_RejectsDuplicatePendingReplacementDebts tmp/civilian-pop-dup-debt-red.xml tmp/civilian-pop-dup-debt-red.log`
  - `tmp/civilian-pop-dup-debt-red.xml`: `0/1` passed
  - failing assertion: duplicate pending replacement debt was not rejected by `ValidateModuleState()`
- Red step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.ExecutePendingReplacements_WhenDuplicateMaturedDebtsExist_SpawnsOnlyOneReplacement tmp/civilian-runtime-dup-debt-red.xml tmp/civilian-runtime-dup-debt-red.log`
  - `tmp/civilian-runtime-dup-debt-red.xml`: `0/1` passed
  - failing assertion: duplicate matured debt produced `2` replacements instead of `1`
- Green step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests tmp/civilian-pop-dup-debt-green.xml tmp/civilian-pop-dup-debt-green.log`: `8/8` passed
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests tmp/civilian-runtime-dup-debt-green.xml tmp/civilian-runtime-dup-debt-green.log`: `9/9` passed
- Regression sweep:
  - `bash scripts/run-unity-tests.sh playmode Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests tmp/maintown-population-infra-post-dup-debt.xml tmp/maintown-population-infra-post-dup-debt.log`: `5/5` passed

## 2026-03-08 Checkpoint 9

- Wired the first automatic replacement-execution lifecycle seam into load finalization:
  - `CivilianPopulationRuntimeBridge.FinalizeAfterLoad()` now resolves `CoreWorldModule.DayCount` from the same save-load registration set
  - matured pending replacement debt now executes during load finalization before the scene placeholder rebuild
  - if no replacements mature, the bridge keeps the existing plain rebuild behavior
- Added focused EditMode coverage for that seam:
  - a loaded dead civilian plus matured pending replacement debt now rebuilds into a new live civilian using the loaded world day as `CreatedAtDay`
  - the debt clears during load finalization and the authored anchor rebuild still produces one live placeholder
- Scope note:
  - this only covers save-load lifecycle execution
  - no same-session world-time event subscription or Monday `08:00` scheduler hook was added yet
  - no appearance-pipeline or final visual assembly behavior changed

## Verification

- Red step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.FinalizeAfterLoad_WhenMaturedReplacementDebtExists_ExecutesReplacementUsingCoreWorldDay tmp/civilian-runtime-finalize-replacement-red.xml tmp/civilian-runtime-finalize-replacement-red.log`
  - `tmp/civilian-runtime-finalize-replacement-red.xml`: `0/1` passed
  - failing assertion: matured pending replacement debt remained queued during `FinalizeAfterLoad()`
- Green step:
  - `bash scripts/run-unity-tests.sh editmode Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.FinalizeAfterLoad_WhenMaturedReplacementDebtExists_ExecutesReplacementUsingCoreWorldDay tmp/civilian-runtime-finalize-replacement-green.xml tmp/civilian-runtime-finalize-replacement-green.log`: `1/1` passed
- Regression sweep:
  - Unity MCP `run_tests` / `get_test_job` because a live Unity editor session already held the project lock:
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests`: `10/10` passed
    - `Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests`: `5/5` passed

## 2026-03-08 Checkpoint 10

- Added the first same-session world-time replacement seam:
  - `CivilianPopulationRuntimeBridge` now subscribes to `CoreWorldController.WorldStateChanged`
  - the bridge caches the observed world day and executes matured pending replacements only when `DayCount` advances
  - same-day time changes do not trigger replacement execution
  - the bridge now exposes a direct `SetCoreWorldController(...)` seam for deterministic wiring/tests while keeping scene auto-discovery as the fallback
- Updated authored scene infrastructure to support that seam:
  - `MainTown` now includes an authored `CoreWorldController`
  - PlayMode coverage now proves the real scene can execute a matured replacement after a same-session day advance without save/load
- Scope note:
  - this still does not implement a final Monday `08:00` scheduler rule
  - this is a day-advance seam only
  - no final civilian appearance assembly or contract-generation behavior changed

## Verification

- Red step:
  - Unity MCP `run_tests` / `get_test_job`:
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.WorldStateChanged_WhenDayAdvancesAndMaturedReplacementDebtExists_ExecutesReplacementWithoutReload`: `0/1` passed
  - failing assertion: matured replacement debt remained queued after same-session day advance
- Green step:
  - Unity MCP `run_tests` / `get_test_job`:
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.WorldStateChanged_WhenDayAdvancesAndMaturedReplacementDebtExists_ExecutesReplacementWithoutReload`: `1/1` passed
    - `Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests.MainTownPopulationRuntime_WorldStateChanged_ExecutesMaturedReplacementAfterDayAdvance`: `1/1` passed
- Regression sweep:
  - Unity MCP `run_tests` / `get_test_job`:
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests`: `11/11` passed
    - `Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests`: `6/6` passed

## Next Step After This One

The next slice should formalize the actual Monday `08:00` scheduler rule on top of the new same-session day-advance seam, then verify that only newly matured queued replacements execute while future debt remains pending. Final STYLE-driven visual assembly should still stay deferred until that scheduler path is stable.

## Checkpoint: Monday 08:00 Replacement Scheduler

- Replaced the temporary day-advance maturity rule with the approved weekly scheduler contract:
  - `CivilianPopulationRuntimeBridge.ExecutePendingReplacements(int currentDay, float currentTimeOfDay)` now executes debt only at the first Monday `08:00` strictly after `QueuedAtDay`
  - `FinalizeAfterLoad()` now evaluates replacement maturity from the loaded `CoreWorldModule` day and time, not day alone
  - same-session `CoreWorldController.WorldStateChanged` handling now tracks full `(DayCount, TimeOfDay)` progression so Monday morning threshold crossings can execute without reload
- Locked the day-precision edge case explicitly:
  - vacancies queued on a Monday do not execute that same Monday at `08:00`
  - because the save model stores only `QueuedAtDay`, Monday-queued debt waits until the following Monday refresh rather than risking a retroactive same-day spawn
- Coverage updates:
  - EditMode bridge tests now verify:
    - load finalization executes debt at Monday `08:00`
    - same-session world updates execute at the Monday morning threshold
    - pre-threshold Monday time stays pending
    - Monday-queued vacancies wait until the following Monday
  - PlayMode `MainTown` infrastructure coverage now verifies the authored scene executes replacements when Monday `08:00` arrives through the real `CoreWorldController`
- Scope note:
  - this is still infrastructure-level placeholder spawning only
  - no final STYLE appearance assembly or contract-generation behavior changed

## Verification

- Red attempt:
  - `bash scripts/run-unity-tests.sh editmode ...`: blocked because the Unity editor already had the project open
  - the initial live Unity MCP session also stopped servicing test requests until the editor was restarted
- Green step:
  - Unity MCP `run_tests` / `get_test_job`:
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.ExecutePendingReplacements_WhenMondayRefreshWindowHasArrived_ReplacesDeadCivilianInSameSlot`: `1/1` passed
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.FinalizeAfterLoad_WhenMondayRefreshWindowHasArrived_ExecutesReplacementUsingCoreWorldState`: `1/1` passed
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.WorldStateChanged_WhenMondayMorningThresholdIsCrossed_ExecutesReplacementWithoutReload`: `1/1` passed
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.ExecutePendingReplacements_WhenVacancyWasQueuedOnMonday_WaitsUntilFollowingMondayRefresh`: `1/1` passed
- Regression sweep:
  - Unity MCP `run_tests` / `get_test_job`:
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests`: `12/12` passed
    - `Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests`: `6/6` passed

## Checkpoint: Purge Invalid Matured Replacement Debt

- Addressed the new PR review thread on dangling/alive replacement debt:
  - `CivilianPopulationModule.ValidateModuleState()` now rejects `pendingReplacements` entries that do not reference an existing dead civilian record
  - `CivilianPopulationRuntimeBridge.ExecutePendingReplacements()` now purges matured debt that points to a missing civilian or a still-alive civilian instead of retrying it forever
- Added focused coverage for both layers:
  - save-module tests now reject pending debt referencing a missing civilian or an alive civilian
  - bridge tests now prove matured invalid debt is removed without spawning a replacement civilian
- Scope note:
  - this hardens malformed-state handling only
  - no contract-targeting or final appearance behavior changed

## Verification

- Red step:
  - Unity MCP `run_tests` / `get_test_job`:
    - `Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests.CivilianPopulationModule_ValidateModuleState_RejectsPendingReplacementReferencingMissingCivilian`: `0/1` passed
      - failing assertion: `ValidateModuleState()` did not throw for dangling debt
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.ExecutePendingReplacements_WhenMaturedDebtReferencesMissingCivilian_PurgesDebtWithoutSpawning`: `0/1` passed
      - failing assertion: matured invalid debt remained queued instead of being purged
- Green step:
  - Unity MCP `run_tests` / `get_test_job`:
    - `Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests.CivilianPopulationModule_ValidateModuleState_RejectsPendingReplacementReferencingMissingCivilian`: `1/1` passed
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.ExecutePendingReplacements_WhenMaturedDebtReferencesMissingCivilian_PurgesDebtWithoutSpawning`: `1/1` passed
- Regression sweep:
  - Unity MCP `run_tests` / `get_test_job`:
    - `Reloader.Core.Tests.EditMode.CivilianPopulationSaveModuleTests`: `10/10` passed
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests`: `14/14` passed
    - `Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests`: `6/6` passed

## Checkpoint: NpcFoundation-Backed Civilian Actors

- Replaced the ad-hoc spawned civilian shell with the shared NPC foundation actor seam:
  - `CivilianPopulationRuntimeBridge` now supports an authored `_npcActorPrefab`
  - `MainTown` assigns `NpcFoundation.prefab` on the population bridge
  - spawned civilians now preserve the shared NPC actor hierarchy/model/collider contract while still layering `MainTownPopulationSpawnedCivilian` metadata and `AmbientCitizenCapability`
- Coverage updates:
  - EditMode bridge coverage now proves `RebuildScenePopulation()` instantiates the assigned actor prefab and preserves its visual hierarchy while adding population metadata
  - PlayMode `MainTown` coverage now proves starter civilians spawn from the authored NPC actor prefab rather than ad-hoc shell objects
- Scope note:
  - this is still not the final STYLE appearance pipeline
  - civilians now use the shared NPC foundation body/model contract, but curated procedural body/clothes/hair assembly remains deferred

## Verification

- Focused checks:
  - Unity MCP `run_tests` / `get_test_job`:
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests.RebuildScenePopulation_WhenActorPrefabIsAssigned_InstantiatesActorPrefabWithPopulationMetadata`: `1/1` passed
    - `Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests.MainTownPopulationRuntime_LoadScene_AutomaticallySeedsAndBuildsStarterPopulation`: `1/1` passed
- Regression sweep:
  - Unity MCP `run_tests` / `get_test_job`:
    - `Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTests`: `15/15` passed
    - `Reloader.World.Tests.PlayMode.MainTownPopulationInfrastructurePlayModeTests`: `6/6` passed
