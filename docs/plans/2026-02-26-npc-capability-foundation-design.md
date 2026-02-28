# NPC Capability Foundation Design

> Status Pointer (2026-02-28): This is a planning/execution artifact. For live implemented-vs-planned status, use `docs/design/v0.1-demo-status-and-milestones.md`.


**Date:** 2026-02-26
**Status:** Implemented (foundation slice shipped; see As-Built Note and `docs/design/v0.1-demo-status-and-milestones.md`)

> Historical planning artifact: use `docs/design/v0.1-demo-status-and-milestones.md` and `docs/design/npcs-and-quests.md` for current status/contract truth.

## Goal
Build a reusable NPC foundation that lets world-building start immediately with drag-and-drop prefabs while keeping role logic extensible and avoiding role-specific code duplication.

## Chosen Approach
Use **capability-based NPC composition** as the primary architecture, with a **decision-provider seam** so behavior trees can be introduced later without rewriting the NPC core.

## Why This Approach
- Supports many roles with optional capabilities per role.
- Minimizes duplication by reusing capability components.
- Works for static NPCs and moving AI NPCs.
- Keeps advanced AI optional and incremental.

## Core Contracts

### Data Contracts (ScriptableObject)
- `NpcDefinition`: identity/presentation defaults and role preset binding.
- `NpcRolePreset`: role type + enabled capability list + optional capability configs.
- `NpcCapabilityConfig`: base for per-capability role configuration.

### Runtime Contracts
- `NpcAgent`: root runtime shell; loads definition/role and orchestrates capabilities.
- `INpcCapability`: lifecycle + optional interaction hooks.
- `INpcActionProvider`: contributes player-facing actions for interaction UI.
- `INpcDecisionProvider`: AI decision seam for now/future.

### AI Evolution Seam
- `RuleBasedDecisionProvider` is default now.
- Later `BehaviorTreeDecisionProvider` can be attached per role/capability without changing `NpcAgent` public contracts.

## Prefab Authoring Model
- Base prefab: `NPC_Base` (contains `NpcAgent`, optional interaction and AI wiring).
- Role prefabs: lightweight variants (police, wardens, vendors, organizers, etc.) changing data/config only.
- Designers can drag role prefab and set `NpcDefinition`/`NpcRolePreset`.

## Scope for This Implementation Slice
- Add foundational data/runtime interfaces and orchestrator.
- Add action aggregation path for UI integration.
- Add decision-provider seam for future BT layer.
- Keep existing vendor interaction working.
- Add EditMode tests for lifecycle/action/decision seams.

## Deferred
- Full behavior trees implementation.
- Full dialogue/quest UI integration.
- Full save payload integration for all capability runtime states.
- Full editor prefab rebuild tool generalization.

## As-Built Note (2026-02-26)
- Foundation contracts were implemented as designed: `NpcDefinition`, `NpcRolePreset`, `NpcCapabilityConfig`, `NpcAgent`, `INpcCapability`, `INpcActionProvider`, and `INpcDecisionProvider`.
- Decision seam is active in runtime via `NpcAiController`; default evaluation uses `RuleBasedDecisionProvider`.
- World-authoring now starts from role prefabs under `Reloader/Assets/_Project/NPCs/Prefabs/Roles/`, then assigning `NpcDefinition`/`NpcRolePreset` plus capability MonoBehaviours on the placed prefab.
- Deferred items remain unchanged for later slices: behavior-tree provider/assets, full dialogue+quest integration, and complete capability-state persistence.
