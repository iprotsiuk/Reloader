# Inventory UI Container Framework Design

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


**Date:** 2026-02-25
**Status:** Approved

## Goals

- Build a unified inventory UI interaction model used across TAB inventory, storage, car inventory, and vendor screens.
- Add standard hover tooltips (name, quantity, category, short stats) for item slots/cards.
- Support drag/drop for player-owned containers with contextual stack behavior.
- Keep vendor item arrangement non-draggable and move to a cart-based buy/sell checkout UX.

## User-Approved Product Decisions

- Approach: full container framework rewrite for inventory/trade UI interactions.
- Stack merge behavior:
  - Same-item merge should combine stacks.
  - If merged total exceeds max stack, target fills to max and remainder stays in source slot.
- Max stack source: per-item configuration (`maxStack` on item definitions).
- Vendor UX:
  - No dragging in vendor inventory.
  - Card-based item list with icon/name/price.
  - Each card has quantity dropdown + `Add to cart`.
  - Right-side cart panel.
  - `Order` opens next screen with delivery option radio buttons and final `Purchase`.
- Selling: cart-based multi-item checkout, not instant per-item sell.

## Architecture

### Core Runtime Model

- `ItemStackState`
  - Fields: `itemId`, `quantity`, `maxStack`, tooltip descriptor refs.
- `ContainerState`
  - Typed owner/role (`PlayerBelt`, `PlayerBackpack`, `CarInventory`, `Storage`, `VendorStock`, `Cart`).
  - Fixed or dynamic slot list of stack states.
- `ContainerPermissions`
  - `canDragOut`, `canDropIn`, `canReorder`, `canSplit`, `canMerge`, `isReadOnly`.
- `TransferEngine`
  - Single authority for move/swap/merge/split validation and mutation.

### UI Interaction Layer

- `InventoryUiDragController`
  - Shared drag state and drop resolution across mouse-driven container UIs.
- `InventoryTooltipService`
  - Produces standardized tooltip view models for standard tooltip content.
- Presenter integrations:
  - TAB inventory presenter: draggable.
  - Storage/car presenters: draggable.
  - Vendor presenter: non-draggable cards + cart workflow.

### Vendor Flow

- Buy screen:
  - Item grid cards on left.
  - Cart panel on right.
  - Cart totals and `Order` action.
- Order screen:
  - Delivery option radio buttons.
  - Delivery details/price section.
  - `Purchase` finalization.
- Sell screen:
  - Player-owned item list/cards.
  - Sell cart and `Confirm Sell` for multi-item checkout.

## Behavior Contracts

### Drag/Drop Contract

- Allowed only where container permissions permit.
- Vendor stock/cart arrangement is blocked by permissions and presenter guards.
- Drop outcomes for player containers:
  - Empty target => move.
  - Occupied different item => swap.
  - Occupied same stackable item => merge with max-stack cap.

### Overflow Merge Contract

- `source.quantity + target.quantity <= target.maxStack`
  - target gets full sum, source clears.
- `source.quantity + target.quantity > target.maxStack`
  - target becomes `target.maxStack`.
  - source keeps `sum - target.maxStack` in origin slot.

### Tooltip Contract (Standard)

- Required fields:
  - Item display name.
  - Current quantity.
  - Category/type label.
  - Short stats summary.

## Integration With Existing Systems

- Keep economy authority in `EconomyRuntime`/`EconomyController`.
- Preserve event-driven cross-system communication via runtime event ports/hub.
- Continue emitting inventory/money/trade result events after successful mutations.
- Extend existing presenters/builders rather than replacing scene wiring pattern.

## Testing Strategy

- EditMode tests:
  - Transfer engine move/swap/merge/overflow/split/error paths.
- PlayMode tests:
  - TAB drag/drop interactions and visual updates.
  - Tooltip content rendering on hover.
  - Vendor buy cart + order screen + purchase path.
  - Vendor sell cart + confirm sell path.
  - Failure handling (insufficient funds, invalid quantity, insufficient stock/capacity).

## Non-Goals For This Pass

- Vendor drag-and-drop trading.
- Deep visual redesign outside functional parity with approved reference UX.
- Unrelated inventory persistence schema changes unless required by new stack metadata.
