# Floating Origin Progress

## Batch Scope

- Tasks 1-3 only.
- Owned paths only under `World/Scripts/Runtime/Origin`, `BootstrapWorldRoot`, `PersistentPlayerRoot`, world EditMode floating-origin tests, and this progress note.
- Left unrelated dirty file `Reloader/Assets/_Project/World/Terrain/MainTown/MainTownTerrainData.asset` untouched.
- Did not edit non-world player tests despite the original plan mentioning one; current ownership rules are stricter.

## Task 1

- Added world-owned red tests for the floating-origin runtime type contract and destination-scene ownership guardrail.
- Ran the plan filter once. XML showed the intended missing world-origin seam failures plus one unrelated pre-existing player test failure in `Reloader.Player.Tests.EditMode.PlayerCameraDefaultsEditModeTests.ApplyDefaults_PushesAuthoredClipPlanesIntoCinemachineLens`, which is outside the owned scope.
- Next command: `bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests|Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests" "$(pwd)/tmp/floating-origin-task1-world-red.xml" "$(pwd)/tmp/floating-origin-task1-world-red.log"`

## Task 2

- Added red coverage for stable/local conversion, bootstrap-owned origin seams, deterministic seam reset, fail-closed bootstrap initialization, and travel preserving the active mapping.
- Implemented the minimal stable/local slice: `DynamicOriginRebaseState`, `StableWorldCoordinateBridge`, bootstrap-owned `DynamicOriginRebaseController` seam, and fail-closed/reset wiring in `BootstrapWorldRoot`.
- Red evidence: `tmp/floating-origin-task2-red.xml` failed only on missing origin types and bootstrap fail-closed ownership.
- Green command: `bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.StableWorldCoordinateBridgeEditModeTests|Reloader.World.Tests.EditMode.BootstrapWorldRootEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests|Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests" "$(pwd)/tmp/floating-origin-task2-green.xml" "$(pwd)/tmp/floating-origin-task2-green.log"`
- Green evidence: `tmp/floating-origin-task2-green.xml` reports `passed="14" failed="0"` and the log exits with `code 0 (Ok)`.
- Remaining out-of-scope workspace dirt: `Reloader/Assets/_Project/World/Terrain/MainTown/MainTownTerrainData.asset` and earlier non-Task-2 edit in `WorldPlayerRootContractEditModeTests.cs`, both left unstaged for this commit.

## Task 3

- Added focused controller/participant tests for horizontal threshold, cooldown, coherent scene shift, canonical player identity, and notification-only participant callbacks.
- Next command: `bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests|Reloader.World.Tests.EditMode.OriginRebaseParticipantEditModeTests|Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests" "$(pwd)/tmp/floating-origin-task3-red.xml" "$(pwd)/tmp/floating-origin-task3-red.log"`
- Red evidence: `tmp/floating-origin-task3-red.log` stopped at local compile errors because `DynamicOriginRebaseController.TryRebaseIfNeeded` did not exist yet.
- Implemented the canonical controller rebase path and notification-only participant callbacks in `Origin/DynamicOriginRebaseController.cs`.
- Green command: `bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests|Reloader.World.Tests.EditMode.OriginRebaseParticipantEditModeTests|Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests" "$(pwd)/tmp/floating-origin-task3-green.xml" "$(pwd)/tmp/floating-origin-task3-green.log"`
- Green evidence: `tmp/floating-origin-task3-green.xml` reports `passed="13" failed="0"` and the log exits with `code 0 (Ok)`.
- Remaining out-of-scope workspace dirt still left unstaged: `Reloader/Assets/_Project/World/Terrain/MainTown/MainTownTerrainData.asset` and earlier `WorldPlayerRootContractEditModeTests.cs` change.
