# Prototype Scope

> **Prerequisites:** Read [core-architecture.md](core-architecture.md) first.
> This document defines what to build at each prototype phase.

---

## Must Have [v0.1]

The minimum viable prototype that demonstrates the core loop:

- [ ] FPS controller (Modular First Person Controller asset)
- [ ] One non-instanced `MainWorld` scene with the player's home and starter workshop room/garage
- [ ] Seamless interiors for core town locations (no scene loading for house/shops/range access)
- [ ] Single-stage press interaction (resize, prime, charge, seat)
- [ ] One caliber (.308 Winchester)
- [ ] One rifle (bolt-action)
- [ ] Basic ballistics (drop, no wind yet)
- [ ] Shoot at a target, see group size
- [ ] Item pickup / drop / persistence (world + workshop)
- [ ] Basic inventory (carry items)
- [ ] Save / load game state with exact restoration (player transforms, dropped item transforms, inventory/containers, world states; NPC transforms included when NPC simulation is enabled)
- [ ] Simple shop (buy components with starting money)
- [ ] Ammo quality affects accuracy

**Data model note:** The SO definitions and instance classes support the full precision system from v0.1 (deviation fields, per-instance sampling, fire-forming tracking, etc.). The gameplay interactions that expose these systems (component sorting UI, annealing interactions, etc.) are implemented in later versions. This avoids data model refactoring later.

---

## Should Have [v0.2]

- [ ] Driving in MainWorld (car to navigate town locations + checkpoint travel transitions)
- [ ] Second caliber
- [ ] Competition (basic bullseye)
- [ ] Money from competitions
- [ ] Case lube mechanic + stuck case consequence
- [ ] Brass fire-forming tracking
- [ ] NPC shopkeeper

---

## Could Have [v1+]

- [ ] Hunting area (one instanced scene, deer)
- [ ] Hunting income (bounties/pelts)
- [ ] Multiple hunting areas
- [ ] Competition tiers
- [ ] Weapon mod system
- [ ] Progressive press
- [ ] Black market
- [ ] Police / game warden
- [ ] Quests
- [ ] Day/night cycle
- [ ] Weather
