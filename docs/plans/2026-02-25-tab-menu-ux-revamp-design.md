# Tab Menu UX Revamp Design

**Date:** 2026-02-25
**Scope:** `tab-inventory` UI Toolkit screen in the current PR branch.

## Goals
- `Tab` toggles the whole menu open/close.
- Menu opens on `Inventory` section by default.
- Top tabs switch between `Inventory`, `Quests`, `Journal`, and `Calendar`.
- Remove overflow/clipping and oversized controls; keep layout on-screen.

## Diagnosis
- Current open state is toggled by direct keyboard polling inside `TabInventoryController`.
- This bypasses gameplay input contract (`IPlayerInputSource`) and can miss the expected flow.
- UI layout uses rigid rows and legacy placeholder sizing; this causes clipping/overlap.

## Chosen Approach (Approved)
Use input-contract wiring (Option 2):
- Extend `IPlayerInputSource` with menu-toggle consume API.
- Implement it in `PlayerInputReader` using the existing InputAction map.
- Inject player input source into `TabInventoryController` and toggle menu from consumed input.
- Keep tab section switching as UI intents.
- Revamp `TabInventory.uxml/.uss` to match expected UX structure and avoid overflow.

## Non-Goals
- Full visual clone of any external game UI.
- New gameplay inventory logic.
- Persistence/schema changes.
