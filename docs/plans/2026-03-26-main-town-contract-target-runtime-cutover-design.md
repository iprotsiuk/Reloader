# MainTown Contract Target Runtime Cutover Design

## Goal

Move MainTown contract targets onto one canonical runtime civilian path so contract targets no longer depend on scene-authored humanoid instances or the currently spawned ambient civilian body.

## Problem

MainTown currently picks a live civilian record from the population roster and then marks that already-spawned ambient civilian as the contract target. In practice this keeps the contract path entangled with scene-authored and ambient NPC presentation state, which is brittle and hard to debug. The recent vendor stretch issue made the core problem obvious: humanoid presentation should come from one runtime path, not a mix of authored scene humans and runtime civilians.

## Decision

Keep the existing roster-driven contract identity for now, but stop using the ambient civilian instance as the target body.

When a contract is available or active:

- pick the same `CivilianPopulationRecord` from the roster as today
- skip that civilian during the normal ambient population rebuild
- spawn one dedicated runtime civilian from `NpcFoundation` at a contract-target anchor
- attach `ContractTargetDamageable` only to that dedicated spawned target body
- resolve the active target back through `TryResolveSpawnedCivilian` using the same `CivilianId`

This is an incremental cutover, not a total contract runtime rewrite.

## Why This Approach

This preserves the parts that already work:

- `StaticContractRuntimeProvider` keeps the same target id semantics
- save/load continues to track the same civilian identity
- contract offer/accept/claim flow stays intact
- the target still comes from the authored civilian roster and appearance generator

But it removes the brittle part:

- the live target body is no longer the same object as the ambient civilian instance

That gives us one stable runtime NPC prefab/presentation path for contract targets without forcing a new save schema or contract-definition model in the same pass.

## Scope

In scope:

- dedicated contract-target anchor support in `CivilianPopulationRuntimeBridge`
- skipping the tracked target during ambient scene rebuild
- spawning one dedicated runtime target body for the tracked contract id
- keeping `TryResolveSpawnedCivilian` working for accepted/offered targets
- updating MainTown tests around target placement and resolution
- wiring MainTown to authored contract-target anchors

Out of scope:

- replacing the roster/contract identity model
- vendor NPC refactors
- ambient civilian movement
- full authored mission scripting
- new police/search design

## Runtime Design

### Data

`CivilianPopulationRuntimeBridge` gains dedicated contract-target anchor ids. The tracked contract target still comes from the existing available/active contract snapshot target id.

### Rebuild Behavior

During `RebuildScenePopulation()`:

1. Clear existing spawned civilians as today.
2. Resolve the tracked contract target id from the provider.
3. Spawn all live civilians except the tracked target id through the ambient path.
4. If the tracked target id exists in the roster and contract-target anchors are configured, spawn one dedicated runtime target body at a contract-target anchor.
5. Refresh the contract offer and contract-target damageables.

### Spawn Rules

- Ambient civilians keep using their own roster spawn anchors.
- The dedicated contract target uses a dedicated contract anchor list.
- If no contract-target anchor resolves, fall back to the old ambient spawn anchor so the system degrades safely during authoring.

### Resolution

`TryResolveSpawnedCivilian(civilianId, out civilian)` continues to work because the dedicated target body still carries `MainTownPopulationSpawnedCivilian` initialized from the same roster record.

## MainTown Authoring

MainTown should own explicit target anchors near the authored lane/reference cluster instead of relying on whatever ambient civilian spawn happened to be chosen. These anchors belong under `MainTownPopulationRuntime` so the bridge can resolve them directly by name.

## Testing

Add regression coverage for:

- when a contract target is tracked, that id is not spawned through the ambient path
- a dedicated target body is spawned at the dedicated target anchor
- non-target civilians stay on the normal ambient path
- MainTown contract slice still resolves the target through the population bridge
- MainTown target remains near the authored lane/reference cluster

## Risk

The main risk is save/load or target-resolution assumptions that currently expect the target body to be the ambient civilian instance. That is why the cutover should preserve `CivilianId` identity and `TryResolveSpawnedCivilian` semantics instead of introducing a new target-id model in the same change.
