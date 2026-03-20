# Prod-Ready Scoped Precision Design

**Date:** 2026-03-19

## Goal

Make high-magnification ADS precise enough for `>1 mile` shots by decoupling true aiming precision from PiP presentation quality.

The player must be able to place aim continuously at extreme magnification even when the scope image itself is visually limited by render-texture resolution.

## Current Problem

The current runtime already keeps aim authority in camera/look state rather than in PiP texture pixels:
- `PlayerLookController` accumulates authoritative `yaw/pitch`
- `RenderTextureScopeController` owns PiP presentation
- firing uses muzzle/camera-aligned weapon runtime, not scope-texel offsets

The precision problem comes from the sensitivity path instead:
- gameplay look reads raw `<Pointer>/delta` with no normalization
- PiP scopes keep the main gameplay camera FOV unchanged
- generic main-camera FOV sensitivity scaling therefore does not provide meaningful high-mag slowdown for PiP optics
- the current ADS sensitivity curve is not aggressive enough for `20x-25x` precision work

Result:
- true aim is not quantized by the PiP texture
- but the resulting angular step per smallest practical mouse increment is still too large for very long-range shots

## Design Principles

1. **Camera / look state stays authoritative**
   - aim is stored as continuous angular state
   - weapon alignment, muzzle direction, and PiP presentation follow that state

2. **Input normalization happens once**
   - raw mouse delta is converted into stable gameplay look units at the input boundary
   - later systems never need to know whether source input was pixels/frame, stick deflection, or another device

3. **PiP scoped precision comes from the optic**
   - PiP scopes must use an explicit optic-driven precision scale
   - do not infer PiP precision from main camera FOV because PiP intentionally preserves world FOV

4. **PiP remains presentation-only**
   - render-texture resolution affects image smoothness and readability only
   - it must never define true aim resolution

5. **Calibration is based on angular step**
   - tune around resulting `mrad per smallest practical mouse increment`
   - not around arbitrary sensitivity percentages

## Architecture

Target data flow:

```text
raw mouse delta
-> normalized look delta
-> user/base sensitivity
-> ADS transition blend
-> optic precision scale
-> authoritative yaw/pitch
-> camera orientation
-> weapon alignment / muzzle solve
-> PiP scope render
```

### Ownership

#### `PlayerInputReader`

Owns input normalization.

Responsibilities:
- read raw input actions
- normalize raw mouse delta into stable gameplay look units
- preserve gamepad / non-mouse input behavior

Non-responsibilities:
- optic math
- PiP logic
- ballistic or reticle math

#### `PlayerLookController`

Owns authoritative aim accumulation.

Responsibilities:
- accumulate `yaw/pitch`
- apply final sensitivity product
- keep menu / focus-target / shot-camera gating behavior

Non-responsibilities:
- deciding which optic needs what scaling
- PiP render policy

#### `AdsStateController`

Owns scoped precision scale for ADS optics.

Responsibilities:
- compute magnification-driven scoped precision
- keep ADS transition blending
- distinguish PiP scopes from non-PiP zoom paths

Non-responsibilities:
- raw mouse normalization
- direct reticle / render-texture quality control

#### `PlayerWeaponController`

Owns the runtime bridge between scoped weapon state and player look.

Responsibilities:
- pass final scoped runtime multiplier into `PlayerLookController`
- keep firing / ballistics independent from PiP texture behavior

#### `RenderTextureScopeController`

Owns visual presentation only.

Responsibilities:
- scope camera FOV
- render-texture allocation / quality
- reticle compositing
- projection offsets for zero / click adjustments

Non-responsibilities:
- deciding minimum true aim step

## Precision Model

The production model is:

```text
final_look_delta =
normalized_mouse_delta
* user_sensitivity
* hipfire_base_scale
* ads_transition_blend
* optic_precision_scale
```

For PiP scopes:
- `optic_precision_scale` is driven by optic magnification or optic scope-camera FOV
- main camera FOV is not used as the source of truth for scoped precision

For non-PiP zoom paths:
- existing main-camera FOV sensitivity scaling can remain if useful
- but it must not leak into PiP scoped tuning and distort high-mag calibration

## Calibration Contract

Calibration must target resulting angular step:

```text
base_step_mrad = hipfire yaw delta from one smallest practical normalized input sample
optic_precision_scale = target_scoped_step_mrad / base_step_mrad
```

Recommended target for extreme high magnification:
- resulting step at high mag: roughly `0.005` to `0.01 mrad`

This is the key distinction:
- `0.005` to `0.01 mrad` is the desired resulting angular step
- the authored scoped multiplier is dimensionless and depends on the normalized base step

## Data Model

The system needs an explicit authored scoped precision contract.

Preferred model:
- one shared magnification-to-precision curve in ADS runtime
- optional per-optic multiplier or override in `OpticDefinition`

This avoids:
- hardcoded controller-specific magic numbers
- ad hoc scope-by-scope patches

Candidate `OpticDefinition` additions:
- `usePrecisionScaleOverride`
- `precisionScaleMultiplier`
- optional authored reference scope FOV if magnification alone is insufficient

## PiP Resolution Policy

PiP resolution is a visual quality policy, not a precision policy.

Recommended production tiers:
- `1024` to `2048` for normal high-mag PiP
- `4096` optional for high-end settings or very large optic lenses
- do not require `8k` for correctness

Expected behavior:
- low PiP resolution can still look visually steppy
- true impact placement should remain precise if input and angular scaling are correct

## Migration Strategy

1. Normalize mouse input in `PlayerInputReader`
2. Split PiP scoped precision from generic FOV sensitivity behavior
3. Author and tune a production long-range precision curve
4. Validate angular-step targets with focused tests
5. Tune PiP resolution separately for visual quality

This sequence keeps the change local to the input/look/ADS/PiP seam and avoids destabilizing unrelated weapon or scene systems.

## Verification

The finished system should prove:
- one smallest practical normalized mouse sample produces a stable hipfire angular step
- PiP scopes derive scoped precision from optic data even when world FOV is unchanged
- high-mag PiP optics achieve the target resulting angular step band
- firing / projectile direction remains independent from PiP texture resolution
- lower PiP quality may reduce visual smoothness but does not reduce true aim precision

## Minimal Safe Runtime Change

The smallest safe production-oriented change is not "raise PiP resolution."

It is:
- normalize mouse input at the boundary
- add an explicit PiP scoped precision scale path
- prevent generic main-camera FOV scaling from silently standing in for PiP optic precision

That creates a stable base for later user sensitivity settings, per-optic tuning, and long-range validation.
