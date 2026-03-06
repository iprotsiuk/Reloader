# Scoped PiP Optics Design

**Date:** 2026-03-05

**Goal:** Replace main-camera FOV zoom for magnified optics with a picture-in-picture scope system that preserves normal peripheral view, supports peripheral blur, and cleanly supports both FFP and SFP reticles.

## Problem

Current magnified optics reduce the main world camera FOV. That causes two issues:

- the entire screen zooms instead of only the optic
- optic lens meshes such as `WWII_Optic_Remote_Range_A_Lens` appear as opaque/grey art instead of functioning as a scoped display

This approach does not scale well to future high-magnification optics such as 20x-35x scopes and makes future FFP/SFP support awkward.

## Approved Direction

Use a hybrid optics pipeline:

- keep the main gameplay camera at its normal field of view
- render a magnified image through a dedicated scope camera
- display that image on the optic lens surface
- keep peripheral vision visible through the main camera
- blur and vignette the peripheral view during scoped ADS
- support reticle behavior through explicit per-optic reticle mode and reticle config assets

## Core Runtime Architecture

### Cameras

- `Main camera`
  - remains the gameplay source of truth for normal view
  - does not change FOV for magnified optics
- `Viewmodel camera`
  - remains unchanged for weapon rendering
- `Scope camera`
  - renders the magnified scene to a render texture
  - changes its own FOV based on optic magnification
  - is enabled only when scoped optics are active in ADS

### Scope Presentation

- optic prefabs expose an explicit eyepiece lens display target
- runtime binds the scope render texture to that lens display target
- source-art opaque lens meshes are no longer treated as final presentation
- the eyepiece display surface is the only place that shows the magnified render

### Reticles

Each optic uses explicit reticle metadata:

- `ReticleType = FFP | SFP`
- reticle asset/config reference
- optional illumination/color/brightness data
- optional reference magnification/subtension metadata

Behavior:

- `FFP`
  - reticle lives in the magnified optical path
  - reticle scales with zoom
- `SFP`
  - reticle is composited after the magnified scene
  - reticle stays visually constant while the scene behind it changes magnification

This hybrid architecture avoids hard-coding future scopes into a single presentation path.

### Eye Relief

The current `SightAnchor` contract remains the alignment reference. Scoped presentation adds eye-relief validity:

- on-axis and valid eye relief -> full scope image
- partially misaligned -> scope shadow / vignette increase
- severely misaligned -> lens image fades or clips while peripheral view remains

## ADS Behavior

### Low-Magnification Optics

Low-magnification optics can continue using the simpler existing path where appropriate.

### Magnified Optics

For magnified optics:

- `AdsStateController` stops mapping magnification to main-camera FOV
- magnification maps to scope-camera FOV only
- `RenderTextureScopeController` becomes the real scoped rendering path
- peripheral blur and vignette activate only during scoped ADS

## Long-Range Rendering and Simulation

High magnification introduces a simulation problem beyond just rendering. A scope camera can render only what it sees, but it cannot show convincing distant NPCs if those actors are despawned or sleeping due to player-distance rules.

Approved phased approach:

### Phase 1

- implement correct scoped PiP visuals
- keep main camera normal
- support lens display, blur, and FFP/SFP reticles

### Phase 2

Add an optical-interest runtime:

- `ScopeInterestTracker`
  - determines the narrow viewed region while scoped
- `LongRangeInterestManager`
  - requests limited far-region activation around the viewed area
- NPC/object runtime tiers:
  - `Dormant`
  - `FarSim`
  - `ScopedActive`

This allows future long-range scopes to promote only a narrow viewed region instead of globally expanding full-detail world processing.

## Data Model Changes

Extend the existing optics definitions with explicit scoped-render and reticle metadata rather than adding controller-side heuristics.

Likely additions:

- optic lens display binding contract on the optic prefab/runtime component
- reticle config asset
- reticle mode enum (`FFP`, `SFP`)
- optional scoped visual thresholds and eye-relief tuning
- continued use of `ScopeRenderProfile` for render texture resolution and scope camera FOV policy

## Failure Behavior

- missing lens display target must log clearly
- missing reticle config must log clearly and fall back deterministically
- no fallback to random child-name discovery beyond the explicit serialized contract added for optic display
- no reintroduction of whole-camera zoom as a silent fallback for scoped optics

## Verification Targets

Automated coverage should prove:

- main camera FOV stays unchanged during scoped ADS
- scope camera FOV changes with magnification
- lens display receives the render texture
- SFP reticle remains constant across zoom changes
- FFP reticle scales across zoom changes
- peripheral blur activates only for scoped ADS
- missing optic display wiring fails loudly

Scene validation should cover the live Kar98k scoped path.

## Rollout

1. Convert the existing PiP stub into the real scoped rendering path.
2. Add explicit optic lens-display and reticle contracts.
3. Switch high-magnification optics to PiP while keeping the main camera unchanged.
4. Add peripheral blur/vignette.
5. Add FFP/SFP reticle behavior.
6. Validate the Kar98k path and preserve the existing attachment-mounting contract.
