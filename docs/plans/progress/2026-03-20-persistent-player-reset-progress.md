# Persistent Player Reset Progress

## Scope

- Plan: `docs/plans/2026-03-20-persistent-player-reset-implementation-plan.md`
- Current slice: Task 1
- Status: Verification complete, committed, and pushed

## Task Checklist

- [x] Review Task 1 plan requirements and local agent guidance
- [x] Record the pre-edit verification baseline
- [x] Update the scoped EditMode tests to assert the approved contract
- [x] Re-run the focused suite and confirm failures point at runtime cutover gaps
- [x] Commit and push the scoped Task 1 changes

## Changed Files

- `docs/plans/progress/2026-03-20-persistent-player-reset-progress.md`
- `Reloader/Assets/_Project/World/Tests/EditMode/WorldPlayerRootContractEditModeTests.cs`
- `Reloader/Assets/_Project/World/Tests/EditMode/WorldPlayerRootContractEditModeTests.cs.meta`
- `Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs`
- `Reloader/Assets/_Project/World/Tests/EditMode/TravelContextEditModeTests.cs`
- `Reloader/Assets/_Project/Player/Tests/EditMode/PlayerLookConfigurationEditModeTests.cs`

## Verification Log

- `bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.WorldPlayerRootContractEditModeTests|Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests|Reloader.World.Tests.EditMode.TravelContextEditModeTests|Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests" "$(pwd)/tmp/persistent-player-reset-task1-red.xml" "$(pwd)/tmp/persistent-player-reset-task1-red.log"`
  - Result: failed before test execution because another Unity instance already had the project open.
- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.*","Reloader.World.Tests.EditMode.TravelContextEditModeTests.*","Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests.*"]`
  - Result: passed `17/17`
  - Interpretation: the pre-edit suite still reflected legacy expectations and did not yet lock the reset contract.
- `bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.WorldPlayerRootContractEditModeTests|Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests|Reloader.World.Tests.EditMode.TravelContextEditModeTests|Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests" "$(pwd)/tmp/persistent-player-reset-task1-red.xml" "$(pwd)/tmp/persistent-player-reset-task1-red.log"`
  - Result: still blocked by the existing Unity editor instance after the test updates.
- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.WorldPlayerRootContractEditModeTests.*","Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.*","Reloader.World.Tests.EditMode.TravelContextEditModeTests.*","Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests.*"]`
  - Result: failed `6/26`, passed `20/26`
  - Failing tests:
    - `Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests.Scene_DoesNotAuthorPlayerLookController("Assets/_Project/World/Scenes/MainTown.unity")`
    - `Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests.Scene_DoesNotAuthorPlayerLookController("Assets/_Project/World/Scenes/IndoorRangeInstance.unity")`
    - `Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.CaptureOrAdoptPlayerRootForScene_WithCanonicalRuntimePlayerRoot_DoesNotSwapToSceneAuthoredPlayerRoot`
    - `Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.CaptureOrAdoptPlayerRootForScene_WithoutCanonicalRuntimePlayerRoot_FailsClosedInsteadOfAdoptingScenePlayerRoot`
    - `Reloader.World.Tests.EditMode.WorldPlayerRootContractEditModeTests.Scene_DoesNotAuthorPlayerRoot("Assets/_Project/World/Scenes/MainTown.unity")`
    - `Reloader.World.Tests.EditMode.WorldPlayerRootContractEditModeTests.Scene_DoesNotAuthorPlayerRoot("Assets/_Project/World/Scenes/IndoorRangeInstance.unity")`
  - Interpretation: the suite is now red on scene-authored player roots/look controllers and `PersistentPlayerRoot` scene adoption, while the anchor-focused travel contract coverage is green.
- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests.*","Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.*"]`
  - Result: failed `4/7`, passed `3/7`
  - Failing tests:
    - `Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests.Scene_DoesNotAuthorPlayerLookController("Assets/_Project/World/Scenes/MainTown.unity")`
    - `Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests.Scene_DoesNotAuthorPlayerLookController("Assets/_Project/World/Scenes/IndoorRangeInstance.unity")`
    - `Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.CaptureOrAdoptPlayerRootForScene_WithCanonicalRuntimePlayerRoot_DoesNotSwapToSceneAuthoredPlayerRoot`
    - `Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests.CaptureOrAdoptPlayerRootForScene_WithoutCanonicalRuntimePlayerRoot_FailsClosedInsteadOfAdoptingScenePlayerRoot`
  - Interpretation: the narrow rerun stays red for the same Task 2 cutover gaps, so the Task 1 follow-up remains doc-only and deferred at the runtime seam.

## Commit / Push

- Commit SHA: `ff2c04f9` (`docs(world): update persistent player reset follow-up`)
- Push status: pushed to `origin/codex/weapon-pose-framework`

## Open Risks / Blockers

- The exact shell command from the plan is blocked while another Unity editor instance holds the project lock.
- `Reloader/Assets/_Project/Player/Tests/EditMode/PlayerLookConfigurationEditModeTests.cs` already had local worktree edits before this task; keep subsequent edits additive to the reset contract update.
- Runtime gaps exposed by the suite:
  - `MainTown` and `IndoorRangeInstance` still author `PlayerRoot` scene objects.
  - Those scene-authored player roots still own `PlayerLookController`.
  - `PersistentPlayerRoot.CaptureOrAdoptPlayerRootForScene(...)` still swaps to or adopts scene-authored player roots instead of failing closed around the canonical runtime owner.

## Review Findings

- Fixed by this follow-up: the progress tracker is being brought back in sync with the actual commit/push state.
- Deferred to Task 2: the `PlayerLookController` coverage gap stays on the canonical runtime-owned player prefab seam, which is not being widened in this slice.
- Deferred to Task 2: the remaining private-field setup in the targeted EditMode tests stays until the canonical public write seam exists for player-root seeding and look configuration.

## Deletion Log

- None in Task 1.
