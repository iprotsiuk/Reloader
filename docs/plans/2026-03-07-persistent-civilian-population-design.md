# Persistent Civilian Population Design

**Date:** 2026-03-07
**Status:** Approved for implementation

## Goal

Create the foundation for a save-persistent, map-wide civilian population in `MainTown` so contracts can later target real town citizens instead of one-off authored placeholders.

## Chosen Approach

Generate a `MainTown`-wide civilian roster when a save is created, persist that roster as data, and respawn runtime civilians from the saved records whenever the scene loads. Dead civilians remain retired in that save, and their vacancies are queued for a scheduled population refresh every Monday at `08:00` at the bus stop.

## Why This Approach

- The town feels stable within a save instead of rerolling randomly on each load.
- Contract targets can later be selected randomly from real living civilians anywhere in `MainTown` without fake descriptions.
- Replacements happen on a believable cadence instead of instant respawns.
- The foundation separates appearance generation from later behavior systems like schedules, voices, and dialogue.

## Population Lifecycle

- On new save creation, generate a persistent civilian roster for the whole `MainTown` scene.
- Vendors and other protected authored roles are excluded from the generated civilian pool and from future contract targeting.
- Loading `MainTown` spawns civilians from the saved roster rather than regenerating them.
- When a civilian dies, that record stays dead in the save and becomes ineligible for future contracts.
- Each retired civilian creates one open population vacancy.
- Vacancies are not filled immediately. They are queued until the scheduled refresh window:
  - every Monday at `08:00`
  - replacement civilians enter through the bus stop / arrival point
  - with the current day-precision save model, a vacancy queued on a Monday waits until the following Monday refresh because time-of-death is not persisted

## Map-Wide Population Slots And Pools

- `MainTown` owns one persistent population roster for the whole scene rather than separate settlement-local rosters.
- The roster is partitioned into authored pools such as `townsfolk`, `quarry_workers`, `hobos`, and `cops`.
- Each pool owns one or more stable `populationSlotId` entries.
- A `populationSlotId` represents the durable world role for that occupant:
  - pool membership
  - future profession / routine binding
  - future area or zone ownership
- Civilians occupy slots; slots do not belong to a specific person.
- When a civilian dies, the slot remains and the future replacement fills that same slot with:
  - a new `civilianId`
  - a new appearance
  - the same pool / role purpose
- Later behavior systems should attach to the slot definition rather than to the current civilian identity.

## Civilian Appearance Record

Each generated civilian record should persist enough appearance data to rebuild the same visible person on load and to describe them accurately in future contracts. Initial appearance fields should cover:

- `populationSlotId`
- `poolId`
- `civilianId`
- `isAlive`
- `isContractEligible`
- `isProtectedFromContracts`
- `baseBodyId`
- `presentationType`
- `hairId`
- `hairColorId`
- `beardId`
- `outfitTopId`
- `outfitBottomId`
- `outerwearId`
- `materialColorIds`
- `generatedDescriptionTags`
- `spawnAnchorId`
- `areaTag`
- `createdAtDay`
- `retiredAtDay`

Behavioral extensions like professions, dialogue, voices, wandering zones, and schedules are explicitly out of scope for this slice, but the record must leave room for those later.

## Generator and Runtime Split

### Generator Library

- A curated appearance-part library under `_Project` defines which bodies, hair parts, outfit parts, and material variants are valid inputs.
- Initial generation uses free random slot selection with only structural guards:
  - body-compatible part selection
  - one active choice per slot
  - no missing required body content

### Persisted Civilian Record

- Save data stores the selected appearance choices and core lifecycle flags for each civilian.
- Save data stores the stable `populationSlotId` so replacements can inherit the same role slot later.
- Save data should not depend on live scene references.

### Runtime Civilian Instance

- Scene load spawns a runtime civilian from one persisted record.
- The runtime instance should align with the existing NPC shell so later slices can attach schedules, dialogue, professions, and contract logic without redoing the prefab contract.

## First Implementation Slice Boundary

This slice should implement:

- appearance generator library contract
- persistent civilian appearance record
- save creation path that generates the initial roster
- scene load path that spawns civilians from saved records
- retirement of dead civilians
- replacement queue entries for later Monday refresh

This slice should not implement:

- contract target selection from the population
- generated contract copy or target portraits
- wandering zones, professions, dialogue, voices, or schedules
- Monday refresh execution beyond recording the owed replacement

## Contract Targeting Guardrails For The Next Slice

- Contract targets should be selected from the whole living `MainTown` population rather than one sub-area.
- Vendors are the only protected exclusion in the initial targeting pass.
- Dead occupants must never be eligible for contract selection.
- Future replacements should become targetable only after they occupy the slot as a live current civilian.
- If the Monday refresh is processed while `MainTown` is not loaded, the replacement should already be in place by the next scene load rather than performing a delayed on-screen arrival.

## Validation Targets

- generator creates structurally valid civilian appearance records
- save/load preserves the generated roster exactly
- `MainTown` respawns the same civilians from saved records
- civilian death retires the record and creates one replacement debt entry
- generated civilians stay separate from vendors / protected authored NPCs

## Next Step After This Slice

Once this foundation is stable, the next slice should add a `MainTownPopulationDefinition` with stable pools and `populationSlotId`s, fill those slots with generated occupants, and then add random contract-target selection from the persistent living population plus appearance-derived contract descriptions so the posted contract text always matches the actual chosen civilian.
