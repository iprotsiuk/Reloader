# Kar98k Scope Calibration Design

**Date:** 2026-03-11

## Goal

Make the Kar98k PiP scope reusable and measurable instead of eyeballed:
- thin/fix the reticle from the clean transparent PNG
- keep player windage/elevation adjustments as runtime scope state
- add separate developer calibration controls so `1 MRAD` on the reticle can be matched to `1 MRAD` of PiP/impact adjustment
- set this optic to a `100 m` mechanical zero with `0.1 MRAD` clicks and `±60` clicks of travel

## Architecture

The implementation must keep three layers separate:

1. **Reticle art / presentation**
   - authored sprite asset
   - FFP/SFP behavior
   - optional visual trim for scale/centering inside the PiP lens

2. **Player-facing scope adjustment**
   - runtime windage/elevation click counts
   - persisted per mounted scope instance
   - clamped by authored scope limits

3. **Developer calibration**
   - base mechanical-zero offset at `100 m`
   - MRAD-per-click mapping
   - projection calibration multiplier / reticle trim used to make the rendered PiP match the reticle subtensions
   - exposed in inspector for Play Mode tuning

The key rule is that player adjustments must never hide bad authoring. If the rifle shoots low at `0` clicks, that is a calibration problem, not something the player state should silently absorb.

## Data Ownership

### Scope / optic authored defaults

Per-optic authored calibration should live with the optic definition / PiP calibration data, not in the player runtime state:
- `mradPerClick`
- `mechanicalZeroOffsetMrad`
- `reticleCompositeScale`
- `reticleCompositeOffset`
- `projectionMradCalibration`

### Player runtime state

Per-scope runtime state remains:
- `currentWindageClicks`
- `currentElevationClicks`
- later `zeroSteps`

### Reticle asset

Reticle art stays in the reticle definition asset:
- sprite
- FFP/SFP mode
- reference magnification

This keeps future scopes/reticles reusable:
- one reticle can be reused across multiple optics
- each optic can keep its own click value, click range, and mechanical-zero calibration

## Tuning Workflow

1. Enter Play Mode with the mounted optic.
2. Adjust calibration values in inspector:
   - base windage/elevation MRAD offsets
   - MRAD-per-click / projection calibration
   - reticle composite scale/offset
3. Observe whether the reticle subtensions and shot placement align.
4. Once values are confirmed, bake them into the authored optic defaults.

## Verification

Tests should prove:
- the Kar98k optic supports `±60` clicks
- `0` clicks means the authored `100 m` mechanical zero offset, not a hidden runtime fudge
- click-to-MRAD mapping is driven by authored values instead of the current arbitrary projection constant
- PiP optics still composite the reticle in the render path rather than leaking a world-space sprite
