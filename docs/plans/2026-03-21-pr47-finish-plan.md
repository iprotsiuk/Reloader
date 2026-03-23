# PR 47 Finish Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Finish PR 47 by closing the remaining valid runtime-review issues and fixing rifle presentation ownership for HIP, ADS, and reload without reintroducing fallback behavior.

**Architecture:** Keep runtime integrity fixes narrow and explicit. For weapon presentation, use one animated presentation seam for HIP/reload motion and preserve `AdsPivot` + hand IK as the exact ADS alignment seam.

**Tech Stack:** Unity 6, C#, Animation Rigging, prefab-authored weapon view contracts, targeted EditMode/PlayMode tests, GitHub review-thread replies via `gh`.

---

### Task 1: Runtime Integrity Review Fixes

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scripts/Runtime/Travel/WorldTravelCoordinator.cs`
- Modify: `Reloader/Assets/_Project/Reloading/Scripts/World/TemporaryReloadingSuppliesSpawner.cs`
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/WorldTravelCoordinatorEditModeTests.cs`
- Test: `Reloader/Assets/_Project/Reloading/Tests/**` or nearest new targeted test file if coverage is missing

**Step 1: Write failing tests**
- Add a test proving a second travel request while one is pending fails closed without overwriting the first request.
- Add a test proving the temporary reloading supplies spawner retries player-root resolution after startup ordering delays instead of giving up forever.

**Step 2: Run targeted tests to verify RED**
- Run the smallest test filters that cover the new assertions.
- Confirm failures reflect missing protections/retry behavior, not test setup mistakes.

**Step 3: Write minimal implementation**
- In `WorldTravelCoordinator`, reject overlapping travel requests early with an explicit warning and preserve the original pending request.
- In `TemporaryReloadingSuppliesSpawner`, add an explicit retry seam tied to later lifecycle events or a bounded polling path until the player root exists, then unsubscribe/stop.

**Step 4: Run targeted tests to verify GREEN**
- Re-run the same focused test filters and confirm they pass.

**Step 5: Commit**
- Commit only the runtime-integrity slice once green.

### Task 2: Weapon Presentation Ownership

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponHandRigController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab`
- Modify: `Reloader/Assets/_Project/Player/Resources/Viewmodels/Characters/FPS_Arms.fbx.meta` only if required for animation hookup
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`
- Test: nearest targeted weapon presentation/runtime tests or add a new local test file under `Reloader/Assets/_Project/Weapons/Tests/PlayMode/`

**Step 1: Write failing tests**
- Add/extend a test proving HIP locomotion moves the rifle through the presentation mount rather than leaving it static while only hands animate.
- Add/extend a test proving reload presentation moves the rifle as part of the same animated seam.
- Add/extend a test proving ADS still keeps weapon alignment authoritative while hands follow the weapon.
- Add/extend a test proving runtime IK rebinding rebuilds the rig graph if needed.

**Step 2: Run targeted tests to verify RED**
- Run only the affected weapon/runtime filters.
- Confirm the failures reflect current static-rifle HIP/reload ownership and any rig-graph seam.

**Step 3: Write minimal implementation**
- Introduce or reuse a viewmodel-animated rifle presentation mount for HIP/reload motion above `AdsPivot`.
- Keep `AdsPivot` and `WeaponAimAligner` as the exact ADS alignment seam.
- Keep hand IK one-way (`weapon -> hands`) and rebuild the rig graph after runtime rebinding if required by the current Unity rig lifecycle.
- Blend ownership smoothly by ADS state; do not reparent the rifle back and forth and do not add fallback paths.

**Step 4: Run targeted tests to verify GREEN**
- Re-run only the local weapon presentation filters and nearest scope/ADS regression coverage.

**Step 5: Commit**
- Commit only the weapon-presentation slice once green.

### Task 3: Review Thread Closure and Verification

**Files:**
- Modify only if required by verified review fixes from Tasks 1-2

**Step 1: Verify non-code review items**
- Re-check the unresolved PR 47 review comments on `StartupMenuFlow.cs` and the player-root fallback thread in `WorldTravelCoordinator.cs`.
- Prepare concise technical replies for items intentionally not implemented in this slice.

**Step 2: Run final targeted verification**
- Re-run the exact runtime-integrity and weapon-presentation test filters touched by this plan.
- Widen only if failures prove coupling.

**Step 3: Reply/resolve GitHub threads**
- Reply in-thread with the technical rationale or fix summary.
- Resolve the threads that are fixed or intentionally answered by contract.

**Step 4: Commit if needed**
- If review-thread replies required a small follow-up code change, make one final commit; otherwise stop at the two slice commits.
