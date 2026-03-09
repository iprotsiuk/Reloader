# Prod-Ready Scoped ADS Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate the scoped PiP optics path to a reusable production contract with explicit authored `SightAnchor`, real eye relief, strict no-fallback mount validation, scope-camera exclusion of `Viewmodel`, and a data model that remains compatible with persistent scope zeroing later.

**Architecture:** `PlayerWeaponController` becomes the runtime assembler for scoped ADS by wiring `AttachmentManager`, `AdsStateController`, `WeaponAimAligner`, `RenderTextureScopeController`, and `ScopeCamera` against the explicit `WeaponViewAttachmentMounts` contract. `WeaponViewPoseTuningHelper` remains coarse presentation tuning, while final scoped eye alignment is camera-authoritative through `WeaponAimAligner`; PiP optics must satisfy strict prefab contracts and fail loudly when misconfigured.

**Tech Stack:** Unity 6.3, C#, URP, Unity PlayMode tests, asset-backed prefab/data authoring under `Reloader/Assets/Game/Weapons/**` and `Reloader/Assets/_Project/Weapons/**`

---

### Task 1: Lock The Runtime Contract With Failing Tests

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write the failing tests**

Add PlayMode coverage for:
- PiP optics without authored `SightAnchor` are rejected loudly and do not synthesize a root anchor
- spawned runtime weapon views and mounted scoped optics render on `Viewmodel`
- runtime `ScopeCamera` excludes `Viewmodel`
- real Kar98k optic resolves a non-root authored `SightAnchor`
- `PlayerWeaponController` wires `WeaponAimAligner` for PiP optics

Representative test names:

```csharp
[UnityTest]
public IEnumerator EquipOptic_PipOpticWithoutSightAnchor_FailsWithoutSyntheticFallback()

[UnityTest]
public IEnumerator EquipScopedWeapon_RuntimeView_UsesViewmodelLayerForMountedOptic()

[UnityTest]
public IEnumerator EquipScopedWeapon_RuntimeBridge_ScopeCameraExcludesViewmodelLayer()

[UnityTest]
public IEnumerator EquipOptic_RealKar98kAsset_UsesNonRootAuthoredSightAnchor()

[UnityTest]
public IEnumerator EquipScopedWeapon_RuntimeBridge_WiresWeaponAimAligner()
```

**Step 2: Run tests to verify they fail**

Run targeted tests in Unity PlayMode and confirm failure for the current behavior.

Suggested command path:

```bash
/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath "/Users/ivanprotsiuk/unity/Reloader/Reloader" \
  -runTests \
  -testPlatform PlayMode \
  -testFilter "Reloader.Weapons.Tests.PlayMode.ScopeAttachmentAdsIntegrationPlayModeTests|Reloader.Weapons.Tests.PlayMode.PlayerWeaponControllerPlayModeTests" \
  -logFile /tmp/prod_scoped_ads_task1.log \
  -quit
```

Expected: failing assertions around synthetic-anchor fallback, missing aim-aligner wiring, or camera/layer contract gaps.

**Step 3: Commit the failing test slice**

```bash
git add \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "test: lock prod scoped ads runtime contract"
```

### Task 2: Enforce Strict Authored Anchor And Scoped Runtime Wiring

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/WeaponAimAligner.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponViewAttachmentMounts.cs` (only if a small API addition is needed; avoid unnecessary contract churn)

**Step 1: Implement the minimal runtime changes**

Required behavior:
- remove synthetic `SightAnchor` creation for scoped optics
- make scoped optic mount fail loudly when `SightAnchor` is missing
- wire `WeaponAimAligner` from `PlayerWeaponController.EnsureScopedAdsRuntimeBridge()`
- source `AdsPivot` from the runtime weapon view explicitly
- stop relying on transform-name fallbacks where explicit `WeaponViewAttachmentMounts` data is available

Implementation notes:
- `AttachmentManager` should use only explicit/authored optic anchors for scoped optics
- `PlayerWeaponController` should become the single runtime assembler for `AdsStateController`, `AttachmentManager`, `WeaponAimAligner`, `RenderTextureScopeController`, and `PeripheralScopeEffects`
- do not introduce new fallback paths to preserve playability

**Step 2: Run the targeted tests**

Run the new failing tests from Task 1 until green.

**Step 3: Commit**

```bash
git add \
  Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs \
  Reloader/Assets/Game/Weapons/Runtime/WeaponAimAligner.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponViewAttachmentMounts.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "feat: enforce authored scoped alignment contract"
```

### Task 3: Enforce Viewmodel Layer And Scope-Camera Exclusion

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerCameraDefaults.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write or extend the failing tests if needed**

If Task 1 did not already add exact layer/culling assertions, add them now:
- spawned weapon view root is moved to `Viewmodel`
- mounted optic/lens/reticle renderers inherit `Viewmodel`
- `ScopeCamera.cullingMask` excludes `Viewmodel`
- pulling the rifle close cannot black out the PiP image by rendering the scope body into the scope camera

**Step 2: Implement the minimal code**

Required behavior:
- normalize spawned runtime weapon view and mounted scoped attachments onto `Viewmodel`
- centralize `ScopeCamera` invariants so it always excludes `Viewmodel`
- keep world/viewmodel/scope camera separation explicit and deterministic

Implementation note:
- prefer a single helper for resolving the `Viewmodel` layer and applying it recursively to spawned runtime view content

**Step 3: Run targeted tests**

Run only the player/runtime optics tests first, then the full scoped suite.

**Step 4: Commit**

```bash
git add \
  Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs \
  Reloader/Assets/_Project/Player/Scripts/PlayerCameraDefaults.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "fix: separate scoped pip rendering from viewmodel"
```

### Task 4: Convert Kar98k To The Full Production Authoring Contract

**Files:**
- Modify: `Reloader/Assets/Low Poly Weapon Pack 4_WWII_1/Prefabs/Attachments/WWII_Optic_Remote_Range_A.prefab`
- Modify: `Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset`
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Author the prefab/content changes**

Required content changes:
- add a real authored `SightAnchor` to the optic prefab at the eye box
- keep `ScopeLensDisplay` on the eyepiece display surface
- keep reticle wiring explicit
- tune `eyeReliefBackOffset` on the optic definition
- tune Kar98k scoped coarse pose on `WeaponViewPoseTuningHelper` for MainTown/PlayerRoot so the aligner has a sane starting pose

**Step 2: Run the Kar98k-focused PlayMode tests**

Run:
- real-asset scope mount test
- PiP lens binding test
- aim-aligner wiring test
- viewmodel/scope-camera separation tests

**Step 3: Perform live MainTown verification**

Manual/live-editor checks:
- equip Kar98k and remote scope
- ADS while scoped
- move rifle close via authored override
- confirm PiP image stays visible
- confirm eye relief feels correct
- confirm scope body does not black out the scope image

**Step 4: Commit**

```bash
git add \
  "Reloader/Assets/Low Poly Weapon Pack 4_WWII_1/Prefabs/Attachments/WWII_Optic_Remote_Range_A.prefab" \
  Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset \
  Reloader/Assets/_Project/World/Scenes/MainTown.unity \
  Reloader/Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "feat: author kar98k for prod scoped ads"
```

### Task 5: Preserve Future Zeroing As A First-Class Extension Point

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs` (only if a small forward-compatible metadata hook is needed)
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs` (only if future state seams need comments/helpers)
- Modify: `docs/design/ads-optics-framework.md` (only if implementation clarifies contract)
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponScopeRuntimeStatePlayModeTests.cs`

**Step 1: Add or confirm forward-compatible optic-state seams**

Goal:
- do not fully implement zeroing UI yet unless explicitly requested
- make sure the runtime architecture does not block persistent optic adjustments later
- keep zeroing as optic state, not scene pose or camera fudge

Minimal acceptable output:
- explicit place for optic runtime state to own `windage` / `elevation`
- explicit comment/test guard that scoped alignment/rendering path remains compatible with saved optic adjustments

**Step 2: Add a regression test**

Example:

```csharp
[Test]
public void ScopedOpticRuntimeState_FutureZeroingBelongsToOpticState_NotPoseOffsets()
```

This can be a narrow state-contract test if no runtime code changes are required yet.

**Step 3: Run tests**

Run the relevant optics/runtime-state tests and confirm the seam is protected.

**Step 4: Commit**

```bash
git add \
  Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponScopeRuntimeStatePlayModeTests.cs \
  docs/design/ads-optics-framework.md
git commit -m "test: reserve persistent zeroing seam for optics"
```

### Task 6: Full Verification And Review

**Files:**
- Verify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`
- Verify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`
- Verify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponScopeRuntimeStatePlayModeTests.cs`

**Step 1: Run the focused verification set**

Run the optics/player/runtime-state suites in PlayMode and capture logs/results.

**Step 2: Review branch delta**

Use git diff and request code review on the completed slice before merge.

**Step 3: Final commit if needed**

Only if verification or review requires a small final fix.

