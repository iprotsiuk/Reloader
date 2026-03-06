# Prototype Scope

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.
> This document defines what to build at each prototype phase.

---

## Delivery Tracking [v0.1]

Live implementation status and milestone tracking now live in [v0.1-demo-status-and-milestones.md](v0.1-demo-status-and-milestones.md).

Use this document (`prototype-scope.md`) for target scope by version only; do not treat unchecked list items here as runtime completion truth.

---

## Must Have [v0.1]

The minimum viable prototype that demonstrates the new core loop:

- [ ] FPS controller (Modular First Person Controller asset)
- [ ] One authored `MainTown` hub with house/workshop, shops, and contract access
- [ ] Seamless interiors for core town locations where practical for the contract loop
- [ ] Single-stage press interaction (resize, prime, charge, seat)
- [ ] One caliber (.308 Winchester)
- [ ] One rifle (bolt-action)
- [ ] One production magnified scope path for premium long-range work
- [ ] Basic ballistics (drop, no wind yet)
- [ ] Range validation flow: shoot a target, see group size, confirm setup
- [ ] One procedurally generated assassination contract loop
- [ ] Police heat + LOS escape window
- [ ] Arrest/death penalty: confiscate carried inventory, respawn at hospital or police station
- [ ] Item pickup / drop / persistence (world + workshop)
- [ ] Basic inventory (carry items)
- [ ] Save / load game state with exact restoration (player transforms, dropped item transforms, inventory/containers, world states; NPC transforms included when NPC simulation is enabled)
- [ ] Simple shops for weapons, optics, and reloading supplies
- [ ] Ammo quality affects long-range contract success

**Data model note:** The SO definitions and instance classes support the full precision system from v0.1 (deviation fields, per-instance sampling, fire-forming tracking, etc.). The gameplay interactions that expose these systems (component sorting UI, annealing interactions, etc.) are implemented in later versions. This avoids data model refactoring later.

---

## Should Have [v0.2]

- [ ] Driving in town-world routes (car to navigate town locations + checkpoint travel transitions)
- [ ] Second caliber
- [ ] More target archetypes and contract modifiers
- [ ] Contract intel gear (rangefinder, spotting tools, surveillance aids)
- [ ] Case lube mechanic + stuck case consequence
- [ ] Brass fire-forming tracking
- [ ] Handlers / fixers / fences as authored NPC roles

---

## Could Have [v1+]

- [ ] Weapon mod system
- [ ] Progressive press
- [ ] Black market
- [ ] Quests
- [ ] Day/night cycle
- [ ] Weather
- [ ] Hunting as an optional side activity
- [ ] Competitions as an optional side activity
