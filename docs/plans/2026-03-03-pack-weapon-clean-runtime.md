# Pack Weapon Clean Runtime Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

> **Migration Update (2026-03-04):** ADS/optics aiming is now implemented under `Reloader/Assets/Game/Weapons/**` with a camera-authoritative `SightAnchor` alignment contract. Treat this plan as historical for `_Project/Weapons` runtime migration and prefer `docs/design/ads-optics-framework.md` for current ADS/scope behavior.

**Goal:** Replace old ADS/reload weapon behavior with pack-driven behavior while keeping inventory as the only gameplay dependency.

**Architecture:** Keep `PlayerWeaponController` as runtime entrypoint to avoid scene/wiring churn, but move ADS/reload/fire timing/presentation to a new pack-runtime subsystem. Inventory remains source of equipped item and ammo consumption. Existing runtime events continue to be raised from the new flow.

**Tech Stack:** Unity C#, existing runtime event hub, inventory runtime, UI Toolkit HUD, pack prefabs/animations.

---

### Task 1: Add Pack Runtime Data + Driver

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/PackRuntime/PackWeaponPresentationConfig.cs`
- Create: `Reloader/Assets/_Project/Weapons/Scripts/PackRuntime/PackWeaponRuntimeDriver.cs`
- Create: `Reloader/Assets/_Project/Weapons/Scripts/PackRuntime/PackWeaponRuntimeState.cs`

**Steps:**
1. Create config/state/driver classes for ADS target FOV, ADS lerp speed, reload duration/anim trigger, fire anim trigger, muzzle VFX/sfx hooks.
2. Driver API should expose `Tick`, `SetAiming`, `TryStartReload`, `NotifyFire`, `CancelReload`, `GetRuntimeState`.
3. Keep classes data-driven and multi-weapon ready (keyed by item id).

### Task 2: Replace ADS/Reload Internals in PlayerWeaponController

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`

**Steps:**
1. Route ADS and reload state transitions through `PackWeaponRuntimeDriver`.
2. Remove old scope-step/legacy zoom behavior from runtime path.
3. Keep inventory coupling only:
   - equip source: belt selected item id
   - reload consume: inventory quantity/remove stack
4. Continue raising existing weapon events so HUD/animation consumers still work during migration.

### Task 3: Wire Pack Assets for One Gun Template

**Files:**
- Modify: `Reloader/Assets/_Project/Weapons/Editor/WeaponsContentBuilder.cs`
- Modify: `Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab` (via builder)

**Steps:**
1. Add pack presentation config creation/binding for one gun (sniper/rifle template).
2. Bind animator + optional audio/vfx references available in pack/project.
3. Ensure generated view prefab has required references for driver.

### Task 4: Validation + Follow-up Migration Hooks

**Files:**
- Modify: `Reloader/Assets/_Project/UI/Tests/PlayMode/AmmoHudControllerWeaponEventsPlayModeTests.cs` (if needed)
- Modify: `Reloader/Assets/_Project/Player/Tests/PlayMode/ViewmodelAnimationAdapterWeaponEventsPlayModeTests.cs` (if needed)

**Steps:**
1. Validate changed scripts compile.
2. Run targeted playmode tests when runner free.
3. Document next step to mirror template for second gun.
