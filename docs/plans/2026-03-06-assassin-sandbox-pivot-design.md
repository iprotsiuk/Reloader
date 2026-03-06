# Assassin Sandbox Pivot Design

## Goal

Realign `Reloader` from a reloading-first sandbox into a precision-assassination sandbox where procedurally generated contracts are the primary economy and progression loop.

Reloading, load development, optics, zeroing, and MOA measurement remain critical, but they now exist to support premium long-range contract kills rather than acting as the main fantasy on their own.

---

## Decision Summary

### Recommended direction

Adopt a `precision-contract sandbox` structure:

- contracts are the main money source
- long-range jobs pay the most
- random murder is allowed but low-value
- police response follows a GTA-like `lose LOS -> survive timer -> clear heat` pattern
- arrest or death confiscates carried inventory and respawns the player at hospital or police station

### Why this direction wins

- It preserves the game's unique technical depth instead of collapsing into a generic crime sandbox.
- It gives reloading and ballistics a concrete player-facing payoff.
- It keeps the recently merged scoped PiP optics work strategically important.

### Rejected directions

- `Broad crime sandbox`: too generic; reloading becomes optional flavor.
- `Pure sniper sim`: too narrow; loses the town-sandbox energy and consequence loop.

---

## Core Fantasy

The player lives in a town-sized sandbox, takes procedurally generated assassination contracts, prepares equipment and ammunition, executes the hit, and escapes the response.

The highest-value jobs emphasize precision shooting:
- correct optic setup
- correct zero
- stable shooting position
- consistent ammo
- patience and route planning

Some premium contracts should be impractical or impossible to solve by simply walking up to the target.

---

## Core Loop

1. Accept a generated contract.
2. Review target intel, payout, risk, and preferred distance band.
3. Build or select the rifle, optic, and ammunition.
4. Validate the setup at the range or another safe test space.
5. Travel to a firing position or staging point.
6. Eliminate the target.
7. Break line of sight and survive the police search window.
8. Cash out if the job resolves cleanly.

Random killing is not the main economic loop. It is mostly a chaos valve that creates police heat and resource loss.

---

## World Structure

`MainTown` remains the hub, but its meaning changes:

- house/workshop = prep space
- shops = procurement space
- indoor range = validation space
- town streets/civilians/police = consequence space
- rooftops, windows, alleys, parking lots, and checkpoints = contract execution and escape space

The world should stay authored. Procedural variety should come from contract generation, target routines, exposure windows, and payout modifiers rather than from a fully procedural city.

---

## Contract Model

Generated contract inputs should come from authored archetypes:

- target type
- scene / anchor
- routine window
- distance band
- witness density
- police risk
- modifiers such as suppressor preference, no-collateral pressure, or narrow timing

The player succeeds by solving a generated problem inside a stable authored world.

---

## Police / Failure Model

First shipped wanted-state shape:

`Clear -> Alerted -> Active Pursuit -> Search -> Clear`

Heat sources:
- witnessed murder
- corpse discovery
- visible weapon brandishing
- gunshots near civilians
- direct police line of sight

Failure consequence:
- police death or arrest confiscates carried inventory
- the player respawns at hospital or police station
- home/workshop storage stays safe unless a later system intentionally expands consequences

---

## System Priorities

### Primary

- assassination contracts
- target generation
- long-range shooting
- police heat / search / escape
- confiscation + respawn consequences
- contract payout + progression

### Supporting but essential

- weapon building
- reloading and load development
- optics and zeroing
- range validation
- persistence
- world travel and staging

### Deferred / demoted

- hunting
- competitions

They may return later as optional side systems, but they must not redefine the main roadmap.

---

## First Milestone

The first milestone after the pivot should be a full vertical slice:

- accept a procedurally generated contract
- prepare rifle / ammo / optic
- optionally validate at the range
- kill a target best solved from distance
- trigger police response if exposed
- escape by breaking line of sight and surviving the countdown
- receive payout on success
- lose carried inventory and respawn on death or arrest

This milestone gives the project its real identity. The existing long-range scope work remains an enabling subsystem inside that slice, not the whole product direction.
