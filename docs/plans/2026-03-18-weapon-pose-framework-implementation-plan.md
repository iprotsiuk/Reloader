# Weapon Pose Framework Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the current scene-owned Kar98k pose tuning and scoped ADS patchwork with a weapon-prefab-owned, cross-scene weapon presentation framework that supports hipfire, ADS, scoped alignment, arms follow constraints, and reload/fire animations without scoped jitter.

**Architecture:** Weapon view prefabs become the source of truth for coarse pose tuning, scoped ADS reference points, and hand grip anchors. `PlayerWeaponController` remains the runtime orchestrator, `WeaponAimAligner` keeps final camera-authoritative scoped alignment on `AdsPivot`, and the player arms follow the weapon through Unity Animation Rigging instead of driving the weapon through arm bones or freezing the entire animator in scoped ADS.

**Tech Stack:** Unity 6.3, URP, C#, Unity Test Framework PlayMode tests, Unity Animation Rigging (`com.unity.animation.rigging`), Unity MCP for prefab/scene/test verification, GitHub CLI for PR creation.

---

### Task 1: Create the branch, baseline review, and failing regression coverage

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevGiveItemCommandPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Create the working branch**

Run:

```bash
git checkout -b codex/weapon-pose-framework
```

Expected: branch switches from `main` to `codex/weapon-pose-framework`.

**Step 2: Add failing tests for the target contracts**

Add PlayMode coverage for:
- spawned weapon view owns pose tuning without relying on `MainTown`
- scoped Kar98k `give test` still aligns in full ADS after pose ownership moves
- scoped ADS does not require freezing the whole viewmodel animator to remain stable
- reload/bolt animation path can keep progressing while scoped ADS is active

**Step 3: Run only the touched tests to confirm failures**

Run via Unity Test Runner / Unity MCP:
- `PlayerWeaponControllerPlayModeTests`
- `DevGiveItemCommandPlayModeTests`
- `ScopeAttachmentAdsIntegrationPlayModeTests`

Expected: new assertions fail because the current scene-owned helper / scoped freeze path still exists.

**Step 4: Commit the failing tests**

```bash
git add Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs \
        Reloader/Assets/_Project/DevTools/Tests/PlayMode/DevGiveItemCommandPlayModeTests.cs \
        Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "test: lock weapon pose framework regression coverage"
```

### Task 2: Move weapon pose ownership onto the runtime weapon prefab

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponViewPoseTuningHelper.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab`
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Make `WeaponViewPoseTuningHelper` prefab-safe**

Implement helper behavior so it:
- self-resolves the active `PlayerWeaponController` after the equipped view spawns
- treats its own GameObject as the equipped weapon view root instead of relying on a scene-root helper
- owns its base pose, attachment pose overrides, and scoped eye-relief offsets from the view prefab

**Step 2: Author Kar98k pose data on the prefab**

Add the helper to `RifleView.prefab` and move the existing Kar98k base + scope override values from `MainTown` into the prefab-owned component.

**Step 3: Remove scene-owned Kar98k tuning**

Delete the old `WeaponViewPoseTuningHelper` authoring from `MainTown.unity`.

**Step 4: Run the narrow tests again**

Run:
- `PlayerWeaponControllerPlayModeTests`
- `DevGiveItemCommandPlayModeTests`

Expected: prefab-owned pose tuning tests pass; any remaining scoped stability tests may still fail.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponViewPoseTuningHelper.cs \
        Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab \
        Reloader/Assets/_Project/World/Scenes/MainTown.unity
git commit -m "refactor: move weapon pose tuning onto runtime view prefabs"
```

### Task 3: Replace scoped ADS freeze patchwork with explicit presentation ownership

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/FpsViewmodelAnimatorDriver.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/Viewmodel/ViewmodelAnimationAdapter.cs`

**Step 1: Remove whole-animator freeze behavior as the scoped stability mechanism**

Delete or narrow the logic that sets animator speed to `0` for stable scoped ADS. Preserve only the minimum root stabilization needed for the arms rig root if still required.

**Step 2: Keep one owner per transform**

Ensure:
- weapon view root = coarse hip/ADS pose
- `AdsPivot` = final scoped solve
- arms animation state = fire/reload/bolt gestures only
- no logic path re-parents or re-drives the weapon root from arm bones

**Step 3: Make the runtime bridge resilient**

When scoped optics are equipped or removed, keep `WeaponAimAligner`, ADS bridge references, and presentation eye-relief offsets synchronized without fallback heuristics or duplicate drivers.

**Step 4: Run the targeted tests**

Run:
- `PlayerWeaponControllerPlayModeTests`
- `ScopeAttachmentAdsIntegrationPlayModeTests`

Expected: scoped ADS remains aligned, and new “animator does not hard-freeze” assertions pass.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs \
        Reloader/Assets/_Project/Player/Scripts/FpsViewmodelAnimatorDriver.cs \
        Reloader/Assets/_Project/Player/Scripts/Viewmodel/ViewmodelAnimationAdapter.cs
git commit -m "refactor: remove scoped viewmodel freeze patchwork"
```

### Task 4: Add weapon-authored hand anchors and Animation Rigging support

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponViewHandAnchors.cs`
- Create: `Reloader/Assets/_Project/Player/Scripts/Viewmodel/WeaponHandRigController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab`
- Modify: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs`

**Step 1: Author weapon grip anchors**

Add serialized left/right grip anchors on weapon view prefabs so different weapons and scopes can be tuned per prefab without scene-specific offsets.

**Step 2: Add a player-side rig controller**

Implement a controller that:
- resolves weapon hand anchors from the equipped view
- binds them to Animation Rigging constraints on the player arms rig
- exposes weights/states for hipfire, ADS, reload, bolt/action interactions
- releases or blends constraints when reload/bolt animation needs hand freedom

**Step 3: Wire the player prefab**

Attach the rig controller to the player prefab and bind its rig/constraints so all scenes using the player prefab inherit the same behavior.

**Step 4: Add tests**

Cover:
- missing hand anchors fails cleanly without breaking equip
- equipped rifle exposes anchors and the rig controller binds to them
- reload path can reduce hand constraint ownership without disabling scoped ADS alignment

**Step 5: Run tests**

Run:
- `PlayerWeaponControllerPlayModeTests`
- `PlayerControllerPlayModeTests`

Expected: new hand-anchor/rig tests pass.

**Step 6: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponViewHandAnchors.cs \
        Reloader/Assets/_Project/Player/Scripts/Viewmodel/WeaponHandRigController.cs \
        Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab \
        Reloader/Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab \
        Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs \
        Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerControllerPlayModeTests.cs
git commit -m "feat: add weapon-driven hand rig constraints"
```

### Task 5: Clean scope/runtime fallback clutter and align contracts

**Files:**
- Modify: `Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs`
- Modify: `Reloader/Assets/Game/Weapons/Runtime/WeaponAimAligner.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs`

**Step 1: Remove stale fallback and duplicate-repair behavior that exists only to support the old scene-owned pose flow**

Keep strict contracts:
- optic prefab provides `SightAnchor`
- weapon view prefab provides `AdsPivot`
- no prefab-name fallback for pose ownership
- no scene-specific recovery path for Kar98k presentation

**Step 2: Keep scope-specific presentation data explicit**

Retain per-scope eye-relief and attachment pose overrides, but source them only from the active equipped view’s pose tuning.

**Step 3: Re-run scoped integration tests**

Run:
- `ScopeAttachmentAdsIntegrationPlayModeTests`
- `DevGiveItemCommandPlayModeTests`

Expected: scoped alignment remains correct; no hidden MainTown dependency remains.

**Step 4: Commit**

```bash
git add Reloader/Assets/Game/Weapons/Runtime/AttachmentManager.cs \
        Reloader/Assets/Game/Weapons/Runtime/WeaponAimAligner.cs \
        Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs \
        Reloader/Assets/_Project/Weapons/Tests/PlayMode/ScopeAttachmentAdsIntegrationPlayModeTests.cs
git commit -m "refactor: simplify scoped weapon runtime ownership"
```

### Task 6: Cross-scene prefab/scene validation and PR creation

**Files:**
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`
- Modify: `Reloader/Assets/_Project/World/Scenes/IndoorRangeInstance.unity`
- Modify: `Reloader/Assets/_Project/World/Scenes/FPArmsTuning/FPArmsTuning.unity`
- Modify: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab`

**Step 1: Verify scene independence**

Open/check the scenes that spawn the player and confirm there is no local Kar98k pose ownership left behind.

**Step 2: Run the filtered verification ladder**

Run:
1. touched PlayMode test filters from Tasks 1-5
2. nearest weapon subsystem PlayMode tests
3. player PlayMode tests if the rig controller changed player behavior

Expected: all touched seams pass without scene-specific setup.

**Step 3: Create the non-draft PR**

Run after pushing the branch:

```bash
git push -u origin codex/weapon-pose-framework
gh pr create --base main --head codex/weapon-pose-framework --title "refactor: ship weapon pose framework cleanup" --body-file .git/PR_BODY_WEAPON_POSE.md
```

Create `.git/PR_BODY_WEAPON_POSE.md` first with:
- problem summary
- architecture changes
- test coverage
- remaining tuning hooks for manual iteration

**Step 4: Final verification commit if needed**

If PR feedback or final verification requires a small patch, commit it separately instead of amending.

```bash
git add <touched-files>
git commit -m "fix: address weapon pose framework review feedback"
```
