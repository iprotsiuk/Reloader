# Law Enforcement & Black Market Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.
> **Related:** [assassination-contracts.md](assassination-contracts.md) for the contract loop that feeds police heat.

---

## Police Heat Model [v0.1]

Implemented core loop:

`Clear -> Alerted -> Active Pursuit -> Search -> Clear`

Implemented now:
- `PoliceHeatRuntime.ReportCrime(...)` is the canonical crime ingress and owns heat/wanted transitions.
- `PoliceHeatController` is a thin law-enforcement wrapper that forwards crime and line-of-sight calls to `PoliceHeatRuntime`.
- `PoliceHeatState` carries heat level, last crime type, search timer, line-of-sight state, wanted level, player identification state, and identification progress.
- `PoliceHeatStateModule` persists current heat state through the save pipeline.
- `ILawEnforcementEvents` is output-only heat broadcasting (`OnHeatChanged` / `RaiseHeatChanged`) and must not be used as a witness-input API.
- `PoliceDispatchCoordinator` listens to heat changes, stages active/search responders from spawned police actors, can materialize dispatch-reserve police from `CivilianPopulationRuntimeBridge`, scales low wanted levels down to one responder, and retires reserves when heat clears.
- The compass HUD reads current heat/dispatch state and shows `WANTED` or `SEARCH` with wanted level and active responder count.

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

## Witness Reporting Contract [v0.1]

Planned next slice:
- Add a dedicated inbound surface: `ILawEnforcementCrimeReporter.ReportCrime(CrimeType crimeType)`.
- Implement the surface by delegating to `PoliceHeatRuntime.ReportCrime(...)`.
- Keep the reporter source-agnostic for v0.1; witnesses, dialogue, future corpse discovery, and scripted tutorial/debug paths can all call the same ingress without becoming heat owners.
- Do not add witness input methods to `ILawEnforcementEvents` or `IGameEventsRuntimeHub`.
- Wire normal civilian actors with a passive witness/death reporter in `CivilianPopulationRuntimeBridge`.
- Reuse `HumanoidDamageReceiver.Died` for the MVP death hook and report `CrimeType.Murder` once.
- Exclude cops, dispatch-only reserve police, and active contract targets from witness reporter injection by default.
- Keep perception local/simple if needed; no shared civilian panic/fleeing or investigation system belongs in this slice.

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

Each offense maps to a consequence tier and can raise police heat. Current v0.1 runtime output is `ILawEnforcementEvents.OnHeatChanged`; if a future `OnCrimeCommitted(CrimeType)` output event is added, it must remain broadcast-only and separate from `ILawEnforcementCrimeReporter` input.
