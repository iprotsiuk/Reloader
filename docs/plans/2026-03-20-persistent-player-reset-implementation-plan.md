# Persistent Player Reset Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the current scene-adopted player/travel/save stack with one canonical runtime-owned player root that survives bootstrap, travel, save/load, dropped-item restore, and death respawn, while also making stable-world ballistic authority first-class under origin rebasing so long-range precision and moving-target hits remain exact.

**Architecture:** Bootstrap owns exactly one runtime player prefab instance for the entire session. Scenes own only entry/spawn/respawn anchors plus local services; they do not own player implementations. Player save state, dropped world-item persistence, death respawn routing, immediate dynamic origin rebasing, stable-world ballistic authority, and the Cinemachine world-camera stack all route through explicit canonical seams, with no reverse-compatibility branches or silent fallback repair.

**Tech Stack:** Unity 6 C#, `SaveCoordinator`/module save pipeline, `Unity.Cinemachine`, prefab-authored player/viewmodel setup, Unity EditMode tests plus targeted headless PlayMode verification, focused scene/policy validators, repo doc/context verification scripts.

---

## Execution Rules

- Do not add or rely on manual live-mode tests in this reset plan. Prefer EditMode, pure runtime unit tests, scene asset contract tests, and targeted headless PlayMode verification only when runtime flight/hit behavior needs it.
- Every green slice ends with: progress doc update, commit, push, PR comment with exact verification commands/results, and thread resolution/closure for any addressed review comments.
- Do not preserve `preferSceneRoot`, `MainWorld` coexistence logic, `PlayerRoot_MainTown` duplication, `Camera.main`/`Transform.Find(...)` recovery, scene-authored player implementations, or temporary near-zero jitter patches.
- Fail closed on missing required anchors/references. Do not add hidden repair behavior.

## Ballistic Authority Model

- Stable-world ballistic authority owns shot integration, impact evaluation, and moving-target precision in a rebasing-safe frame.
- Rebased local scene/runtime owns player/world placement, travel anchors, and any transform shift that keeps the world near the origin.
- Near-object/viewmodel render space owns muzzle flash, weapon pose, optic/viewmodel presentation, and other camera-adjacent visuals only.
- The muzzle-origin firing contract stays intact: launch data still comes from the authored muzzle on the weapon view, but that launch data is promoted into stable-world authority before hit truth is finalized.
- Projectile path observers, dev traces, and event ports are display/notification surfaces, not the final source of truth for long-range accuracy under rebasing.

Acceptance criteria for this slice:
- long-range shots retain precision after an origin shift or scene travel
- shots against moving targets resolve correctly after rebasing, with no visible hitch from local transform changes
- muzzle-origin firing remains intact for all weapon views and optics
- no temporary float-world patch, fallback replay, or compatibility shim is added

## Progress Doc Requirements

- Create: `docs/plans/progress/2026-03-20-persistent-player-reset-progress.md`
- Update that progress doc before every commit and after every review round with:
  - current task number/status
  - exact changed file paths
  - exact verification commands and whether they passed
  - commit SHA and push status
  - open risks/blockers
  - deleted files/helpers/tests/docs completed in that slice
- Once this reset owns the split-viewmodel work, update `docs/plans/progress/2026-03-20-split-viewmodel-redesign-progress.md` to point at the reset progress doc so the two trackers do not drift.

## Review Checkpoints

- Checkpoint A: after the canonical runtime player root exists and `MainTown`/`IndoorRangeInstance` no longer author player implementations.
- Checkpoint B: after player save state, dropped-item exact restore, and death-respawn routing are green in EditMode.
- Checkpoint C: after origin rebasing, stable-world ballistic authority, and Cinemachine restoration are green, before legacy deletions.

### Task 1: Lock The Single-Owner Contract In EditMode Tests And Progress Tracking

**Files:**
- Create: `docs/plans/progress/2026-03-20-persistent-player-reset-progress.md`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/WorldPlayerRootContractEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/TravelContextEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/Player/Tests/EditMode/PlayerLookConfigurationEditModeTests.cs`

**Step 1: Write the failing tests and tracker shell**

- Create the progress doc with scope, task checklist, verification log section, and deletion log section.
- Add/EditMode regressions that require:
  - one runtime-owned player root only
  - no scene-authored `PlayerRoot` dependency in travel contracts
  - explicit anchor-driven spawn resolution
  - no `PlayerRoot_MainTown`-specific prefab contract

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.WorldPlayerRootContractEditModeTests|Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests|Reloader.World.Tests.EditMode.TravelContextEditModeTests|Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests" "$(pwd)/tmp/persistent-player-reset-task1-red.xml" "$(pwd)/tmp/persistent-player-reset-task1-red.log"
```

Expected: fail because the current runtime still allows scene adoption, `PlayerRoot_MainTown`-specific expectations, and scene-owned player assumptions.

**Step 3: Make only the minimum test/doc updates needed to freeze the target contract**

- Do not implement runtime behavior yet.
- Make the tests describe the approved end state and remove assertions that codify legacy scene-owned player behavior.

**Step 4: Re-run the same suite until the failing surface matches the intended cutover work**

Run the same command again.

Expected: failures now point at real runtime gaps, not stale legacy expectations.

**Step 5: Commit, push, and comment**

```bash
git add docs/plans/progress/2026-03-20-persistent-player-reset-progress.md Reloader/Assets/_Project/World/Tests/EditMode/WorldPlayerRootContractEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/TravelContextEditModeTests.cs Reloader/Assets/_Project/Player/Tests/EditMode/PlayerLookConfigurationEditModeTests.cs
git commit -m "test(world): lock persistent player reset contract"
git push
```

PR note must include the red-suite command/result and the new progress-doc path.

### Task 2: Make Bootstrap Own The Canonical Runtime Player Prefab

**Files:**
- Create: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot.prefab`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/PersistentPlayerRoot.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerCameraDefaults.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs`

**Step 1: Write the failing runtime-owner tests**

- Require bootstrap to instantiate/load one canonical runtime player prefab.
- Require `PersistentPlayerRoot` to stop adopting scene-local player implementations.
- Require `WorldTravelCoordinator` to move/reposition the existing runtime player instead of capturing a scene-authored replacement.

**Step 2: Run the focused red suite**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests" "$(pwd)/tmp/persistent-player-reset-task2-red.xml" "$(pwd)/tmp/persistent-player-reset-task2-red.log"
```

Expected: fail because `CaptureOrAdoptPlayerRootForScene(..., preferSceneRoot: true)` and related scene-adoption semantics still exist.

**Step 3: Implement the canonical runtime owner**

- Introduce `PlayerRoot.prefab` as the canonical runtime player prefab.
- Change bootstrap so it owns player creation/retention.
- Delete `preferSceneRoot` behavior from `PersistentPlayerRoot`.
- Make travel reposition the existing runtime player only.
- Keep `DontDestroyOnLoad` ownership on the runtime player path only.

**Step 4: Re-run the suite**

Run the same command again.

Expected: pass.

**Step 5: Commit, push, and checkpoint**

```bash
git add Reloader/Assets/_Project/Player/Prefabs/PlayerRoot.prefab Reloader/Assets/_Project/World/Scripts/Runtime/PersistentPlayerRoot.cs Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs Reloader/Assets/_Project/Player/Scripts/PlayerCameraDefaults.cs Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs docs/plans/progress/2026-03-20-persistent-player-reset-progress.md
git commit -m "refactor(world): make bootstrap own the runtime player"
git push
```

Stop for Checkpoint A review before changing scene assets.

### Task 3: Convert Scenes To Spawn/Respawn Anchors And Remove Scene-Owned Player Implementations

**Files:**
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/PlayerSpawnAnchor.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/PlayerSpawnAnchorKind.cs`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/PlayerSpawnAnchorEditModeTests.cs`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/WorldScenePlayerAnchorContractEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/SceneEntryPoint.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/TravelSceneTrigger.cs`
- Modify: `Reloader/Assets/_Project/World/Editor/WorldSceneContractValidator.cs`
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Scenes/IndoorRangeInstance.unity`
- Modify: `Reloader/Assets/Scenes/Bootstrap.unity`
- Delete from scene content: authored `PlayerRoot` GameObjects in `MainTown.unity` and `IndoorRangeInstance.unity`

**Step 1: Write the failing scene contract tests**

- Require `MainTown` and `IndoorRangeInstance` to expose only anchors/entry points for player placement.
- Require hospital and police respawn anchors in `MainTown`.
- Require scenes to fail validation if an authored `PlayerRoot` still exists.

**Step 2: Run the focused red suite**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.PlayerSpawnAnchorEditModeTests|Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests|Reloader.World.Tests.EditMode.TravelContextEditModeTests" "$(pwd)/tmp/persistent-player-reset-task3-red.xml" "$(pwd)/tmp/persistent-player-reset-task3-red.log"
```

Expected: fail because scenes still encode player implementations instead of anchor-only ownership.

**Step 3: Implement the anchor-only scene contract**

- Add explicit spawn/return/hospital/police anchor components.
- Keep scene entry IDs stable.
- Remove authored `PlayerRoot` instances from `MainTown` and `IndoorRangeInstance`.
- Make validators reject scenes that reintroduce player implementations.

**Step 4: Re-run the suite**

Run the same command again.

Expected: pass.

**Step 5: Commit, push, and update the PR**

```bash
git add Reloader/Assets/_Project/World/Scripts/Runtime/Travel/PlayerSpawnAnchor.cs Reloader/Assets/_Project/World/Scripts/Runtime/Travel/PlayerSpawnAnchorKind.cs Reloader/Assets/_Project/World/Scripts/Runtime/Travel/SceneEntryPoint.cs Reloader/Assets/_Project/World/Scripts/Runtime/Travel/TravelSceneTrigger.cs Reloader/Assets/_Project/World/Editor/WorldSceneContractValidator.cs Reloader/Assets/_Project/World/Scenes/MainTown.unity Reloader/Assets/_Project/World/Scenes/IndoorRangeInstance.unity Reloader/Assets/Scenes/Bootstrap.unity Reloader/Assets/_Project/World/Tests/EditMode/PlayerSpawnAnchorEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/WorldScenePlayerAnchorContractEditModeTests.cs docs/plans/progress/2026-03-20-persistent-player-reset-progress.md
git commit -m "refactor(world): convert scenes to player anchors only"
git push
```

### Task 4: Add Canonical Player Save State And Exact Restore For Travel/Save/Load

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/PlayerStateModule.cs`
- Create: `Reloader/Assets/_Project/Player/Scripts/PlayerStateRuntimeBridge.cs`
- Create: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerStateSaveModuleTests.cs`
- Create: `Reloader/Assets/_Project/Player/Tests/EditMode/PlayerStateRuntimeBridgeEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveCoordinator.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/WorldObjectStateModule.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldObjectPersistenceRuntimeBridge.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Persistence/WorldObjectStateApplyService.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/WorldObjectStateSaveModuleTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/WorldObjectStateContractsTests.cs`

**Step 1: Write the failing save/restore tests**

- Require player transform, current scene/anchor, selected belt slot, and arrest/death recovery metadata to round-trip through a new `PlayerState` module.
- Require restore order to rehydrate player state before anchor placement finalization.
- Require dropped floor-item world-state records to continue restoring exactly after travel/save/load.

**Step 2: Run the focused red suite**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.PlayerStateSaveModuleTests|Reloader.Player.Tests.EditMode.PlayerStateRuntimeBridgeEditModeTests|Reloader.Core.Tests.EditMode.WorldObjectStateSaveModuleTests|Reloader.Core.Tests.EditMode.WorldObjectStateContractsTests" "$(pwd)/tmp/persistent-player-reset-task4-red.xml" "$(pwd)/tmp/persistent-player-reset-task4-red.log"
```

Expected: fail because there is no canonical player-state module/bridge yet.

**Step 3: Implement the save module and bridge**

- Register `PlayerStateModule` in `SaveBootstrapper` with deterministic ordering.
- Add a player runtime bridge tied to the canonical runtime player root.
- Keep dropped-item persistence on `WorldObjectState`; do not split it into a second fallback path.

**Step 4: Re-run the suite**

Run the same command again.

Expected: pass.

**Step 5: Commit, push, and note schema changes clearly**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Save/Modules/PlayerStateModule.cs Reloader/Assets/_Project/Player/Scripts/PlayerStateRuntimeBridge.cs Reloader/Assets/_Project/Core/Scripts/Save/SaveBootstrapper.cs Reloader/Assets/_Project/Core/Scripts/Save/SaveCoordinator.cs Reloader/Assets/_Project/Core/Scripts/Save/Modules/WorldObjectStateModule.cs Reloader/Assets/_Project/Core/Scripts/Persistence/WorldObjectPersistenceRuntimeBridge.cs Reloader/Assets/_Project/Core/Scripts/Persistence/WorldObjectStateApplyService.cs Reloader/Assets/_Project/Core/Tests/EditMode/PlayerStateSaveModuleTests.cs Reloader/Assets/_Project/Player/Tests/EditMode/PlayerStateRuntimeBridgeEditModeTests.cs Reloader/Assets/_Project/Core/Tests/EditMode/WorldObjectStateSaveModuleTests.cs Reloader/Assets/_Project/Core/Tests/EditMode/WorldObjectStateContractsTests.cs docs/plans/progress/2026-03-20-persistent-player-reset-progress.md
git commit -m "feat(save): add canonical player state restore path"
git push
```

### Task 5: Keep Runtime-Dropped Floor Items On The Canonical Persistence Path Only

**Files:**
- Modify: `Reloader/Assets/_Project/Core/Scripts/Persistence/RuntimeDroppedObjectPersistenceTracker.cs`
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/World/RuntimeDroppedItemFactory.cs`
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/World/RuntimeDroppedItemSpawnRestorer.cs`
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/WorldObjectStateContractsTests.cs`
- Create: `Reloader/Assets/_Project/Inventory/Tests/EditMode/RuntimeDroppedItemPersistenceEditModeTests.cs`

**Step 1: Write the failing dropped-item exact-restore tests**

- Require runtime drops to persist scene path, object ID, item definition ID, stack quantity, and transform through:
  - immediate drop
  - scene reload simulation
  - save/load simulation
- Require no alternate drop-restore fallback outside `WorldObjectPersistenceRuntimeBridge`.

**Step 2: Run the focused red suite**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Inventory.Tests.EditMode.RuntimeDroppedItemPersistenceEditModeTests|Reloader.Core.Tests.EditMode.WorldObjectStateContractsTests" "$(pwd)/tmp/persistent-player-reset-task5-red.xml" "$(pwd)/tmp/persistent-player-reset-task5-red.log"
```

Expected: fail until the canonical player-state cutover stops any duplicate/scene-owned restore assumptions.

**Step 3: Tighten the single-path dropped-item contract**

- Keep `RuntimeDroppedObjectPersistenceTracker` and `RuntimeDroppedItemSpawnRestorer` wired only through the canonical world-object state path.
- Delete any duplicate restore helper or scene-local drop respawn workaround uncovered during implementation.

**Step 4: Re-run the suite**

Run the same command again.

Expected: pass.

**Step 5: Commit, push, and stop for Checkpoint B review**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Persistence/RuntimeDroppedObjectPersistenceTracker.cs Reloader/Assets/_Project/Inventory/Scripts/World/RuntimeDroppedItemFactory.cs Reloader/Assets/_Project/Inventory/Scripts/World/RuntimeDroppedItemSpawnRestorer.cs Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs Reloader/Assets/_Project/Core/Tests/EditMode/WorldObjectStateContractsTests.cs Reloader/Assets/_Project/Inventory/Tests/EditMode/RuntimeDroppedItemPersistenceEditModeTests.cs docs/plans/progress/2026-03-20-persistent-player-reset-progress.md
git commit -m "fix(persistence): keep dropped floor items on the single restore path"
git push
```

### Task 6: Add Hospital And Police Respawn Routing

**Files:**
- Create: `Reloader/Assets/_Project/Core/Scripts/Events/PlayerRespawnReason.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/PlayerRespawnCoordinator.cs`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/PlayerRespawnCoordinatorEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IGameEventsRuntimeHub.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs`
- Modify: `Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceStopDialogueRuntime.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/Modules/PlayerStateModule.cs`
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs`
- Modify: `Reloader/Assets/_Project/LawEnforcement/Tests/EditMode/PoliceStopDialogueRuntimeTests.cs`

**Step 1: Write the failing respawn tests**

- Require death/severe-injury respawn requests to land at the `Hospital` anchor.
- Require police-capture respawn requests to land at the `Police` anchor.
- Require save/load to preserve pending respawn reason/anchor when applicable.

**Step 2: Run the focused red suite**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.PlayerRespawnCoordinatorEditModeTests|Reloader.Contracts.Tests.EditMode.ContractEscapeResolutionRuntimeTests|Reloader.LawEnforcement.Tests.EditMode.PoliceStopDialogueRuntimeTests" "$(pwd)/tmp/persistent-player-reset-task6-red.xml" "$(pwd)/tmp/persistent-player-reset-task6-red.log"
```

Expected: fail because no respawn routing contract exists yet.

**Step 3: Implement explicit respawn routing**

- Route respawn through explicit anchor IDs only.
- Publish explicit respawn events through the runtime hub.
- Do not teleport to ad-hoc fallback transforms.

**Step 4: Re-run the suite**

Run the same command again.

Expected: pass.

**Step 5: Commit, push, and record the new anchor IDs**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Events/PlayerRespawnReason.cs Reloader/Assets/_Project/World/Scripts/Runtime/Travel/PlayerRespawnCoordinator.cs Reloader/Assets/_Project/World/Tests/EditMode/PlayerRespawnCoordinatorEditModeTests.cs Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs Reloader/Assets/_Project/Core/Scripts/Runtime/IGameEventsRuntimeHub.cs Reloader/Assets/_Project/Core/Scripts/Runtime/ContractEscapeResolutionRuntime.cs Reloader/Assets/_Project/LawEnforcement/Scripts/Runtime/PoliceStopDialogueRuntime.cs Reloader/Assets/_Project/Core/Scripts/Save/Modules/PlayerStateModule.cs Reloader/Assets/_Project/World/Scenes/MainTown.unity Reloader/Assets/_Project/Core/Tests/EditMode/ContractEscapeResolutionRuntimeTests.cs Reloader/Assets/_Project/LawEnforcement/Tests/EditMode/PoliceStopDialogueRuntimeTests.cs docs/plans/progress/2026-03-20-persistent-player-reset-progress.md
git commit -m "feat(world): add explicit hospital and police respawn routing"
git push
```

### Task 7: Implement Immediate Dynamic Origin Rebasing

**Files:**
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Origin/DynamicOriginRebaseController.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Origin/DynamicOriginRebaseState.cs`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/DynamicOriginRebaseControllerEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/PersistentPlayerRoot.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerLookController.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs`

**Step 1: Write the failing rebase tests**

- Require world content and anchors to rebase immediately once the configured threshold is crossed.
- Require the canonical player root, spawn anchors, and dropped-world-item records to remain coherent after a rebase.
- Require that no temporary near-zero patch or dual-jitter strategy is introduced.

**Step 2: Run the focused red suite**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests" "$(pwd)/tmp/persistent-player-reset-task7-red.xml" "$(pwd)/tmp/persistent-player-reset-task7-red.log"
```

Expected: fail because no dynamic origin rebase seam exists yet.

**Step 3: Implement immediate rebasing**

- Rebase the world immediately on threshold breach.
- Keep the player lifetime root authoritative during rebase.
- Do not add a separate temporary near-zero correction path.

**Step 4: Re-run the suite**

Run the same command again.

Expected: pass.

**Step 5: Commit and push**

```bash
git add Reloader/Assets/_Project/World/Scripts/Runtime/Origin/DynamicOriginRebaseController.cs Reloader/Assets/_Project/World/Scripts/Runtime/Origin/DynamicOriginRebaseState.cs Reloader/Assets/_Project/World/Tests/EditMode/DynamicOriginRebaseControllerEditModeTests.cs Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs Reloader/Assets/_Project/World/Scripts/Runtime/PersistentPlayerRoot.cs Reloader/Assets/_Project/Player/Scripts/PlayerLookController.cs Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs docs/plans/progress/2026-03-20-persistent-player-reset-progress.md
git commit -m "feat(world): add immediate dynamic origin rebasing"
git push
```

### Task 8: Make Stable-World Ballistic Authority Survive Rebasing

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Tests/EditMode/WeaponBallisticsAuthorityEditModeTests.cs`
- Create: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponBallisticsAuthorityPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/ProjectileImpactPayload.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/IWeaponEvents.cs`
- Modify: `Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevTraceRuntime.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/DynamicOriginRebaseControllerEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write the failing ballistic-authority tests**

- Require long-range shots to resolve identically before and after an origin shift.
- Require moving-target hits to stay correct when the world is rebased during flight.
- Require muzzle-origin launch data to remain authoritative at fire time without turning the local projectile path into final truth.
- Require raw `Vector3` weapon event consumers and dev traces to stop acting like the authoritative ballistic source of truth.

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Weapons.Tests.EditMode.WeaponBallisticsAuthorityEditModeTests|Reloader.Core.Tests.EditMode.InventoryEventContractsTests|Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests" "$(pwd)/tmp/persistent-player-reset-task8-red.xml" "$(pwd)/tmp/persistent-player-reset-task8-red.log"
bash scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.WeaponBallisticsAuthorityPlayModeTests|Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests|Reloader.Weapons.Tests.PlayMode.PlayerWeaponControllerPlayModeTests" "$(pwd)/tmp/persistent-player-reset-task8-play-red.xml" "$(pwd)/tmp/persistent-player-reset-task8-play-red.log"
```

Expected: fail because the current projectile path, event contracts, and rebasing behavior do not yet have a stable-world ballistic authority seam.

**Step 3: Implement the stable-world authority seam**

- Promote shot state into the stable-world authority path before hit resolution.
- Keep muzzle-origin firing intact, but stop treating the rebased local projectile path as the final arbiter of long-range truth.
- Update dev traces and event consumers so they read the stable authority output instead of reconstructing final truth from raw `Vector3` vectors.
- Delete any helper or fallback that exists only to paper over float-world assumptions.

**Step 4: Re-run the suite**

Run the same two commands again.

Expected: pass.

**Step 5: Commit, push, and stop for review before the camera/viewmodel slice**

```bash
git add Reloader/Assets/_Project/Weapons/Tests/EditMode/WeaponBallisticsAuthorityEditModeTests.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponBallisticsAuthorityPlayModeTests.cs Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs Reloader/Assets/_Project/Weapons/Scripts/Ballistics/ProjectileImpactPayload.cs Reloader/Assets/_Project/Core/Scripts/Runtime/IWeaponEvents.cs Reloader/Assets/_Project/Core/Scripts/Runtime/DefaultRuntimeEvents.cs Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs Reloader/Assets/_Project/DevTools/Scripts/Runtime/DevTraceRuntime.cs Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs Reloader/Assets/_Project/World/Tests/EditMode/DynamicOriginRebaseControllerEditModeTests.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs docs/plans/progress/2026-03-20-persistent-player-reset-progress.md
git commit -m "feat(weapons): make ballistic authority rebasing-safe"
git push
```

### Task 9: Restore Cinemachine/Brain World Camera And Keep Viewmodel Separate

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerCameraDefaults.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerLookController.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/Editor/PlayerRigMenu.cs`
- Modify: `Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Editor/WeaponsSceneWiring.cs`
- Modify: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot.prefab`
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Scenes/IndoorRangeInstance.unity`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerWeaponControllerViewmodelCameraResolutionTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerWeaponControllerWeaponPresentationRootTests.cs`
- Modify: `Reloader/Assets/_Project/Player/Tests/EditMode/PlayerRigMenuEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownPeripheralScopeWiringEditModeTests.cs`

**Step 1: Write the failing camera/viewmodel tests**

- Require a `CinemachineBrain` + `CinemachineCamera` world-camera path.
- Require the viewmodel camera to remain separate and layered under the same shared pivot contract.
- Require no `Camera.main` or hierarchy-name fallback to resolve the world camera.

**Step 2: Run the focused red suite**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.PlayerWeaponControllerViewmodelCameraResolutionTests|Reloader.Core.Tests.EditMode.PlayerWeaponControllerWeaponPresentationRootTests|Reloader.Player.Tests.EditMode.PlayerRigMenuEditModeTests|Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests|Reloader.World.Tests.EditMode.MainTownPeripheralScopeWiringEditModeTests" "$(pwd)/tmp/persistent-player-reset-task9-red.xml" "$(pwd)/tmp/persistent-player-reset-task9-red.log"
```

Expected: fail until the camera/tooling drift is fully aligned to the canonical Cinemachine contract.

**Step 3: Restore the camera stack**

- Make the world camera path explicitly Cinemachine/Brain-based again.
- Keep `WeaponPresentationRoot` and first-person arms on the separate viewmodel path.
- Delete any stale non-Cinemachine or scene-local rescue logic exposed during the cutover.

**Step 4: Re-run the suite**

Run the same command again.

Expected: pass.

**Step 5: Commit, push, and stop for Checkpoint C review**

```bash
git add Reloader/Assets/_Project/Player/Scripts/PlayerCameraDefaults.cs Reloader/Assets/_Project/Player/Scripts/PlayerLookController.cs Reloader/Assets/_Project/Player/Scripts/Editor/PlayerRigMenu.cs Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs Reloader/Assets/_Project/Weapons/Editor/WeaponsSceneWiring.cs Reloader/Assets/_Project/Player/Prefabs/PlayerRoot.prefab Reloader/Assets/_Project/World/Scenes/MainTown.unity Reloader/Assets/_Project/World/Scenes/IndoorRangeInstance.unity Reloader/Assets/_Project/Core/Tests/EditMode/PlayerWeaponControllerViewmodelCameraResolutionTests.cs Reloader/Assets/_Project/Core/Tests/EditMode/PlayerWeaponControllerWeaponPresentationRootTests.cs Reloader/Assets/_Project/Player/Tests/EditMode/PlayerRigMenuEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/MainTownPeripheralScopeWiringEditModeTests.cs docs/plans/progress/2026-03-20-persistent-player-reset-progress.md
git commit -m "refactor(player): restore Cinemachine world camera contract"
git push
```

### Task 10: Delete Legacy Scenes, Prefabs, Helpers, Tests, And Drifted Docs

**Files:**
- Delete: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab`
- Delete: `Reloader/Assets/Scenes/MainWorld.unity`
- Delete: `Reloader/Assets/Scenes/MainWorld_Level02Only.unity`
- Delete: `Reloader/Assets/Scenes/MainWorld_Scaffold.unity`
- Delete: `Reloader/Assets/_Project/Core/Editor/SceneMergeMaintenance.cs`
- Delete or replace: `Reloader/Assets/_Project/World/Tests/PlayMode/RoundTripTravelPlayModeTests.cs`
- Delete or replace any newly-obsolete helper/test surfaced by the cutover, including:
  - `Camera.main` recovery tests
  - `preferSceneRoot` coverage
  - `PlayerRoot_MainTown`-specific prefab tests
  - MainWorld-only persistence test fixtures
  - raw `Vector3` ballistic event-contract assumptions
  - float-world projectile-path reconstruction helpers that still claim final authority
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/WorldObjectStateSaveModuleTests.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/WorldObjectStateContractsTests.cs`
- Modify: `docs/design/core-architecture.md`
- Modify: `docs/design/weapons-and-ballistics.md`
- Modify: `docs/design/save-and-progression.md`
- Modify: `docs/design/save-contract-quick-reference.md`
- Modify: `docs/design/world-and-scenes.md`
- Modify: `docs/design/world-and-vehicles.md`
- Modify: `docs/design/world-scene-contracts.md`
- Modify: `docs/design/weapon-view-attachment-runtime.md`
- Modify: `docs/design/v0.1-demo-status-and-milestones.md`
- Modify: `docs/plans/progress/2026-03-20-split-viewmodel-redesign-progress.md`

**Step 1: Write the failing cleanup/contract tests**

- Replace `MainWorld` scene-path fixtures in EditMode tests with `MainTown`/`IndoorRangeInstance` fixtures.
- Add assertions that deleted assets/helpers are gone and no compatibility-only scene paths remain in the active contracts.

**Step 2: Run the focused red suite and doc checks**

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.WorldObjectStateSaveModuleTests|Reloader.Core.Tests.EditMode.WorldObjectStateContractsTests|Reloader.World.Tests.EditMode.WorldScenePersistencePolicyValidatorTests" "$(pwd)/tmp/persistent-player-reset-task10-red.xml" "$(pwd)/tmp/persistent-player-reset-task10-red.log"
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected: fail until MainWorld/compatibility wording and fixtures are removed.

**Step 3: Perform the deletions and doc cleanup**

- Delete the obsolete scenes, prefab, editor helper, and stale tests instead of leaving them parked.
- Rewrite docs to describe only the canonical runtime-owned player path and current save/world topology.

**Step 4: Re-run the same verification**

Run the same four commands again.

Expected: all pass.

**Step 5: Commit, push, and close the PR threads**

```bash
git add -A
git commit -m "chore(world): remove legacy player and mainworld compatibility paths"
git push
```

Before merging or handing off, close resolved review threads and update the progress doc with the exact deleted file list.

## Final Verification Sweep

Run this exact non-live sweep before declaring the reset complete:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.WorldPlayerRootContractEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests|Reloader.World.Tests.EditMode.PlayerSpawnAnchorEditModeTests|Reloader.World.Tests.EditMode.PlayerRespawnCoordinatorEditModeTests|Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests|Reloader.Weapons.Tests.EditMode.WeaponBallisticsAuthorityEditModeTests|Reloader.Core.Tests.EditMode.PlayerStateSaveModuleTests|Reloader.Player.Tests.EditMode.PlayerStateRuntimeBridgeEditModeTests|Reloader.Core.Tests.EditMode.WorldObjectStateSaveModuleTests|Reloader.Core.Tests.EditMode.WorldObjectStateContractsTests|Reloader.Core.Tests.EditMode.PlayerWeaponControllerViewmodelCameraResolutionTests|Reloader.Core.Tests.EditMode.PlayerWeaponControllerWeaponPresentationRootTests|Reloader.Player.Tests.EditMode.PlayerRigMenuEditModeTests|Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests|Reloader.World.Tests.EditMode.MainTownPeripheralScopeWiringEditModeTests|Reloader.World.Tests.EditMode.WorldScenePersistencePolicyValidatorTests" "$(pwd)/tmp/persistent-player-reset-final.xml" "$(pwd)/tmp/persistent-player-reset-final.log"
bash scripts/run-unity-tests.sh playmode "Reloader.Weapons.Tests.PlayMode.WeaponBallisticsAuthorityPlayModeTests|Reloader.Weapons.Tests.PlayMode.WeaponProjectilePlayModeTests|Reloader.Weapons.Tests.PlayMode.PlayerWeaponControllerPlayModeTests" "$(pwd)/tmp/persistent-player-reset-final-playmode.xml" "$(pwd)/tmp/persistent-player-reset-final-playmode.log"
bash scripts/verify-docs-and-context.sh
bash scripts/verify-extensible-development-contracts.sh
bash .agent/skills/reviewing-design-docs/scripts/audit-docs-context.sh
```

Expected: all pass with no manual live-mode dependency, no MainWorld compatibility references in active contracts, and no scene-owned player implementation left in runtime scenes.
