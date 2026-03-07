# MainTown Population Slots Progress

## Status

- [x] Design direction approved
- [x] Implementation plan written
- [ ] Runtime implementation started

## 2026-03-07 Checkpoint

- Approved the next structural slice after the persistent population foundation:
  - `MainTown` owns one map-wide population roster
  - the roster is partitioned into fixed pools like `townsfolk`, `quarry_workers`, `hobos`, and `cops`
  - each occupant should eventually live in a stable `populationSlotId`
  - the slot owns the durable world role while the occupant can change over time
  - replacements should later refill the same slot with a new civilian identity and appearance
- Locked future targeting guardrails:
  - contracts should later choose from the whole living `MainTown` population
  - vendors are the first protected exclusion
  - dead occupants must never be eligible for contract targeting
- Deferred from this slice:
  - contract target selection
  - Monday `08:00` replacement execution
  - professions, schedules, wandering zones, dialogue, and voices
  - committed STYLE appearance-part curation

## Next Step After This One

Once slot-driven `MainTown` population is stable, the next slice should curate the first committed appearance-part pool from the STYLE kit and wire real visual assembly/prefab selection so generated civilians use approved bodies, hair, clothes, and color variants in-game.
