# Split Viewmodel Redesign Architecture Reset Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Reset first-person weapon presentation around a stable split-viewmodel architecture where camera motion, weapon presentation, hand sync, optic PiP, and muzzle-origin firing each have one explicit owner. Eliminate the current mix of helper-authored pose rewrites, scoped repair fallbacks, and scene-specific rescue logic that makes Kar98k tuning brittle and blocks reuse across irons, non-PiP optics, and PiP optics.

**Architecture Decision:** Treat the weapon view as an authored runtime presentation shell mounted under `CameraPivot/WeaponPresentationRoot`, separate from `PlayerArms`. The weapon view owns coarse HIP and ADS presentation through authored child transforms and explicit anchors. The hand rig follows the weapon. The scoped PiP camera renders from authored optic data plus one explicit alignment seam, never from stacked helper rewrites. Old helper-driven fallback ownership is deleted rather than preserved behind compatibility branches.

**Tech Stack:** Unity 6 C#, prefab-driven weapon authoring, Unity Animation Rigging, URP PiP optics, Unity Test Framework editmode/playmode coverage, Unity MCP/editor validation, targeted git-scoped slices only.

---

## Why This Is A Reset

This effort is not a continuation of the old helper-removal thread.

The old thread tried to tune or delete `WeaponViewPoseTuningHelper` / `WeaponAimAligner` inside the existing ownership model. The new effort resets the ownership model itself:
- split the weapon root from the arms branch permanently
- make authored weapon/optic anchors the source of truth
- keep one runtime writer per transform seam
- delete scene- and helper-era fallbacks instead of porting them forward

The redesign should be judged against the final ownership contract, not whether legacy helper behavior is preserved.

## Goals

- Remove pose-independent rifle/housing micro-step artifacts by keeping weapon presentation on a clean shared camera subtree with one runtime owner per transform seam.
- Support one reusable presentation model for:
  - iron sights
  - non-PiP optics
  - PiP optics
  - future rifles/pistols that use the same attachment and hand-rig contract
- Keep PiP optics stable without blurring the PiP image or hiding world/render-target problems behind reticle hacks.
- Preserve muzzle-origin firing from authored weapon geometry, not camera-center hitscan fakery.
- Make authored weapon prefabs reusable across `MainTown`, `IndoorRange`, tuning scenes, and future scenes without scene-local pose rescue components.
- Delete stale fallback logic, helper rescue paths, and scene-specific hardcoding instead of widening them.

## Non-Goals

- Do not ship a new full-body first-person animation system in this pass.
- Do not broaden into third-person weapon ownership or NPC weapon presentation.
- Do not redesign projectile ballistics, zeroing math, or attachment economy data.
- Do not make PiP optional by degrading all scopes into simple overlays.
- Do not preserve compatibility for every legacy helper field if the new ownership contract replaces it cleanly.
- Do not run broad full-regression passes unless a focused failure proves unexpected coupling.

## Ownership Model

### Stable Hierarchy

- `PlayerRoot/CameraPivot`
  - `WeaponPresentationRoot`
    - live equipped weapon view
  - `PlayerArms`
    - animated first-person arms only

### Runtime Owners

- `PlayerLookController`
  - owns shared yaw/pitch camera subtree motion only
  - must not indirectly drive weapon pose through arm-bone ancestry
- `PlayerWeaponController`
  - owns equip state, runtime view spawn/mount, and firing/reload orchestration
  - mounts the weapon only under `WeaponPresentationRoot`
  - owns no hidden pose fallback derived from `ik_hand_gun`, `PlayerArms`, or scene helpers
- Weapon view prefab
  - owns coarse HIP and ADS presentation through explicit authored child anchors/transforms
  - owns `MuzzleTransform`, `IronSightAnchor`, `AdsPivot`, hand anchors, and attachment slots
- `WeaponHandRigController`
  - owns hand follow only
  - reads equipped weapon hand anchors and drives rig targets
  - never owns weapon placement
- Scoped alignment seam
  - one explicit runtime seam may align optic view data to the camera for scoped ADS
  - that seam must own only the alignment transform or projection correction assigned to it
  - it must not coexist with another helper continuously rewriting the outer weapon root
- `AttachmentManager` / optic runtime
  - owns mounted optic content and authored `SightAnchor` resolution
  - no root-name fallback, optic-root fallback, or synthesized anchors

## PiP Constraints

- PiP image must remain sharp. Peripheral blur/downscale applies outside the PiP image only.
- Reticle center and rendered world center must stay aligned through one deterministic path.
- Composite reticle paths must use stable pixel math and explicit aspect handling where required.
- PiP optics must rely on explicit authored `SightAnchor` and explicit runtime scope-camera data.
- No fallback may silently replace a missing `SightAnchor`, `ScopeLensDisplay`, render target, or mount anchor.
- PiP state must be reusable across all compatible scoped weapons, not Kar98k-specific.

## Muzzle-Origin Firing Constraints

- Projectile spawn origin must remain the authored weapon `MuzzleTransform`.
- Recoil, firing FX, muzzle audio, muzzle flash, and ballistic origin must stay tied to the equipped weapon view, not the screen center.
- Optic alignment changes must not move the muzzle origin off the weapon or substitute camera-origin firing.
- Verification for any slice touching scoped alignment must re-check muzzle-origin fire behavior.

## Reuse Across Weapon Types

The target contract must work for:
- irons-only guns
- non-PiP optics that still use authored sight alignment
- PiP optics with separate scope camera and reticle path

Required reusable authored data on the weapon view:
- `WeaponViewAttachmentMounts`
- `MuzzleTransform`
- `IronSightAnchor`
- `AdsPivot` when scoped alignment is needed
- left/right hand anchors
- explicit optic/muzzle slots

Required reusable authored data on optic prefabs/assets:
- deterministic `SightAnchor`
- explicit reticle/composite-reticle data when applicable
- explicit eye-relief baseline and PiP behavior flags

No weapon type should need scene-local pose helpers or name-based transform discovery.

## Deletion Policy

Delete old fallback/helper behavior when the replacement seam is verified. Do not keep both.

Priority deletions:
- scene-local pose rescue/tuning helpers once prefab-owned presentation replaces them
- runtime fallback that derives weapon ownership from `ik_hand_gun`, `PlayerArms`, or animator hierarchy
- scope/optic fallback that invents anchors, falls back to optic root, or silently repairs missing authored references
- duplicate stable-scoped writers that continue to touch `AdsPivot` or the outer weapon root after hold mode is reached
- tests that assert helper existence rather than the new authored ownership contract

Rules:
- if a fallback only exists to preserve legacy MainTown or Kar98k behavior, remove it
- if a helper field survives temporarily, mark it transitional and give it an exit slice
- do not add new compatibility branches unless a focused failing test proves a temporary bridge is necessary

## Implementation Slices

### Slice 1: Freeze The Final Contract In Docs And Tests

**Outcome:** the redesign has one documented target model and failing tests describe the final ownership contract instead of legacy helper behavior.

**Scope:**
- add/adjust focused editmode/playmode tests for:
  - `WeaponPresentationRoot` sibling ownership
  - hand rig follows weapon only
  - scoped ADS has one active alignment writer
  - PiP optics require explicit authored anchors
  - muzzle-origin firing remains weapon-authored

**Gate:**
- failing tests are intentional and focused
- docs/progress updated before runtime rewrites expand

### Slice 2: Lock Shared Camera And Weapon Root Ownership

**Outcome:** the live equipped weapon is always mounted under `CameraPivot/WeaponPresentationRoot`, never under `PlayerArms` or animator bones.

**Scope:**
- remove remaining `ik_hand_gun` / arms-branch ownership fallbacks
- align player prefab, scene wiring, and editor tooling to the same sibling-root contract
- ensure `PlayerLookController` drives only the shared camera subtree

**Gate:**
- focused hierarchy/wiring tests pass
- visual validation confirms sibling roots in prefab and active scene

### Slice 3: Move Presentation Ownership Onto Weapon/Optic Authoring

**Outcome:** HIP/ADS presentation comes from weapon prefab authoring, not scene helpers or continuous helper rewrites.

**Scope:**
- move coarse pose data onto weapon-prefab child anchors/transforms
- preserve explicit optic-specific overrides only where authored data requires it
- remove scene-local pose tuning components from active authoring

**Gate:**
- equipped weapon spawns correctly in multiple scenes without scene-local pose helper
- no missing-script/helper-rescue warnings

### Slice 4: Collapse Scoped Alignment To One Runtime Seam

**Outcome:** scoped ADS uses one explicit alignment owner with no duplicate stable writers.

**Scope:**
- keep exactly one seam for final camera-to-optic alignment
- ensure outer weapon root, `AdsPivot`, projection offset, and reticle path each have a single writer
- remove stable-scoped repair loops and helper overlap

**Gate:**
- focused scoped ADS stability tests pass
- tiny-turn scoped validation shows no repeat root rewrite or repair churn

### Slice 5: Hand Rig Follows Weapon, Never Owns It

**Outcome:** `WeaponHandRigController` syncs hands from equipped weapon anchors only.

**Scope:**
- bind left/right hand targets from equipped view anchors
- keep right-hand ownership explicit where animation-authored behavior is intentional
- ensure reload/bolt interactions can temporarily reduce hand influence without moving the weapon root

**Gate:**
- focused hand-rig/controller playmode tests pass
- reload/fire/scoped interactions remain functional

### Slice 6: Harden PiP Rendering And Reticle Contracts

**Outcome:** PiP optics share a deterministic render pipeline with stable reticle alignment and no fallback masking.

**Scope:**
- ensure scope camera, render texture, reticle composition, and lens display use explicit authored/runtime data
- keep PiP sharp and exclude viewmodel/peripheral blur contamination
- remove any remaining reticle/root fallback behavior that hides bad authored data

**Gate:**
- focused PiP/reticle tests pass
- live scoped visual verification confirms stable center and acceptable performance

### Slice 7: Delete Transitional Helpers And Old Contract Tests

**Outcome:** legacy helper/remount/fallback code is gone and the test suite describes only the new model.

**Scope:**
- remove obsolete helper components, custom editors, dead serialized fields, and stale tests
- update docs/design references to the new split-viewmodel contract

**Gate:**
- targeted compile clean
- no missing-script warnings
- progress doc updated with final deletions and deferred risks

## Verification Gates

Every slice must satisfy all relevant gates before the next slice expands:

### Gate A: Focused Automated Coverage

- editmode/playmode tests only for the touched seam
- red-to-green evidence recorded when behavior changes
- no test added that encodes old helper ownership as the desired outcome

### Gate B: Console And Compile Cleanliness

- Unity compile succeeds for touched assemblies
- console is cleared and re-read after targeted verification
- no missing-script, mount-fallback, or scope-repair spam is allowed

### Gate C: Visible Runtime Contract Check

- player prefab and active scene show sibling `WeaponPresentationRoot` and `PlayerArms`
- weapon mounts under `WeaponPresentationRoot`
- hand rig targets the equipped weapon anchors only

### Gate D: Scoped/PiP Acceptance

- scoped ADS remains stable through tiny yaw changes
- PiP image stays sharp and centered
- reticle path does not drift from the rendered world center
- muzzle-origin firing remains correct while scoped

### Gate E: Reuse Check

- one irons weapon
- one non-PiP optic weapon
- one PiP optic weapon

At least one representative from each category must pass the focused contract checks before the redesign is considered reusable.

## Initial Risks

- authored prefab data may still live on transforms that `PlayerWeaponController` normalizes or reparents at runtime
- stable-scoped state may still depend on aligner existence rather than the new ownership contract
- right-hand animation ownership may hide weapon-root assumptions if tests only cover left-hand IK success
- PiP reticle/render-target fixes can mask transform ownership bugs if runtime seams are not isolated first
- old scene/prefab serialization can leave missing-script noise after helper deletion unless cleanup is explicit

## Exit Criteria

The redesign is ready to replace the old system when:
- no scene-local pose helper is required for weapon presentation
- no runtime fallback derives weapon ownership from the arms hierarchy
- scoped ADS uses one explicit alignment seam only
- muzzle-origin firing remains weapon-authored
- irons, non-PiP optics, and PiP optics all pass focused verification
- old helper/fallback code and tests have been deleted or rewritten
