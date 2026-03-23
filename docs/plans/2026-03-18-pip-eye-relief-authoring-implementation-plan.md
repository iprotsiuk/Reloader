# PiP Eye Relief Authoring Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make PiP scopes honor the optic-authored eye-relief baseline while keeping the prefab-owned scoped pose helper as the per-weapon correction layer.

**Architecture:** `OpticDefinition.eyeReliefBackOffset` remains the global optic knob, and `WeaponViewPoseTuningHelper.ScopedAdsEyeReliefBackOffset` remains the additive runtime correction forwarded into `WeaponAimAligner`. The runtime no longer special-cases PiP optics to zero-out the optic baseline.

**Tech Stack:** Unity, NUnit edit-mode tests, existing `Game/Weapons` ADS runtime, markdown design docs.

---

### Task 1: Lock the regression with a failing test

**Files:**
- Modify: `/Users/ivanprotsiuk/unity/Reloader/Reloader/Assets/_Project/Core/Tests/EditMode/WeaponViewPoseTuningHelperScopedRootPoseTests.cs`

**Step 1: Write the failing test**

Add an edit-mode test around `WeaponAimAligner.ResolveOpticEyeReliefBackOffset(...)` that creates an `OpticDefinition`, sets `_visualModePolicy` to `RenderTexturePiP`, sets `_eyeReliefBackOffset`, and asserts the helper returns the authored value instead of `0f`.

**Step 2: Run test to verify it fails**

Run: `bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.WeaponAimAlignerScopedPoseHoldTests.ResolveOpticEyeReliefBackOffset_UsesAuthoredOpticBaselineForAllVisualModes" tmp/weapon-eye-relief-red.xml tmp/weapon-eye-relief-red.log`

Expected: FAIL on the `RenderTexturePiP` case because the helper still returns `0f`.

**Step 3: Commit**

```bash
git add /Users/ivanprotsiuk/unity/Reloader/Reloader/Assets/_Project/Core/Tests/EditMode/WeaponViewPoseTuningHelperScopedRootPoseTests.cs
git commit -m "test: lock PiP optic eye relief baseline"
```

### Task 2: Apply the minimal runtime fix

**Files:**
- Modify: `/Users/ivanprotsiuk/unity/Reloader/Reloader/Assets/Game/Weapons/Runtime/WeaponAimAligner.cs`

**Step 1: Write minimal implementation**

Remove the PiP-only `0f` branch in `ResolveOpticEyeReliefBackOffset(...)` so the helper always returns `activeOptic.EyeReliefBackOffset` when an optic is present.

**Step 2: Run test to verify it passes**

Run: `bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.WeaponAimAlignerScopedPoseHoldTests.ResolveOpticEyeReliefBackOffset_UsesAuthoredOpticBaselineForAllVisualModes" tmp/weapon-eye-relief-green.xml tmp/weapon-eye-relief-green.log`

Expected: PASS for both `Auto` and `RenderTexturePiP`.

**Step 3: Commit**

```bash
git add /Users/ivanprotsiuk/unity/Reloader/Reloader/Assets/Game/Weapons/Runtime/WeaponAimAligner.cs /Users/ivanprotsiuk/unity/Reloader/Reloader/Assets/_Project/Core/Tests/EditMode/WeaponViewPoseTuningHelperScopedRootPoseTests.cs
git commit -m "fix: apply optic eye relief to PiP scopes"
```

### Task 3: Update the authored contract and verify the surrounding slice

**Files:**
- Modify: `/Users/ivanprotsiuk/unity/Reloader/docs/design/ads-optics-framework.md`
- Modify: `/Users/ivanprotsiuk/unity/Reloader/docs/design/weapon-view-attachment-runtime.md`

**Step 1: Update docs**

Document the authored model explicitly:
- optic definition owns the global baseline
- weapon view pose tuning owns the additive per-weapon/per-attachment correction
- PiP scopes use the same contract instead of a separate zeroed path

**Step 2: Run targeted verification**

Run:
- `bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.WeaponAimAlignerScopedPoseHoldTests" tmp/weapon-eye-relief-suite.xml tmp/weapon-eye-relief-suite.log`
- `bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.WeaponViewPoseTuningHelperScopedRootPoseTests" tmp/weapon-view-pose-root.xml tmp/weapon-view-pose-root.log`

Expected: both suites pass.

**Step 3: Commit**

```bash
git add /Users/ivanprotsiuk/unity/Reloader/docs/design/ads-optics-framework.md /Users/ivanprotsiuk/unity/Reloader/docs/design/weapon-view-attachment-runtime.md /Users/ivanprotsiuk/unity/Reloader/docs/plans/2026-03-18-pip-eye-relief-authoring-implementation-plan.md
git commit -m "docs: clarify PiP eye relief authoring"
```
