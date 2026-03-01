# Workbench Mount Graph Design

**Date:** 2026-03-01
**Status:** Approved

## Goals

- Add a generic, multi-slot workbench system where mount slots do not hardcode item category semantics.
- Support nested mount topology (mounted items can expose their own child slots).
- Use strict data-driven compatibility (tags + rules), not type-switch logic.
- Support tool-specific behavior via data contracts (for example Forster Co-Ax compatibility patterns).
- Expand workbench runtime/UI to include setup and operation modes.
- Persist and restore full mounted graph state (including nested child slots) through save/load.

## Non-Goals (This Iteration)

- No bench spatial differentiation yet (center/edge ergonomics, leverage penalties, position constraints).
- No visual 3D mounting/animation pass beyond existing interaction/UI scope.
- No economy progression balancing for all equipment families (foundation first).
- No final UX polish pass (copy/layout refinements can follow after runtime stabilization).

## Key Decisions

- Scope level: full expansion for this iteration, but layered internally to reduce integration risk.
- Mount schema: data-driven item definitions with optional child slots.
- Compatibility model: strict rule/tag engine (`requiredTags`, `forbiddenTags`, optional profile rules).
- Bench topology: multiple generic top-level mount slots from day one.

## Architecture

### Domain Model

- `WorkbenchDefinition`
  - Declares top-level mount slots for a bench archetype.
- `MountSlotDefinition`
  - `slotId`, `requiredTags[]`, `forbiddenTags[]`, optional `ruleProfileId`.
- `MountableItemDefinition`
  - Item identity + provided capabilities (`providedTags[]`) + optional child slots.
- `CompatibilityRuleSet`
  - Deterministic evaluator for slot-item fit plus structured failure reasons.

### Runtime Model

- `WorkbenchRuntimeState`
  - Owns mounted graph for one bench instance.
- `MountNode`
  - Mounted item plus its instantiated child slots.
- `MountSlotState`
  - Occupancy + resolved validation state.
- `WorkbenchLoadoutController`
  - Public API: `CanInstall`, `Install`, `CanUninstall`, `Uninstall`, graph query.
- `ReloadingOperationGate`
  - Computes operation availability from mounted graph + required capability contracts.

### UI Model

- Workbench UI has two modes:
  - `Setup`: browse slot tree, install/uninstall, inspect incompatibility reasons.
  - `Operate`: run steps with explicit enabled/disabled reason surface.
- Controllers remain thin; compatibility and gating decisions live in runtime services.

## Compatibility Contract

### Matching Rules

- Install succeeds only when:
  - `requiredTags ⊆ item.providedTags`
  - `forbiddenTags ∩ item.providedTags == ∅`
  - optional profile rule returns `valid`.
- Install failure returns structured diagnostics:
  - missing required tags,
  - forbidden conflicts,
  - profile-level rule conflict.

### Co-Ax vs Classic Example

- Classic press provides tags like `press`, `requires-shellholder-classic`.
- Co-Ax press provides tags like `press`, `requires-shellholder-coax`, `supports-floating-die-jaw`.
- Classic shellholder provides `shellholder-classic`.
- Co-Ax jaw assembly provides `shellholder-coax`.
- Child slot requirements enforce compatibility by data only; no tool-specific hardcode path.

## Persistence Contract

- Save payload stores mounted graph recursively per bench instance id:
  - root slot occupancy,
  - mounted item ids,
  - nested child slot occupancy.
- Load restores graph top-down with deterministic ordering.
- Missing/invalid items at load are handled as recoverable validation failures (log + slot left empty).

## Interaction Flows

### Setup Flow

1. Player opens workbench.
2. Selects setup mode.
3. Chooses a slot and candidate item.
4. System validates against strict compatibility contract.
5. On success, mount graph updates; on failure, reason is surfaced directly.

### Operate Flow

1. Player switches to operate mode.
2. `ReloadingOperationGate` derives currently available operations.
3. Disabled operations show explicit setup requirement reasons.
4. Runtime operation controller executes only gate-approved actions.

## Testing Strategy

### EditMode

- Compatibility engine matrix tests (required/forbidden/profile checks).
- Nested slot install/uninstall graph mutation tests.
- Co-Ax/classic compatibility tests.
- Persistence payload roundtrip + partial-invalid restore behavior.

### PlayMode

- Setup flow from UI intents through runtime mounting.
- Operate mode gating state changes when mounts change.
- Save/load restore of mounted graph and gate state.
- End-to-end bench setup + stage availability smoke loop.

## Risks and Mitigations

- Risk: model complexity in nested graphs.
  - Mitigation: strict layered core before broad UI behavior wiring.
- Risk: compatibility rules become untraceable.
  - Mitigation: structured failure diagnostics and deterministic test matrix.
- Risk: save/load drift with evolving definitions.
  - Mitigation: schema-gated payload and migration policy at first integration.

## Acceptance Criteria

- Bench supports multiple top-level generic mount slots.
- Mounted item can expose child slots and enforce child compatibility via data.
- Co-Ax/classic incompatibility is represented and enforced via tags/rules.
- Workbench setup and operation modes are both functional.
- Operation availability is derived from mounted graph.
- Mounted graph persists and restores correctly across save/load.
