# Persistent Player Reset Progress

## Scope

- Plan: `docs/plans/2026-03-20-persistent-player-reset-implementation-plan.md`
- Current slice: Task 3
- Status: Follow-up verification complete; commit/push pending

## Task Checklist

- [x] Review Task 3 plan requirements, local agent guidance, and the in-progress scene/player cutover worktree state
- [x] Write the failing anchor-contract and validator coverage for entry-point anchor ownership
- [x] Record the focused red verification for anchor-only scenes and validator fail-closed behavior
- [x] Implement the anchor-only scene/validator contract and fix authored entry-point anchor wiring
- [x] Re-run the focused Task 3 suite to green
- [x] Widen to the nearest player/world editmode tests that still encoded scene-owned player assumptions
- [x] Commit and push the scoped Task 3 changes
- [x] Address Task 3 follow-up review holes in validator/contract coverage
- [ ] Commit and push the scoped Task 3 follow-up changes

## Changed Files

- `docs/plans/progress/2026-03-20-persistent-player-reset-progress.md`
- `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/PlayerSpawnAnchor.cs`
- `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/PlayerSpawnAnchorKind.cs`
- `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/SceneEntryPoint.cs`
- `Reloader/Assets/_Project/World/Editor/WorldSceneContractValidator.cs`
- `Reloader/Assets/_Project/World/Data/SceneContracts/MainTownWorldSceneContract.asset`
- `Reloader/Assets/_Project/World/Data/SceneContracts/IndoorRangeInstanceWorldSceneContract.asset`
- `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- `Reloader/Assets/_Project/World/Scenes/IndoorRangeInstance.unity`
- `Reloader/Assets/_Project/World/Tests/EditMode/PlayerSpawnAnchorEditModeTests.cs`
- `Reloader/Assets/_Project/World/Tests/EditMode/WorldScenePlayerAnchorContractEditModeTests.cs`
- `Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs`
- `Reloader/Assets/_Project/World/Tests/EditMode/MainTownPeripheralScopeWiringEditModeTests.cs`
- `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/PlayerSpawnAnchor.cs.meta`
- `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/PlayerSpawnAnchorKind.cs.meta`
- `Reloader/Assets/_Project/World/Tests/EditMode/PlayerSpawnAnchorEditModeTests.cs.meta`
- `Reloader/Assets/_Project/World/Tests/EditMode/WorldScenePlayerAnchorContractEditModeTests.cs.meta`
- Follow-up scope:
  - `Reloader/Assets/_Project/World/Editor/WorldSceneContractValidator.cs`
  - `Reloader/Assets/_Project/World/Data/SceneContracts/MainTownWorldSceneContract.asset`
  - `Reloader/Assets/_Project/World/Tests/EditMode/WorldScenePlayerAnchorContractEditModeTests.cs`
  - `docs/plans/progress/2026-03-20-persistent-player-reset-progress.md`

## Verification Log

- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.PlayerSpawnAnchorEditModeTests.*","Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.*","Reloader.World.Tests.EditMode.TravelContextEditModeTests.*"]`
  - Result: failed `5/27`, passed `22/27`
  - Failing tests:
    - `Reloader.World.Tests.EditMode.PlayerSpawnAnchorEditModeTests.SceneEntryPoint_EnsureSpawnAnchorContract_ReplacesForeignAnchorReferenceWithSiblingAnchor`
    - `Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.MainTownScene_UsesExplicitPlayerSpawnAnchors_AndNoSceneOwnedPlayerRoot`
    - `Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.Validator_RejectsSceneEntryPointWhenAnchorKindDoesNotMatch`
    - `Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.Validator_RejectsSceneEntryPointWithoutExplicitSpawnAnchor`
    - `Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.Validator_RejectsSceneOwnedPlayerImplementationComponents_WhenPlayerRootIsRenamed`
  - Interpretation: `SceneEntryPoint` did not re-bind to the sibling anchor, `MainTown` still serialized its return entry-point kind incorrectly, and the validator still ignored missing/mismatched anchor wiring plus renamed scene-owned player components.
- Unity MCP `refresh_unity`
  - Parameters: `compile=request`, `wait_for_ready=true`
  - Result: refresh and compile requested before the narrowed re-run.
- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.PlayerSpawnAnchorEditModeTests.SceneEntryPoint_EnsureSpawnAnchorContract_ReplacesForeignAnchorReferenceWithSiblingAnchor","Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.Validator_RejectsSceneEntryPointWithoutExplicitSpawnAnchor","Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.Validator_RejectsSceneEntryPointWhenAnchorKindDoesNotMatch","Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.Validator_RejectsSceneOwnedPlayerImplementationComponents_WhenPlayerRootIsRenamed"]`
  - Result: passed `4/4`
  - Interpretation: the repaired `SceneEntryPoint`/validator seams now fail closed for foreign anchor refs, missing anchors, mismatched anchor kinds, and renamed scene-owned player components.
- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.PlayerSpawnAnchorEditModeTests.*","Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.*","Reloader.World.Tests.EditMode.TravelContextEditModeTests.*"]`
  - Result: passed `27/27`
  - Interpretation: the focused Task 3 anchor-only scene contract is green.
- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests.*","Reloader.World.Tests.EditMode.MainTownPeripheralScopeWiringEditModeTests.*","Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests.*"]`
  - Result: failed `2/16`, passed `14/16`
  - Failing tests:
    - `Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests.MainTownScene_PlayerRoot_UsesWeaponHandRigController_AndNoSceneOwnedPoseHelper`
    - `Reloader.World.Tests.EditMode.MainTownPeripheralScopeWiringEditModeTests.MainTownScene_PlayerRoot_WiresPeripheralScopeEffectsToScreenMask`
  - Interpretation: the nearest tests still codified the removed scene-owned `PlayerRoot` and `PlayerRoot_MainTown.prefab` assumptions.
- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests.*","Reloader.World.Tests.EditMode.MainTownPeripheralScopeWiringEditModeTests.*","Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests.*"]`
  - Result: passed `14/14`
  - Interpretation: the adjacent wiring tests now assert the canonical runtime `PlayerRoot.prefab` contracts instead of scene-owned player content.
- Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.PlayerSpawnAnchorEditModeTests.*","Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.*","Reloader.World.Tests.EditMode.TravelContextEditModeTests.*","Reloader.World.Tests.EditMode.WorldSceneContractValidatorEditModeTests.*","Reloader.World.Tests.EditMode.WorldPlayerRootContractEditModeTests.*","Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests.*","Reloader.World.Tests.EditMode.MainTownPeripheralScopeWiringEditModeTests.*","Reloader.Player.Tests.EditMode.PlayerLookConfigurationEditModeTests.*"]`
  - Result: passed `48/48`
  - Interpretation: the Task 3 seam plus adjacent player-root/validator/wiring coverage is green together.
- Task 3 follow-up red slice:
  - Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.DefaultSceneContracts_RequirePlayerAnchors_AndNoPlayerRoot","Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.Validator_RejectsCanonicalPlayerImplementationComponent_OmittedFromLegacySubset"]`
  - Result: failed `2/2`
  - Interpretation: `MainTownEntry_Return` was still not required as a `SceneEntryPoint` component contract in the asset, and the validator still missed canonical runtime-player components outside the original handwritten subset.
- Task 3 follow-up green slice:
  - Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.DefaultSceneContracts_RequirePlayerAnchors_AndNoPlayerRoot","Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.Validator_RejectsCanonicalPlayerImplementationComponent_OmittedFromLegacySubset","Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.Validator_RejectsSceneOwnedPlayerImplementationComponents_WhenPlayerRootIsRenamed"]`
  - Result: passed `3/3`
  - Interpretation: the `MainTown` contract now binds the authored return object to a `SceneEntryPoint` contract, and the validator now derives forbidden custom player-implementation types from the canonical `PlayerRoot.prefab` surface instead of a partial denylist.
- Task 3 follow-up file green:
  - Unity MCP `run_tests`
  - Mode: `EditMode`
  - Filter: `group_names=["Reloader.World.Tests.EditMode.WorldScenePlayerAnchorContractEditModeTests.*"]`
  - Result: passed `8/8`
  - Interpretation: the strengthened contract asset and validator behavior are green together inside the narrow world-scene player-anchor suite.

## Commit / Push

- Task 3 commit SHA: `d12da6fee6f7f72322d71556382daa6739ffc22c`
- Task 3 push status: pushed to `origin/codex/weapon-pose-framework`
- Task 3 follow-up commit SHA: pending
- Task 3 follow-up push status: pending

## Open Risks / Blockers

- The exact shell command from the plan was not used in this slice; verification ran through the active Unity MCP editor instance so the scene/prefab changes could be validated without fighting project-lock/editor-state churn.
- No Task 3 blockers remain. Later tasks still need the save/respawn/runtime restore path that consumes the new respawn anchors.

## Review Findings

- Fixed in Task 3: `MainTown` and `IndoorRangeInstance` now serialize only anchors/entry points for player placement and no longer author scene-owned `PlayerRoot` content.
- Fixed in Task 3: `SceneEntryPoint` now re-binds to the sibling `PlayerSpawnAnchor` instead of preserving stale foreign references.
- Fixed in Task 3: `WorldSceneContractValidator` now rejects missing/mismatched entry-point anchor contracts and renamed scene-owned player implementation components, not just a root object literally named `PlayerRoot`.
- Fixed in Task 3 follow-up: the nearest player/world editmode tests now assert the canonical runtime `PlayerRoot.prefab` ownership contracts instead of stale `PlayerRoot_MainTown` / scene-owned player assumptions.
- Fixed in Task 3 follow-up: `MainTownEntry_Return` is now required by the contract asset as an authored `SceneEntryPoint` with an explicit `_playerSpawnAnchor` reference instead of relying only on object-path presence and the global entry-id list.
- Fixed in Task 3 follow-up: the validator now derives forbidden custom runtime-player implementation component types from the canonical `PlayerRoot.prefab` serialization surface, so omitted canonical components such as `PlayerShopVendorController` fail validation too.

## Deletion Log

- Deleted from scene content in this slice: authored `PlayerRoot` implementations and their scene-local first-person/combat/player-controller wiring from `MainTown` and `IndoorRangeInstance`.
- Deleted from local test assumptions in this slice: `MainTown` scene-owned `PlayerRoot` expectations and `PlayerRoot_MainTown.prefab` assertions in the nearest world wiring suites.
