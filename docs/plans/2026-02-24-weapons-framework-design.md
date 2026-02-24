# Weapons Framework v0.1 Design

**Date:** 2026-02-24  
**Scope:** Future-proof weapon framework first, with one rifle as the first implemented content item.

## Goals

- Build a reusable weapons foundation under `Reloader/Assets/_Project/Weapons/` before adding multiple guns.
- Deliver one complete playable rifle loop: pickup, inventory belt selection, equip, projectile shooting, reload.
- Keep architecture event-driven and save-compatible with current `GameEvents` + `SaveCoordinator` contracts.
- Provide prefab-based authoring so rifle pickup/view can be placed manually later in scene.

## Non-Goals (This Slice)

- Multiple gun types/content packs in first implementation.
- Final recoil polish, audio polish, VFX polish, and advanced ballistic effects (wind/Coriolis/spin drift).
- Full damage system for all actors; only generic impact/damage interface needed now.

## Approaches Considered

1. Vertical slice with ballistic-ready interfaces only (hitscan first)  
   - Pros: fastest initial delivery  
   - Cons: requires later refactor to true projectile ballistics
2. Hardcoded single rifle controller  
   - Pros: quick prototype  
   - Cons: poor extensibility, rewrite risk
3. Full framework first with one rifle content item (**selected**)  
   - Pros: best long-term structure and reuse, minimizes rewrite  
   - Cons: higher upfront complexity and implementation time

## Architecture

### Feature layout

Create/extend a dedicated weapons feature area:

- `Reloader/Assets/_Project/Weapons/Scripts/Data/`  
  Weapon definitions and shared enums/config data.
- `Reloader/Assets/_Project/Weapons/Scripts/Runtime/`  
  `WeaponRuntimeState`, ammo/chamber state, equip/reload/cooldown state.
- `Reloader/Assets/_Project/Weapons/Scripts/Controllers/`  
  `PlayerWeaponController` to bridge inventory selection and firing lifecycle.
- `Reloader/Assets/_Project/Weapons/Scripts/Ballistics/`  
  Projectile behavior, ballistic integration hooks, hit dispatch.
- `Reloader/Assets/_Project/Weapons/Scripts/World/`  
  Pickup bridge components implementing inventory pickup contracts.
- `Reloader/Assets/_Project/Weapons/Prefabs/`  
  Rifle pickup prefab, held rifle view prefab, projectile prefab.
- `Reloader/Assets/_Project/Weapons/Data/`  
  One rifle definition asset for first content item.

### System boundaries

- Inventory remains ownership source of truth (`itemId` in belt/backpack).
- Weapons framework resolves selected belt `itemId` into weapon definition/runtime.
- Weapon operations publish events; UI/audio/VFX consume via event bus.
- Ballistics pipeline resolves impacts via interface (`IDamageable`/impact receiver), avoiding direct coupling.

## Data Flow

1. Input layer queues fire/reload actions.
2. Player selects belt slot (`1..5`) using existing inventory flow.
3. Weapon controller reads selected `itemId` and resolves via registry.
4. On valid fire:
   - checks cooldown/reload/chamber/ammo state,
   - spawns projectile from muzzle transform,
   - updates chamber/mag state,
   - raises weapon fired event.
5. Projectile simulates movement and gravity; on collision:
   - emits hit event with impact payload,
   - forwards damage through damage interface.
6. Reload updates mag/reserve/chamber state and raises reload event.

## Runtime State Contract

`WeaponRuntimeState` (per weapon `itemId`):

- `itemId`
- `isEquipped`
- `chamberLoaded`
- `magCount`
- `reserveCount`
- `isReloading`
- `nextFireTime`

This contract is intentionally generic for future rifle/pistol/shotgun support.

## Input Contract

- Keep `Attack` action as fire (`Mouse0`) mapped through `PlayerInputReader`.
- Add `Reload` action (`R`) and expose `ConsumeReloadPressed()`.
- Preserve existing pickup (`E`) and belt selection input behavior.

## Event Contract Additions

Extend `GameEvents` with weapons lifecycle events (exact signatures finalized in implementation plan), covering at minimum:

- weapon equipped / unequipped
- weapon fired
- weapon reloaded
- projectile hit

## Persistence Strategy

Persist per-weapon runtime state keyed by `itemId`, including at least:

- `chamberLoaded`
- `magCount`
- `reserveCount`

Implement via inventory module extension or dedicated weapons save module, integrated with `SaveCoordinator`. On load, runtime state is restored for owned items.

## Prefab and Content Plan

- Create one rifle pickup prefab (inventory pickup bridge + visual root).
- Create one held-rifle prefab with explicit muzzle transform.
- Create one projectile prefab (collider, rigidbody or deterministic motion, lifetime logic).
- Create one rifle definition asset bound to the framework.
- Do not auto-place in scene; user will place instances manually.

## Testing Strategy (TDD)

### EditMode

- Weapon runtime state transitions (fire, dry fire, cooldown, reload).
- ItemId-to-definition registry resolution.
- Save/load payload roundtrip for weapon state.
- New weapon event contract tests.

### PlayMode

- Belt selection equips expected weapon.
- Pickup -> inventory -> equip path works for rifle item.
- Projectile spawns and applies impact against test target receiver.
- Reload command updates runtime state while equipped.

## Risks and Mitigations

- Risk: Framework over-scope delays usable gameplay.  
  Mitigation: enforce one-rifle-only content while keeping reusable abstractions.
- Risk: Save schema churn while systems are still evolving.  
  Mitigation: keep payload minimal and keyed by stable `itemId`.
- Risk: Tight coupling between inventory and weapons logic.  
  Mitigation: use registry + event boundaries and avoid direct manager-to-manager dependency.

## Success Criteria

- One rifle can be picked up, selected from belt, fired as projectile weapon, and reloaded.
- Weapon state survives save/load for implemented fields.
- New behavior is covered by EditMode/PlayMode tests and integrated with current architecture conventions.
- Additional weapons can be added as data/prefab content without redesigning the core framework.
