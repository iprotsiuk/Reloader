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

Enums:
- `AdsVisualMode`: `Auto`, `Mask`, `RenderTexturePiP`
- `OpticCategory`: `Irons`, `RedDot`, `Holo`, `Prism`, `LPVO`, `ScopeHighMag`

## Runtime Components [v0.1]

- `AttachmentManager`
  - `EquipOptic(OpticDefinition)`
  - `UnequipOptic()`
  - `GetActiveSightAnchor()`
  - `EquipMuzzle(MuzzleAttachmentDefinition)` / `UnequipMuzzle()`
  - exposes `ActiveOpticDefinition` and `ActiveMuzzleDefinition`
  - fallback to `IronSightAnchor`

- `AdsStateController`
  - tracks ADS state + `AdsT`
  - handles variable zoom (1x-40x clamp)
  - applies world FOV mapping
  - computes sensitivity/sway scales
  - drives scope mask / PiP state

- `WeaponAimAligner`
  - `LateUpdate` alignment
  - camera-authoritative transform solve
  - optional eye-relief offset
  - debug gizmos for camera/sight/error

- `ScopeMaskController`
  - scope mask UI + outside darkening + reticle scaling

- `RenderTextureScopeController`
  - lightweight PiP stub
  - disabled by default unless active in ADS

## Visual Mode Policy [v0.1]

`AdsVisualMode.Auto` behavior:
- magnification `<= 2x` -> no mask
- magnification `>= 4x` -> mask

`Mask` forces mask mode.

`RenderTexturePiP` enables PiP path (stub runtime in v0.1).

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

### Optic prefab

```text
OpticPrefab
 |- SightAnchor
```

`SightAnchor` = eye position behind optic, not glass surface.

## Integration Notes [v0.1]

- This ADS/optics framework is implemented under `Assets/Game/Weapons`.
- Existing `_Project/Weapons` runtime can coexist while migration continues.
- For FPS aiming/scope behavior work, prefer this implemented framework contract.
