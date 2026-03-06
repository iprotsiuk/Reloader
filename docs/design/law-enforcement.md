# Law Enforcement & Black Market Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.
> **Related:** [assassination-contracts.md](assassination-contracts.md) for the contract loop that feeds police heat.

---

## Police Heat Model [v0.1]

The first police loop should be simple and readable:

`Clear -> Alerted -> Active Pursuit -> Search -> Clear`

Heat sources:
- witnessed murder
- corpse discovery
- visible weapon brandishing
- gunshots in populated areas
- police line of sight during escape

Resolution rule:
- once police lose direct line of sight, a cooldown starts
- if the player stays hidden long enough, the search collapses and active pursuit ends
- the exact timer can ship as a simple tuned value (`30-60` seconds) before more advanced suspicion systems exist

---

## Arrest, Death, and Confiscation [v0.1]

Failure needs a concrete resource penalty.

Rules:
- If police arrest the player, carried inventory is confiscated.
- If police kill the player, carried inventory is also lost.
- The player respawns at either hospital or police station depending on failure type.
- Home/workshop storage remains intact unless a later system explicitly introduces stash raids.

---

## Policing Surface [v0.2]

| Authority | Where | Checks For |
|-----------|-------|-----------|
| Town police | Town, roads | Murder response, visible weapons, wanted suspects |
| Patrol units | Streets, parking lots, major intersections | Search pressure and active pursuit |
| Road units | Roads, exits, checkpoints | Escape routes, vehicle stops, last-known-direction sweeps |

---

## Black Market [v1+]

- Shady NPC dealers in specific locations (back alleys, remote areas)
- Sell restricted/illegal items and contract support gear
- Higher risk: can be caught during transaction
- No warranty: items may be defective or traceable
- Useful for dangerous high-payout jobs, but never safer than legal prep

---

## Consequences [v0.1]

| Offense | Consequence |
|---------|-------------|
| Random public murder | Immediate high heat, rapid police response |
| Weapon brandish in public | Low-to-medium heat, witness-driven escalation |
| Resisting / fleeing | Extended pursuit, larger search radius |
| Arrest | Carried inventory confiscated, respawn at police station, money/time penalty |
| Killed by police | Carried inventory confiscated, respawn at hospital |
| Repeat chaos | Faster police escalation and worse civilian reactions |

---

## Data Model [v1+]

`CrimeType` enum:
- `Murder`
- `AttemptedMurder`
- `Brandishing`
- `IllegalWeapon`
- `IllegalAttachment`
- `IllegalAmmo`
- `Trespassing`
- `Resisting`
- `Fleeing`
- `BlackMarketTransaction`

Each offense maps to a consequence tier and can raise police heat. The law enforcement system emits `OnCrimeCommitted(CrimeType)` through the runtime event hub (`IGameEventsRuntimeHub`) so other systems can react (heat state, NPC relationships, contract failure, economy penalties; formerly `GameEvents.OnCrimeCommitted`).
