# Scoped PiP Optics Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace main-camera FOV zoom for scoped optics with a PiP lens-rendering path that preserves peripheral view, adds peripheral blur for scoped ADS, and supports explicit FFP/SFP reticle behavior.

**Architecture:** Extend the implemented `Assets/Game/Weapons` ADS runtime instead of creating a parallel optics system. High-magnification optics will use a dedicated scope camera + render texture + explicit optic lens-display contract, while `AdsStateController` keeps the main camera FOV stable and drives scoped visual state, reticle behavior, and peripheral effects.

**Tech Stack:** Unity 6.3, C#, URP, Unity PlayMode/EditMode tests, Unity MCP for scene/prefab inspection and validation.

---

### Task 1: Add Failing Coverage For PiP Scope Camera And Main-Camera FOV Preservation

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`
- Reference: `Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs`
- Reference: `Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs`

**Step 1: Write the failing tests**

Add play mode tests that prove:

- magnified scoped ADS does not reduce the main world camera FOV
- scope camera FOV changes as magnification changes
- non-scoped/low-mag behavior remains unaffected where expected

**Step 2: Run test to verify it fails**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/ivanprotsiuk/Documents/unity/Reloader/Reloader -runTests -testPlatform PlayMode -testFilter "PlayerWeaponControllerPlayModeTests|ScopeAttachmentAdsIntegrationPlayModeTests" -logFile -
```

Expected:

- new assertions fail because `AdsStateController` still maps magnification into `_worldCamera.fieldOfView`

**Step 3: Write minimal implementation**

Modify:

- `Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs`

Implement the minimum branching needed so magnified scoped optics preserve main-camera FOV and drive magnification through the scope-camera path instead.

**Step 4: Run test to verify it passes**

Run the same PlayMode command and confirm the new tests pass.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs
git commit -m "test: cover scoped PiP FOV behavior"
```

### Task 2: Add Explicit Scope Lens Display Contract And Real Render-Texture Binding

**Files:**
- Create: `Reloader/Assets/Game/Weapons/Runtime/ScopeLensDisplay.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs`
- Modify: `Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Write the failing test**

Add coverage that proves:

- a mounted optic can expose an explicit lens display target
- the render texture is assigned to that display target during scoped ADS
- missing display wiring logs a clear warning and does not silently fall back to an opaque grey lens

**Step 2: Run test to verify it fails**

Run the same filtered PlayMode suite and confirm the new lens-binding assertions fail.

**Step 3: Write minimal implementation**

Implement:

- `ScopeLensDisplay` component with explicit output binding
- `RenderTextureScopeController` management of a real render texture and output assignment
- runtime optic-instance discovery of `ScopeLensDisplay`
- deterministic warning path for missing lens display wiring

Use Unity MCP to inspect the scoped optic prefab and live scene object wiring after code changes.

**Step 4: Run tests to verify they pass**

Run the filtered PlayMode suite again.

**Step 5: Commit**

```bash
git add Reloader/Assets/Game/Weapons/Runtime/ScopeLensDisplay.cs Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "feat: bind scoped render textures to optic lenses"
```

### Task 3: Add Peripheral Scoped Blur/Vignette Path

**Files:**
- Create or modify: `Reloader/Assets/Game/Weapons/Runtime/PeripheralScopeEffects.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`
- Unity assets/prefabs as needed for scoped effect wiring

**Step 1: Write the failing test**

Add coverage that proves:

- peripheral scoped effects activate only while magnified scoped ADS is active
- peripheral effects do not activate for hip fire or non-scoped optics

**Step 2: Run test to verify it fails**

Run the filtered PlayMode suite and confirm effect-state assertions fail.

**Step 3: Write minimal implementation**

Implement a scoped peripheral-effects component that:

- activates only when scoped PiP visuals are active
- exposes inspectable runtime state for tests
- is driven by `AdsStateController`

Use the cheapest viable implementation first so the architecture is correct before tuning visuals.

**Step 4: Run tests to verify they pass**

Run the filtered PlayMode suite again.

**Step 5: Commit**

```bash
git add Reloader/Assets/Game/Weapons/Runtime/PeripheralScopeEffects.cs Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "feat: add scoped peripheral effects"
```

### Task 4: Add Explicit Reticle Contracts With FFP/SFP Behavior

**Files:**
- Create: `Reloader/Assets/Game/Weapons/WeaponDefinitions/ScopeReticleDefinition.cs`
- Create or modify: `Reloader/Assets/Game/Weapons/Runtime/ScopeReticleController.cs`
- Modify: `Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Write the failing tests**

Add play mode tests that prove:

- FFP reticles change scale as magnification changes
- SFP reticles maintain constant apparent scale as magnification changes
- missing reticle configuration logs clearly and falls back deterministically

**Step 2: Run test to verify it fails**

Run the filtered PlayMode suite and confirm the new reticle-behavior assertions fail.

**Step 3: Write minimal implementation**

Implement:

- reticle definition asset contract
- reticle type enum (`FFP`, `SFP`)
- runtime reticle controller that exposes inspectable scale/state for tests
- integration from optic definition through render-texture scope runtime

Prefer explicit data/config over hard-coded optic-specific logic.

**Step 4: Run tests to verify they pass**

Run the filtered PlayMode suite again.

**Step 5: Commit**

```bash
git add Reloader/Assets/Game/Weapons/WeaponDefinitions/ScopeReticleDefinition.cs Reloader/Assets/Game/Weapons/Runtime/ScopeReticleController.cs Reloader/Assets/Game/Weapons/WeaponDefinitions/OpticDefinition.cs Reloader/Assets/Game/Weapons/Runtime/RenderTextureScopeController.cs Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "feat: add FFP and SFP scope reticles"
```

### Task 5: Unity MCP Validation, Full Verification, Review, And PR

**Files:**
- Verify runtime scene/prefab wiring touched by the tasks above
- No planned source-file target; use actual changed files from Tasks 1-4

**Step 1: Verify scene/prefab wiring in Unity MCP**

Inspect the Kar98k scoped runtime path and ensure:

- the intended runtime weapon view prefab still spawns
- the mounted optic exposes the intended lens display target
- scope camera / peripheral effect references are wired

**Step 2: Run full targeted verification**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/ivanprotsiuk/Documents/unity/Reloader/Reloader -runTests -testPlatform PlayMode -testFilter "PlayerWeaponControllerPlayModeTests|ScopeAttachmentAdsIntegrationPlayModeTests" -logFile -
```

Then run any additional focused commands required by changed files.

**Step 3: Request code review**

- request review for the full implementation range
- fix Critical and Important feedback before creating the PR

**Step 4: Create PR to `main`**

Use GitHub CLI:

```bash
gh pr create --base main --head feat/scoped-pip-optics --title "feat: add scoped PiP optics" --body-file /tmp/scoped-pip-pr.md
```

**Step 5: Final commit if needed**

```bash
git add <changed files>
git commit -m "fix: address scoped PiP review feedback"
```
