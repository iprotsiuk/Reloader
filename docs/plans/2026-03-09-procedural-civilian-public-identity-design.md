# Procedural Civilian Public Identity Design

**Date:** 2026-03-09
**Status:** Approved for implementation

## Goal

Make generated `MainTown` civilians feel like real contract targets by giving every occupant a persisted public identity and contract-facing visual description while preserving stable save/load behavior and slot-driven replacement rules.

## Chosen Approach

Keep the current stable internal `civilianId` as the hidden save/runtime key and add a separate persisted public identity layer to each `CivilianPopulationRecord`:

- `firstName`
- `lastName`
- `nickname` optional

Contracts, combat-target display, and future NPC-facing systems should use the public identity. Save/load, replacement tracking, and internal lookup should continue using `civilianId`.

## Why This Approach

- Save/load stays deterministic because public identity is persisted, not regenerated from changing name pools or generator rules.
- Contracts become readable and memorable without breaking the current slot-driven civilian runtime.
- Monday replacements can inherit the same world role while still becoming genuinely new people.
- The identity split is future-proof for dialogue, police reports, handlers, or fixer intel without overloading the internal save key.

## Identity Model

Each generated civilian has two identity layers:

### Internal Identity

- `civilianId` remains the hidden stable runtime/save key, for example `citizen.mainTown.0007`
- `populationSlotId`, `poolId`, `spawnAnchorId`, and `areaTag` remain the stable world-role anchors

### Public Identity

- `firstName`
- `lastName`
- `nickname` optional and usually empty

Default public display format:

- `First Last`

Nickname usage:

- Most civilians should have no nickname.
- A small minority can have a flavor nickname, for example `Derek "Socks" Mullen`.
- Nicknames should support tone and memorability, but should not replace the readable main contract target name.

## Slot Versus Occupant Contract

The slot owns the town function. The occupant owns the person.

### Slot-Owned State

- `populationSlotId`
- `poolId`
- `spawnAnchorId`
- `areaTag`
- future occupation / behavior / routine intent
- future protected-role behavior

### Occupant-Owned State

- `civilianId`
- public identity fields
- appearance snapshot
- alive/dead lifecycle flags
- created/retired timestamps

This means a replacement civilian should preserve the same slot purpose but become a different person.

## Generation Rules

On fresh save generation, each occupant should receive:

- one persisted first name
- one persisted last name
- an optional nickname with low probability
- one persisted appearance snapshot
- one persisted set of visual-description tags for contract copy

Name tone:

- believable by default
- occasional weirdness
- not parody names for every civilian

## Save, Load, Travel, And Sleep Rules

- Save/load must restore the exact same occupant record, including public identity and appearance.
- Fast travel, scene reload, and sleeping must rebuild the same current occupants from the saved records.
- No part of public identity or appearance should reroll during restore.

## Death And Monday Replacement Rules

- When a civilian dies, that occupant remains dead in the save and becomes permanently ineligible for contracts in that save.
- The existing replacement-debt flow remains slot-driven.
- On the Monday replacement pass, the same slot receives a new occupant package.
- The new occupant package must preserve the slot/world-role fields but generate:
  - a new `civilianId`
  - a new public identity
  - a new nickname chance
  - a new appearance snapshot

After generation, the replacement occupant must persist across save/load, travel, and sleep exactly like any other current civilian.

## Contract-Facing Identity Rules

Contract text must always describe the live occupant currently filling the slot, never the historical occupant who used to hold that role.

Rules:

- Contract `TargetDisplayName` should use the current occupant public name, not `civilianId`.
- Contract `TargetDescription` should use the current occupant visual-description tags.
- If a nickname exists, it can appear as flavor in briefing text or an extra line, but not replace the main readable target name.
- Contract target combat/display seams should also use the current occupant public name instead of `civilianId`.
- Cached or prior target copy must not survive when the old occupant dies and a new occupant later fills the slot.

Practical result:

- if a quarry-worker occupant dies
- the replacement remains a quarry-worker slot occupant
- the next contract must describe the replacement's name and look, not the dead target's identity

## Data Contract Additions

`CivilianPopulationRecord` should add persisted public identity fields:

- `firstName`
- `lastName`
- `nickname`

The record should continue storing the current appearance snapshot and generated visual-description tags so contract copy always matches the live occupant.

Because the save payload shape changes, the civilian save module and global save schema should bump so old saves fail cleanly rather than restoring partially valid records.

## Validation Targets

- fresh save generation creates one unique public identity per live occupant
- save/load preserves public identity and appearance exactly
- fast travel / sleeping keep the same living occupants unchanged
- killed civilians never reappear as live occupants in the same save
- Monday replacement creates a new occupant in the same slot with a different public identity and a different appearance snapshot
- published contracts use the current occupant public name and current occupant visual description
- replacement contracts never leak the dead occupant's old identity text

## Out Of Scope

This slice should not implement:

- full authored occupations beyond existing slot/pool semantics
- daily schedules or route simulation
- civilian dialogue trees driven by generated names
- police paperwork or underworld intel systems

Those future systems should consume the same persisted public-identity layer rather than invent a second naming model later.
