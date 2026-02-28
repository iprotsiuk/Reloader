# Belt HUD (Reusable Prefab) — Design

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


**Date:** 2026-02-24  
**Status:** Approved

## Goal
Add a reusable belt HUD so the 5-slot belt mechanic is visually testable in-game, using the external "Post-apocalyptic Survival UI" assets.

## Scope
- Build a reusable HUD prefab (not scene-local throwaway).
- Show exactly 5 belt slots only.
- Reflect slot occupancy and selected slot state in real time.
- Use selected external asset sprite as slot frame.

Out of scope:
- TAB menu shell
- Backpack panel/counter
- Drag/drop inventory UI
- Per-item custom icons

## Behavior Contract
- Slot keys `1..5` always select slot index `0..4`.
- Selected slot is shown with brighter tint + slightly larger scale.
- Empty slot can be selected and still shows selected visuals.
- Occupied slot shows placeholder icon (generic icon for now).
- Pressing the same selected key again is a no-op.

## Architecture
- `BeltHud` prefab under `_Project/UI/Prefabs`.
- `BeltHudPresenter` script drives visuals from runtime inventory state.
- `BeltHudBootstrap` script instantiates/wires HUD once per scene runtime.

Data source:
- `PlayerInventoryController.Runtime` (`BeltSlotItemIds`, `SelectedBeltIndex`).
- Event-driven refresh via runtime inventory event ports (`IInventoryEvents` via `IGameEventsRuntimeHub`).

## Asset Mapping
Source package path:
`/Users/ivanprotsiuk/Documents/assets/LOWPOLY/Post-apocalyptic Survival UI`

Chosen visuals:
- Slot frame: `Assets/Post-apocalyptic Survival UI/Sprites/Component/Component_12.png`
- Placeholder occupied icon: neutral icon from package (for first pass)

## Layout
- Bottom-center horizontal belt row.
- Exactly 5 slots with consistent spacing.
- Slot labels: `1..5`.
- Resolution-safe anchors for desktop aspect ratios.

## Validation Checklist
- Selection highlight moves correctly on keys `1..5`.
- Empty selected slot remains highlighted.
- Pickup inserts item to next free slot and icon appears.
- If selected slot was empty and gets filled, slot remains selected and now shows icon.
- HUD prefab is reusable and scene-agnostic via bootstrap wiring.

## Risks and Mitigations
- Risk: Missing scene references.
  - Mitigation: Bootstrap resolves `PlayerInventoryController` at runtime and logs clear warning if absent.

- Risk: Visual mismatch at different resolutions.
  - Mitigation: Anchor-based layout and constrained slot sizing.

- Risk: Over-coupling to future TAB menu.
  - Mitigation: Keep HUD purely belt-only and read-only over runtime state.
