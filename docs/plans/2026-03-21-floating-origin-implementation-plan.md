# Floating Origin Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a canonical floating-origin runtime that removes far-from-origin player/view jitter, establishes stable/local coordinate conversion seams, and prepares the runtime for rebasing-safe ELR ballistics and moving-target authority.

**Architecture:** The runtime will use a rebased local Unity scene for play/rendering and a canonical stable/local origin mapping owned by a dedicated world runtime seam. Slice 1 implements the rebase controller, coordinate conversion API, and explicit rebase participation without yet migrating projectile or NPC target authority into stable simulation.

**Tech Stack:** Unity 6 C#, prefab-authored `PlayerRoot`, `PersistentPlayerRoot`, `BootstrapWorldRoot`, targeted EditMode and PlayMode tests, scene/prefab contract checks.

---

### Task 1: Freeze The Floating-Origin Contract In Tests

**Files:**
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/DynamicOriginRebaseControllerEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/Player/Tests/EditMode/PlayerCameraDefaultsEditModeTests.cs`

**Step 1: Write the failing tests**

Add tests that require:

- default rebase threshold to be `500m`
- rebase distance to be measured from the canonical runtime player root in horizontal space only
- rebasing to use one cooldown-backed canonical trigger instead of ADS-specific rules
- persistent player root identity to survive rebasing
- travel/player seams to expose explicit rebase-safe ownership with no scene adoption fallback

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests|Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests|Reloader.Player.Tests.EditMode.PlayerCameraDefaultsEditModeTests" "$(pwd)/tmp/floating-origin-task1-red.xml" "$(pwd)/tmp/floating-origin-task1-red.log"
```

Expected: FAIL because no dynamic origin rebase seam exists yet.

**Step 3: Tighten the tests until they only describe the approved design**

- keep the tests focused on contract, not implementation detail
- do not add any temporary near-zero or ADS-rebase assertions

**Step 4: Re-run the same suite**

Expected: failures should now point at the missing runtime seams only.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Tests/EditMode/DynamicOriginRebaseControllerEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/PersistentPlayerRootEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs Reloader/Assets/_Project/Player/Tests/EditMode/PlayerCameraDefaultsEditModeTests.cs
git commit -m "test(world): lock floating origin slice one contract"
```

### Task 2: Add Stable/Local Coordinate Runtime State

**Files:**
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Origin/DynamicOriginRebaseState.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Origin/StableWorldCoordinateBridge.cs`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/StableWorldCoordinateBridgeEditModeTests.cs`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/BootstrapWorldRootEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs`

**Step 1: Write the failing bridge tests**

Add tests for:

- `LocalToStable`
- `StableToLocal`
- horizontal distance calculation from the player root
- round-trip conversion staying within tolerance
- offset updates after rebase preserving stable truth
- `BootstrapWorldRoot.Initialize()` creating exactly one canonical origin-state / bridge / controller seam and reusing it across repeated initialization
- travel preparation and entry placement preserving or explicitly updating the active stable/local mapping instead of silently recreating or zeroing it

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.StableWorldCoordinateBridgeEditModeTests|Reloader.World.Tests.EditMode.BootstrapWorldRootEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests|Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests" "$(pwd)/tmp/floating-origin-task2-red.xml" "$(pwd)/tmp/floating-origin-task2-red.log"
```

Expected: FAIL because no stable/local bridge exists yet.

**Step 3: Implement the minimal runtime bridge**

Create:

- `DynamicOriginRebaseState` to store:
  - current stable origin offset
  - current local origin offset
  - last rebase time
- `StableWorldCoordinateBridge` with:
  - `Vector3 LocalToStable(Vector3 localPosition)`
  - `Vector3 StableToLocal(Vector3 stablePosition)`
  - `Vector3 LocalDirectionToStable(Vector3 localDirection)`
  - `float ComputeHorizontalDistanceFromLocalOrigin(Vector3 localPosition)`

Update `BootstrapWorldRoot` to ensure one canonical instance exists at runtime.

Also require the bootstrap seam to fail closed when no valid bootstrap root can own the origin mapping, instead of inventing ad-hoc instances from fallback paths.

**Step 4: Re-run the suite**

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Scripts/Runtime/Origin/DynamicOriginRebaseState.cs Reloader/Assets/_Project/World/Scripts/Runtime/Origin/StableWorldCoordinateBridge.cs Reloader/Assets/_Project/World/Tests/EditMode/StableWorldCoordinateBridgeEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/BootstrapWorldRootEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs Reloader/Assets/_Project/World/Scripts/Runtime/BootstrapWorldRoot.cs
git commit -m "feat(world): add stable local coordinate bridge"
```

### Task 3: Add The Dynamic Rebase Controller And Participant Seam

**Files:**
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Origin/IOriginRebaseParticipant.cs`
- Create: `Reloader/Assets/_Project/World/Scripts/Runtime/Origin/DynamicOriginRebaseController.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/PersistentPlayerRoot.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/DynamicOriginRebaseControllerEditModeTests.cs`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/OriginRebaseParticipantEditModeTests.cs`

**Step 1: Write the failing controller tests**

Add tests for:

- default serialized values:
  - `RebaseDistanceMeters = 500`
  - cooldown present and tunable
- rebase triggers once when horizontal player distance crosses threshold
- cooldown prevents immediate retrigger
- participants are notified exactly once per rebase
- player root ownership remains canonical
- rebasing happens as one coherent global local-scene shift with one `localShift` / `stableShift` pair per trigger
- `IOriginRebaseParticipant` remains notification-only and is not used as per-object catch-up or fallback reposition logic

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests|Reloader.World.Tests.EditMode.OriginRebaseParticipantEditModeTests|Reloader.World.Tests.EditMode.PersistentPlayerRootEditModeTests" "$(pwd)/tmp/floating-origin-task3-red.xml" "$(pwd)/tmp/floating-origin-task3-red.log"
```

Expected: FAIL because no controller or participant seam exists yet.

**Step 3: Implement the controller**

Create:

- `IOriginRebaseParticipant`
  - `void OnBeforeOriginRebase(Vector3 localShift, Vector3 stableShift)`
  - `void OnAfterOriginRebase(Vector3 localShift, Vector3 stableShift)`
- `DynamicOriginRebaseController`
  - serialized player root reference or resolve through `PersistentPlayerRoot`
  - serialized `RebaseDistanceMeters`
  - serialized `RebaseCooldownSeconds`
  - computes horizontal player distance
  - triggers one coherent local-scene shift when threshold is crossed

Make the controller own the single canonical rebase operation. Participants may react to that shift, but they must not become a hidden second path for moving scene objects independently.

Update `PersistentPlayerRoot` only as needed so the controller always resolves the canonical runtime player root, never a scene-owned substitute.

**Step 4: Re-run the suite**

Expected: PASS.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Scripts/Runtime/Origin/IOriginRebaseParticipant.cs Reloader/Assets/_Project/World/Scripts/Runtime/Origin/DynamicOriginRebaseController.cs Reloader/Assets/_Project/World/Scripts/Runtime/PersistentPlayerRoot.cs Reloader/Assets/_Project/World/Tests/EditMode/DynamicOriginRebaseControllerEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/OriginRebaseParticipantEditModeTests.cs
git commit -m "feat(world): add dynamic origin rebase controller"
```

### Task 4: Make Player, Cameras, And Travel Rebase-Safe

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerLookController.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerCameraDefaults.cs`
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs`
- Modify: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot.prefab`
- Modify: `Reloader/Assets/Scenes/Bootstrap.unity`
- Create: `Reloader/Assets/_Project/World/Tests/PlayMode/DynamicOriginRebasePlayModeTests.cs`

**Step 1: Write the failing continuity test**

Add a PlayMode test that:

- spawns/uses the canonical runtime player root
- moves the player beyond `500m`
- forces the rebase controller to tick
- verifies the relative offset from player to selected nearby prop/NPC/anchor remains unchanged
- verifies no duplicate player root is created
- performs `rebase -> travel -> verify -> travel back -> verify` so travel entry placement stays coherent with the active stable/local mapping
- verifies the camera/arms/viewmodel stack is still bound on the canonical runtime player after rebase and after each travel hop

**Step 2: Run the focused red suite**

Run:

```bash
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.DynamicOriginRebasePlayModeTests" "$(pwd)/tmp/floating-origin-task4-red.xml" "$(pwd)/tmp/floating-origin-task4-red.log"
```

Expected: FAIL because player/camera/travel seams are not yet rebase-aware.

**Step 3: Implement the minimal integrations**

- update player movement/look code only where world-position assumptions break under rebasing
- ensure camera defaults and runtime camera stack continue to resolve correctly after rebase
- add an explicit post-rebase validation/rebind path for the runtime camera/viewmodel references rather than assuming the existing travel-only repair path is sufficient
- wire the controller into `PlayerRoot.prefab` and `Bootstrap.unity`
- ensure travel keeps the canonical player root and origin bridge coherent after scene moves

**Step 4: Re-run the focused PlayMode test and the nearest EditMode suites**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests|Reloader.Player.Tests.EditMode.PlayerCameraDefaultsEditModeTests" "$(pwd)/tmp/floating-origin-task4-edit.xml" "$(pwd)/tmp/floating-origin-task4-edit.log"
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.DynamicOriginRebasePlayModeTests" "$(pwd)/tmp/floating-origin-task4-play.xml" "$(pwd)/tmp/floating-origin-task4-play.log"
```

Expected: PASS.

After the runtime suites pass, read back the mutated `PlayerRoot.prefab` and `Bootstrap.unity` wiring in Unity and verify the serialized reference chain for the runtime camera stack plus any new origin controller/bridge references. Do not rely on runtime green tests alone for prefab/scene authoring safety.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs Reloader/Assets/_Project/Player/Scripts/PlayerLookController.cs Reloader/Assets/_Project/Player/Scripts/PlayerCameraDefaults.cs Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs Reloader/Assets/_Project/Player/Prefabs/PlayerRoot.prefab Reloader/Assets/Scenes/Bootstrap.unity Reloader/Assets/_Project/World/Tests/PlayMode/DynamicOriginRebasePlayModeTests.cs
git commit -m "feat(player): make player and cameras rebase safe"
```

### Task 5: Lock The Slice-One Acceptance Surface

**Files:**
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/DynamicOriginRebaseControllerEditModeTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/PlayMode/DynamicOriginRebasePlayModeTests.cs`
- Create: `Reloader/Assets/_Project/World/Tests/EditMode/StableWorldSliceOneContractEditModeTests.cs`
- Modify: `docs/plans/2026-03-21-floating-origin-design.md`

**Step 1: Add the final contract tests**

Require:

- repeated rebases do not accumulate drift
- props/NPCs/anchors keep their relative offsets around the player
- one canonical rebase path exists
- no ADS-triggered rebase path exists
- stable/local coordinate bridge remains the only approved conversion seam

**Step 2: Run the final focused verification ladder**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.World.Tests.EditMode.StableWorldCoordinateBridgeEditModeTests|Reloader.World.Tests.EditMode.DynamicOriginRebaseControllerEditModeTests|Reloader.World.Tests.EditMode.OriginRebaseParticipantEditModeTests|Reloader.World.Tests.EditMode.WorldTravelCoordinatorEditModeTests|Reloader.World.Tests.EditMode.StableWorldSliceOneContractEditModeTests|Reloader.Player.Tests.EditMode.PlayerCameraDefaultsEditModeTests" "$(pwd)/tmp/floating-origin-final-edit.xml" "$(pwd)/tmp/floating-origin-final-edit.log"
bash scripts/run-unity-tests.sh playmode "Reloader.World.Tests.PlayMode.DynamicOriginRebasePlayModeTests" "$(pwd)/tmp/floating-origin-final-play.xml" "$(pwd)/tmp/floating-origin-final-play.log"
```

Expected: PASS.

**Step 3: Update the design doc if naming or seams changed during implementation**

- keep design and runtime naming aligned
- do not leave stale architecture names behind

**Step 4: Commit**

```bash
git add Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/DynamicOriginRebaseControllerEditModeTests.cs Reloader/Assets/_Project/World/Tests/PlayMode/DynamicOriginRebasePlayModeTests.cs Reloader/Assets/_Project/World/Tests/EditMode/StableWorldSliceOneContractEditModeTests.cs docs/plans/2026-03-21-floating-origin-design.md
git commit -m "test(world): lock floating origin slice one acceptance"
```

### Task 6: Queue The Next Slice Without Implementing It Here

**Files:**
- Create: `docs/plans/progress/2026-03-21-floating-origin-progress.md`
- Modify: `docs/plans/2026-03-21-floating-origin-design.md`

**Step 1: Write the follow-on queue**

Record the next-slice seams:

- stable projectile authority
- stable target authority for all shootable NPCs
- bullet-cam projection from stable projectile state
- ELR hit resolution using stable projectile state against stable target state

**Step 2: Commit**

```bash
git add docs/plans/progress/2026-03-21-floating-origin-progress.md docs/plans/2026-03-21-floating-origin-design.md
git commit -m "docs(world): queue floating origin follow-on slices"
```
