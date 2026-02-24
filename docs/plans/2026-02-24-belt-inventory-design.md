# Belt Inventory (5 Slots) — Design

**Date:** 2026-02-24  
**Status:** Approved

## Goal
Implement a future-proof inventory foundation centered on a 5-slot quick-access belt (`1..5`) with event-driven contracts, while keeping runtime logic decoupled from the future TAB menu shell.

## Scope
- Add pickup-to-inventory flow: player looks at item, presses `E`, item is stored.
- Add fixed belt quick slots (5) addressed by keyboard `1..5`.
- Add slot-based selection behavior without equip/unequip state.
- Add forward-compatible inventory model for future backpack expansion and TAB inventory UI.
- Extend save payload to persist inventory/belt selection state.

Out of scope for this slice:
- Full TAB UI implementation
- Drag/drop inventory UI
- Backpack purchase economy flow
- Item usage/consumption semantics beyond selection

## Final Behavior Contract

### Pickup
- Player looks at a world item and presses `E`.
- Item attempts to insert into inventory using this order:
1. First empty belt slot (`0..4`)
2. First available backpack slot (when backpack capacity > 0)
- Newly picked up items are **not auto-selected** by item identity.
- If insertion fails (no space/invalid item), publish rejection event.

### Belt Selection
- Pressing `1..5` always sets `selectedBeltIndex` to that slot.
- Pressing the already selected slot key is a no-op.
- Empty slots can be selected.
- Selection is slot-based, not equip-based.

### Selected Empty Slot + Later Pickup
- If selected slot is empty and a later pickup fills that same slot, that item becomes active implicitly because the selected slot now references an item.

## Architecture
Chosen approach: event-driven inventory with placeholder registry boundaries (future-proof path).

### Runtime Components
- `PlayerInventoryRuntime` (new): owns belt/backpack state and slot-selection logic.
- `PlayerInventoryController` (new): bridges player input and world interaction to inventory runtime/events.
- Existing input reader extended to expose pickup + belt slot key presses.

### Decoupling Rule
TAB is a future multi-tab shell (inventory, quests, manuals, etc.). Inventory runtime must not depend on TAB/UI types. TAB later becomes a consumer/editor of inventory state.

## Event Contracts (Core)
Extend `GameEvents` with inventory events:
- `OnItemPickupRequested(string itemId)`
- `OnItemStored(string itemId, InventoryArea area, int index)`
- `OnItemPickupRejected(string itemId, PickupRejectReason reason)`
- `OnBeltSelectionChanged(int selectedBeltIndex)`
- `OnInventoryChanged()`

Add shared enums:
- `InventoryArea { Belt, Backpack }`
- `PickupRejectReason { NoSpace, InvalidItem }`

## Data Model
`PlayerInventoryRuntime` state:
- `string[] beltSlotItemIds = new string[5]`
- `List<string> backpackItemIds`
- `int backpackCapacity` (starts at 0)
- `int selectedBeltIndex` (default `-1`, then set via `1..5`)

## Save Contract Updates
Inventory module payload evolves to include:
- `carriedItemIds` (retained for compatibility)
- `beltSlotItemIds`
- `backpackItemIds`
- `backpackCapacity`
- `selectedBeltIndex`

Backward compatibility requirement:
- Existing saves containing only `carriedItemIds` must load safely, defaulting new fields.

## Input Contract
Extend player input map/reader with:
- `Pickup` action (`E`)
- Belt select actions for `1..5` (or equivalent unified mapping)

Reader API additions:
- `bool ConsumePickupPressed()`
- `int ConsumeBeltSelectPressed()` returning `0..4` or `-1`

## Testing Strategy

### EditMode (required)
- Pickup fills first empty belt slot.
- Pickup rejected when no space and backpack locked.
- `1..5` always updates selected slot (including empty).
- Re-pressing same selected slot is no-op.
- Selected empty slot later filled -> selected slot now resolves to active item.

### Save tests (required)
- Inventory payload round-trip for new fields.
- Backward compatibility load for legacy payload shape.

### PlayMode smoke (optional)
- Key presses update slot selection.
- `E` pickup triggers store/reject path.

## Risks and Mitigations
- Risk: Input mapping drift between editor asset and code names.
- Mitigation: Centralize action names and add focused reader tests.

- Risk: Event surface growth causing accidental tight coupling.
- Mitigation: Keep payloads minimal (IDs/enums/indices), no UI references.

- Risk: Save evolution regressions.
- Mitigation: Backward-compat tests for payload deserialization defaults.

## Next Step
Create a detailed implementation plan and execute via TDD in small vertical slices.
