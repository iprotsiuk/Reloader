# Assassination Contracts Design

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.
> **Related:** [law-enforcement.md](law-enforcement.md) for police heat and arrest flow, [world-and-vehicles.md](world-and-vehicles.md) for world topology and escape geography, [weapons-and-ballistics.md](weapons-and-ballistics.md) for the precision-shooting stack.

---

## Core Fantasy [v0.1]

`Reloader` is a first-person assassination sandbox with hardcore prep.

The player earns money primarily by taking assassination contracts, preparing the right rifle/ammo/optic setup, executing the kill, and escaping the response. Reloading, load development, zeroing, and MOA measurement remain central, but they exist to support expressive contract kills rather than replace them.

Design rule:
- Assassination sandbox first.
- Hardcore prep second.
- Do not drift into a pure realism sim where the workshop loop becomes more important than the job itself.

Random murder is allowed, but it is intentionally low-value and usually creates more police heat than profit.

---

## Contract Loop [v0.1]

The baseline loop is:

1. Accept a contract.
2. Read target intel, payout, and risk.
3. Build or select the rifle, optic, ammo, and specialty round if the job calls for it.
4. Optionally validate the setup at the range or another safe test location.
5. Travel to a firing or staging position.
6. Eliminate the target under the contract's restrictions.
7. Break line of sight, survive the search window, and cash out.

This loop is the primary economy spine for the project.

---

## Contract Generation [v0.1]

Procedural generation should operate on authored world content, not replace it.

Generated contract fields:
- `contractId`
- `targetArchetype`
- `targetScene` / `targetAnchor`
- `scheduleWindow`
- `distanceBand`
- `witnessDensity`
- `policeRisk`
- `payout`
- `failurePolicy`
- `objectivePolicy`
- `optionalModifiers` (shot through glass, suppressed shot, no civilian casualties, time-of-day, limited window)

Design rule:
- Keep the town, buildings, and vantage structure authored.
- Randomize the target package, route, timing, and exposure rather than attempting a fully procedural city first.

---

## Target Model [v0.1]

Targets should be generated from reusable archetypes instead of hand-authored one-off missions.

Baseline target archetypes:
- `StreetRoutineTarget` — visible in public with witnesses nearby
- `WindowTarget` — exposed from inside or on a balcony for a narrow timing window
- `TransitTarget` — moving between two authored anchors
- `GuardedTarget` — requires longer distance, cleaner angle, or patience

Target contracts should expose:
- routine anchors
- exposure windows
- bodyguard / witness risk
- contract-completion confirmation rules

The contract should also surface player-facing restrictions clearly in the Contracts tab so the player can read the job shape before committing to prep.

Baseline player-facing restriction categories:
- shot method or environmental requirement
- collateral / witness tolerance
- timing window
- escape / stealth requirement
- specialty-ammo or equipment hint

---

## Premium Long-Range Jobs [v0.1]

High-value contracts should favor long-range execution.

Rules:
- The top payouts come from shots where optics, zeroing, ammo consistency, and shooting position matter.
- Some contracts should be intentionally impractical or impossible to solve by walking directly to the target.
- Long-range success must feel earned through preparation, not scripted scope magic.
- Specialty ammo should matter because it changes the solution space, not because it adds spreadsheet noise.

This is why the shipped scoped PiP runtime is part of the current contract foundation, while live zero-step wiring/persistence and load-quality systems remain first-order priorities after the fantasy pivot.

---

## Failure, Heat, and Cash-Out [v0.1]

Contract success is not only the kill. The player must survive the aftermath.

Contract result rules:
- `Completed` — target eliminated and payout awarded after the heat window resolves
- `Failed` — target survives or an explicit contract failure rule is broken
- `Botched` — target eliminated but police response, witness cascade, or collateral damage destroys the economic upside

Important rule:
- Wrong-target kills are not a universal fail condition.
- Failure rules should be defined per contract through structured policy, then surfaced in the Contracts tab.
- Ordinary sandbox contracts should usually allow the player to kill whoever they want, while higher-level or authored contracts can opt into stricter failure rules.

Police response details live in [law-enforcement.md](law-enforcement.md), but contract design assumes:
- witnessed violence raises heat quickly
- line-of-sight break starts the escape countdown
- arrest or death confiscates carried inventory and respawns the player at hospital or police station

---

## Future Growth [v0.2]

Follow-up expansions can add:
- richer target archetypes and social routines
- multi-stage contracts with setup objectives
- contract handlers, fixers, fences, and black-market support
- specialty ammo families that solve distinct problems
- spotting gear, rangefinders, and better intel tools
- vehicle-assisted escape routes and checkpoint sniping jobs
- underworld progression through workshop upgrades, contacts, intel access, and higher-value contract tiers

Optional side systems such as hunting or competitions must not displace the contract loop as the top-level progression spine.
