# Viewmodel Camera Setup

This setup uses a strict two-camera FPS pipeline:
- `WorldCamera`: renders world geometry.
- `ViewmodelCamera`: renders only weapon/arms on `Viewmodel` layer and shares the `CameraPivot` presentation basis with `PlayerArms` and `WeaponPresentationRoot`.
- `ScopeCamera`: renders PiP scope imagery and must exclude `Viewmodel`.

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
- Parent: `CameraPivot`, not `WorldCamera`.

3. Put weapon and arms meshes on `Viewmodel` layer.

4. Create or runtime-spawn `ScopeCamera`.
- Parent: `WorldCamera`
- Local pose: identity
- Culling mask: `WorldCamera` mask with `Viewmodel` removed
- Clear flags/background: match `WorldCamera`
- Enabled only while PiP scoped ADS is active

Strict rule:
- if `ScopeCamera` renders `Viewmodel`, the PiP setup is broken
- if stable magnified ADS has more than one live transform owner for the scoped solve, scoped jitter is expected

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
- `RenderTextureScopeController` for PiP optics

## 3. Required Optic Prefab Layout

```text
OpticPrefab
 |- SightAnchor
```

`SightAnchor` must represent the player eye position behind the optic, not the lens surface.

For PiP optics also author:
- `ScopeLensDisplay` on the eyepiece display surface
- optional `ScopeReticleController`

## 4. Runtime Wiring

`AttachmentManager`
- `ScopeSlot` -> `ViewModelRoot/Attachments/ScopeSlot`
- `IronSightAnchor` -> `ViewModelRoot/Defaults/IronSightAnchor`
- resolves explicit authored optic `SightAnchor`
- must not synthesize a generic anchor for misconfigured optics during development

`AdsStateController`
- `WorldCamera` -> world camera
- `ViewmodelCamera` -> viewmodel camera
- `AttachmentManager` -> same object reference
- `ScopeMaskController` -> HUD scope mask controller
- `RenderTextureScopeController` -> PiP runtime owner when the weapon supports scoped optics
- `WeaponDefinition` -> currently equipped weapon definition

`WeaponAimAligner`
- `AdsPivot` -> `ViewModelRoot/AdsPivot`
- `CameraTransform` -> `WorldCamera.transform`
- `AttachmentManager` -> same object reference
- `AdsStateController` -> same object reference
- applies `OpticDefinition.eyeReliefBackOffset`
- stable magnified ADS is a hold state; do not keep rewriting `AdsPivot` every frame during that state unless parent-chain ownership has been redesigned and revalidated
- `ViewmodelCamera` should stay on the shared `CameraPivot` basis so the weapon root and viewmodel camera resolve from the same presentation frame

`RenderTextureScopeController`
- `ScopeCamera` -> dedicated scope camera
- active optic must expose `ScopeLensDisplay`
- PiP optics should bind explicit reticle data when present
- current runtime already applies mechanical-zero plus windage/elevation projection shifts and relies on `AttachmentManager` snapshot restore for per-optic adjustment state

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

## 6. Example Setup: Canik TP9 + Red Dot

### Prefab
- Weapon viewmodel uses required layout above.
- Red dot optic prefab contains `SightAnchor`.

### ScriptableObjects
`WeaponDefinition`:
- `weaponId`: `weapon-canik-tp9`
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
- No PiP lens rendering required.

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
- `ScopeRenderProfile` optional for fixed-FOV/resolution calibration

Expected:
- PiP lens rendering activates in ADS when configured for `RenderTexturePiP`.
- `SightAnchor` and eye relief define final scoped eye position.
- Mouse wheel smoothly changes zoom.
- Sensitivity and sway scales reduce with magnification.
- Scope camera never renders the rifle or scope body.

## 8. Validation Checklist

Use one test scene with player, arms, and both cameras.

1. Equip Canik TP9 + red dot, hold ADS.
- `SightAnchor` aligns with camera.
- No jitter.
- No scope mask.

2. Equip ELR + 5-25x scope, hold ADS.
- `SightAnchor` aligns with camera.
- PiP lens output appears if configured.
- Scope camera excludes `Viewmodel`.

3. Scroll wheel while ADS on ELR.
- Magnification changes smoothly.
- World FOV narrows as zoom increases.

4. Swap optics at runtime.
- New `SightAnchor` takes effect immediately.

5. Pull scoped weapon close with authored pose tuning.
- scope image stays visible
- optic body does not black out PiP view
- eye relief remains stable

Current adjustment-state note:
- the shipped PiP path already restores per-optic windage/elevation snapshots across re-equip and distinct optic state keys
- `ScopeAdjustmentController` and `_Project/Weapons` `WeaponScopeRuntimeState` both carry zero-step state, but the live player PiP bridge currently exposes only windage/elevation clicks
- keep adjustment persistence in optic runtime state rather than mutating `AdsPivot`, `SightAnchor`, or scope-camera transforms

6. Confirm debug alignment output.
- `WeaponAimAligner` gizmos show camera axis, sight axis, and error line.
- Error decreases to near zero when fully ADS.

## 9. Scoped Jitter Guardrail

Regression recorded on `2026-03-19`:
- rewriting `AdsPivot.localPosition/localRotation` every `LateUpdate` during already-stable magnified ADS reintroduced the scoped vibration bug
- the change was reverted in commit `307bbfa8`

Why it happened:
- the scoped weapon parent chain can still move slightly from camera/body turn, viewmodel-root stabilization, and hand-rig updates
- making `WeaponAimAligner` a continuous writer again caused the child scoped solve to fight those parent-chain systems

Practical rule:
- once magnified scoped ADS reaches its held state, do not add another continuous corrective writer to `AdsPivot` as a local fix for perceived drift
- investigate the upstream parent-chain motion instead

If scoped turning looks chunky or double-driven:
1. check whether the rifle parent chain is still being moved during held ADS
2. check `FpsViewmodelAnimatorDriver` and `WeaponHandRigController` timing before touching `WeaponAimAligner`
3. validate any attempted fix in live runtime with camera turn + PiP active, not only in a seam test

## 10. PiP Note

`RenderTextureScopeController` is the PiP scope-image owner:
- enabled only while ADS and when optic policy is `RenderTexturePiP`
- owns scope-camera FOV and render-texture binding
- depends on explicit optic prefab authoring (`SightAnchor`, `ScopeLensDisplay`, reticle wiring when needed)

Current scoped render-quality runtime contract:
- `Scoped PiP Resolution %` is a percentage of native screen baseline:
  - `100` equals the current native/current-game square baseline, taken from the larger effective screen dimension
  - `10` to `400` scales linearly from that baseline for all PiP optics unless profile overrides exist
- `Peripheral Blur %` controls peripheral suppression while scoped PiP is active:
  - increasing it increases vignette strength and shrinks the clear center
  - world resolution is also downscaled proportionally as a low-cost fallback tradeoff while PiP is active
  - this is intentionally not a full-screen effect pass yet; it is the progressive path for future blur quality tuning
