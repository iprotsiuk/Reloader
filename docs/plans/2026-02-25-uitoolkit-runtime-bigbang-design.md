# UI Toolkit Runtime Big-Bang Migration Design

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


**Date:** 2026-02-25  
**Status:** Approved

## Goals

- Replace all runtime uGUI screens with UI Toolkit in one cut:
  - Belt HUD
  - TAB Inventory UI (including tooltip and drag interactions)
  - Ammo HUD
  - Trade UI
  - Reloading Workbench UI
- Remove old runtime uGUI UI assets and wiring immediately after parity.
- Allow visual redesign.
- Keep post-handoff customization fast: move controls, rewire actions, add/remove buttons without domain refactors.

## User-Approved Decisions

- Migration style: big-bang rewrite (no fallback toggle).
- Visual direction: redesign allowed.
- Scope: runtime UI only (no editor tooling migration in this pass).
- UI stack: UI Toolkit with UXML/USS templates as the primary authoring surface.
- Architecture choice: Data-model-first rewrite.

## Architecture [v0.1]

### Three-Layer Runtime Model

1. UI State Layer
- Per-screen render contracts (`BeltHudUiState`, `TabInventoryUiState`, `AmmoHudUiState`, `TradeUiState`, `ReloadingWorkbenchUiState`).
- State contains only render-ready values (labels, icon keys, visibility, enablement, selection, validation text).

2. UI Controller Layer
- Per-screen controller computes and publishes state.
- Controllers consume domain events/services and map view intents to domain actions.
- Domain integration stays in adapters/interfaces owned by screen modules.

3. UI Toolkit View Layer
- UXML/USS templates and thin binders.
- Binders query elements once, apply state deltas, and emit typed intents.

### Hard Rule: View Is Dumb

Views and binders may only:
- Query named elements.
- Render current UI state.
- Emit user intents.

Views and binders must never:
- Call gameplay/economy/reloading services.
- Subscribe to gameplay event sources directly.
- Execute business rules or domain validation.
- Mutate game state.

All domain wiring belongs to controller/adapters.

## Extension Contract [v0.1]

### 1) Action Mapping Table

- Per-screen action mapping resolves intent keys to controller commands.
- Example contract key shape:
  - `inventory.slot.primary`
  - `inventory.slot.secondary`
  - `trade.confirm`
  - `reloading.operation.execute`
- Remapping behavior must require no UXML edits.

### 2) Screen Composition Config

- Declarative component registration per screen (regions/panels/components).
- Composition determines which modules are mounted and in what order.
- Enables adding/removing/reordering UI components without controller rewrites.

### 3) Element Naming Conventions

- Stable namespaced IDs/classes for queries and styling.
- Convention:
  - Root: `<screen>__root`
  - Element: `<screen>__<component>-<role>`
  - Repeated item: `<screen>__<component>-item`
- Examples:
  - `inventory__slot-grid`
  - `inventory__tooltip-title`
  - `trade__confirm-button`
  - `ammo__count-label`

### 4) Per-Screen Module Boundaries

Each runtime screen owns:
- UXML + USS
- UI state DTO(s)
- controller
- binder/view adapter
- tests
- registration/composition entry

No shared mega-controller and no cross-screen direct coupling.

## Data Flow [v0.1]

- Domain event -> controller -> new state -> view render
- User interaction -> view intent -> controller -> domain action -> resulting state/event refresh

This keeps behavior deterministic and rewiring centralized.

## Error Handling [v0.1]

- Controllers convert domain failures into explicit state (`errorBanner`, `disabledReason`, validation messages).
- Views only render failure state; they do not infer business logic.
- Recoverable actions are intent-based (`retry`, `close`, `clear`).

## Performance Rules [v0.1]

- HUD update paths must use minimal state diffs.
- Avoid full visual tree rebuilds in high-frequency updates.
- Cache queried elements in binder initialization.
- Prefer class toggles and value updates over repeated style object churn.
- Reuse repeated slot/card element containers where possible.

## Testing Strategy [v0.1]

- Controller unit tests:
  - intent -> expected domain call path
  - domain event -> expected state transition
- UI Toolkit PlayMode tests:
  - critical interaction paths for all five screens
- Contract tests:
  - required element names exist in UXML
  - action mapping keys resolve
  - composition configuration validity
- Migration gate:
  - active runtime scenes must have no runtime dependency on old uGUI presenters/prefabs.

## Non-Goals [v0.1]

- Migrating editor windows/custom inspectors/prefab builder editor tooling.
- Re-architecting unrelated gameplay systems.
- Introducing global event indirection beyond explicit per-screen interfaces.

