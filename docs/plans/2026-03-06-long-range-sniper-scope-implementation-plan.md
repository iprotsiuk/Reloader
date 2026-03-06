# Long-Range Sniper Scope Framework Implementation Plan

> **For Implementers:** Execute this as a subsystem plan under the assassination-contract sandbox roadmap.

**Goal:** Ship the first production long-range magnified scope as a fixed `10x` Kar98k PiP optic with optic-instance click-based zeroing and a runtime contract that extends cleanly to future `5-25x FFP` scopes.

**Architecture:** Keep the existing camera-authoritative magnified-scope runtime under `Reloader/Assets/Game/Weapons/**` and extend it instead of creating a separate fixed-scope system. The first slice formalizes authored fixed-power scope data, optic-instance zero state, reticle/turret unit consistency, and a scope-only long-range observation seam while preserving the current authored-anchor + `AdsPivot` + `WeaponAimAligner` contract.

**Tech Stack:** Unity 6, C#, URP, ScriptableObject-authored weapon/optic data, PlayMode/EditMode tests under Unity Test Framework.

**Roadmap Placement:** This plan is for a premium long-range contract subsystem inside the assassination-sandbox pivot. It is not a replacement for the broader contract/police/world roadmap.

---

### Task 1: Lock the authored fixed-10x Kar98k scope contract

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Write the failing tests**

Add focused tests that assert:

- the real Kar98k optic asset is explicit fixed `10x`
- the optic does not expose variable zoom
- the optic remains `RenderTexturePiP`

Suggested test names:

- `RealKar98kOpticAsset_IsFixedTenPowerLegacyScope`
- `RealKar98kOpticAsset_DisablesVariableZoom`

**Step 2: Run the tests to verify they fail**

Run targeted Unity tests for the new asset-contract coverage.

Expected: failure because the asset/schema does not yet fully express fixed-power legacy scope semantics.

**Step 3: Write the minimal implementation**

Extend `OpticDefinition` with explicit authored fields for:

- scope family
- angular unit
- reticle focal plane
- fixed magnification where applicable
- zeroing support metadata

Update `Kar98kScopeRemoteA.asset` to:

- fixed `10x`
- no variable zoom
- legacy fixed-power family
- explicit angular unit and reticle metadata

**Step 4: Run the tests to verify they pass**

Re-run only the new optic-asset tests plus the existing real-Kar98k mount/asset tests.

**Step 5: Commit**

```bash
git add Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs \
        Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset \
        Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "feat: author kar98k as fixed 10x scope"
```

### Task 2: Add optic-instance click-based zero state

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponScopeRuntimeState.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponScopeRuntimeStatePlayModeTests.cs`

**Step 1: Write the failing tests**

Add tests that assert:

- scope runtime state stores `elevationClicks` and `windageClicks` as integer counts
- click counts persist through runtime state snapshots
- fixed-power scope runtime still exposes stable magnified state with no zoom drift

Suggested test names:

- `WeaponScopeRuntimeState_PreservesElevationAndWindageClickCounts`
- `WeaponScopeRuntimeState_FixedPowerScope_HasStableMagnificationState`

**Step 2: Run the tests to verify they fail**

Run only the new scope-runtime tests.

Expected: failure because click-based state is not fully modeled yet.

**Step 3: Write the minimal implementation**

Extend the runtime scope state to hold:

- optic instance id / key
- fixed vs variable magnification mode
- current magnification
- elevation clicks
- windage clicks

Keep click counts as integers. Do not store derived float offsets as canonical data.

**Step 4: Run the tests to verify they pass**

Re-run the targeted state tests and existing `WeaponScopeRuntimeStatePlayModeTests`.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponScopeRuntimeState.cs \
        Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs \
        Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs \
        Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponScopeRuntimeStatePlayModeTests.cs
git commit -m "feat: store click-based optic zero state"
```

### Task 3: Make reticle/turret units explicit and consistent

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/Runtime/ScopeReticleController.cs`
- Modify: `Reloader/Assets/Game/Weapons/WeaponDefinitions/ScopeReticleDefinition.cs`
- Modify: `Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Write the failing tests**

Add tests that assert:

- a `MOA` optic reports `MOA` reticle subtensions and `0.25 MOA` click steps
- a `MRAD` optic reports `MRAD` reticle subtensions and `0.1 mil` click steps
- the reticle controller consumes the authored unit metadata without inventing conversions implicitly

Suggested test names:

- `ScopeReticle_MoaOptic_UsesQuarterMoaClickContract`
- `ScopeReticle_MradOptic_UsesTenthMilClickContract`

**Step 2: Run the tests to verify they fail**

Expected: failure because unit metadata is not yet wired end-to-end.

**Step 3: Write the minimal implementation**

Introduce explicit enums/fields for:

- angular unit
- reticle focal plane
- click step size

Ensure `ScopeReticleController` reads the authored unit contract instead of assuming scale behavior from magnification alone.

**Step 4: Run the tests to verify they pass**

Re-run the new reticle/unit tests and the existing reticle behavior suite.

**Step 5: Commit**

```bash
git add Reloader/Assets/Game/Weapons/Runtime/ScopeReticleController.cs \
        Reloader/Assets/Game/Weapons/WeaponDefinitions/ScopeReticleDefinition.cs \
        Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs \
        Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "feat: add unit-correct reticle and turret contracts"
```

### Task 4: Apply zeroing to aiming reference, not projectile physics

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/ScopeReticleController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/WeaponAimAligner.cs` only if required for data flow, not for zero math
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write the failing tests**

Add tests that assert:

- changing elevation/windage clicks changes the optic aiming reference state
- camera-authoritative alignment stays intact
- weapon pose / `AdsPivot` are not mutated as a side effect of zeroing

Suggested test names:

- `ScopedOptic_ZeroClicks_DoNotMoveAdsPivot`
- `ScopedOptic_ZeroClicks_UpdateReticleAimReference`

**Step 2: Run the tests to verify they fail**

Expected: failure because zeroing is not yet applied through a real aiming-reference path.

**Step 3: Write the minimal implementation**

Add an explicit angular-offset calculation path that:

- derives angular offsets from integer clicks
- feeds reticle/aim-reference logic
- does not move the camera or authored rig

Do not touch projectile physics code in this task.

**Step 4: Run the tests to verify they pass**

Re-run the new zeroing tests plus the existing scoped alignment tests.

**Step 5: Commit**

```bash
git add Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs \
        Reloader/Assets/Game/Weapons/Runtime/ScopeReticleController.cs \
        Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "feat: route optic zeroing through aiming reference"
```

### Task 5: Formalize fixed-power magnified scope behavior in ADS runtime

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Write the failing tests**

Add tests that assert:

- fixed `10x` scopes ignore zoom input
- fixed `10x` scopes keep PiP active with stable scope-camera FOV
- magnified-scope state still flows through the same PiP runtime used by future variable scopes

Suggested test names:

- `FixedTenPowerScope_IgnoresZoomInput`
- `FixedTenPowerScope_UsesStableScopeCameraFov`

**Step 2: Run the tests to verify they fail**

Expected: failure because the runtime still assumes generic zoomable optics behavior.

**Step 3: Write the minimal implementation**

Make `AdsStateController` and `RenderTextureScopeController` read:

- fixed-power scope family
- fixed magnification field

Ensure zoom input is ignored for fixed-power optics without creating a separate runtime path.

**Step 4: Run the tests to verify they pass**

Re-run the new fixed-power tests plus the existing PiP optic tests.

**Step 5: Commit**

```bash
git add Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs \
        Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs \
        Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "feat: support fixed-power pip scopes"
```

### Task 6: Add a scope-only long-range observation policy seam

**Files:**
- Create: `Reloader/Assets/Game/Weapons/Runtime/LongRangeObservationController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write the failing tests**

Add tests that assert:

- long-range observation policy activates only during magnified PiP scoped ADS
- hip fire and non-scoped aim do not activate it
- the policy can report the current visibility band using current optic state

Suggested test names:

- `LongRangeObservation_ActivatesOnlyForMagnifiedScopedPip`
- `LongRangeObservation_ReportsBandFromActiveScope`

**Step 2: Run the tests to verify they fail**

Expected: failure because no dedicated observation policy seam exists.

**Step 3: Write the minimal implementation**

Create a controller that:

- listens to current scoped PiP active state
- reads active optic and magnification
- reports a current observation band

Do not implement final impostor rendering in this task. Only establish the real runtime seam.

**Step 4: Run the tests to verify they pass**

Re-run the targeted observation tests plus scoped PiP integration tests.

**Step 5: Commit**

```bash
git add Reloader/Assets/Game/Weapons/Runtime/LongRangeObservationController.cs \
        Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs \
        Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs \
        Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "feat: add scope-only long-range observation policy"
```

### Task 7: Wire the Kar98k fixed 10x scope in authored content and live scenes

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab`
- Modify: `Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs`
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownPeripheralScopeWiringEditModeTests.cs`
- Test: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs`

**Step 1: Write the failing tests**

Add or extend asset/scene tests that assert:

- MainTown player root still wires the authored scoped runtime bridge correctly
- the current Kar98k optic is the fixed `10x` authored optic
- scene/prefab content does not rely on fallback behavior

**Step 2: Run the tests to verify they fail**

Expected: failure if any authored scene/prefab path still depends on generic or variable scope assumptions.

**Step 3: Write the minimal implementation**

Update scene/prefab/editor wiring so the Kar98k live path uses the authored fixed `10x` contract consistently.

Do not reintroduce prefab-name heuristics or editor-only fallback behavior.

**Step 4: Run the tests to verify they pass**

Re-run the edit-mode content tests and the existing MainTown scoped asset tests.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/World/Scenes/MainTown.unity \
        Reloader/Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab \
        Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs \
        Reloader/Assets/_Project/World/Tests/EditMode/MainTownPeripheralScopeWiringEditModeTests.cs \
        Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs
git commit -m "feat: wire kar98k fixed 10x scope content"
```

### Task 8: Full verification pass for the fixed-10x production scope

**Files:**
- Verify only

**Step 1: Run targeted PlayMode tests**

Run:

- fixed `10x` asset tests
- fixed-power ADS tests
- zeroing runtime tests
- scoped PiP integration tests
- scoped alignment tests

**Step 2: Run targeted EditMode tests**

Run:

- MainTown content-wiring tests
- world edit-mode suite affected by new references

**Step 3: Do a live Unity check**

In `MainTown`:

- equip Kar98k with current scope
- enter ADS
- verify scope behaves as fixed `10x`
- verify no zoom input changes magnification
- verify optic still lines up correctly
- verify no pose-tuning or camera hack is required for zeroing framework

**Step 4: Commit verification summary if needed**

If the repo uses validation notes for this workstream, add a concise validation note under `docs/plans/`.

**Step 5: Push**

- push the active branch once the subsystem work is verified and reviewed
