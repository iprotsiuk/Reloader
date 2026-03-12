# Kar98k Scope Calibration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the arbitrary Kar98k PiP click math with authored MRAD calibration, widen the scope to `±60` clicks, and expose inspector tuning controls that separate developer calibration from player runtime adjustments.

**Architecture:** Keep reticle art, player click state, and developer calibration as separate layers. The PiP scope controller converts authored MRAD calibration plus current click state into projection offsets, while the reticle remains a composite PiP overlay. Inspector tuning lives on authored optic calibration data so other scopes can reuse the same pattern.

**Tech Stack:** Unity 6, URP, ScriptableObject optic definitions, MonoBehaviour runtime controllers, NUnit/Unity PlayMode tests.

---

### Task 1: Add failing tests for authored scope calibration contract

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Write the failing tests**
- Add a test proving the real Kar98k optic exposes `±60` click limits.
- Add a test proving PiP reticle/projection mapping is driven by authored MRAD calibration values, not just the old fixed projection offset.
- Add a test proving the developer calibration state is separate from the runtime click counts.

**Step 2: Run tests to verify they fail**
- Run the new focused PlayMode tests.
- Expected: failures because the current scope still clamps at `±20` and uses arbitrary projection offset math.

**Step 3: Commit**
- Do not commit yet unless the slice is isolated and green.

### Task 2: Add authored optic calibration data

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset`

**Step 1: Add authored calibration fields**
- Add fields for:
  - `mradPerClick`
  - `mechanicalZeroOffsetMrad`
  - `reticleCompositeScale`
  - `reticleCompositeOffset`
  - `projectionMradCalibration`
  - authored click limits/defaults if those are moved into optic data

**Step 2: Keep defaults reusable**
- Use generic names and comments so other scopes can reuse the same data path.

### Task 3: Wire runtime click limits and calibration mapping

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/Runtime/ScopeAdjustmentController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs`

**Step 1: ScopeAdjustmentController**
- Add a way to configure runtime limits/defaults from authored optic data.
- Preserve runtime player click state semantics.

**Step 2: AttachmentManager**
- Apply authored optic adjustment limits/defaults when a scope is mounted.

**Step 3: RenderTextureScopeController**
- Remove reliance on the old arbitrary `projectionOffsetPerClick` behavior.
- Convert:
  - authored mechanical-zero MRAD offset
  - plus runtime click counts * `mradPerClick`
  - through a proper MRAD-to-projection mapping
- Apply optional calibration trim for reticle scale/offset in the PiP composite path.

### Task 4: Replace the reticle sprite with the clean PNG and expose calibration knobs

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteAReticle.asset`
- Modify: `Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kEbr7cReticle.png`
- Modify: `Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kEbr7cReticle.png.meta`

**Step 1: Import/use the clean PNG**
- Ensure the new PNG stays transparent and single-sprite.

**Step 2: Keep reticle tuning explicit**
- Do not bury reticle scale/offset in arbitrary code constants.
- Route any visual trim through authored calibration fields that are visible in inspector.

### Task 5: Verify the focused slice

**Files:**
- Modify as needed based on test outcomes only.

**Step 1: Focused verification**
- Run the new focused scope calibration tests.
- Re-run existing PiP reticle tests that prove:
  - reticle composites through the PiP path
  - world-space reticle does not leak outside the lens

**Step 2: Broader regression spot-check**
- Re-run the scope adjustment tests that cover ADS input behavior and tooltip state.

**Step 3: Hygiene**
- Run `git diff --check` on touched files.

### Task 6: Checkpoint and PR prep

**Files:**
- Commit only the scope calibration slice.

**Step 1: Commit**
- Use a focused commit message for authored scope calibration.

**Step 2: Push**
- Push to a fresh branch for a non-draft PR.

**Step 3: Review**
- Request `@codex` review after verification.
