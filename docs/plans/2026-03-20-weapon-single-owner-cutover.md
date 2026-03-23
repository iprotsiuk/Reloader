# Weapon Single-Owner Cutover Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make `WeaponPresentationRoot` the only weapon owner and `WeaponHandRigController` the only hand-sync owner, while treating `ik_hand_gun`, `ik_hand_l`, and `ik_hand_r` as animation bones only.

**Architecture:** The live weapon view must be owned only through `CameraPivot/WeaponPresentationRoot`. The hand rig stays separate: `WeaponHandRigController` reads the equipped view and drives hand IK, but it must not infer weapon ownership from the animator bone chain. `PlayerRigMenu` and `WeaponsSceneWiring` already model the sibling-root contract correctly, so this cutover only removes the stale `ik_hand_gun` fallback in the remaining MainTown wiring path and tightens the regression tests around that split.

**Tech Stack:** Unity 6 C#, Unity Test Framework, editor wiring, playmode/editmode tests, visible Unity scene/prefab validation.

---

### Task 1: Remove the last `ik_hand_gun` ownership fallback

**Files:**
- Modify: `Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs`
- Modify: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerWeaponControllerWeaponPresentationRootTests.cs`
- Modify: `Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs`

**Step 1: Write the failing tests**

Update `MainTownCombatWiringEditModeTests.MainTownCombatWiring_ResolveWeaponViewParent_PrefersWeaponPresentationRoot_OverLegacyIkHandGun` so the legacy arms hierarchy is only background noise. The assertion must require `WeaponPresentationRoot` under `CameraPivot`, and it must fail if the helper returns `ik_hand_gun`, `PlayerArms`, or the animator root.

Update `PlayerWeaponControllerWeaponPresentationRootTests` so a legacy `ik_hand_gun` bone existing under `PlayerArms` does not count as a weapon owner or mount source. Keep `WeaponPresentationRoot` as the only acceptable weapon parent.

If the current test shape still depends on `ik_hand_gun` as a seed, replace that expectation with an explicit negative case that proves the controller resolves or creates `WeaponPresentationRoot` instead.

**Step 2: Run the focused editmode filter and confirm it fails**

Run:

```bash
bash scripts/run-unity-tests.sh editmode "Reloader.Core.Tests.EditMode.PlayerWeaponControllerWeaponPresentationRootTests|Reloader.World.Tests.EditMode.MainTownCombatWiringEditModeTests" tmp/weapon-single-owner-red.xml tmp/weapon-single-owner-red.log
```

Expected: fail because `MainTownCombatWiring.ResolveWeaponViewParent` still has the legacy bone/root fallback.

**Step 3: Implement the minimal code change**

In `MainTownCombatWiring.ResolveWeaponViewParent`, delete `FindDescendantByName(root, "ik_hand_gun") ?? root`.

Replace it with the same explicit `CameraPivot/WeaponPresentationRoot` create-or-find flow already used by `WeaponsSceneWiring`, and keep the viewmodel layer assignment on the root.

Do not add any new bone-derived ownership logic. If the helper cannot resolve `CameraPivot`, return `null` instead of falling back to a bone or animator root.

Do not change `PlayerWeaponController` unless the new tests expose an actual stale acceptance path there; the cutover should stay local to the one remaining fallback.

**Step 4: Re-run the same editmode filter**

Run the same command again.

Expected: pass.

**Step 5: Commit the green slice**

```bash
git add Reloader/Assets/_Project/World/Editor/MainTownCombatWiring.cs Reloader/Assets/_Project/Core/Tests/EditMode/PlayerWeaponControllerWeaponPresentationRootTests.cs Reloader/Assets/_Project/World/Tests/EditMode/MainTownCombatWiringEditModeTests.cs
git commit -m "fix: remove legacy ik_hand_gun ownership fallback"
```

---

### Task 2: Prove the hand rig still only syncs hands, not ownership

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponHandRigControllerPlayModeTests.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write the failing playmode regression**

Add a case to `WeaponHandRigControllerPlayModeTests` that builds a `CameraPivot` tree containing `PlayerArms`, `Armature`, `ik_hand_root`, and `ik_hand_gun`, then equips a real weapon view with `WeaponViewHandAnchors`.

The test must assert that `SyncHandTargets()` copies grip transforms from `EquippedWeaponViewTransform` only. It should fail if any hierarchy search still treats `ik_hand_gun` as the source of hand targets or ownership.

Keep the existing positive hand-rig tests intact; the new regression should focus only on the legacy-bone isolation rule.

**Step 2: Run the targeted playmode filter and confirm it fails**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.Weapons.Tests.PlayMode.WeaponHandRigControllerPlayModeTests tmp/weapon-hand-rig-red.xml tmp/weapon-hand-rig-red.log
```

Expected: fail until the new regression is satisfied.

**Step 3: Implement the smallest possible cleanup**

If the new case exposes a stale setup assumption, remove that assumption.

Otherwise leave `WeaponHandRigController` runtime logic alone. Its job is already limited to syncing hand targets from the equipped weapon view, and it should not gain any new ownership logic.

In `PlayerWeaponControllerPlayModeTests`, keep the integration coverage that proves the equipped view still spawns, the hand anchors survive, and the hand rig still activates during scoped ADS. If this slice needs a small helper cleanup to make the split clearer, do that here instead of widening the runtime surface.

**Step 4: Re-run the hand-rig class and the controller playmode class**

Run:

```bash
bash scripts/run-unity-tests.sh playmode Reloader.Weapons.Tests.PlayMode.WeaponHandRigControllerPlayModeTests tmp/weapon-hand-rig-green.xml tmp/weapon-hand-rig-green.log
bash scripts/run-unity-tests.sh playmode Reloader.Weapons.Tests.PlayMode.PlayerWeaponControllerPlayModeTests tmp/weapon-player-controller-green.xml tmp/weapon-player-controller-green.log
```

Expected: both pass, including `ScopedAds_WithMagnifiedOptic_DoesNotFreezePackAnimator`.

**Step 5: Commit the verification slice**

```bash
git add Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponHandRigControllerPlayModeTests.cs Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "test: lock hand sync to equipped weapon view"
```

---

### Task 3: Final visible-editor validation

**Files:**
- Inspect: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab`
- Inspect: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Open the authored prefab and scene in Unity**

Open `PlayerRoot_MainTown.prefab` and `MainTown.unity` after the code/tests are green.

**Step 2: Verify the hierarchy visually**

Confirm that `CameraPivot` has sibling children `WeaponPresentationRoot` and `PlayerArms`.

Confirm that `WeaponHandRigController` is on `PlayerRoot`, and that `ik_hand_gun`, `ik_hand_l`, and `ik_hand_r` remain inside the animation rig only.

**Step 3: Capture the visible evidence**

Take a Unity hierarchy or scene screenshot that shows the sibling-root contract.

Stop immediately if any weapon parent still hangs off a bone chain or `PlayerArms`.

**Step 4: Close out without asset churn**

If the editor view matches the contract, do not make extra prefab or scene edits. The validation step exists to prove the authored content already matches the single-owner model.
