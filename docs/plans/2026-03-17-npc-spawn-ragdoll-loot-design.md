# NPC Spawn, Ragdoll, And Corpse Loot Design

## Goal

Make developer-spawned NPC testing usable and deterministic by:

- fixing `spawn npc` autosuggest so viable ids are discoverable in-console
- adding explicit `spawn npc random` and `spawn npc randomContract`
- ensuring killable humanoid NPCs keep a single shared ragdoll/death presentation path
- making dead humanoid NPCs lootable through the existing storage system with unique per-body storage instances

This slice prioritizes one authoritative runtime path over fallback-heavy parallel systems.

## As-Built Status (2026-03-17)

Shipped runtime behavior in this branch:

- `spawn npc ` suggests catalog ids plus explicit `random` and `randomContract` tokens
- `spawn npc random` uses the catalog spawn service and the shared crosshair/fallback pose resolver
- `spawn npc randomContract` routes through `CivilianPopulationRuntimeBridge.TrySpawnDebugContractCivilian(...)`
- bridge-spawned contract civilians receive `HumanoidHitboxRig`, `HumanoidDamageReceiver`, `HumanoidRagdollController`, `HumanoidCorpseLootController`, and `ContractTargetDamageable`
- lethal contract-capable civilians keep the body active, ragdoll, receive the final impulse, and expose unique empty corpse storage

Current limitation:

- the slice does not yet upgrade the authored role prefabs themselves to a fully pre-authored ragdoll stack; the shared contract is applied at runtime by the population bridge and backed by the ragdoll controller's root-body fallback when authored rigidbodies are absent

---

## Decision Summary

### Recommended direction

Adopt a single authored/runtime seam for killable humanoid NPCs:

- `spawn npc <id>` remains catalog-driven
- `spawn npc random` chooses from the same catalog
- `spawn npc randomContract` uses the same humanoid combat + lootable-corpse stack as real contract-eligible civilians
- all killable spawned humanoids present death through the shared `HumanoidDamageReceiver` -> `HumanoidRagdollController` -> `ContractTargetDamageable` chain
- corpse looting reuses `WorldStorageContainer` rather than introducing NPC-only inventory logic

### Why this direction wins

- It removes the hidden-id usability problem without inventing another spawn console.
- It avoids the “multiple systems overriding each other” failure mode by routing contract deaths and corpse interaction through one stack.
- It keeps future authored NPC prefab work aligned with the same runtime contracts instead of preserving a procedural-only exception.
- It reuses the existing storage UI/runtime seam, so corpse looting ships as a thin world interaction layer instead of a second inventory implementation.

### Rejected direction

- Adding special-case procedural-only fallbacks for `randomContract` while leaving authored humanoids on separate death/loot behavior.
  - This would ship faster, but it would preserve the same duplication and override risk that already caused trouble in weapon presentation flows.

---

## Command UX

### `spawn npc`

The console should treat `spawn npc` like `give item`:

- `spawn npc` and `spawn npc ` both surface viable next-token suggestions
- suggestions come directly from the spawn catalog plus explicit synthetic tokens:
  - `random`
  - `randomContract`
- `spawn npc <prefix>` filters catalog entries and the synthetic tokens by prefix

### Execution semantics

- `spawn npc <catalog-id>`
  - spawns the authored catalog prefab
- `spawn npc random`
  - randomly selects one viable catalog entry and spawns it
- `spawn npc randomContract`
  - spawns a contract-eligible humanoid civilian/test target at the crosshair hit point or camera-forward fallback position

The command stays suggest-only when no third token is supplied. Bare `spawn npc` must not spawn anything.

---

## Unified Humanoid Death Path

### Runtime rule

Any humanoid NPC that can be killed in this slice must satisfy the same minimum combat/death contract:

- `HumanoidHitboxRig`
- `HumanoidDamageReceiver`
- `HumanoidRagdollController`
- `ContractTargetDamageable` when the NPC is contract-relevant
- corpse loot presenter using `WorldStorageContainer`

### Presentation rule

Lethal hits must:

- disable animator and conflicting runtime behaviours
- enable ragdoll rigidbodies/colliders
- apply the final directional impulse to the struck body or torso fallback
- preserve the GameObject so the body remains in-world for looting

If a humanoid is intended to be killable, missing ragdoll/loot wiring is a content bug, not a runtime excuse to despawn it.

---

## Corpse Loot Model

### Storage reuse

Dead NPCs should be lootable through the existing storage pipeline:

- attach or author a `WorldStorageContainer` on the NPC root
- give each NPC instance a unique `containerId`
- register an empty `StorageContainerRuntime` when the corpse state begins
- expose the corpse through the same storage interaction flow the player already uses

### Instance identity

Each corpse storage instance must be unique per NPC instance. Two dead NPCs must never resolve to the same `containerId` or runtime container.

Initial contents are intentionally empty in this slice.

---

## Authoring Contract

### Authored humanoid prefabs

Prefab authoring should still move toward one shared combat-capable humanoid base rather than leaving ragdoll and corpse interaction as procedural-only add-ons.

Current shipped state for this slice:

- `randomContract` resolves through `CivilianPopulationRuntimeBridge` so the debug command uses the same runtime seam as live contract civilians
- procedural/runtime wiring currently adds the shared killable-humanoid contract when authored prefabs are still thin
- broader authored prefab hardening remains follow-up work, but killable runtime targets already preserve a body through the shared ragdoll + corpse-loot path

### Procedural civilians

`CivilianPopulationRuntimeBridge` currently configures contract eligibility and supplies the shared killable-humanoid stack for its spawned civilians, including explicit `randomContract` debug spawns.

---

## Testing Boundary

### EditMode

- `spawn npc` suggestion coverage for trailing-space and prefix cases
- `random` and `randomContract` token resolution rules
- corpse storage id uniqueness and empty-container initialization for spawned humanoids

### PlayMode

- `spawn npc random` spawns a viable catalog NPC at the crosshair/fallback pose
- `spawn npc randomContract` spawns a killable contract-eligible humanoid with the shared combat stack
- lethal hits preserve the body, trigger ragdoll takeover, and apply the last impulse
- dead NPCs expose unique lootable storage containers

### Manual Unity validation

In Play Mode:

1. `spawn npc `
2. verify autosuggest shows catalog ids plus `random` and `randomContract`
3. `spawn npc random`
4. `spawn npc randomContract`
5. kill both spawned humanoids
6. verify both bodies ragdoll instead of despawning
7. verify both bodies can open storage independently

---

## PR Workflow

This slice should ship through a non-draft PR to `main` with:

- focused commits
- updated design/plan docs reflecting the unified path decision
- targeted Unity test evidence
- PR comment tagging `@codex` for review
