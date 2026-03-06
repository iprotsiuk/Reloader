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
- optional reticle sprite
- optional `ScopeRenderProfile` (`renderTextureResolution`, `scopeCameraFov`)

Contract notes:
- `eyeReliefBackOffset` is part of the production scoped-ADS contract and is applied by `WeaponAimAligner` after anchor alignment.
- `RenderTexturePiP` optics must provide explicit prefab authoring for `SightAnchor` and `ScopeLensDisplay`.
- scoped optics must remain future-proof for persistent user zeroing (`windage` / `elevation`) so optic state can be saved per configured optic after player adjustment

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
  - uses `IronSightAnchor` only when no optic is equipped
  - must not synthesize anchors for misconfigured optics

- `AdsStateController`
  - tracks ADS state + `AdsT`
  - handles variable zoom (1x-40x clamp)
  - reacts to optic hot-swap via `AttachmentManager.ActiveOpticChanged`
  - clamps/normalizes magnification state on optic swap without controller reset
  - applies world FOV mapping
  - computes sensitivity/sway scales
  - drives scope mask / PiP state

- `WeaponAimAligner`
  - `LateUpdate` alignment
  - camera-authoritative transform solve
  - production eye-relief offset from `OpticDefinition.eyeReliefBackOffset`
  - debug gizmos for camera/sight/error

- `ScopeMaskController`
  - scope mask UI + outside darkening + reticle scaling
  - runtime state is externally inspectable via `IsMaskVisible` / `CurrentAlpha`

- `RenderTextureScopeController`
  - PiP scope-image owner
  - drives lens render-texture binding and reticle application
  - scope camera must exclude `Viewmodel` content
  - must remain compatible with future persistent optic zeroing so reticle/optic adjustment state can shift point of aim without reworking the scope pipeline

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
- Correctness order for PiP scopes is:
  1. authored `SightAnchor`
  2. authored `eyeReliefBackOffset`
  3. scope camera exclusion of `Viewmodel`
  4. coarse pose tuning
- future zeroing support must be implemented as persistent optic state, not as scene-only pose offsets or hardcoded camera fudges
- Missing anchors, missing lens displays, or scope cameras rendering `Viewmodel` are development bugs, not acceptable degraded behavior.

## Integration Notes [v0.1]

- This ADS/optics framework is implemented under `Assets/Game/Weapons`.
- Existing `_Project/Weapons` runtime can coexist while migration continues.
- For FPS aiming/scope behavior work, prefer this implemented framework contract.
