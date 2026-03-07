# MainTown Population Slots Design

**Date:** 2026-03-07
**Status:** Approved for implementation

## Goal

Define a map-wide persistent population structure for `MainTown` so generated civilians occupy stable world-role slots instead of existing as an undifferentiated roster.

## Chosen Approach

Add a `MainTownPopulationDefinition` asset that owns fixed pool counts and stable `populationSlotId` entries for the whole scene. Generated civilians will occupy those slots, persist across save/load, and be replaced into the same slot after retirement so the world role survives even when the occupant changes.

## Why This Approach

- The scene can support distinct groups like `townsfolk`, `quarry_workers`, `hobos`, and `cops` without hardcoding counts in runtime code.
- Replacement civilians inherit the same world role cleanly because the slot persists while the occupant changes.
- Later systems like professions, schedules, wandering zones, and contract targeting can bind to the slot instead of to one specific NPC identity.
- Contract selection can later target the whole living `MainTown` population while still excluding protected roles like vendors.

## MainTown-Wide Population Model

- `MainTown` owns one persistent population roster for the whole scene.
- The roster is partitioned into authored pools, for example:
  - `townsfolk`
  - `quarry_workers`
  - `hobos`
  - `cops`
- Each pool owns a fixed number of stable `populationSlotId`s.
- Each slot represents the durable world role for one occupant:
  - pool membership
  - future profession / behavior identity
  - future area or zone ownership
  - future replacement anchor
- The occupant of a slot can change over time, but the slot itself persists.

## Slot And Occupant Responsibilities

### Slot Definition

Each slot definition should describe stable authored world meaning:

- `populationSlotId`
- `poolId`
- `areaTag`
- `spawnAnchorId`
- `isProtectedFromContracts`
- future schedule / zone bindings

### Occupant Record

Each occupant record should describe the current civilian filling the slot:

- `populationSlotId`
- `poolId`
- `civilianId`
- `isAlive`
- `isContractEligible`
- `isProtectedFromContracts`
- appearance fields already defined in the persistent population foundation
- `createdAtDay`
- `retiredAtDay`

The slot owns the role; the occupant owns the current identity and appearance.

## Replacement Rules

- When a civilian dies, the occupant record stays dead in that save.
- The slot remains open and records replacement debt for the future refresh.
- A replacement civilian later fills the same `populationSlotId` with:
  - a new `civilianId`
  - a new appearance
  - the same pool and world-role purpose
- If the Monday `08:00` refresh happens while `MainTown` is loaded, a later slice may stage visible arrival through the bus stop.
- If `MainTown` is not loaded at the refresh time, the replacement should already be in place by the next time the scene loads.

## Contract Guardrails

- Future contracts should select targets from the whole living `MainTown` population, not from one sub-area only.
- Vendors are the only protected exclusion for the initial targeting pass.
- Dead occupants must never be eligible for contract selection.
- Future protected story/special NPCs should opt into `isProtectedFromContracts` without changing the targeting algorithm.

## This Slice

This slice should implement:

- `MainTownPopulationDefinition`
- fixed pools and stable `populationSlotId` entries
- occupant records keyed by slot
- runtime generation that fills slots instead of emitting an anonymous roster
- `MainTown` spawn-from-records using slot-driven occupants

This slice should not implement:

- contract target selection
- Monday refresh execution
- professions, schedules, wandering zones, dialogue, or voices
- full visual asset curation for the STYLE kit

## Validation Targets

- `MainTownPopulationDefinition` validates fixed pools and unique `populationSlotId`s
- newly generated rosters assign one occupant per slot
- save/load preserves slot ownership and occupant identity separately
- `MainTown` spawns live occupants from persisted slots
- retired occupants do not respawn until a later replacement pass

## Next Step After This Slice

Once slot-driven `MainTown` population is stable, the next slice should curate the first committed appearance-part pool from the STYLE kit and wire real visual assembly/prefab selection so generated civilians use approved bodies, hair, clothes, and color variants in-game.
