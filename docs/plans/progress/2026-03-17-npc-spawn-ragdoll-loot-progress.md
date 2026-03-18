# NPC Spawn, Ragdoll, And Corpse Loot Progress

## Status

- [x] Debug spawn commands now support explicit random NPC spawning.
- [x] Contract-eligible NPCs now keep a ragdoll body on death.
- [x] Corpse loot uses unique per-body storage instances.
- [ ] Broader authored contract tuning and content expansion remain outside this slice.

## Shipped In This Slice

- `spawn npc random` spawns a random debug NPC at the current crosshair hit point.
- `spawn npc randomContract` spawns a contract-eligible civilian through the live `CivilianPopulationRuntimeBridge` at the current spawn pose.
- The contract-eligible spawn path uses the shared civilian bridge instead of a separate fallback system, so debug spawning and contract spawning stay on one runtime seam.
- Contract-eligible civilians now carry the shared combat stack needed for lethal damage, ragdoll presentation, and corpse loot.
- Lethal hits keep the NPC in-world as a ragdoll and apply the final impulse instead of immediately despawning the body.
- Each corpse exposes a unique storage container instance for post-kill looting.
- Corpse containers are empty for now; the important contract is unique ownership, not starting contents.

## Verification Snapshot

- EditMode coverage on the civilian bridge exercises the contract-target seam for procedurally spawned civilians.
- PlayMode coverage for the dev spawn command reaches the bridge path for `randomContract`.
- PlayMode coverage for the ragdoll controller verifies the fallback root body still applies lethal impulse when authored ragdoll bodies are absent.

## Resulting Contract

- Dev tools no longer require opaque NPC ids for basic spawning during iteration.
- Contract-eligible NPCs now leave a lootable body behind when they die.
- Corpse looting stays on the existing storage abstraction, so later item population can remain data-driven without a separate corpse-inventory system.

## Notes

- This note is intentionally narrow and reflects the shipped runtime slice, not the whole assassination-contract roadmap.
- The wider contract-target, payout, and scene flow remain tracked by the existing vertical-slice plan and progress notes.
