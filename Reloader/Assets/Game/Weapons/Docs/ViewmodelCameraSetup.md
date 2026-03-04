# Viewmodel Camera Setup

This setup uses a strict two-camera FPS pipeline:
- `WorldCamera`: renders world geometry.
- `ViewmodelCamera`: renders only weapon/arms on `Viewmodel` layer.

ADS alignment is camera-driven:
- Weapon `SightAnchor` is aligned to the `WorldCamera` transform by moving `AdsPivot` in code.
- The camera never chases the weapon.

## 1. Scene Camera Configuration

1. Create `WorldCamera`.
- Culling mask: everything except `Viewmodel`.
- Typical FOV: from `WeaponDefinition.defaultWorldFov`.

2. Create `ViewmodelCamera`.
- Culling mask: only `Viewmodel`.
- Clear flags: Depth only.
- Depth: higher than `WorldCamera`.
- Near clip: small (for weapon clipping safety).
- FOV: from `WeaponDefinition.defaultViewmodelFov`.

3. Put weapon and arms meshes on `Viewmodel` layer.

## 2. Required Weapon Prefab Layout

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

Attach these components on the viewmodel rig root (or a controller object):
- `AttachmentManager`
- `AdsStateController`
- `WeaponAimAligner`
- Optional: `RenderTextureScopeController`

## 3. Required Optic Prefab Layout

```text
OpticPrefab
 |- SightAnchor
```

`SightAnchor` must represent the player eye position behind the optic, not the lens surface.

## 4. Runtime Wiring

`AttachmentManager`
- `ScopeSlot` -> `ViewModelRoot/Attachments/ScopeSlot`
- `IronSightAnchor` -> `ViewModelRoot/Defaults/IronSightAnchor`

`AdsStateController`
- `WorldCamera` -> world camera
- `ViewmodelCamera` -> viewmodel camera
- `AttachmentManager` -> same object reference
- `ScopeMaskController` -> HUD scope mask controller
- `RenderTextureScopeController` -> optional stub for PiP mode
- `WeaponDefinition` -> currently equipped weapon definition

`WeaponAimAligner`
- `AdsPivot` -> `ViewModelRoot/AdsPivot`
- `CameraTransform` -> `WorldCamera.transform`
- `AttachmentManager` -> same object reference
- `AdsStateController` -> same object reference

## 5. Scope Mask UI Setup

Create HUD structure:

```text
HUDCanvas
 |- ScopeMaskRoot (RectTransform + CanvasGroup)
 |   |- Circular mask art
 |   |- Outside darkening images
 |   |- Reticle Image
```

Attach `ScopeMaskController` and assign:
- `CanvasGroup`
- `MaskRoot`
- `OutsideDarkenGraphics`
- `ReticleImage`
- `ReticleTransform`

Mask behavior:
- `AdsVisualMode.Auto`
  - magnification <= 2x: no mask
  - magnification >= 4x: mask

## 6. Example Setup: Pistol + Red Dot

### Prefab
- Weapon viewmodel uses required layout above.
- Red dot optic prefab contains `SightAnchor`.

### ScriptableObjects
`WeaponDefinition`:
- `weaponId`: `pistol_9mm`
- `adsInTime`: `0.10`
- `adsOutTime`: `0.08`
- `baseAdsSensitivityScale`: `0.9`
- `baseAdsSwayScale`: `0.8`
- `defaultWorldFov`: `75`
- `defaultViewmodelFov`: `60`

`OpticDefinition`:
- `opticId`: `reddot_compact`
- `category`: `RedDot`
- `isVariableZoom`: `false`
- `magnificationMin`: `1`
- `magnificationMax`: `1`
- `magnificationStep`: `0.25` (unused for fixed zoom)
- `visualModePolicy`: `Auto`
- `eyeReliefBackOffset`: `0.0`

Expected:
- ADS alignment is correct.
- No scope mask.

## 7. Example Setup: ELR Rifle + 5-25x Scope

### Prefab
- Rifle viewmodel uses required layout above.
- Scope optic prefab contains `SightAnchor` correctly positioned for eye box.

### ScriptableObjects
`WeaponDefinition`:
- `weaponId`: `elr_338`
- `adsInTime`: `0.18`
- `adsOutTime`: `0.14`
- `baseAdsSensitivityScale`: `0.6`
- `baseAdsSwayScale`: `0.5`
- `defaultWorldFov`: `75`
- `defaultViewmodelFov`: `58`

`OpticDefinition`:
- `opticId`: `scope_5_25`
- `category`: `ScopeHighMag`
- `isVariableZoom`: `true`
- `magnificationMin`: `5`
- `magnificationMax`: `25`
- `magnificationStep`: `0.5`
- `visualModePolicy`: `Auto`
- `eyeReliefBackOffset`: `0.025`
- `ScopeRenderProfile` optional for PiP migration

Expected:
- Scope mask activates in ADS.
- Mouse wheel smoothly changes zoom.
- Sensitivity and sway scales reduce with magnification.

## 8. Validation Checklist

Use one test scene with player, arms, and both cameras.

1. Equip pistol + red dot, hold ADS.
- `SightAnchor` aligns with camera.
- No jitter.
- No scope mask.

2. Equip ELR + 5-25x scope, hold ADS.
- `SightAnchor` aligns with camera.
- Scope mask appears.

3. Scroll wheel while ADS on ELR.
- Magnification changes smoothly.
- World FOV narrows as zoom increases.

4. Swap optics at runtime.
- New `SightAnchor` takes effect immediately.

5. Confirm debug alignment output.
- `WeaponAimAligner` gizmos show camera axis, sight axis, and error line.
- Error decreases to near zero when fully ADS.

## 9. PiP Note

`RenderTextureScopeController` is a lightweight stub:
- disabled by default
- enabled only while ADS and when optic policy is `RenderTexturePiP`
- intended as migration path to full PiP scopes
