# Kar98k Generic Attachments Design

**Date:** 2026-03-04  
**Status:** Approved for implementation  
**Scope:** Generic attachment framework + demo rifle wiring

## Goal
Implement a reusable inventory-backed weapon attachment flow and wire it end-to-end for the demo rifle based on `WWII_Recon_A_PreSet` (fictional post-WW2 Kar98k in `.308`).

## User Experience
1. Open TAB inventory.
2. Right-click equipped rifle item.
3. Select `Attachments`.
4. A dedicated attachments window opens.
5. Slot rows render from weapon attachment data (initially `Scope`, `Muzzle`).
6. Each slot offers compatible, owned attachments.
7. Selecting an attachment performs atomic swap:
   - New attachment removed from inventory.
   - Previous attachment returned to inventory (if present).
   - Weapon runtime/view updates immediately.

## Architecture
Use a generic attachment domain layer under project-owned paths in `_Project/Weapons` and bridge to existing runtime in `Assets/Game/Weapons` for visual/ADS behavior.

- Data-driven compatibility via ScriptableObject data and runtime state.
- No per-weapon bespoke branching.
- Camera-authoritative ADS remains unchanged.
- Existing item id `weapon-rifle-01` remains active for compatibility with tests and scene wiring.

## Core Design Decisions
- Use generic slot model now, but only activate `Scope` and `Muzzle` for this milestone.
- Keep extension point for future `Barrel` slot without enabling it yet.
- Attachments are non-consumable inventory equipment items; swaps move ownership between weapon and inventory.
- Deterministic behavior only; no random fallback for slot resolution.

## Data Model Changes
- Add generic attachment slot enum/type.
- Add weapon attachment compatibility config (per-weapon allowed item ids per slot).
- Extend weapon runtime state with equipped attachment ids by slot.
- Add attachment item metadata to map inventory item ids to runtime definitions (optic/muzzle).

## Runtime Changes
- Add swap service/transaction in `_Project/Weapons` to validate ownership + compatibility and apply inventory/runtime mutation atomically.
- Add bridge from equipped attachment ids into:
  - `AttachmentManager.EquipOptic(...)` for optics.
  - Existing muzzle runtime bridge/equip path for muzzle devices.
- Preserve ADS state and current camera authority when optic changes.

## UI Changes
- Add `Attachments` action to TAB context menu for weapon items.
- Add reusable attachments window component:
  - Dynamic slot rows.
  - Compatible-owned dropdown filtering.
  - Current attachment display.
  - Immediate apply swap action.

## Content Wiring
- Use `Assets/Low Poly Weapon Pack 4_WWII_1/Prefabs/Weapons/Weapons_PreSet/WWII_Recon_A_PreSet.prefab` as demo rifle source view via project-owned wrapper/config path.
- Define compatible scope and muzzle attachments for this rifle from owned inventory items.
- Keep `.308` ammo path unchanged (`ammo-factory-308-147-fmj`).

## Validation
- PlayMode coverage for:
  - compatibility filtering,
  - atomic swap inventory/runtime mutation,
  - scope hot-swap ADS/mask behavior,
  - muzzle swap + deterministic fire override path.
- Manual flow check in TAB inventory for end-to-end swap and immediate visual/runtime effect.

## Non-Goals (This Iteration)
- Barrel slot gameplay activation.
- Full `.308` reload extension work (next phase after rifle attachment slice).
