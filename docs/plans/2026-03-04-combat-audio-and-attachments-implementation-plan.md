# Combat Audio + Attachments (Scope/Magazine/Muzzle) Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a scalable combat audio pipeline and data-driven scope/magazine/muzzle attachment behaviors (including detachable magazine reload visuals) without modifying third-party asset packs.

**Architecture:** Keep third-party prefabs/meshes as visual sources only and implement behavior in project-owned wrappers/runtime systems. Extend the implemented ADS framework under `Assets/Game/Weapons/**` for scope behavior, and bridge `_Project/Weapons` fire/reload events into attachment/audio modules. Keep everything data-driven through ScriptableObjects and prefab sockets.

**Tech Stack:** Unity 6.3 (6000.3.x), C#, ScriptableObjects, existing `AttachmentManager`/`AdsStateController`/`WeaponAimAligner`, `_Project/Weapons` runtime (`PlayerWeaponController`), PlayMode tests.

---

### Task 1: Import + Catalog External Combat Audio Assets

**Files:**
- Create: `Reloader/Assets/_Project/Audio/SFX/Gunshots/` (imported assets)
- Create: `Reloader/Assets/_Project/Audio/SFX/Impacts/` (imported assets)
- Create: `Reloader/Assets/_Project/Audio/SFX/Footsteps/` (imported assets)
- Create: `Reloader/Assets/_Project/Audio/Data/CombatAudioCatalog.asset`
- Create: `Reloader/Assets/_Project/Audio/Scripts/CombatAudioCatalog.cs`

**Step 1: Import clips from external folder**
- Source: `/Users/ivanprotsiuk/Documents/SOUNDS`
- Copy clips into categorized folders under `Assets/_Project/Audio/SFX/**`.

**Step 2: Configure import presets**
- Gunshots: PCM/ADPCM, Load In Background off, Preload on.
- Impacts/footsteps: compressed in memory where appropriate.

**Step 3: Define catalog SO**
- Add strongly-typed fields/lists for shot/impact/footstep clip groups.

**Step 4: Verify catalog resolves clips**
- Manual inspector validation + one playmode smoke test that loads the asset.

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/Audio

git commit -m "feat(audio): import and catalog combat sfx assets"
```

### Task 2: Add Weapon Combat Audio Runtime Emitter

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponCombatAudioEmitter.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponCombatAudioEmitterPlayModeTests.cs`

**Step 1: Write failing test**
- Fire event invokes emitter with muzzle position and active weapon id.
- Reload start/end invoke optional reload audio hooks.

**Step 2: Implement minimal emitter**
- Resolve clips by weapon/attachment from `CombatAudioCatalog`.
- Play one-shot at muzzle or pooled AudioSource.

**Step 3: Wire into controller event flow**
- Hook on fire/reload transitions in existing runtime flow.

**Step 4: Run targeted test**
```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform playmode -testResults "$(pwd)/.tmp/weapon-audio.xml" -testFilter "Reloader.Weapons.Tests.PlayMode.WeaponCombatAudioEmitterPlayModeTests" -quit
```

**Step 5: Commit**
```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponCombatAudioEmitter.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponCombatAudioEmitterPlayModeTests.cs

git commit -m "feat(weapons): add combat audio emitter for fire and reload"
```

### Task 3: Add Muzzle Attachment Definition + Runtime Hooks

**Files:**
- Create: `Reloader/Assets/Game/Weapons/WeaponDefinitions/MuzzleAttachmentDefinition.cs`
- Create: `Reloader/Assets/Game/Weapons/Runtime/MuzzleAttachmentRuntime.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/PistolView.prefab`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/MuzzleAttachmentRuntimePlayModeTests.cs`

**Step 1: Write failing test**
- Equipping muzzle attachment changes fire VFX/audio profile.

**Step 2: Implement definition + runtime**
- Definition fields: muzzle prefab, flash prefab, fire clip override, light flash params.
- Runtime resolves active muzzle socket and emits configured effects on fire.

**Step 3: Wire sockets**
- Ensure view prefabs expose stable `Muzzle` socket for runtime.

**Step 4: Run targeted tests**
- `MuzzleAttachmentRuntimePlayModeTests` + existing weapon fire tests.

**Step 5: Commit**
```bash
git add Reloader/Assets/Game/Weapons/WeaponDefinitions/MuzzleAttachmentDefinition.cs \
  Reloader/Assets/Game/Weapons/Runtime/MuzzleAttachmentRuntime.cs \
  Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs \
  Reloader/Assets/_Project/Weapons/Prefabs/PistolView.prefab \
  Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/MuzzleAttachmentRuntimePlayModeTests.cs

git commit -m "feat(attachments): add data-driven muzzle attachment runtime"
```

### Task 4: Add Detachable Magazine Runtime Visuals

**Files:**
- Create: `Reloader/Assets/Game/Weapons/WeaponDefinitions/MagazineAttachmentDefinition.cs`
- Create: `Reloader/Assets/Game/Weapons/Runtime/DetachableMagazineRuntime.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Animations/PlayerArmsAnimationEventReceiver.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/PistolView.prefab`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/DetachableMagazineRuntimePlayModeTests.cs`

**Step 1: Write failing test**
- Reload start hides/simulates detached mag.
- Reload insert event restores equipped magazine visual.

**Step 2: Implement runtime module**
- Manage attached mag renderer and optional dropped mag prop spawn.
- Use deterministic timing hooks from existing reload events.

**Step 3: Add prefab sockets/refs**
- `MagazineSocket` + optional `MagazineDropSocket` in view prefabs.

**Step 4: Validate with playmode tests**
- Ensure no null refs when weapon has non-detachable magazine profile.

**Step 5: Commit**
```bash
git add Reloader/Assets/Game/Weapons/WeaponDefinitions/MagazineAttachmentDefinition.cs \
  Reloader/Assets/Game/Weapons/Runtime/DetachableMagazineRuntime.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Animations/PlayerArmsAnimationEventReceiver.cs \
  Reloader/Assets/_Project/Weapons/Prefabs/PistolView.prefab \
  Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/DetachableMagazineRuntimePlayModeTests.cs

git commit -m "feat(attachments): add detachable magazine reload visuals"
```

### Task 5: Extend Scope Attachment Integration with Existing ADS Framework

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs`
- Modify: `Reloader/Assets/Game/Weapons/UI/ScopeMaskController.cs`
- Create: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Write failing test**
- Scope swap updates active sight anchor immediately.
- High-mag scope activates mask mode, low-mag does not.

**Step 2: Implement integration updates**
- Ensure active scope metadata (zoom range, reticle, visual mode) hot-swaps without controller reset.

**Step 3: Verify zoom + sensitivity behavior**
- Keep 1x-40x clamp and smooth interpolation intact on optic change.

**Step 4: Run targeted tests**
- Existing ADS tests + new scope integration tests.

**Step 5: Commit**
```bash
git add Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs \
  Reloader/Assets/Game/Weapons/Runtime/AdsStateController.cs \
  Reloader/Assets/Game/Weapons/UI/ScopeMaskController.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs

git commit -m "feat(ads): integrate scope attachment hot-swap with mask/zoom"
```

### Task 6: Add Footstep + Impact Audio Routing

**Files:**
- Create: `Reloader/Assets/_Project/Audio/Scripts/FootstepAudioRouter.cs`
- Create: `Reloader/Assets/_Project/Audio/Scripts/ImpactAudioRouter.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs` (or existing movement event source)
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs`
- Test: `Reloader/Assets/_Project/Audio/Tests/PlayMode/FootstepAndImpactAudioPlayModeTests.cs`

**Step 1: Write failing tests**
- Footsteps emit clips by locomotion cadence.
- Projectile impacts emit correct clip group.

**Step 2: Implement routers**
- Keep surface routing simple first (default + optional tag/material map).

**Step 3: Run tests**
- Targeted playmode tests + smoke in `MainTown` test scene.

**Step 4: Commit**
```bash
git add Reloader/Assets/_Project/Audio/Scripts \
  Reloader/Assets/_Project/Player/Scripts/PlayerMover.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs \
  Reloader/Assets/_Project/Audio/Tests/PlayMode/FootstepAndImpactAudioPlayModeTests.cs

git commit -m "feat(audio): add footstep and impact audio routing"
```

### Task 7: Prefab + Scene Wiring for Demo Validation

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/PistolView.prefab`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/PistolPickup.prefab`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/RiflePickup.prefab`
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Wire attachment sockets and runtime components**
- Scope slot, muzzle socket, magazine socket/drop socket.

**Step 2: Replace missing-script third-party behavior nodes with wrapper-owned runtime components**
- Keep mesh visuals, remove dependence on missing vendor scripts.

**Step 3: Manual validation checklist**
- Fire shot audio + muzzle flash.
- Reload shows detachable mag behavior.
- Scope ADS alignment/mask/zoom works.
- No missing script warnings on weapon visual prefabs.

**Step 4: Commit**
```bash
git add Reloader/Assets/_Project/Weapons/Prefabs \
  Reloader/Assets/_Project/World/Scenes/MainTown.unity

git commit -m "chore(weapons): wire combat audio and attachment demo prefabs"
```

### Task 8: Final Verification + Docs Sync

**Files:**
- Modify: `docs/design/ads-optics-framework.md`
- Modify: `docs/design/weapons-and-ballistics.md`
- Create: `docs/plans/2026-03-04-combat-audio-and-attachments-validation.md`

**Step 1: Run verification commands**
```bash
bash scripts/verify-docs-and-context.sh || true
bash scripts/verify-extensible-development-contracts.sh
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_EDITOR" -batchmode -projectPath "$(pwd)/Reloader" -runTests -testPlatform playmode -testResults "$(pwd)/.tmp/combat-audio-attachments.xml" -testFilter "Reloader.Weapons.Tests.PlayMode|Reloader.UI.Tests.PlayMode" -quit
```

**Step 2: Record known failures with evidence**
- If any unrelated guardrails fail, document explicitly.

**Step 3: Commit docs/validation report**
```bash
git add docs/design/ads-optics-framework.md docs/design/weapons-and-ballistics.md docs/plans/2026-03-04-combat-audio-and-attachments-validation.md

git commit -m "docs: sync combat audio and attachment runtime contracts"
```
