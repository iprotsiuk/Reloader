# Prod-Ready Scoped Precision Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Normalize raw look input, make PiP scoped precision come from explicit optic-driven scaling instead of main-camera FOV, and verify that high-mag ADS remains precise even when PiP keeps gameplay FOV unchanged.

**Architecture:** Keep `PlayerLookController` as the sole yaw/pitch authority. Move raw mouse normalization into `PlayerInputReader`, compute high-mag PiP precision in `AdsStateController`, bridge the final runtime multiplier through `PlayerWeaponController`, and keep `RenderTextureScopeController` presentation-only.

**Tech Stack:** Unity 6, URP, Input System, ScriptableObject optic definitions, MonoBehaviour runtime controllers, NUnit/Unity PlayMode tests.

---

### Task 1: Add focused failing tests for the current precision gap

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Write the failing tests**

Add tests that prove:
- PiP scoped precision is not allowed to depend on main-camera FOV changes alone
- high magnification applies a dedicated scoped runtime multiplier even when gameplay FOV stays fixed
- normalized mouse input produces the same angular result for the same raw sample in the intended calibration seam

Example assertions to add:

```csharp
[Test]
public void PlayerLookController_Tick_PipScopedPrecision_DoesNotRequireLowerMainCameraFov()
{
    // Arrange a PiP scoped runtime multiplier with unchanged world FOV.
    // Assert the yaw delta is reduced by runtime scoped precision, not by camera FOV tricks.
}

[Test]
public void AdsStateController_PipOptic_HighMagnification_UsesExplicitPrecisionScale()
{
    // Arrange a PiP optic at 25x with world FOV unchanged.
    // Assert CurrentSensitivityScale matches the authored high-mag precision curve.
}
```

**Step 2: Run tests to verify they fail**

Run:

```bash
Unity PlayMode filter for PlayerControllerPlayModeTests scoped precision tests
Unity PlayMode filter for ScopeAttachmentAdsIntegrationPlayModeTests scoped precision tests
```

Expected:
- current behavior either still depends on generic FOV scaling expectations or lacks the explicit PiP high-mag precision contract you want

**Step 3: Commit**

Do not commit yet. Keep the slice uncommitted until the runtime and tests are green.

### Task 2: Normalize mouse delta at the input boundary

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs`
- Optional modify: `Reloader/Assets/_Project/Player/InputSystem_Actions.inputactions`

**Step 1: Add explicit mouse normalization**

Implement a local helper in `PlayerInputReader` so mouse look from `<Pointer>/delta` is scaled into stable gameplay look units before assignment to `LookInput`.

Target shape:

```csharp
private const float MouseDeltaNormalizationScale = 0.02f;

private Vector2 ReadLookInput(bool isDevConsoleVisible)
{
    var rawLook = !isDevConsoleVisible && _lookAction != null
        ? _lookAction.ReadValue<Vector2>()
        : Vector2.zero;

    if (Mouse.current != null)
    {
        return rawLook * MouseDeltaNormalizationScale;
    }

    return rawLook;
}
```

If the input action asset is changed instead:
- add an explicit `ScaleVector2(...)` processor to the `<Pointer>/delta` binding
- keep the scale value mirrored in tests and design docs

Prefer code normalization first if you want the runtime rule to stay obvious in C# and avoid hidden action-asset magic.

**Step 2: Keep non-mouse behavior stable**

Do not apply mouse normalization to right-stick/gamepad look.

**Step 3: Run the smallest relevant player look tests**

Run:

```bash
Unity PlayMode filter for PlayerControllerPlayModeTests look-input seams
```

Expected:
- tests updated for normalization pass
- non-ADS look behavior remains stable after calibration updates

### Task 3: Split PiP scoped precision from generic FOV scaling

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerLookController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`

**Step 1: Add an explicit PiP scoped precision contract in ADS runtime**

Extend `AdsStateController` with:
- a clearly named long-range precision curve for PiP optics
- a helper that chooses PiP precision based on optic magnification
- separation between generic FOV-based look scaling and PiP precision scaling

Target shape:

```csharp
[SerializeField] private AnimationCurve _pipPrecisionScaleByMagnification = new AnimationCurve(
    new Keyframe(1f, 1f),
    new Keyframe(4f, 0.4f),
    new Keyframe(10f, 0.12f),
    new Keyframe(20f, 0.06f),
    new Keyframe(25f, 0.04f));
```

Use this curve only for PiP optics. Keep it explicit.

**Step 2: Stop treating main-camera FOV as PiP precision authority**

In `PlayerLookController`, add a runtime gate so FOV-based look scaling can be disabled for PiP scoped ADS.

Candidate shape:

```csharp
[SerializeField] private bool _allowFovSensitivityScaling = true;

public bool AllowFovSensitivityScaling
{
    get => _allowFovSensitivityScaling;
    set => _allowFovSensitivityScaling = value;
}
```

Then:

```csharp
var fovScale = _allowFovSensitivityScaling ? GetFieldOfViewSensitivityScale() : 1f;
sensitivity *= fovScale;
```

Drive that flag from weapon runtime only while PiP scoped ADS is active.

**Step 3: Bridge the explicit PiP precision multiplier**

In `PlayerWeaponController`, keep using the existing runtime bridge, but pass the final PiP scoped precision product rather than relying on unchanged world FOV to imply precision.

The runtime bridge should remain:
- small
- scalar-only
- free of reticle or render-texture logic

**Step 4: Run targeted ADS/PiP tests**

Run:

```bash
Unity PlayMode filter for ScopeAttachmentAdsIntegrationPlayModeTests
Unity PlayMode filter for PlayerControllerPlayModeTests ADS sensitivity seams
```

Expected:
- PiP high-mag precision works with fixed world FOV
- non-PiP zoom paths still behave as expected

### Task 4: Add optic-level override hooks without forcing per-optic magic numbers

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs`
- Optional modify: `Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset`

**Step 1: Add optional per-optic precision override data**

Add minimal override support such as:

```csharp
[SerializeField] private bool _usePrecisionScaleOverride;
[SerializeField, Min(0.001f)] private float _precisionScaleMultiplier = 1f;
```

Expose read-only accessors.

**Step 2: Apply override on top of the shared PiP precision curve**

In `AdsStateController`, use:

```csharp
finalPipPrecision = sharedCurve.Evaluate(CurrentMagnification) * optic.PrecisionScaleMultiplier;
```

Only use per-optic overrides when needed.

**Step 3: Keep Kar98k data close to default initially**

Do not start by hardcoding bespoke Kar98k-only controller logic. Use the shared curve first, then use the asset override only if calibration proves the optic needs a correction factor.

### Task 5: Add explicit calibration helpers for long-range tuning

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`
- Optional create: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopedPrecisionCalibrationTests.cs`
- Optional docs update: `docs/plans/2026-03-19-prod-ready-scoped-precision-design.md`

**Step 1: Add a measurable angular-step seam**

Add a test or helper that computes resulting angular step from one normalized input sample:

```csharp
var deltaDegrees = yawAfter - yawBefore;
var deltaMrad = deltaDegrees * 17.4533f;
Assert.That(deltaMrad, Is.LessThan(expectedUpperBound));
```

**Step 2: Calibrate the high-mag target band**

Tune the shared PiP precision curve until the high-mag seam lands in the desired target range:
- roughly `0.005` to `0.01 mrad` per smallest practical normalized sample

Do this with the test seam, not by eyeballing the scope image.

### Task 6: Keep PiP visual quality separate from precision

**Files:**
- Modify only if required: `Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs`
- Optional modify: `Reloader/Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset`

**Step 1: Leave PiP authority alone**

Do not move precision logic into `RenderTextureScopeController`.

**Step 2: Add or tune visual-quality defaults only if needed**

If the image still looks too steppy after the precision fix:
- raise the default high-mag visual budget to `2048`
- keep `4096` as an optional higher-end setting
- do not require `8k`

This is a visual-quality follow-up, not part of the true precision fix.

### Task 7: Focused verification ladder

**Files:**
- No new files unless test failures require local additions

**Step 1: Run smallest player look tests**

Run:

```bash
Unity PlayMode filter for PlayerControllerPlayModeTests
```

Focus on:
- ADS sensitivity
- FOV scaling
- normalized look behavior

**Step 2: Run smallest PiP/optic tests**

Run:

```bash
Unity PlayMode filter for ScopeAttachmentAdsIntegrationPlayModeTests
```

Focus on:
- PiP optics with unchanged world FOV
- reticle/render path non-regression
- scoped runtime bridge behavior

**Step 3: Hygiene**

Run:

```bash
git diff --check
```

Expected:
- no whitespace or merge-artifact issues

### Task 8: Checkpoint and review

**Files:**
- Commit only the input/look/ADS/PiP precision slice when green

**Step 1: Commit**

Use a focused message such as:

```bash
git commit -m "feat: add optic-driven PiP scoped precision scaling"
```

**Step 2: Review**

Request review on:
- input normalization scope
- PiP vs non-PiP FOV scaling split
- high-mag calibration evidence
