# Persistent Player Reset Progress

## Scope

- Plan: `docs/plans/2026-03-20-persistent-player-reset-implementation-plan.md`
- Current slice: Task 2
- Status: Verification complete; commit/push pending

## Task Checklist

- [x] Review Task 2 plan requirements and local agent guidance
- [x] Write the failing runtime-owner EditMode coverage
- [x] Record the focused red verification for bootstrap/runtime-player ownership
- [x] Implement the canonical runtime-player ownership cutover
- [x] Re-run the focused Task 2 suite to green
- [ ] Commit and push the scoped Task 2 changes

## Changed Files

- `docs/plans/progress/2026-03-20-persistent-player-reset-progress.md`
- `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot.prefab`
- `Reloader/Assets/_Project/World/Scripts/Runtime/PersistentPlayerRoot.cs`
- `Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs`
- `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs`
- `Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs`
- `Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs`

## Verification Log

- `bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests" "$(pwd)/tmp/persistent-player-reset-task2-red.xml" "$(pwd)/tmp/persistent-player-reset-task2-red.log"`
  - Result: failed before test execution because another Unity instance already had the project open.
- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.*","Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests.*"]`
  - Result: failed `5/7`, passed `2/7`
  - Failing tests:
    - `Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.Initialize_CreatesSinglePersistentRootWithCanonicalRuntimePlayerPrefab`
    - `Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.MoveRuntimePlayerRootToScene_WithCanonicalRuntimePlayerRoot_DoesNotSwapToSceneAuthoredPlayerRoot`
    - `Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.MoveRuntimePlayerRootToScene_WithoutCanonicalRuntimePlayerRoot_FailsClosedInsteadOfAdoptingScenePlayerRoot`
    - `Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests.PreparePersistentPlayerRootForTravel_DoesNotAdoptSceneAuthoredOriginPlayerRoot`
    - `Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests.RepositionPlayerToEntryPoint_WithoutCanonicalRuntimePlayerRoot_FailsClosedAndLeavesScenePlayerRootUntouched`
  - Interpretation: bootstrap still lacked the canonical prefab, `PersistentPlayerRoot` still exposed scene-adoption semantics, and travel still allowed non-canonical player resolution.
- Unity MCP `refresh_unity`
  - Result: refresh requested with compile requested.
- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.*","Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests.*"]`
  - Result: passed `7/7`
  - Interpretation: bootstrap now owns a prefab-backed runtime player, `PersistentPlayerRoot` moves only that runtime instance, and travel fails closed when the canonical runtime player is missing.

## Commit / Push

- Commit SHA: pending
- Push status: pending

## Open Risks / Blockers

- The exact shell command from the plan is blocked while another Unity editor instance holds the project lock.
- The canonical prefab load is currently asset-path-driven inside the editor because Task 2 cannot update scene assets yet; Bootstrap scene serialization for that prefab must wait for Task 3.
- `MainTown` and `IndoorRangeInstance` still author `PlayerRoot` scene objects and player look/controller ownership; removing those scene-authored implementations must wait for Task 3.

## Review Findings

- Fixed in Task 2: bootstrap now creates and retains one canonical runtime player prefab instance.
- Fixed in Task 2: `PersistentPlayerRoot` no longer adopts or swaps to scene-authored player roots.
- Fixed in Task 2: travel reuses the existing runtime-owned player only and fails closed when that runtime path is missing.
- Deferred to Task 3: scene assets still author legacy player roots and related controller wiring.

## Deletion Log

- Deleted from runtime behavior: `PersistentPlayerRoot` scene-adoption / `preferSceneRoot` semantics and `WorldTravelCoordinator` scene-root fallback resolution.
