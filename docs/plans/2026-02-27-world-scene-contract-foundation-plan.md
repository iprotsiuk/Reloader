# World Scene Contract Foundation Plan

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


## Objective

Build a scalable, low-regression scene foundation for many towns/instances by replacing implicit scene assumptions with explicit, testable contracts.

## Phase A: Documentation + Process (Immediate)

- Adopt `docs/design/world-scene-contracts.md` as contract baseline.
- Use MCP authoring checklist for world-scene mutation sessions.
- Keep deterministic wiring tools for active scenes.

## Phase B: Contract Data + Validators

- Add `WorldSceneContract` ScriptableObject schema.
- Add one contract asset per runtime world scene.
- Add editor validator command: `Reloader/World/Validate All Scene Contracts`.
- Add fast EditMode contract tests for required object/component/reference presence.

## Phase C: Behavior Gates

- Add or expand PlayMode travel and combat-flow smoke tests per scene role.
- Keep tests targeted so scene-specific regressions fail with actionable messages.

## Phase D: Templates + Scale

- Add scene templates per role (`TownHub`, `ActivityInstance`) with required baseline objects.
- Reduce manual one-off scene wiring by defaulting new scenes to contract-compliant templates.

## Success Metrics

- New scene onboarding time decreases.
- Scene regression rate drops (especially partial-wiring failures).
- World-scene PRs pass contract + flow gates consistently.
