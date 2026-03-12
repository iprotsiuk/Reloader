# Kar98k Reticle And PiP Adjustment Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the Kar98k PiP scope use a transparent FFP EBR-7C reticle and visibly shift the PiP scene under the fixed reticle when windage/elevation adjustments change.

**Architecture:** Reuse the existing scoped PiP split. Keep reticle binding in `ScopeReticleController` via the authored Kar98k `ScopeReticleDefinition`, and teach `RenderTextureScopeController` to apply a clamped projection offset sourced from the active `ScopeAdjustmentController`.

**Tech Stack:** Unity C#, ScriptableObject weapon data, authored Unity assets, NUnit PlayMode tests, Unity MCP/headless Unity verification.

---

### Task 1: Lock Failing Scope-Adjustment Visualization Tests

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Write the failing test**

- Add a PlayMode test that builds a scoped PiP optic with a reticle and live `ScopeAdjustmentController`.
- Capture the scope camera state at zero adjustment.
- Apply windage/elevation clicks through the live ADS path.
- Assert the PiP-rendering state changes while the reticle controller remains centered.

**Step 2: Run test to verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.Weapons.Tests.PlayMode.ScopeAttachmentAdsIntegrationPlayModeTests tmp/scope-adjustment-red.xml tmp/scope-adjustment-red.log
```

Expected:
- New test fails because scope adjustments currently do not affect PiP rendering.

**Step 3: Write minimal implementation**

- Add only the smallest runtime hook needed to expose and apply PiP offset from scope adjustment values.

**Step 4: Run test to verify it passes**

Run the same focused PlayMode test command and confirm the new test passes.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs
git commit -m "feat: visualize scoped adjustments through pip offset"
```

### Task 2: Implement Clamped PiP Offset From Scope Adjustments

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs` only if needed to surface active adjustment state cleanly

**Step 1: Write the failing test**

- Extend the focused PlayMode test to assert offset clamping at large click values.

**Step 2: Run test to verify it fails**

Run the focused PlayMode scope-attachment test command again.

Expected:
- Clamp assertion fails before runtime code is updated.

**Step 3: Write minimal implementation**

- Resolve active scope adjustment state from the existing attachment/ADS runtime path.
- Apply a normalized offset to the scope camera projection or equivalent PiP render path.
- Clamp the offset to a safe authored max.

**Step 4: Run test to verify it passes**

- Re-run the focused PlayMode tests.

**Step 5: Commit**

```bash
git add Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "feat: clamp pip offset for scoped adjustments"
```

### Task 3: Author The Kar98k Transparent FFP Reticle

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteAReticle.asset`
- Modify: `Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset` only if needed
- Create or modify: transparent Kar98k reticle texture/sprite asset under `Reloader/Assets/_Project/Weapons/Data/Attachments/`

**Step 1: Write the failing test**

- Add or extend a PlayMode test proving the real Kar98k optic binds the authored reticle sprite and that the reticle definition mode is `Ffp`.

**Step 2: Run test to verify it fails**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.Weapons.Tests.PlayMode.ScopeAttachmentAdsIntegrationPlayModeTests tmp/kar98k-reticle-red.xml tmp/kar98k-reticle-red.log
```

Expected:
- Test fails because the current authored asset is not the requested transparent EBR-7C FFP setup.

**Step 3: Write minimal implementation**

- Create/update the transparent reticle sprite.
- Point the Kar98k reticle asset at that sprite.
- Ensure the reticle definition mode is `Ffp`.

**Step 4: Run test to verify it passes**

- Re-run the focused reticle tests.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteAReticle.asset Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset Reloader/Assets/_Project/Weapons/Data/Attachments/<reticle-sprite-files> Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "feat: author kar98k ebr reticle"
```

### Task 4: Run Regression Verification And Update Progress Notes

**Files:**
- Modify: `docs/plans/progress/2026-03-07-maintown-population-slots-progress.md` only if this slice touches active PR notes, otherwise create a scoped progress note if needed

**Step 1: Run targeted verification**

```bash
bash scripts/run-unity-tests.sh playmode Reloader.Weapons.Tests.PlayMode.ScopeAttachmentAdsIntegrationPlayModeTests tmp/scope-reticle-regression.xml tmp/scope-reticle-regression.log
bash scripts/verify-docs-and-context.sh
git diff --check
```

**Step 2: Confirm expected output**

- Scope attachment PlayMode tests pass.
- Docs/context verification passes.
- `git diff --check` is clean.

**Step 3: Record progress**

- Note that Kar98k now uses the transparent FFP EBR-style reticle and that adjustment input visibly shifts PiP.

**Step 4: Commit**

```bash
git add docs/plans/progress/...
git commit -m "docs: record kar98k reticle and pip adjustment slice"
```
