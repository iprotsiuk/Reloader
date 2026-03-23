# ADS + Optics Framework [v0.1 Implemented]

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) and [weapons-and-ballistics.md](weapons-and-ballistics.md) first.
> **Runtime doc in project assets:** `Reloader/Assets/Game/Weapons/Docs/ViewmodelCameraSetup.md`.

## Scope [v0.1]

Implemented FPS ADS/optics framework for PC under:

- `Reloader/Assets/Game/Weapons/Common/`
- `Reloader/Assets/Game/Weapons/WeaponDefinitions/`
- `Reloader/Assets/Game/Weapons/Runtime/`
- `Reloader/Assets/Game/Weapons/UI/`

This is a code-driven alignment system (no per-weapon ADS animation combinatorics).

## Core Contract [v0.1]

- **Camera is source of truth.**
- During ADS, the viewmodel rig (`AdsPivot`) is moved to align active `SightAnchor` to camera.
- **Do not move camera to chase weapon.**
- Alignment runs in `LateUpdate`.
- `WeaponViewPoseTuningHelper` provides coarse weapon presentation tuning only. Final scoped eye alignment is owned by `WeaponAimAligner`.
- PiP optics are strict authored content. Missing anchors or lens-display wiring must fail loudly during development instead of falling back silently.

Alignment model:

`delta = Camera_world * inverse(SightAnchor_world)`

Apply `delta` to `AdsPivot` with smoothing + ADS blend (`AdsT`).

## Data Model [v0.1]

### `WeaponDefinition` (ADS framework)

Path: `Reloader/Assets/Game/Weapons/WeaponDefinitions/WeaponDefinition.cs`

Fields:
- `weaponId`
- `viewModelPrefab`
- `adsInTime`
- `adsOutTime`
- `baseAdsSensitivityScale`
- `baseAdsSwayScale`
- `defaultWorldFov`
- `defaultViewmodelFov`

### `OpticDefinition` (ADS framework)

Path: `Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs`

Fields:
- `opticId`
- `category` (`OpticCategory`)
- `opticPrefab`
- `isVariableZoom`
- `magnificationMin`
- `magnificationMax`
- `magnificationStep`
- `visualModePolicy` (`AdsVisualMode`)
- `eyeReliefBackOffset`
- `scopeReticleDefinition` (`ScopeReticleDefinition`)
- `mradPerClick`
- `minWindageClicks` / `maxWindageClicks`
- `minElevationClicks` / `maxElevationClicks`
- `mechanicalZeroOffsetMrad`
- `projectionCalibrationMultiplier`
- `compositeReticleScale`
- `compositeReticleOffset`
- optional `ScopeRenderProfile` (`renderTextureResolution`, `scopeCameraFov`)

Contract notes:
- `eyeReliefBackOffset` is part of the production scoped-ADS contract and is applied by `WeaponAimAligner` after anchor alignment for all optic visual modes, including `RenderTexturePiP`.
- weapon prefabs may add a per-weapon/per-attachment correction through `WeaponViewPoseTuningHelper.ScopedAdsEyeReliefBackOffset`; final scoped eye relief is `OpticDefinition.eyeReliefBackOffset + WeaponViewPoseTuningHelper` runtime offset.
- `RenderTexturePiP` optics must provide explicit prefab authoring for `SightAnchor` and `ScopeLensDisplay`.
- `ScopeReticleDefinition.Mode` supports both `Ffp` and `Sfp`; current PiP runtime scales FFP reticles with magnification and keeps SFP reticles visually stable.
- `AttachmentManager` persists `ScopeAdjustmentSnapshot` data per optic/state key during runtime re-equip flows.
- The live PiP bridge currently applies windage/elevation clicks during scoped ADS. Zero-step state exists on `ScopeAdjustmentController` and `_Project/Weapons` `WeaponScopeRuntimeState`, but live zero-step input/save wiring is still narrower than the shipped windage/elevation path.

Enums:
- `AdsVisualMode`: `Auto`, `Mask`, `RenderTexturePiP`
- `OpticCategory`: `Irons`, `RedDot`, `Holo`, `Prism`, `LPVO`, `ScopeHighMag`

## Runtime Components [v0.1]

- `AttachmentManager`
  - `EquipOptic(OpticDefinition)`
  - `UnequipOptic()`
  - `GetActiveSightAnchor()`
  - `ActiveOpticChanged` event for hot-swap listeners
  - `EquipMuzzle(MuzzleAttachmentDefinition)` / `UnequipMuzzle()`
  - exposes `ActiveOpticDefinition` and `ActiveMuzzleDefinition`
  - restores per-optic `ScopeAdjustmentSnapshot` values keyed by optic state
  - uses `IronSightAnchor` only when no optic is equipped
  - must not synthesize anchors for misconfigured optics

- `AdsStateController`
  - tracks ADS state + `AdsT`
  - handles fixed-power and variable zoom optics (`MagnificationMin` for fixed optics, authored range clamp for variable optics)
  - reacts to optic hot-swap via `AttachmentManager.ActiveOpticChanged`
  - clamps/normalizes magnification state on optic swap without controller reset
  - applies world FOV mapping
  - computes sensitivity/sway scales
  - applies live windage/elevation adjustments only while scoped PiP ADS is active
  - drives scope mask / PiP state

- `WeaponAimAligner`
  - `LateUpdate` alignment
  - camera-authoritative transform solve
  - production eye-relief offset from authored `OpticDefinition.eyeReliefBackOffset`
  - additive per-weapon presentation correction from `WeaponViewPoseTuningHelper.ScopedAdsEyeReliefBackOffset`
  - debug gizmos for camera/sight/error
  - stable magnified ADS is intentionally a hold mode after final alignment; do not turn it back into a continuous corrective writer without redesigning parent-chain ownership first

- `ScopeMaskController`
  - scope mask UI + outside darkening + reticle scaling
  - runtime state is externally inspectable via `IsMaskVisible` / `CurrentAlpha`

- `RenderTextureScopeController`
  - PiP scope-image owner
  - drives lens render-texture binding and reticle application
  - applies projection offsets from mechanical zero plus live windage/elevation clicks
  - composites authored reticles and respects FFP/SFP scaling rules
  - scope camera must exclude `Viewmodel` content

## Visual Mode Policy [v0.1]

`AdsVisualMode.Auto` behavior:
- magnification `<= 2x` -> no mask
- magnification `>= 4x` -> mask

`Mask` forces mask mode.

`RenderTexturePiP` enables the full PiP path:
- main camera keeps gameplay FOV
- scope camera renders only world content for the optic lens
- reticle behavior is driven from explicit optic data
- scope camera must not render `Viewmodel`

## Prefab Conventions [v0.1]

### Weapon viewmodel prefab

```text
ViewModelRoot
 |- AdsPivot
 |- Attachments
 |   |- ScopeSlot
 |- Defaults
 |   |- IronSightAnchor
 |- Muzzle
 |- Eject
```

Production authoring rules:
- `AdsPivot` is required for camera-authoritative scoped alignment.
- weapon and arms meshes must render on the `Viewmodel` layer.
- runtime-scoped PiP must render from a separate scope camera that excludes `Viewmodel`.

### Optic prefab

```text
OpticPrefab
 |- SightAnchor
```

`SightAnchor` = eye position behind optic, not glass surface.

PiP optic contract:

```text
OpticPrefab
 |- SightAnchor
 |- EyepieceLens (MeshRenderer + ScopeLensDisplay)
 |- OptionalReticle (ScopeReticleController / equivalent)
```

Strict development rule:
- do not synthesize `SightAnchor`
- do not fall back to optic root
- do not silently accept missing `ScopeLensDisplay` for `RenderTexturePiP`

## Production Migration Rules [v0.1]

- Future optics must follow the reusable authored contract instead of one-off scene tuning.
- `WeaponViewPoseTuningHelper` gets the rifle close; `WeaponAimAligner` makes the scope actually line up.
- In stable magnified ADS, `WeaponAimAligner` must not continuously rewrite `AdsPivot` every frame on top of live parent-chain motion. Camera/body turn, viewmodel stabilization, and hand-rig motion can still move the rifle hierarchy, and a second live `AdsPivot` writer reintroduces scoped vibration.
- Correctness order for PiP scopes is:
  1. authored `SightAnchor`
  2. authored `eyeReliefBackOffset`
  3. authored weapon-view `ScopedAdsEyeReliefBackOffset` correction when a specific weapon/scope pairing needs a different eye box
  4. scope camera exclusion of `Viewmodel`
  5. coarse pose tuning
- keep adjustment persistence in optic runtime state, not scene-only pose offsets or hardcoded camera fudges; current repo already restores runtime windage/elevation snapshots while live zero-step exposure is still partial
- Missing anchors, missing lens displays, or scope cameras rendering `Viewmodel` are development bugs, not acceptable degraded behavior.

## Scoped Stability Regression Note [2026-03-19]

We hit a regression by changing `WeaponAimAligner` stable scoped ADS from:
- solve once on entry to held magnified ADS, then stop writing `AdsPivot`

to:
- recompute and write `AdsPivot.localPosition/localRotation` every `LateUpdate` while held scoped ADS stayed active

That change looked correct in a narrow seam test, but it reintroduced the vibration bug we had already fought:
- the rifle parent chain can still move slightly from camera/body turn
- `FpsViewmodelAnimatorDriver` scoped stabilization still clamps the viewmodel root
- `WeaponHandRigController` still drives hand targets in `LateUpdate`
- turning `WeaponAimAligner` back into a continuous held-ADS writer created another live transform owner on top of those systems

Observed result:
- at some view angles, the scoped rifle and PiP no longer looked smoothly locked
- instead they showed vibration / jitter because parent motion and child corrective writes fought each other frame to frame

Rule going forward:
- treat stable magnified ADS as a single-live-owner state
- do not add a second continuous `AdsPivot` writer during that state as a local fix for perceived parent drift
- if scoped turning looks stepped, investigate which parent-chain system is still moving the rifle during hold instead of making `WeaponAimAligner` continuously chase it again

Testing lesson:
- a seam test that proves `AdsPivot` can keep correcting synthetic parent drift is not enough
- any change to held scoped ADS ownership must also be validated against the real runtime stack: camera turn, viewmodel stabilization, hand rig, and PiP scope camera together

## Integration Notes [v0.1]

- This ADS/optics framework is implemented under `Assets/Game/Weapons`.
- Existing `_Project/Weapons` runtime can coexist while migration continues.
- Current repo evidence includes `ScopeAttachmentAdsIntegrationPlayModeTests` (`RealKar98kOpticAsset_PipReticle_CompositesIntoScopeRenderPath`, `ScopedReticle_FfpScalesWithMagnification`, `ScopedReticle_SfpRemainsStableAcrossMagnification`, `EquipOptic_ScopeAdjustmentController_UsesPendingStateKeyForDistinctScopeInstances`) and `WeaponScopeRuntimeStatePlayModeTests` (`ApplyZeroSteps_UsesConfiguredStepSize`, `AdjustmentSnapshot_RestoresZoomZeroWindageAndElevation`).
- For FPS aiming/scope behavior work, prefer this implemented framework contract.
