# Chest Storage System Design

**Date:** 2026-02-28
**Status:** Approved

## Goals

- Add a first usable world container: one chest placed near the outdoor workbench in `MainTown`.
- Chest has 20 functional slots.
- Chest content persists forever for regular containers (save/load + travel).
- UI shows chest inventory on the left and player inventory on the right with drag/drop transfer.
- Lay plumbing for future workbench-linked container access (read/query only for now).

## Non-Goals (This Slice)

- No special item-type filters yet (chest accepts all items).
- No render-in-slot/world-display system for mounted guns yet.
- No consumption-from-linked-storage crafting behavior yet; only wiring contracts.

## Architecture

### Core Runtime

- Add a generic world container model keyed by stable `containerId`.
- Introduce runtime slot storage for arbitrary container capacities.
- Add policy enum to support:
  - `Persistent` (regular chest, never auto-cleared)
  - `DailyReset` (future trash/special bins)

### World Interaction

- Add a world-facing container component with:
  - stable `containerId`
  - `displayName`
  - `slotCapacity`
  - `policy`
- Add a player interaction provider/controller that resolves the targeted container and opens/closes storage UI context.

### UI

- Add chest storage UI controller + view binder.
- Layout contract:
  - left panel: chest slots
  - right panel: player inventory (belt + backpack)
- Reuse drag/drop intent semantics used by existing inventory UI.

### Persistence

- Add save module payload for container storage keyed by `containerId`.
- Restore container runtime state on load.
- `Persistent` containers retain data indefinitely until explicit player action.
- `DailyReset` behavior is policy-driven for future special containers.

### Workbench Plumbing (No Functional Crafting Consumption Yet)

- Add bench-side link component that references linked container IDs.
- Expose runtime query path for linked container IDs/items.
- Do not consume linked items in crafting in this slice.

## Data Flow

- Player targets container + presses interact.
- Storage UI opens with active `containerId` context.
- Drag/drop payload identifies source/target among:
  - `container:{id}`
  - `belt`
  - `backpack`
- Transfer engine applies move/merge/swap rules.
- Success raises inventory/container-changed events and triggers UI refresh.

## Persistence Contract

- Regular chest uses `Persistent` and never clears automatically.
- Container content persists across:
  - scene travel
  - save/load
  - game restarts
- Future special bins can use `DailyReset` and policy cleanup hooks.

## First Placement

- Place one chest near outdoor reloading workbench in `MainTown`.
- Set:
  - `slotCapacity = 20`
  - `policy = Persistent`
  - stable scene-authored `containerId`

## Testing Strategy

- Runtime transfer tests: container <-> player move/merge/swap.
- UI tests: left chest/right player render + drag intent routing.
- Save tests: container block round-trip and policy behavior.
- Scene checks: `MainTown` contains chest with expected configuration and interaction hook.

## Future Extensions

- Multiple chest/safe variants with size/type restrictions.
- Bench-linked consumption flow for reloading inputs.
- Wall mounts with rendered stored-weapon representation.
- Sell-bin/trash-bin policies with scheduled cleanup/economy hooks.
