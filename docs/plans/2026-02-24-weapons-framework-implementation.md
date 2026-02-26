# Weapons Framework Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a reusable weapons framework with projectile shooting, pickup/inventory equip flow, and save-ready runtime state, implemented first with one rifle.

**Architecture:** Add a modular `Weapons` feature layer that resolves inventory `itemId` selection into weapon runtime state, drives projectile firing/reload workflows, and publishes events through runtime event ports/hub (`IGameEventsRuntimeHub`/`IWeaponEvents`). Persist minimal per-weapon runtime state keyed by `itemId` so gameplay survives save/load.

**Tech Stack:** Unity 6.3, C#, Unity Input System, NUnit (EditMode/PlayMode), existing runtime event contracts (`IGameEventsRuntimeHub` + ports) and `SaveCoordinator` modules.

---

### Task 1: Extend Input Contract for Fire and Reload

**Files:**
- Modify: `Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs`
- Modify: `Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs`
- Modify: `Reloader/Assets/_Project/Player/InputSystem_Actions.inputactions`
- Test: `Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs` (test doubles only)

**Step 1: Write failing test**

- Add a PlayMode test-double assertion path that requires `ConsumeFirePressed()` and `ConsumeReloadPressed()` on `IPlayerInputSource`-implementing fake.

**Step 2: Run test to verify it fails**

Run: `Unity EditMode/PlayMode targeted test command for Player tests`  
Expected: compile/test failure because the interface members do not exist.

**Step 3: Write minimal implementation**

- Add to `IPlayerInputSource`:
  - `bool ConsumeFirePressed();`
  - `bool ConsumeReloadPressed();`
- In `PlayerInputReader`:
  - resolve `Attack` action as fire and queue per-frame press,
  - add serialized reload action name defaulting to `Reload`, queue reload press,
  - implement consume methods with one-shot semantics consistent with existing jump/pickup behavior.
- In `.inputactions`:
  - add `Reload` action bound to `<Keyboard>/r`.

**Step 4: Run tests to verify pass**

Run: targeted Player PlayMode tests.  
Expected: tests pass and no compile errors from input interface changes.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Player/Scripts/IPlayerInputSource.cs \
  Reloader/Assets/_Project/Player/Scripts/PlayerInputReader.cs \
  Reloader/Assets/_Project/Player/InputSystem_Actions.inputactions \
  Reloader/Assets/_Project/Player/Tests/PlayMode/PlayerInventoryControllerPlayModeTests.cs
git commit -m "feat: add fire and reload input contract"
```

### Task 2: Add Weapon Event Contracts

**Files:**
- Modify (historical/retired path): `Reloader/Assets/_Project/Core/Scripts/Events/GameEvents.cs`
- Runtime contract target: `IGameEventsRuntimeHub` + `IWeaponEvents` under `Core/Scripts/Events` runtime contracts.
- Modify: `Reloader/Assets/_Project/Core/Scripts/Events/InventoryEventsTypes.cs` (only if new enums needed)
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs`

**Step 1: Write failing test**

Add EditMode tests asserting new events fire with expected payload:
- `OnWeaponEquipped(string itemId)`
- `OnWeaponFired(string itemId, Vector3 origin, Vector3 direction)`
- `OnWeaponReloaded(string itemId, int magCount, int reserveCount)`
- `OnProjectileHit(string itemId, Vector3 point, float damage)`

**Step 2: Run test to verify it fails**

Run: EditMode event contract tests.  
Expected: missing event members/raise methods.

**Step 3: Write minimal implementation**

Add events and raise methods in runtime weapon event ports/hub contracts with payloads required by tests.

**Step 4: Run tests to verify pass**

Run: EditMode event contract tests.  
Expected: event tests pass.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Events/*Runtime* \
  Reloader/Assets/_Project/Core/Tests/EditMode/InventoryEventContractsTests.cs
git commit -m "feat: add weapon gameplay event contracts"
```

### Task 3: Create Weapon Core Data and Runtime State

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Data/WeaponDefinition.cs`
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponRuntimeState.cs`
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponRegistry.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/WeaponRuntimeStateTests.cs`

**Step 1: Write failing test**

Add EditMode tests covering:
- fire consumes chamber/mag state,
- dry fire when no chambered round,
- reload moves rounds from reserve to mag,
- registry resolves `itemId` to definition.

**Step 2: Run test to verify it fails**

Run: new EditMode test file.  
Expected: missing classes/methods.

**Step 3: Write minimal implementation**

- `WeaponDefinition` SO with baseline serialized fields:
  - `itemId`, `displayName`, `magazineCapacity`, `fireIntervalSeconds`, `projectileSpeed`, `projectileGravityMultiplier`, `baseDamage`, `maxRangeMeters`.
- `WeaponRuntimeState` methods:
  - `CanFire(now)`, `TryFire(now, out fireData)`, `TryReload()`.
- `WeaponRegistry` maps definition assets by `itemId`, supports deterministic lookup.

**Step 4: Run tests to verify pass**

Run: new weapon runtime EditMode tests.  
Expected: pass.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Data/WeaponDefinition.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponRuntimeState.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Runtime/WeaponRegistry.cs \
  Reloader/Assets/_Project/Core/Tests/EditMode/WeaponRuntimeStateTests.cs
git commit -m "feat: add weapon definitions runtime state and registry"
```

### Task 4: Implement Projectile Ballistics and Impact Pipeline

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs`
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/ProjectileImpactPayload.cs`
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/IDamageable.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs`

**Step 1: Write failing test**

Add PlayMode tests verifying:
- projectile moves forward and drops under gravity,
- projectile despawns on lifetime expiry,
- projectile collision invokes damage receiver and raises `OnProjectileHit`.

**Step 2: Run test to verify it fails**

Run: projectile PlayMode tests.  
Expected: missing projectile classes and hit behavior.

**Step 3: Write minimal implementation**

- Implement projectile movement with deterministic per-frame integration.
- On collision:
  - compose impact payload,
  - call `IDamageable.ApplyDamage(payload)` if present,
  - raise projectile hit event,
  - destroy/despawn projectile.

**Step 4: Run tests to verify pass**

Run: projectile PlayMode tests.  
Expected: pass.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Ballistics/WeaponProjectile.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Ballistics/ProjectileImpactPayload.cs \
  Reloader/Assets/_Project/Weapons/Scripts/Ballistics/IDamageable.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponProjectilePlayModeTests.cs
git commit -m "feat: add projectile ballistics and impact pipeline"
```

### Task 5: Add Player Weapon Controller (Equip, Fire, Reload)

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs`
- Modify: `Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs` (integration points only)
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs`

**Step 1: Write failing test**

Add PlayMode tests for:
- selecting belt slot with known rifle item equips weapon,
- fire input spawns projectile and decrements ammo state,
- reload input updates ammo state and raises reload event,
- empty chamber/mag path produces dry-fire no projectile.

**Step 2: Run test to verify it fails**

Run: weapon controller PlayMode tests.  
Expected: missing controller/integration behavior.

**Step 3: Write minimal implementation**

- Resolve selected belt `itemId` from inventory runtime.
- Lookup definition via `WeaponRegistry`.
- Maintain runtime state per owned `itemId`.
- Handle fire/reload consume calls and spawn projectile from serialized muzzle transform.
- Raise equip/fire/reload events.

**Step 4: Run tests to verify pass**

Run: weapon controller PlayMode tests.  
Expected: pass.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Scripts/Controllers/PlayerWeaponController.cs \
  Reloader/Assets/_Project/Inventory/Scripts/PlayerInventoryController.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/PlayerWeaponControllerPlayModeTests.cs
git commit -m "feat: add player weapon controller equip fire reload flow"
```

### Task 6: Add World Pickup Bridge for Weapon Items

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Scripts/World/WeaponPickupTarget.cs`
- Create: `Reloader/Assets/_Project/Weapons/Scripts/World/PlayerWeaponPickupResolver.cs`
- Test: `Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponPickupFlowPlayModeTests.cs`

**Step 1: Write failing test**

Add PlayMode test ensuring:
- weapon pickup target resolves through inventory pickup flow,
- on pickup, item id enters belt/backpack,
- pickup world object marks itself picked up.

**Step 2: Run test to verify it fails**

Run: pickup flow PlayMode test.  
Expected: missing pickup bridge classes.

**Step 3: Write minimal implementation**

- Implement `IInventoryPickupTarget` on weapon pickup component.
- Implement resolver compatible with current inventory controller contract.
- Keep setup inspector-driven and prefab-friendly.

**Step 4: Run tests to verify pass**

Run: pickup flow PlayMode tests.  
Expected: pass.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Scripts/World/WeaponPickupTarget.cs \
  Reloader/Assets/_Project/Weapons/Scripts/World/PlayerWeaponPickupResolver.cs \
  Reloader/Assets/_Project/Weapons/Tests/PlayMode/WeaponPickupFlowPlayModeTests.cs
git commit -m "feat: add weapon pickup bridge for inventory flow"
```

### Task 7: Persist Weapon Runtime State Through Save Pipeline

**Files:**
- Modify or Create (choose one design path):
  - `Reloader/Assets/_Project/Core/Scripts/Save/Modules/InventoryModule.cs` (extend payload), or
  - `Reloader/Assets/_Project/Core/Scripts/Save/Modules/WeaponsModule.cs` (new domain module)
- Modify: `Reloader/Assets/_Project/Core/Scripts/Save/SaveModuleRegistration.cs`
- Test: `Reloader/Assets/_Project/Core/Tests/EditMode/WeaponSaveModuleCompatibilityTests.cs`

**Step 1: Write failing test**

Add EditMode tests for payload roundtrip by `itemId` containing `chamberLoaded`, `magCount`, `reserveCount`.

**Step 2: Run test to verify it fails**

Run: new save compatibility tests.  
Expected: payload missing weapon state.

**Step 3: Write minimal implementation**

- Implement capture/restore/validation for weapon runtime payload.
- Register module in deterministic restore order.
- Keep compatibility behavior for missing payloads.

**Step 4: Run tests to verify pass**

Run: save compatibility tests.  
Expected: pass.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Core/Scripts/Save/Modules \
  Reloader/Assets/_Project/Core/Scripts/Save/SaveModuleRegistration.cs \
  Reloader/Assets/_Project/Core/Tests/EditMode/WeaponSaveModuleCompatibilityTests.cs
git commit -m "feat: persist weapon runtime state in save pipeline"
```

### Task 8: Author First Rifle Content and Prefabs

**Files:**
- Create: `Reloader/Assets/_Project/Weapons/Data/Weapons/<RifleName>.asset`
- Create: `Reloader/Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab`
- Create: `Reloader/Assets/_Project/Weapons/Prefabs/RiflePickup.prefab`
- Create: `Reloader/Assets/_Project/Weapons/Prefabs/RifleView.prefab`
- Optional import source: `/Users/ivanprotsiuk/Documents/assets/LOWPOLY/poly_megaweaponskit/POLY - Mega Weapons Kit.unitypackage`

**Step 1: Write failing test**

Add PlayMode integration test requiring an authored rifle definition and projectile prefab references to exist and be instantiable.

**Step 2: Run test to verify it fails**

Run: prefab/content PlayMode tests.  
Expected: missing assets/references.

**Step 3: Write minimal implementation**

- Import rifle model assets from provided source package.
- Create one rifle definition with valid `itemId` and projectile settings.
- Build prefabs and wire serialized references.
- Keep prefab reusable; no scene auto-placement.

**Step 4: Run tests to verify pass**

Run: prefab/content integration tests.  
Expected: pass.

**Step 5: Commit**

```bash
git add Reloader/Assets/_Project/Weapons/Data/Weapons \
  Reloader/Assets/_Project/Weapons/Prefabs
git commit -m "feat: add first rifle data and prefab set"
```

### Task 9: Full Verification and Documentation Sync

**Files:**
- Modify (if needed): `docs/design/weapons-and-ballistics.md`
- Modify (if needed): `.cursor/rules/core-events-context.mdc` (if event contract routing scope changes)

**Step 1: Run full failing/passing verification matrix**

- Run targeted new EditMode tests.
- Run targeted new PlayMode tests.
- Run existing inventory/player/UI regression tests.

**Step 2: Confirm green outputs and capture evidence**

Expected: zero failures, no compile errors.

**Step 3: Update docs/routing only if contracts changed**

- If new event contracts materially expand routing, update `.cursor/rules/core-events-context.mdc` in same change.
- If player-visible weapon behavior contract changed, update relevant design docs.

**Step 4: Final commit**

```bash
git add docs/design/weapons-and-ballistics.md .cursor/rules/core-events-context.mdc
git commit -m "docs: sync weapon framework contracts and routing"
```
