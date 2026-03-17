# Weapon Presentation Final Cut Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Move weapon presentation completely out of the arms branch so scoped ADS has one transform ownership path only.

**Architecture:** `CameraPivot` owns two sibling roots: `WeaponPresentationRoot` for the live weapon and `PlayerArms` for visuals. `PlayerWeaponController` mounts only under `WeaponPresentationRoot`. Authoring and tests are updated to the same contract, and legacy animated-parent fallback logic is deleted.

**Tech Stack:** Unity 6 C#, prefabs/scenes/editor wiring, NUnit editmode/playmode tests, Unity MCP validation.

---

### Task 1: Lock the runtime parent contract to `CameraPivot/WeaponPresentationRoot`

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerWeaponControllerWeaponPresentationRootTests.cs`

**Step 1: Write/adjust the failing test**
- Extend the existing weapon-presentation-root tests so they assert the resolved parent is a direct child of `CameraPivot`, not `PlayerArms`.
- Add a case that proves legacy `ik_hand_gun` ancestry is rejected and not used to seed the new root.

**Step 2: Run the focused editmode test filter and verify it fails**
Run: Unity editmode filter for `PlayerWeaponControllerWeaponPresentationRootTests`
Expected: failure because current runtime still resolves/creates the presentation root under `PlayerArms`.

**Step 3: Implement the minimal runtime change**
- Change `ResolveDefaultWeaponViewParent` / `ResolveViewmodelRoot` adjacent logic so `WeaponPresentationRoot` resolves under `CameraPivot`.
- Delete the legacy placement branch that seeds the root from `ik_hand_gun` world pose.
- Keep `ApplyViewmodelLayer` on the weapon root.

**Step 4: Run the focused test again and make it pass**
- Re-run the same editmode test filter.
- Validate the touched script with Unity validation.

### Task 2: Align authoring and editor wiring to the new sibling hierarchy

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/Editor/PlayerRigMenu.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Editor/WeaponsSceneWiring.cs`
- Modify: `Reloader/Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab`
- Modify: `Reloader/Assets/_Project/World/Scenes/MainTown.unity`

**Step 1: Inspect the authored hierarchy contract**
- Confirm current prefab/scene/editor wiring still nests `WeaponPresentationRoot` under `PlayerArms`.

**Step 2: Update authoring code**
- Make `PlayerRigMenu` create `CameraPivot/WeaponPresentationRoot` and `CameraPivot/PlayerArms` as siblings.
- Make `WeaponsSceneWiring` enforce the same structure.

**Step 3: Update authored assets**
- Move the serialized `WeaponPresentationRoot` objects in `PlayerRoot_MainTown.prefab` and `MainTown.unity` to the new sibling location.
- Ensure `_weaponViewParent` points at the moved root.

**Step 4: Verify authored contract**
- Use Unity hierarchy inspection and/or targeted editmode validation/tests to confirm the new path exists and old nested path is gone.

### Task 3: Remove remaining local runtime dependency on arms-owned scoped state

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/FpsViewmodelAnimatorDriver.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/PlayerWeaponControllerScopedViewmodelStabilizationTests.cs`

**Step 1: Tighten the failing test coverage**
- Keep the scoped stabilization tests focused on the real ownership rule: stabilization applies to arms visuals only, while the weapon remains independently mounted.

**Step 2: Implement the runtime cleanup**
- Keep `FpsViewmodelAnimatorDriver` responsible only for `PlayerArms` normalization.
- Ensure `PlayerWeaponController` scoped stabilization does not depend on the weapon living under the arms branch.
- Delete any remaining legacy assumptions that a valid weapon parent can be derived from the animator hierarchy.

**Step 3: Run the focused tests and validation**
- Run the scoped stabilization editmode test filter.
- Validate both touched scripts with Unity.

### Task 4: Clean up local redundancy and legacy fallback code in the hotspot

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: nearest local tests only if needed

**Step 1: Remove dead/local legacy branches**
- Delete the obsolete animated-parent compatibility logic that only existed to bridge old `ik_hand_gun` ownership.
- Keep one runtime mount path.

**Step 2: Re-run nearest tests**
- `PlayerWeaponControllerWeaponPresentationRootTests`
- `PlayerWeaponControllerScopedViewmodelStabilizationTests`
- nearest relevant playmode filter if needed for weapon equip/scoped bridge

**Step 3: Validate completion**
- Confirm no runtime code path still mounts a weapon under `PlayerArms`/`ik_hand_gun`.
- Confirm `WeaponPresentationRoot` is sibling to `PlayerArms` in authored content.
- Confirm touched scripts validate cleanly.
